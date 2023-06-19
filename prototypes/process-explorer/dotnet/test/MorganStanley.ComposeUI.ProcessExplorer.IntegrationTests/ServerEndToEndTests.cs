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
using Google.Protobuf.Collections;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MorganStanley.ComposeUI.ProcessExplorer.Abstractions;
using MorganStanley.ComposeUI.ProcessExplorer.Abstractions.Subsystems;
using MorganStanley.ComposeUI.ProcessExplorer.Core.DependencyInjection;
using MorganStanley.ComposeUI.ProcessExplorer.Server.DependencyInjection;
using MorganStanley.ComposeUI.ProcessExplorer.Server.Server.Abstractions;
using ProcessExplorer.Abstractions.Infrastructure.Protos;
using Xunit;

namespace MorganStanley.ComposeUI.ProcessExplorer.IntegrationTests;

public class ServerEndToEndTests : IAsyncLifetime
{
    private IHost? _host;
    public readonly string Host = "localhost";
    public readonly int Port = 5056;

    public async Task DisposeAsync()
    {
        if (_host != null)
            await _host.StopAsync();
    }

    //TODO(Lilla): investigate why the integrationtests are keeping to fail on CI, but works everytime locally.
    [Fact(Skip = "Local end to end test")]
    public async Task Client_can_connect()
    {
        var client = CreateGrpcClient();

        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(2));

        var messages = new List<Message>();

        using var call = client.Subscribe(new Empty(), cancellationToken: cancellationTokenSource.Token);

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

        Assert.True(messages.Count >= 1);
        Assert.Equal(ActionType.SubscriptionAliveAction, messages[0].Action);
    }

    //TODO(Lilla): investigate why the integrationtests are keeping to fail on CI, but works everytime locally.
    [Fact(Skip = "Local end to end test")]
    public async Task Client_can_subscribe_and_receive_messages()
    {
        // defining here some dummy subsystems to trigger the ProcessExplorer backend to send information about it to the defined ui connections. (not just the subscription alive notification)
        var aggregator = _host?.Services.GetRequiredService<IProcessInfoAggregator>();
        if (aggregator == null) throw new ArgumentNullException(nameof(aggregator));

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

        await aggregator.SubsystemController.InitializeSubsystems(subsystems);

        var client = CreateGrpcClient();
        var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var messages = new List<Message>();

        using var call = client.Subscribe(new Empty(), cancellationToken: cancellationTokenSource.Token);

        //try catch block to avoid OperationCanceledException due to that we are just waiting for 2 seconds
        try
        {
            await foreach (var message in call.ResponseStream.ReadAllAsync())
            {
                messages.Add(message);
            }
        }
        catch (RpcException) { }

        // We just need to receive SubscriptionAlive and a subsystems collection, skipping if that some error occurred


        Assert.True(messages.Count >= 2);
        Assert.Equal(ActionType.SubscriptionAliveAction, messages[0].Action);
        Assert.Equal(ActionType.AddSubsystemsAction, messages[1].Action);
        Assert.Single(messages[1].Subsystems);
        Assert.Contains(dummyId.ToString(), messages[1].Subsystems.Keys);

        //In Proto3, all fields are optional and have a default value. For example, a string field has a default value of empty string ("") and an int field has a default value of zero (0).
        //If you want to create a proto message without a certain field, you have to set its value to the default value.
        var result = messages[1].Subsystems[dummyId.ToString()];

        Assert.NotNull(result);
        Assert.Equal(dummySubsystemInfo.Name, result.Name);
        Assert.Equal(dummySubsystemInfo.State, result.State);
        Assert.Equal(dummySubsystemInfo.UIType, result.UiType);
        Assert.Equal(dummySubsystemInfo.StartupType, result.StartupType);
        Assert.Equal(dummySubsystemInfo.Path, result.Path);
        Assert.Equal(dummySubsystemInfo.AutomatedStart, result.AutomatedStart);
        Assert.Empty(result.Arguments);
        Assert.Empty(result.Url);
        Assert.Empty(result.Description);
    }

    [Fact(Skip = "Local end to end test")]
    public void Client_can_send_message()
    {
        var client = CreateGrpcClient();
        var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(2));

        var message = new Message()
        {
            Action = ActionType.SubscriptionAliveAction,
            Description = "dummy message"
        };

        Empty? result = null;
        try
        {
            result = client.Send(message, cancellationToken: cancellationTokenSource.Token);
        }
        catch (RpcException) { }

        Assert.NotNull(result);
        Assert.IsType<Empty>(result);
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
