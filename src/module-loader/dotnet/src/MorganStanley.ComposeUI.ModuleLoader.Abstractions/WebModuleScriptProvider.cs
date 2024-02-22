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

namespace MorganStanley.ComposeUI.ModuleLoader;

[Flags]
public enum WebModuleScriptProviderFlags
{
    /// <summary>
    /// No flags (default)
    /// </summary>
    None = 0,

    /// <summary>
    /// The script target is in a window that was created using <c>window.open()</c>.
    /// </summary>
    Popup = 1,

    /// <summary>
    /// The script target is frame.
    /// </summary>
    Frame = 2,
}

public sealed record WebModuleScriptProviderParameters(Uri Uri, WebModuleScriptProviderFlags Flags);

public delegate ValueTask<string> WebModuleScriptProvider(
    IModuleInstance moduleInstance,
    WebModuleScriptProviderParameters parameters);