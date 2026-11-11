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

namespace MorganStanley.ComposeUI.ModuleLoader;

/// <summary>
/// Defines which native modules, with which arguments and environment variables, the module loader may start.
/// </summary>
/// <remarks>
/// The defaults are restrictive: only executables under the module root directory can be started,
/// and environment variables that change how a process loads code are rejected.
/// </remarks>
public sealed class NativeModuleLaunchOptions
{
    /// <summary>
    /// The name of the environment variable that overrides the default module root directory.
    /// </summary>
    public const string RepositoryRootEnvironmentVariableName = "COMPOSEUI_MODULE_REPOSITORY_ROOT";

    /// <summary>
    /// The directories that native modules can be started from.
    /// When the list is empty, the directory in the <see cref="RepositoryRootEnvironmentVariableName"/> environment variable is used,
    /// or the <c>modules</c> directory next to the executable if the variable is not set.
    /// </summary>
    public IList<string> AllowedRoots { get; } = new List<string>();

    /// <summary>
    /// Optional list of the executables that can be started, as file names or full paths.
    /// A file name matches an executable with that name in any directory under the allowed roots.
    /// When the list is empty, any executable under the allowed roots can be started.
    /// </summary>
    public IList<string> AllowedExecutables { get; } = new List<string>();

    /// <summary>
    /// The maximum number of arguments a native module can be started with.
    /// </summary>
    public int MaxArgumentCount { get; set; } = 64;

    /// <summary>
    /// The maximum length of an argument of a native module.
    /// </summary>
    public int MaxArgumentLength { get; set; } = 8192;

    /// <summary>
    /// The names of the environment variables that a manifest cannot set, compared without regard to case.
    /// A trailing <c>*</c> matches any ending.
    /// </summary>
    /// <remarks>
    /// The default list contains the variables that make a process load code from somewhere else:
    /// the search paths, the loader and runtime variables of .NET, native loaders, Node.js, Java, Python, Perl and Ruby,
    /// and the shell and system directory variables.
    /// </remarks>
    public IList<string> BlockedEnvironmentVariables { get; } = new List<string>
    {
        "PATH",
        "PATHEXT",
        "COMSPEC",
        "SYSTEMROOT",
        "WINDIR",
        "PSMODULEPATH",
        "BASH_ENV",
        "ENV",
        "LD_*",
        "DYLD_*",
        "DOTNET_*",
        "COR_*",
        "CORECLR_*",
        "COMPLUS_*",
        "ASPNETCORE_HOSTINGSTARTUPASSEMBLIES",
        "NODE_OPTIONS",
        "NODE_PATH",
        "JAVA_TOOL_OPTIONS",
        "_JAVA_OPTIONS",
        "JDK_JAVA_OPTIONS",
        "PYTHONPATH",
        "PYTHONHOME",
        "PYTHONSTARTUP",
        "PERL5OPT",
        "PERL5LIB",
        "RUBYOPT",
        "RUBYLIB"
    };

    /// <summary>
    /// Turns the checks off. A warning is logged each time a native module is started this way.
    /// </summary>
    public bool DisableLaunchGuard { get; set; }

    /// <summary>
    /// Gets the directories that native modules can be started from:
    /// <see cref="AllowedRoots"/>, or the default root if the list is empty.
    /// </summary>
    public IReadOnlyList<string> GetEffectiveRoots()
    {
        if (AllowedRoots.Count > 0)
        {
            return AllowedRoots.ToArray();
        }

        var root = Environment.GetEnvironmentVariable(RepositoryRootEnvironmentVariableName);

        return new[] { string.IsNullOrEmpty(root) ? Path.Combine(AppContext.BaseDirectory, "modules") : root };
    }
}
