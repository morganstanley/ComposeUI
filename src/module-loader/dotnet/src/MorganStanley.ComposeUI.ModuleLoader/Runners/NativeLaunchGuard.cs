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

            var realPath = ResolveLinks(fullPath);
            var realRoot = ResolveLinks(Path.GetFullPath(root));

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
        foreach (var name in variables.Keys)
        {
            if (name.Length == 0 || name.Contains('=') || name.Contains('\0'))
            {
                throw new ModuleLoaderException($"The native module '{moduleId}' cannot be started. '{name}' is not a valid environment variable name.");
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

    /// <summary>
    /// Resolves the symbolic links and junctions in a full path, one component at a time.
    /// </summary>
    private static string ResolveLinks(string fullPath)
    {
        var root = Path.GetPathRoot(fullPath) ?? string.Empty;
        var current = root;

        foreach (var segment in fullPath.Substring(root.Length).Split([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar], StringSplitOptions.RemoveEmptyEntries))
        {
            var next = Path.Combine(current, segment);
            FileSystemInfo info = Directory.Exists(next) ? new DirectoryInfo(next) : new FileInfo(next);

            current = info.Exists && info.ResolveLinkTarget(returnFinalTarget: true) is { } target
                ? target.FullName
                : next;
        }

        return current;
    }

    private static StringComparison PathComparison =>
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
}
