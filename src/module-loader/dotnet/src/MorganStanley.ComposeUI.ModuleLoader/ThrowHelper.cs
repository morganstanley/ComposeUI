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

internal static class ThrowHelper
{
    public static ModuleLoaderException NativePathRejected(string moduleId, IEnumerable<string> errors) =>
        NativeModuleCannotBeStarted(moduleId, string.Join(" ", errors));

    public static ModuleLoaderException ExecutableNotAllowed(string moduleId, string fileName) =>
        NativeModuleCannotBeStarted(moduleId, $"'{fileName}' is not on the list of the allowed executables.");

    public static ModuleLoaderException TooManyArguments(string moduleId, int count, int maxCount) =>
        NativeModuleCannotBeStarted(moduleId, $"It has {count} arguments, and at most {maxCount} are allowed.");

    public static ModuleLoaderException ArgumentTooLong(string moduleId, int maxLength) =>
        NativeModuleCannotBeStarted(moduleId, $"An argument is longer than {maxLength} characters.");

    public static ModuleLoaderException ArgumentContainsNul(string moduleId) =>
        NativeModuleCannotBeStarted(moduleId, "An argument contains a NUL character.");

    public static ModuleLoaderException InvalidEnvironmentVariableName(string moduleId, string name) =>
        NativeModuleCannotBeStarted(moduleId, $"'{name}' is not a valid environment variable name.");

    public static ModuleLoaderException EnvironmentVariableValueContainsNul(string moduleId, string name) =>
        NativeModuleCannotBeStarted(moduleId, $"The value of the environment variable '{name}' contains a NUL character.");

    public static ModuleLoaderException EnvironmentVariableBlocked(string moduleId, string name) =>
        NativeModuleCannotBeStarted(moduleId, $"The environment variable '{name}' cannot be set.");

    private static ModuleLoaderException NativeModuleCannotBeStarted(string moduleId, string reason) =>
        new($"The native module '{moduleId}' cannot be started. {reason}");
}
