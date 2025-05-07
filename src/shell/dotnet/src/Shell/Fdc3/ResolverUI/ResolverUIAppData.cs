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

using Finos.Fdc3;

namespace MorganStanley.ComposeUI.Shell.Fdc3.ResolverUI;

/// <summary>
///     Model containing information about the app that can resolve the intent.
/// </summary>
public class ResolverUIAppData
{
    /// <summary>
    ///     App specific information.
    /// </summary>
    public IAppMetadata AppMetadata { get; set; }

    /// <summary>
    ///     Icon that can be visualized on the ResolverUI.
    /// </summary>
    public IIcon? Icon { get; set; }
}