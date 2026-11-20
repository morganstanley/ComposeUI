// Morgan Stanley makes this available to you under the Apache License,
// Version 2.0 (the "License"). You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0.
//
// See the NOTICE file distributed with this work for additional information
// regarding copyright ownership. Unless required by applicable law or agreed
// to in writing, software distributed under the License is distributed on an
// "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
// or implied. See the License for the specific language governing permissions
// and limitations under the License.

namespace MorganStanley.ComposeUI.ModuleLoader.Tests.Runners;

/// <summary>
/// A fact that is reported as skipped, and not as passed, when the process is not allowed to create symbolic links.
/// </summary>
public sealed class SymlinkFactAttribute : FactAttribute
{
    public SymlinkFactAttribute()
    {
        if (!CanCreateSymbolicLinks())
        {
            Skip = "Creating symbolic links is not permitted for this process.";
        }
    }

    private static bool CanCreateSymbolicLinks()
    {
        var directory = Path.Combine(Path.GetTempPath(), "composeui-symlink-probe-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);

        try
        {
            var target = Path.Combine(directory, "target.txt");
            File.WriteAllText(target, "");
            File.CreateSymbolicLink(Path.Combine(directory, "link.txt"), target);
            return true;
        }
        catch (Exception e) when (e is UnauthorizedAccessException or IOException)
        {
            return false;
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
