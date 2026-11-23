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

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Protocol;

public class ContextMetadata : IContextMetadata, IAppProvidableContextMetadata
{
    public AppIdentifier? Source { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? TraceId { get; set; }
    public IDetachedSignature? Signature { get; set; }
    public IAntiReplayClaims? AntiReplay { get; set; }
    public Dictionary<string, object>? Custom { get; set; }

    IAppIdentifier? IContextMetadata.Source => Source;
    DateTime IContextMetadata.Timestamp => Timestamp;
    string? IContextMetadata.TraceId => TraceId;
    string? IAppProvidableContextMetadata.TraceId => TraceId;
    IDetachedSignature? IAppProvidableContextMetadata.Signature => Signature;
    IAntiReplayClaims? IAppProvidableContextMetadata.AntiReplay => AntiReplay;
    Dictionary<string, object>? IAppProvidableContextMetadata.Custom => Custom;
}
