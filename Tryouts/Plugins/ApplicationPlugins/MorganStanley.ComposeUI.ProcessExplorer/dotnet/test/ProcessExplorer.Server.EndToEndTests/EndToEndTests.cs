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

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ProcessExplorer.Abstractions;
using ProcessExplorer.Abstractions.Infrastructure.Protos;
using ProcessExplorer.Abstractions.Subsystems;
using ProcessExplorer.Server.DependencyInjection;
using ProcessExplorer.Server.Server.Abstractions;
using Xunit;
using FluentAssertions;
using ProcessExplorer.Core.DependencyInjection;

namespace ProcessExplorer.Server.IntegrationTests;

public class EndToEndTests : IAsyncLifetime
{
    private IHost? _host;
    public readonly string Host = "localhost";
    public readonly int Port = 5056;

    public async Task DisposeAsync()
    {
        if (_host != null)
            await _host.StopAsync();
    }

    [Fact]
    public async Task Client_can_connect()
    {
        var client = CreateGrpcClient();

        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(1));

        var messages = new List<Message>();

        var call = client.Subscribe(new Empty(), cancellationToken: cancellationTokenSource.Token);

        // We want to receive the message, that the subscription is established, and we do not want to wait.
        // Due to that no processes/subsystems/runtime information have not been declared it will just receive the subscription alive notification

        try
        {
            await foreach (var message in call.ResponseStream.ReadAllAsync())
            {
                messages.Add(message);
            }
        }
        catch (RpcException) { }

        messages.Count.Should().Be(1);
        messages[0].Action.Should().Be(ActionType.SubscriptionAliveAction); 
    }

    [Fact]
    public async Task Client_can_subscribe_and_receive_messages()
    {
        // defining here some dummy subsystems to trigger the ProcessExplorer backend to send information about it to the defined ui connections. (not just the subscription alive notification)
        var aggregator = _host?.Services.GetRequiredService<IProcessInfoAggregator>();

        var dummyId = Guid.NewGuid();

        var dummySubsystemInfo = new SubsystemInfo()
        {
            State = SubsystemState.Running,
            Name = "DummySubsystem",
            UIType = "dummyUiType",
            StartupType = "dummyStartUpType",
            Path = "dummyPath",
            AutomatedStart = false,
        };
        
        var subsystems = new Dictionary<Guid, SubsystemInfo>()
            {
                { dummyId, dummySubsystemInfo }
            };

        if (aggregator != null && aggregator.SubsystemController != null)
        {
            await aggregator.SubsystemController.InitializeSubsystems(subsystems);
        }

        var client = CreateGrpcClient();
        var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        var messages = new List<Message>();

        var call = client.Subscribe(new Empty(), cancellationToken: cancellationTokenSource.Token);

        //try catch block to avoid OperationCanceledException due to that we are just waiting for 1 second
        try
        {
            await foreach (var message in call.ResponseStream.ReadAllAsync())
            {
                messages.Add(message);
            }
        }
        catch(RpcException) { }

        // We just need to receive SubscriptionAlive and a subsystems collection
        messages.Count.Should().Be(2);
        messages[0].Action.Should().Be(ActionType.SubscriptionAliveAction);
        messages[1].Action.Should().Be(ActionType.AddSubsystemsAction);
        messages[1].Subsystems.Count.Should().Be(1);
        messages[1].Subsystems.Should().ContainKey(dummyId.ToString());

        //In Proto3, all fields are optional and have a default value. For example, a string field has a default value of empty string ("") and an int field has a default value of zero (0).
        //If you want to create a proto message without a certain field, you have to set its value to the default value.
        var result = messages[1].Subsystems[dummyId.ToString()];
        result.Should().NotBeNull();
        result.Name.Should().BeEquivalentTo(dummySubsystemInfo.Name);
        result.State.Should().BeEquivalentTo(dummySubsystemInfo.State);
        result.UiType.Should().BeEquivalentTo(dummySubsystemInfo.UIType);
        result.StartupType.Should().BeEquivalentTo(dummySubsystemInfo.StartupType);
        result.Path.Should().BeEquivalentTo(dummySubsystemInfo.Path);
        result.AutomatedStart.Should().Be(dummySubsystemInfo.AutomatedStart);
        result.Arguments.Should().BeEmpty();
        result.Url.Should().BeNullOrEmpty();
        result.Description.Should().BeNullOrEmpty();
    }

    [Fact]
    public void Client_can_send_message()
    {
        var client = CreateGrpcClient();
        var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(1));

        var message = new Message()
        {
            Action = ActionType.SubscriptionAliveAction,
            Description = "dummy message"
        };

        var act = () => client.Send(message, cancellationToken: cancellationTokenSource.Token);
        act.Should().NotThrow();

        var result = client.Send(message, cancellationToken: cancellationTokenSource.Token);
        result.Should().BeOfType<Empty>();
    }

    public async Task InitializeAsync()
    {
        IHostBuilder builder = new HostBuilder();

        builder.ConfigureServices(
             (context, services) => services
                 .AddProcessExplorerWindowsServerWithGrpc(pe => pe.UseGrpc())
                 .ConfigureSubsystemLauncher(Start, Stop, CreateDummyStartType, CreateDummyStopType)
                 .Configure<ProcessExplorerServerOptions>(op =>
                 {
                     op.Host = Host;
                     op.Port = Port;
                 }));

        _host = builder.Build();
        await _host.StartAsync();
    }

    private ProcessExplorerMessageHandler.ProcessExplorerMessageHandlerClient CreateGrpcClient()
    {
        var channel = GrpcChannel.ForAddress($"http://{Host}:{Port}/");
        var client = new ProcessExplorerMessageHandler.ProcessExplorerMessageHandlerClient(channel);

        return client;
    }

    private static DummyStartType CreateDummyStartType(Guid id, string name)
    {
        return new DummyStartType(id, name);
    }

    private static DummyStopType CreateDummyStopType(Guid id)
    {
        return new DummyStopType(id);
    }

    private static void Start(DummyStartType dummy) { }
    private static void Stop(DummyStopType dummy) { }

    private record DummyStartType(Guid id, string name);
    private record DummyStopType(Guid id);
}
