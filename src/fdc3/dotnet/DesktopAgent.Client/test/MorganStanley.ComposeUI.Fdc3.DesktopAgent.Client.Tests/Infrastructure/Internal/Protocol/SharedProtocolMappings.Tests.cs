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

using FluentAssertions;
using AppIdentifier = MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Protocol.AppIdentifier;
using AppMetadata = MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Protocol.AppMetadata;
using ContextMetadata = MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Protocol.ContextMetadata;
using Icon = MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Protocol.Icon;
using Screenshot = MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Protocol.Screenshot;
using Finos.Fdc3;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Client.Tests.Infrastructure.Internal.Protocol;

public class SharedProtocolMappingsTests
{
    [Fact]
    public void ContextMetadata_explicit_interface_members_return_backing_values()
    {
        var metadata = new ContextMetadata
        {
            Source = new AppIdentifier { AppId = "source-app", InstanceId = "source-instance" },
            TraceId = "trace-id",
            Custom = new Dictionary<string, object> { ["key"] = "value" }
        };

        var contextMetadata = (IContextMetadata)metadata;
        var appProvidableMetadata = (IAppProvidableContextMetadata)metadata;

        contextMetadata.Source!.AppId.Should().Be("source-app");
        contextMetadata.TraceId.Should().Be("trace-id");
        appProvidableMetadata.TraceId.Should().Be("trace-id");
        appProvidableMetadata.Custom!.Should().ContainKey("key");
    }

    [Fact]
    public void AppMetadata_explicit_interface_collections_return_backing_values()
    {
        var appMetadata = new AppMetadata
        {
            AppId = "app-id",
            Icons = new[] { new Icon { Src = "icon.png" } },
            Screenshots = new[] { new Screenshot { Src = "screenshot.png" } }
        };

        var appMetadataInterface = (IAppMetadata)appMetadata;

        appMetadataInterface.Icons.Should().HaveCount(1);
        appMetadataInterface.Screenshots.Should().HaveCount(1);
        appMetadataInterface.AppId.Should().Be("app-id");
    }
}
