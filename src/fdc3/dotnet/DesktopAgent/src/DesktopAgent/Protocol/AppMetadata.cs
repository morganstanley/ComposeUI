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

using MorganStanley.Fdc3;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Protocol;

public class AppMetadata : IAppMetadata
{
    public string? Name { get; set; }

    public string? Version { get; set; }

    public string? Title { get; set; }

    public string? Tooltip { get; set; }

    public string? Description { get; set; }

    public IEnumerable<IIcon> Icons { get; set; } = Enumerable.Empty<IIcon>();

    public IEnumerable<IImage> Screenshots { get; set; } = Enumerable.Empty<IImage>();

    public string? ResultType { get; set; }

    public string AppId { get; set; }

    public string? InstanceId { get; set; }
}
