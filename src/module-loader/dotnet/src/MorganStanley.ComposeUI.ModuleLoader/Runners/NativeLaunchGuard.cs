/*
* Morgan Stanley makes this available to you under the Apache License,
* Version 2.0 (the "License"). You may obtain a copy of the License at
*
*      http://www.apache.org/licenses/LICENSE-2.0.
*
* See the NOTICE file distributed with this work for additional information
* regarding copyright ownership. Unless required by applicable law or agreed
* to in writing, software distributed under the License is distributed on an
* "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
* or implied. See the License for the specific language governing permissions
* and limitations under the License.
*/

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace MorganStanley.ComposeUI.ModuleLoader.Runners;

/// <summary>
/// Checks what a native module is allowed to start with, right before the process is started.
/// </summary>
internal static class NativeLaunchGuard
{
    /// <summary>
    /// Validates the launch of a native module.
    /// </summary>
    /// <returns>The full path of the executable to start, with every link resolved.</returns>
    /// <exception cref="ModuleLoaderException">The module is not allowed to be started this way.</exception>
    public static string Validate(string moduleId, NativeManifestDetails details, NativeModuleLaunchOptions options)
    {
        var path = ValidatePath(moduleId, details.Path, options);

        ValidateExecutable(moduleId, path, options);
        ValidateArguments(moduleId, details.Arguments, options);
        ValidateEnvironmentVariables(moduleId, details.EnvironmentVariables, options);

        return path;
    }

    private static string ValidatePath(string moduleId, Uri path, NativeModuleLaunchOptions options)
    {
        var errors = new List<string>();

        foreach (var root in options.GetEffectiveRoots())
        {
            if (!NativePathValidator.TryValidate(path, root, out var fullPath, out var error))
            {
                errors.Add(error);
                continue;
            }

            string realPath;
            string realRoot;

            try
            {
                realPath = ResolveLinks(fullPath, GetLinkTarget);
                realRoot = ResolveLinks(Path.GetFullPath(root), GetLinkTarget);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                errors.Add($"The links of the path cannot be resolved: {e.Message}");
                continue;
            }

            if (NativePathValidator.TryValidate(NativePathValidator.ToFileUri(realPath), realRoot, out _, out error))
            {
                return realPath;
            }

            errors.Add($"The path resolves through a link to '{realPath}', which is outside of '{realRoot}'.");
        }

        throw new ModuleLoaderException($"The native module '{moduleId}' cannot be started. {string.Join(" ", errors)}");
    }

    private static void ValidateExecutable(string moduleId, string path, NativeModuleLaunchOptions options)
    {
        if (options.AllowedExecutables.Count == 0)
        {
            return;
        }

        var fileName = Path.GetFileName(path);

        if (!options.AllowedExecutables.Any(allowed =>
                string.Equals(allowed, fileName, PathComparison) || string.Equals(allowed, path, PathComparison)))
        {
            throw new ModuleLoaderException($"The native module '{moduleId}' cannot be started. '{fileName}' is not on the list of the allowed executables.");
        }
    }

    private static void ValidateArguments(string moduleId, string[] arguments, NativeModuleLaunchOptions options)
    {
        if (arguments.Length > options.MaxArgumentCount)
        {
            throw new ModuleLoaderException($"The native module '{moduleId}' cannot be started. It has {arguments.Length} arguments, and at most {options.MaxArgumentCount} are allowed.");
        }

        foreach (var argument in arguments)
        {
            if (argument.Length > options.MaxArgumentLength)
            {
                throw new ModuleLoaderException($"The native module '{moduleId}' cannot be started. An argument is longer than {options.MaxArgumentLength} characters.");
            }

            if (argument.Contains('\0'))
            {
                throw new ModuleLoaderException($"The native module '{moduleId}' cannot be started. An argument contains a NUL character.");
            }
        }
    }

    private static void ValidateEnvironmentVariables(string moduleId, Dictionary<string, string> variables, NativeModuleLaunchOptions options)
    {
        foreach (var (name, value) in variables)
        {
            if (name.Length == 0 || name.Contains('=') || name.Contains('\0'))
            {
                throw new ModuleLoaderException($"The native module '{moduleId}' cannot be started. '{name}' is not a valid environment variable name.");
            }

            // On Windows the environment is passed to the process as one block that is separated by NUL characters,
            // so a NUL in a value would add variables that are not checked here.
            if (value != null && value.Contains('\0'))
            {
                throw new ModuleLoaderException($"The native module '{moduleId}' cannot be started. The value of the environment variable '{name}' contains a NUL character.");
            }

            if (options.BlockedEnvironmentVariables.Any(pattern => Matches(pattern, name)))
            {
                throw new ModuleLoaderException($"The native module '{moduleId}' cannot be started. The environment variable '{name}' cannot be set.");
            }
        }
    }

    private static bool Matches(string pattern, string name) =>
        pattern.EndsWith("*", StringComparison.Ordinal)
            ? name.StartsWith(pattern.Substring(0, pattern.Length - 1), StringComparison.OrdinalIgnoreCase)
            : string.Equals(pattern, name, StringComparison.OrdinalIgnoreCase);

    // Linux stops after 40 links in a path, which also stops links that point to each other.
    private const int MaxFollowedLinks = 40;

    private static readonly char[] Separators = [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar];

    /// <summary>
    /// Resolves the symbolic links and junctions in a full path the way the operating system does:
    /// one component at a time, and the target of a link is resolved the same way.
    /// </summary>
    /// <remarks>
    /// On Unix .NET does not give a canonical path for a link, so the text that the link stores is resolved here:
    /// it can run through other links, and ".." in it must be applied to the directory that the path got to,
    /// which is not the directory that comes before it in the text when a link was followed.
    /// </remarks>
    /// <param name="fullPath">The full path to resolve.</param>
    /// <param name="getLinkTarget">Gets the target of the link at a path, or null when the path is not a link.</param>
    internal static string ResolveLinks(string fullPath, Func<string, string?> getLinkTarget)
    {
        var root = Path.GetPathRoot(fullPath)!;
        var current = root;
        var pending = new Stack<string>();
        PushSegments(pending, fullPath.Substring(root.Length));
        var followedLinks = 0;

        while (pending.Count > 0)
        {
            var segment = pending.Pop();

            if (segment == ".")
            {
                continue;
            }

            if (segment == "..")
            {
                // The path so far has no links, so its parent is the real parent. The root has no parent.
                current = Path.GetDirectoryName(current) ?? current;
                continue;
            }

            var next = Path.Combine(current, segment);
            var target = getLinkTarget(next);

            if (target == null)
            {
                current = next;
                continue;
            }

            if (++followedLinks > MaxFollowedLinks)
            {
                throw new IOException($"Too many levels of links in '{fullPath}'.");
            }

            if (Path.IsPathRooted(target))
            {
                var targetRoot = Path.GetPathRoot(target)!;
                current = targetRoot;
                PushSegments(pending, target.Substring(targetRoot.Length));
            }
            else
            {
                // A relative target starts at the directory of the link, which is where the path got to.
                PushSegments(pending, target);
            }
        }

        return current;
    }

    private static void PushSegments(Stack<string> pending, string path)
    {
        foreach (var segment in path.Split(Separators, StringSplitOptions.RemoveEmptyEntries).Reverse())
        {
            pending.Push(segment);
        }
    }

    [ExcludeFromCodeCoverage(Justification = "The operating system decides which link lookup runs, so only one of them can run on a machine.")]
    private static string? GetLinkTarget(string path) =>
        OperatingSystem.IsWindows() ? GetFinalLinkTarget(path) : new FileInfo(path).LinkTarget;

    // Windows resolves the whole target path, so this is the final path.
    private static string? GetFinalLinkTarget(string path)
    {
        FileSystemInfo info = Directory.Exists(path) ? new DirectoryInfo(path) : new FileInfo(path);

        return info.Exists ? info.ResolveLinkTarget(returnFinalTarget: true)?.FullName : null;
    }

    [ExcludeFromCodeCoverage(Justification = "The operating system decides, so only one of the two results can be seen on a machine.")]
    private static StringComparison PathComparison =>
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
}
