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

using System.Net;
using System.Net.WebSockets;
using LocalCollector.Communicator;
using ProcessExplorer.Core.Factories;
using Super.RPC;
using SuperRPC_POC.ClientBehavior;
using SuperRPC_POC.Infrastructure;

namespace SuperRPC_POC;

public class SuperRpcWebSocketMiddlewareV2
{
    private readonly RequestDelegate _next;
    private readonly IModuleLoaderInformationReceiver _collector;

    public SuperRpcWebSocketMiddlewareV2(RequestDelegate next, IModuleLoaderInformationReceiver collector) 
    {
        _next = next;
        _collector = collector;

        //fire - and - forget
        _collector.SubscribeToProcessExplorerTopicAsync();
        _collector.SubscribeToProcessExplorerChangedElementTopicAsync();
        _collector.SubscribeToEnableProcessMonitorTopicAsync();
        _collector.SubscribeToDisableProcessMonitorTopicAsync();
        _collector.SubscribeToSubsystemsTopicAsync();
        _collector.SubscribeToRuntimeInformationTopicAsync();
    }

    private SuperRPC SetupRpc(RPCReceiveChannel channel, ICommunicator? communicator)
    {
        var rpc = new SuperRPC(() => Guid.NewGuid().ToString("N"));
        SuperRPCWebSocket.RegisterCustomDeserializer(rpc);
        rpc.Connect(channel);

        if (communicator != null)
        {
            rpc.RegisterHostObject("communicator", communicator, new ObjectDescriptor
            {
                Functions = new FunctionDescriptor[] {
                "AddRuntimeInfo",
                "AddRuntimeInfos",
                "AddConnectionCollection",
                "UpdateConnectionInformation",
                "UpdateEnvironmentVariableInformation",
                "UpdateRegistrationInformation",
                "UpdateModuleInformation",
                "TerminateProcess" }
            });
        }

        if (_collector.ProcessInfoAggregator != null)
        {
            rpc.RegisterHostObject("processController", _collector.ProcessInfoAggregator, new ObjectDescriptor
            {
                Functions = new FunctionDescriptor[] { "RemoveProcessById" }
            });
        }

        if (_collector.SubsystemController != null)
        { //Subsystems handler on UI:
            rpc.RegisterHostObject("subsystemController", _collector.SubsystemController, new ObjectDescriptor
            {
                Functions = new FunctionDescriptor[] {
                "LaunchSubsystem",
                "LaunchSubsystems",
                "RestartSubsystem",
                "RestartSubsystems",
                "ShutdownSubsystem",
                "ShutdownSubsystems" }
            });
        }

        return rpc;
    }

    private SuperRPC SetupRpc(RPCReceiveChannel channel)
    {
        var rpc = new SuperRPC(() => Guid.NewGuid().ToString("N"));
        SuperRPCWebSocket.RegisterCustomDeserializer(rpc);
        rpc.Connect(channel);
        return rpc;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path == "/processes")
        {
            if (context.WebSockets.IsWebSocketRequest)
            {
                using WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();

                // set behavior of the infocollector's changes
                ICommunicator? communicator = null;

                // set implementation of ui notifications
                var uiHandler = new UIHandler();


                if (_collector.ProcessInfoAggregator != null)
                {
                    communicator = ProcessAggregatorFactory.CreateCommunicator(_collector.ProcessInfoAggregator);
                }

                var rpcWebSocketHandler = SuperRPCWebSocket.CreateHandler(webSocket);

                // registering proxy objects, what we are using
                var rpc = SetupRpc(rpcWebSocketHandler.ReceiveChannel, communicator);

                // after we get those objects what we want to use then we should add this ui handler to the collection because the relationship can be 1:N
                if (_collector.ProcessInfoAggregator != null)
                {
                    uiHandler.InitSuperRPCForProcessAsync(rpc)
                        .ContinueWith(_ => _collector.ProcessInfoAggregator.AddUiConnection(uiHandler));

                    await rpcWebSocketHandler.StartReceivingAsync(_collector.ProcessInfoAggregator, uiHandler);
                }
                else
                {
                    await rpcWebSocketHandler.StartReceivingAsync(uiHandler: uiHandler);
                }
            }
            else
            {
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            }
            return;
        }

        // another endpoint --- same functionality with infocollector as infoaggregator
        if (context.Request.Path == "/subsystems")
        {
            if (context.WebSockets.IsWebSocketRequest)
            {
                using WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();
                var rpcWebSocketHandler = SuperRPCWebSocket.CreateHandler(webSocket);

                var rpc = SetupRpc(rpcWebSocketHandler.ReceiveChannel, null);

                var uiHandler = new UIHandler();

                if (_collector.ProcessInfoAggregator != null)
                {
                    uiHandler.InitSuperRPCForSubsystemAsync(rpc)
                        .ContinueWith(_ => _collector.ProcessInfoAggregator.AddUiConnection(uiHandler));

                    await rpcWebSocketHandler.StartReceivingAsync(_collector.ProcessInfoAggregator, uiHandler);
                }
            }
            else
            {
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            }
            return;
        }

        if (context.Request.Path == "/collector-rpc")
        {
            if (context.WebSockets.IsWebSocketRequest)
            {
                using WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();
                ICommunicator? collectorHandler = null;

                if (_collector.ProcessInfoAggregator != null)
                {
                    collectorHandler = ProcessAggregatorFactory.CreateCommunicator(_collector.ProcessInfoAggregator);
                }

                var rpcWebSocketHandler = SuperRPCWebSocket.CreateHandler(webSocket);
                var rpc = SetupRpc(rpcWebSocketHandler.ReceiveChannel, collectorHandler);
                await rpcWebSocketHandler.StartReceivingAsync();
            }
            else
            {
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            }
            return;
        }

        if(context.Request.Path == "/module-loader-init")
        {
            if (context.WebSockets.IsWebSocketRequest)
            {
                using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
                var rpcWebSocketHandler = SuperRPCWebSocket.CreateHandler(webSocket);
                var rpc = SetupRpc(rpcWebSocketHandler.ReceiveChannel);
                await rpcWebSocketHandler.StartReceivingAsync();
            }
        }

        await _next(context);
    }
}
