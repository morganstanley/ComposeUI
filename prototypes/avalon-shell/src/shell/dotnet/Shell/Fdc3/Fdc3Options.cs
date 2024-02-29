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

using Microsoft.Extensions.Options;
using MorganStanley.ComposeUI.Fdc3.AppDirectory;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.DependencyInjection;

namespace MorganStanley.ComposeUI.Shell.Fdc3;

/// <summary>
///     Configuration root for FDC3 features. This object is configured under the <c>FDC3</c> section.
/// </summary>
public class Fdc3Options : IOptions<Fdc3Options>
{
    /// <summary>
    ///     When set to <c>true</c>, it will enable the Fdc3 backend service.
    /// </summary>
    public bool EnableFdc3 { get; set; }

    /// <summary>
    ///     Options for the FDC3 Desktop Agent
    /// </summary>
    public Fdc3DesktopAgentOptions DesktopAgent { get; set; } = new();

    /// <summary>
    ///     Options for the FDC3 App Directory
    /// </summary>
    public AppDirectoryOptions AppDirectory { get; set; } = new();

    /// <inheritdoc />
    public Fdc3Options Value => this;
}