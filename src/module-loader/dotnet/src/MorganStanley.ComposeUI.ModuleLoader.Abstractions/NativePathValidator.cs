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
using System.Security;

namespace MorganStanley.ComposeUI.ModuleLoader;

/// <summary>
/// Checks that the path of a native module is a file path under an allowed root directory.
/// </summary>
public static class NativePathValidator
{
    /// <summary>
    /// Validates a native module path against an allowed root directory.
    /// Relative paths are resolved against <paramref name="allowedRoot"/>, never against the working directory.
    /// </summary>
    /// <param name="path">The path of the native module.</param>
    /// <param name="allowedRoot">The directory that must contain the module.</param>
    /// <param name="fullPath">When the method returns true, the canonical full path of the module; otherwise an empty string.</param>
    /// <param name="error">When the method returns false, the reason; otherwise an empty string.</param>
    /// <returns>True if the path is a file path under <paramref name="allowedRoot"/>.</returns>
    /// <exception cref="ArgumentException"><paramref name="allowedRoot"/> is null or empty.</exception>
    public static bool TryValidate(Uri? path, string allowedRoot, out string fullPath, out string error)
    {
        if (string.IsNullOrWhiteSpace(allowedRoot))
        {
            throw new ArgumentException("The allowed root must not be empty.", nameof(allowedRoot));
        }

        fullPath = string.Empty;
        error = string.Empty;

        if (path == null)
        {
            error = "The path is missing.";
            return false;
        }

        string candidate;

        if (path.IsAbsoluteUri)
        {
            if (!path.IsFile)
            {
                error = $"Only file paths are supported, but the path is a '{path.Scheme}' uri.";
                return false;
            }

            candidate = path.LocalPath;
        }
        else
        {
            candidate = path.OriginalString;
        }

        if (string.IsNullOrWhiteSpace(candidate))
        {
            error = "The path is empty.";
            return false;
        }

        try
        {
            var root = Path.GetFullPath(allowedRoot);
            var trimmedRoot = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var rootWithSeparator = (trimmedRoot.Length == 0 ? root : trimmedRoot) + Path.DirectorySeparatorChar;

            var resolved = Path.GetFullPath(Path.IsPathRooted(candidate) ? candidate : Path.Combine(root, candidate));

            if (resolved.Length <= rootWithSeparator.Length || !resolved.StartsWith(rootWithSeparator, PathComparison))
            {
                error = $"The path must be under '{rootWithSeparator}', but it resolves to '{resolved}'.";
                return false;
            }

            fullPath = resolved;
            return true;
        }
        catch (Exception e) when (e is ArgumentException or NotSupportedException or PathTooLongException or SecurityException)
        {
            error = $"The path is not valid: {e.Message}";
            return false;
        }
    }

    private static StringComparison PathComparison =>
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
}
