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
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.DependencyInjection;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Infrastructure.Internal;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Contracts;
using MorganStanley.ComposeUI.Messaging.Abstractions;
using MorganStanley.ComposeUI.Messaging.Abstractions.Exceptions;
using AppMetadata = MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Protocol.AppMetadata;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Tests.Infrastructure.Internal;

public class ResolverUICommunicatorTests
{
    private static readonly JsonSerializerOptions JsonOptions = SerializerOptionsHelper.JsonSerializerOptionsWithContextSerialization;

    private readonly AppMetadata _app1 = new() { AppId = "app", InstanceId = "instance1" };
    private readonly AppMetadata _app2 = new() { AppId = "app", InstanceId = "instance2" };
    private readonly List<(string Topic, string? Payload)> _calls = new();
    private readonly Mock<ILogger<ResolverUICommunicator>> _logger = new();

    [Fact]
    public async Task SendResolverUIRequest_returns_the_candidate_of_the_agent_that_the_resolver_selected()
    {
        var communicator = CreateCommunicator(request => Reply(new ResolverUIResponse { SelectedCandidateId = request.Candidates.ElementAt(1).Id }));

        var response = await communicator.SendResolverUIRequestAsync(new IAppMetadata[] { _app1, _app2 });

        response.Should().NotBeNull();
        response!.Error.Should().BeNull();
        response.AppMetadata.Should().BeSameAs(_app2);
        _calls.Should().ContainSingle().Which.Topic.Should().Be(Fdc3Topic.ResolverUI);
    }

    [Fact]
    public async Task SendResolverUIRequest_sends_the_candidates_with_opaque_ids_that_are_different_for_every_request()
    {
        var requests = new List<ResolverUIRequest>();
        var communicator = CreateCommunicator(request =>
        {
            requests.Add(request);
            return Reply(new ResolverUIResponse { Error = ResolveError.UserCancelledResolution });
        });

        await communicator.SendResolverUIRequestAsync(new IAppMetadata[] { _app1, _app2 });
        await communicator.SendResolverUIRequestAsync(new IAppMetadata[] { _app1, _app2 });

        var first = requests[0].Candidates.ToArray();
        first.Should().HaveCount(2);
        first.Select(c => c.Id).Should().OnlyHaveUniqueItems().And.NotContainNulls().And.NotContain(string.Empty);
        first.Select(c => c.Id).Should().NotContain(new[] { "app", "instance1", "instance2" });
        first.Select(c => c.AppMetadata.InstanceId).Should().Equal("instance1", "instance2");
        requests[1].Candidates.Select(c => c.Id).Should().NotIntersectWith(first.Select(c => c.Id));
    }

    [Fact]
    public async Task SendResolverUIRequest_rejects_an_id_that_was_not_offered()
    {
        var communicator = CreateCommunicator(_ => Reply(new ResolverUIResponse { SelectedCandidateId = "not-offered" }));

        var response = await communicator.SendResolverUIRequestAsync(new IAppMetadata[] { _app1, _app2 });

        response.Should().NotBeNull();
        response!.Error.Should().Be(ResolveError.ResolverUnavailable);
        response.AppMetadata.Should().BeNull();
    }

    [Fact]
    public async Task SendResolverUIRequest_rejects_an_id_that_was_offered_in_an_earlier_request()
    {
        var earlierIds = new List<string>();
        var communicator = CreateCommunicator(request =>
        {
            if (earlierIds.Count == 0)
            {
                earlierIds.AddRange(request.Candidates.Select(c => c.Id));
                return Reply(new ResolverUIResponse { Error = ResolveError.UserCancelledResolution });
            }

            return Reply(new ResolverUIResponse { SelectedCandidateId = earlierIds[0] });
        });

        await communicator.SendResolverUIRequestAsync(new IAppMetadata[] { _app1, _app2 });
        var response = await communicator.SendResolverUIRequestAsync(new IAppMetadata[] { _app1, _app2 });

        response!.Error.Should().Be(ResolveError.ResolverUnavailable);
        response.AppMetadata.Should().BeNull();
    }

    [Fact]
    public async Task SendResolverUIRequest_rejects_a_response_without_a_selection_and_without_an_error()
    {
        var communicator = CreateCommunicator(_ => Reply(new ResolverUIResponse()));

        var response = await communicator.SendResolverUIRequestAsync(new IAppMetadata[] { _app1, _app2 });

        response!.Error.Should().Be(ResolveError.ResolverUnavailable);
        response.AppMetadata.Should().BeNull();
    }

    [Fact]
    public async Task SendResolverUIRequest_rejects_a_response_that_names_an_app_and_does_not_select_a_candidate()
    {
        var communicator = CreateCommunicator(_ => Reply(new ResolverUIResponse
        {
            AppMetadata = new AppMetadata { AppId = "app", InstanceId = "instance-of-someone-else" }
        }));

        var response = await communicator.SendResolverUIRequestAsync(new IAppMetadata[] { _app1, _app2 });

        response!.Error.Should().Be(ResolveError.ResolverUnavailable);
        response.AppMetadata.Should().BeNull();
    }

    [Fact]
    public async Task SendResolverUIRequest_ignores_the_metadata_that_the_resolver_returns_next_to_the_selected_id()
    {
        var communicator = CreateCommunicator(request => Reply(new ResolverUIResponse
        {
            SelectedCandidateId = request.Candidates.First().Id,
            AppMetadata = new AppMetadata { AppId = "other", InstanceId = "other-instance" }
        }));

        var response = await communicator.SendResolverUIRequestAsync(new IAppMetadata[] { _app1, _app2 });

        response!.Error.Should().BeNull();
        response.AppMetadata.Should().BeSameAs(_app1);
    }

    [Fact]
    public async Task SendResolverUIRequest_returns_the_error_of_the_resolver()
    {
        var communicator = CreateCommunicator(_ => Reply(new ResolverUIResponse { Error = ResolveError.UserCancelledResolution }));

        var response = await communicator.SendResolverUIRequestAsync(new IAppMetadata[] { _app1, _app2 });

        response!.Error.Should().Be(ResolveError.UserCancelledResolution);
        response.AppMetadata.Should().BeNull();
    }

    [Fact]
    public async Task SendResolverUIRequest_returns_an_error_and_does_not_throw_when_the_response_is_malformed()
    {
        var communicator = CreateCommunicator(_ => "this is not json");

        var response = await communicator.SendResolverUIRequestAsync(new IAppMetadata[] { _app1, _app2 });

        response!.Error.Should().Be(ResolveError.ResolverUnavailable);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SendResolverUIRequest_returns_an_error_and_logs_a_warning_when_the_resolver_does_not_host_the_endpoint(bool allowLegacy)
    {
        var communicator = CreateCommunicator(
            _ => throw new MessagingException("UnknownEndpoint", "The endpoint is not registered."),
            allowLegacy);

        var response = await communicator.SendResolverUIRequestAsync(new IAppMetadata[] { _app1, _app2 });

        var topic = allowLegacy ? Fdc3Topic.LegacyResolverUI : Fdc3Topic.ResolverUI;
        response!.Error.Should().Be(ResolveError.ResolverUnavailable);
        response.AppMetadata.Should().BeNull();
        _logger.Verify(
            l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) => state.ToString()!.Contains($"could not be reached on the {topic} topic (UnknownEndpoint)")),
                It.IsAny<MessagingException>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SendResolverUIIntentRequest_returns_an_error_and_logs_a_warning_when_the_resolver_does_not_host_the_endpoint()
    {
        var communicator = CreateCommunicator(_ => throw new MessagingException("UnknownEndpoint", "The endpoint is not registered."));

        var response = await communicator.SendResolverUIIntentRequestAsync(new[] { "ViewChart", "ViewNews" });

        response!.Error.Should().Be(ResolveError.ResolverUnavailable);
        response.SelectedIntent.Should().BeNull();
        _logger.Verify(
            l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) => state.ToString()!.Contains($"could not be reached on the {Fdc3Topic.ResolverUIIntent} topic (UnknownEndpoint)")),
                It.IsAny<MessagingException>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SendResolverUIRequest_returns_null_when_the_resolver_gives_no_response()
    {
        var communicator = CreateCommunicator(_ => null);

        var response = await communicator.SendResolverUIRequestAsync(new IAppMetadata[] { _app1, _app2 });

        response.Should().BeNull();
    }

    [Fact]
    public async Task SendResolverUIRequest_uses_the_legacy_endpoint_and_contract_only_when_it_is_allowed_and_logs_a_warning()
    {
        ResolverUIRequest? sent = null;
        var communicator = CreateCommunicator(
            request =>
            {
                sent = request;
                return Reply(new ResolverUIResponse { AppMetadata = _app2 });
            },
            allowLegacy: true);

        var response = await communicator.SendResolverUIRequestAsync(new IAppMetadata[] { _app1, _app2 });

        _calls.Should().ContainSingle().Which.Topic.Should().Be(Fdc3Topic.LegacyResolverUI);
        sent!.AppMetadata.Select(a => a.InstanceId).Should().Equal("instance1", "instance2");
        response!.Error.Should().BeNull();
        response.AppMetadata!.InstanceId.Should().Be("instance2");
        _logger.Verify(
            l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task SendResolverUIRequest_uses_the_contract_with_candidate_ids_when_there_are_no_options()
    {
        var communicator = CreateCommunicator(
            request => Reply(new ResolverUIResponse { SelectedCandidateId = request.Candidates.First().Id }),
            withOptions: false);

        var response = await communicator.SendResolverUIRequestAsync(new IAppMetadata[] { _app1 });

        response!.AppMetadata.Should().BeSameAs(_app1);
        _calls.Should().ContainSingle().Which.Topic.Should().Be(Fdc3Topic.ResolverUI);
    }

    private ResolverUICommunicator CreateCommunicator(Func<ResolverUIRequest, string?> reply, bool allowLegacy = false, bool withOptions = true)
    {
        var messaging = new Mock<IMessaging>();
        messaging
            .Setup(_ => _.InvokeServiceAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns((string topic, string? payload, CancellationToken _) =>
            {
                _calls.Add((topic, payload));
                var request = JsonSerializer.Deserialize<ResolverUIRequest>(payload!, JsonOptions)!;
                return ValueTask.FromResult(reply(request));
            });

        _logger.Setup(l => l.IsEnabled(It.IsAny<LogLevel>())).Returns(true);

        return new ResolverUICommunicator(
            messaging.Object,
            _logger.Object,
            withOptions ? Options.Create(new Fdc3DesktopAgentOptions { AllowLegacyResolverUI = allowLegacy }) : null);
    }

    private static string Reply(ResolverUIResponse response) => JsonSerializer.Serialize(response, JsonOptions);
}
