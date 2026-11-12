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

using System.Text.Json;
using Finos.Fdc3;
using FluentAssertions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Moq;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.DependencyInjection;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Contracts;
using MorganStanley.ComposeUI.Messaging.Abstractions;
using AppMetadata = MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Protocol.AppMetadata;

namespace MorganStanley.ComposeUI.Shell.Fdc3.ResolverUI;

public class ResolverUIServiceTests
{
    private static readonly JsonSerializerOptions JsonOptions = SerializerOptionsHelper.JsonSerializerOptionsWithContextSerialization;

    private readonly Mock<IResolverUIProjector> _projector = new();
    private readonly Dictionary<string, ServiceHandler> _handlers = new();

    [Fact]
    public async Task StartAsync_registers_the_resolver_service_with_candidate_ids_and_not_the_legacy_one_by_default()
    {
        await StartAsync();

        _handlers.Keys.Should().BeEquivalentTo(
            new[] { Fdc3Topic.ResolverUI, "ComposeUI/fdc3/v2.0/resolverUIIntent" });
        Fdc3Topic.ResolverUI.Should().Be("ComposeUI/fdc3/v2.0/v2/resolverUI");
    }

    [Fact]
    public async Task StartAsync_registers_the_legacy_resolver_service_as_well_when_it_is_allowed()
    {
        await StartAsync(allowLegacy: true);

        _handlers.Keys.Should().Contain(new[] { Fdc3Topic.ResolverUI, Fdc3Topic.LegacyResolverUI });
    }

    [Fact]
    public async Task The_resolver_service_returns_the_id_of_the_candidate_that_the_user_selected()
    {
        _projector
            .Setup(_ => _.ShowResolverUI(It.IsAny<IEnumerable<IAppMetadata>>(), It.IsAny<TimeSpan>()))
            .Returns((IEnumerable<IAppMetadata> apps, TimeSpan _) => new ValueTask<ResolverUIResponse>(new ResolverUIResponse { AppMetadata = apps.ElementAt(1) }));
        await StartAsync();

        var response = await InvokeAsync(Fdc3Topic.ResolverUI, CandidatesRequest(("id-a", "appA"), ("id-b", "appB")));

        response.SelectedCandidateId.Should().Be("id-b");
        response.Error.Should().BeNull();
    }

    [Fact]
    public async Task The_resolver_service_shows_the_candidates_of_the_request_to_the_user()
    {
        IEnumerable<IAppMetadata>? shown = null;
        _projector
            .Setup(_ => _.ShowResolverUI(It.IsAny<IEnumerable<IAppMetadata>>(), It.IsAny<TimeSpan>()))
            .Returns((IEnumerable<IAppMetadata> apps, TimeSpan _) =>
            {
                shown = apps.ToArray();
                return new ValueTask<ResolverUIResponse>(new ResolverUIResponse { Error = ResolveError.UserCancelledResolution });
            });
        await StartAsync();

        await InvokeAsync(Fdc3Topic.ResolverUI, CandidatesRequest(("id-a", "appA"), ("id-b", "appB")));

        shown!.Select(a => a.AppId).Should().Equal("appA", "appB");
    }

    [Fact]
    public async Task The_resolver_service_returns_the_error_of_the_resolver_window()
    {
        _projector
            .Setup(_ => _.ShowResolverUI(It.IsAny<IEnumerable<IAppMetadata>>(), It.IsAny<TimeSpan>()))
            .Returns(new ValueTask<ResolverUIResponse>(new ResolverUIResponse { Error = ResolveError.UserCancelledResolution }));
        await StartAsync();

        var response = await InvokeAsync(Fdc3Topic.ResolverUI, CandidatesRequest(("id-a", "appA"), ("id-b", "appB")));

        response.Error.Should().Be(ResolveError.UserCancelledResolution);
        response.SelectedCandidateId.Should().BeNull();
    }

    [Fact]
    public async Task The_resolver_service_returns_an_error_when_the_resolver_window_returns_an_app_that_is_not_a_candidate()
    {
        _projector
            .Setup(_ => _.ShowResolverUI(It.IsAny<IEnumerable<IAppMetadata>>(), It.IsAny<TimeSpan>()))
            .Returns(new ValueTask<ResolverUIResponse>(new ResolverUIResponse { AppMetadata = new AppMetadata { AppId = "appA" } }));
        await StartAsync();

        var response = await InvokeAsync(Fdc3Topic.ResolverUI, CandidatesRequest(("id-a", "appA"), ("id-b", "appB")));

        response.Error.Should().Be(ResolveError.ResolverUnavailable);
        response.SelectedCandidateId.Should().BeNull();
    }

    [Fact]
    public async Task The_legacy_resolver_service_returns_the_selected_app_itself()
    {
        _projector
            .Setup(_ => _.ShowResolverUI(It.IsAny<IEnumerable<IAppMetadata>>(), It.IsAny<TimeSpan>()))
            .Returns((IEnumerable<IAppMetadata> apps, TimeSpan _) => new ValueTask<ResolverUIResponse>(new ResolverUIResponse { AppMetadata = apps.ElementAt(1) }));
        await StartAsync(allowLegacy: true);
        var request = JsonSerializer.Serialize(
            new ResolverUIRequest { AppMetadata = new IAppMetadata[] { new AppMetadata { AppId = "appA" }, new AppMetadata { AppId = "appB" } } },
            JsonOptions);

        var response = await InvokeAsync(Fdc3Topic.LegacyResolverUI, request);

        response.AppMetadata!.AppId.Should().Be("appB");
        response.SelectedCandidateId.Should().BeNull();
    }

    private async Task StartAsync(bool allowLegacy = false)
    {
        var messagingMock = new Mock<IMessaging>();
        messagingMock
            .Setup(_ => _.RegisterServiceAsync(It.IsAny<string>(), It.IsAny<ServiceHandler>(), It.IsAny<CancellationToken>()))
            .Callback((string name, ServiceHandler handler, CancellationToken _) => _handlers[name] = handler)
            .Returns(ValueTask.FromResult(Mock.Of<IAsyncDisposable>()));

        var serviceProviderMock = new Mock<IServiceProvider>();
        serviceProviderMock.Setup(_ => _.GetService(typeof(IMessaging))).Returns(messagingMock.Object);
        serviceProviderMock
            .Setup(_ => _.GetService(typeof(IOptions<Fdc3DesktopAgentOptions>)))
            .Returns(Options.Create(new Fdc3DesktopAgentOptions { AllowLegacyResolverUI = allowLegacy }));

        var hostMock = new Mock<IHost>();
        hostMock.Setup(_ => _.Services).Returns(serviceProviderMock.Object);

        await new ResolverUIService(hostMock.Object, _projector.Object).StartAsync(CancellationToken.None);
    }

    private async Task<ResolverUIResponse> InvokeAsync(string topic, string request)
    {
        var json = await _handlers[topic](request);
        return JsonSerializer.Deserialize<ResolverUIResponse>(json!, JsonOptions)!;
    }

    private static string CandidatesRequest(params (string Id, string AppId)[] candidates) =>
        JsonSerializer.Serialize(
            new ResolverUIRequest
            {
                Candidates = candidates.Select(c => new ResolverUICandidate { Id = c.Id, AppMetadata = new AppMetadata { AppId = c.AppId } }).ToArray()
            },
            JsonOptions);
}
