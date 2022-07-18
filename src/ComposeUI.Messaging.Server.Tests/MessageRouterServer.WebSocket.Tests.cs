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

using System.Net.WebSockets;
using ComposeUI.Messaging.Server.Tests.TestUtils;
using FluentAssertions.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ComposeUI.Messaging.Server.Tests;

public class MessageRouterServerWebSocketTests
{
    public MessageRouterServerWebSocketTests()
    {
        App = new WebApplicationFactory<Program>();
        WebSocketUri = new Uri(App.Server.BaseAddress, relativeUri: "ws");
    }

    protected readonly WebApplicationFactory<Program> App;
    protected readonly Uri WebSocketUri;

    [Fact]
    public async Task Client_can_connect_and_receives_ConnectResponse()
    {
        var client = await ConnectAsync();
        client.State.Should().Be(WebSocketState.Open);
    }

    [Fact]
    public async Task Client_can_reconnect_with_existing_id()
    {
        var client1 = await App.Server.CreateWebSocketClient().ConnectAsync(WebSocketUri, CancellationToken.None);
        var clientId = await ConnectAndWaitForResponse(client1);
        client1.Dispose();
        await Task.Delay(100);
        var client2 = await App.Server.CreateWebSocketClient().ConnectAsync(WebSocketUri, CancellationToken.None);
        await client2.SendUtf8BytesAsync($@" {{ ""type"": ""Connect"", ""clientId"": ""{clientId}"" }}");
        var response = await client2.ReceiveJsonAsync();
        response.Should().ContainSubtree($@" {{ ""type"": ""ConnectResponse"", ""clientId"": ""{clientId}"" }} ");
    }

    [Fact]
    public async Task Client_can_subscribe_and_receive_messages()
    {
        var publisher = await ConnectAsync();
        var subscriber = await ConnectAsync();

        await subscriber.SendUtf8BytesAsync(@" { ""type"": ""Subscribe"", ""topic"": ""a/b/c"" } ");
        await Task.Delay(100);
        await publisher.SendUtf8BytesAsync(@" { ""type"": ""Publish"", ""topic"": ""a/b/c"", ""payload"": ""xyz"" } ");
        var updateMessage = await subscriber.ReceiveJsonAsync();

        updateMessage.Should()
            .ContainSubtree(@" { ""type"": ""Update"", ""topic"": ""a/b/c"", ""payload"": ""xyz"" } ");
    }

    [Fact]
    public async Task Client_can_register_itself_as_a_service()
    {
        var client = await ConnectAsync();

        await client.SendUtf8BytesAsync(
            @" { ""type"": ""RegisterService"", ""serviceName"": ""test-service"" } ");
        var response = await client.ReceiveJsonAsync();

        response.Should()
            .ContainSubtree(@" { ""type"": ""RegisterServiceResponse"", ""serviceName"": ""test-service"" } ");
        response.Should().NotHaveElement("error");
    }

    [Fact]
    public async Task Duplicate_service_registrations_result_in_error()
    {
        var client1 = await ConnectAsync();
        var client2 = await ConnectAsync();

        await client1.SendUtf8BytesAsync(
            @" { ""type"": ""RegisterService"", ""serviceName"": ""test-service"" } ");
        var response1 = await client1.ReceiveJsonAsync();
        await Task.Delay(100);

        await client2.SendUtf8BytesAsync(
            @" { ""type"": ""RegisterService"", ""serviceName"": ""test-service"" } ");
        var response2 = await client2.ReceiveJsonAsync();
        response2.Should().HaveElement("error");
    }

    [Fact]
    public async Task Client_can_invoke_a_registered_service()
    {
        var service = await ConnectAsync();
        var client = await ConnectAsync();

        await service.SendUtf8BytesAsync(@" { ""type"": ""RegisterService"", ""serviceName"": ""testService"" }");
        _ = await service.ReceiveJsonAsync(); // RegisterServiceResponse
        await Task.Delay(100);
        await client.SendUtf8BytesAsync(
            @" { ""type"": ""Invoke"", ""serviceName"": ""testService"", ""payload"": ""xyz"", ""requestId"": ""1"" } ");
        await Task.Delay(100);
        var request = await service.ReceiveJsonAsync();

        request.Should()
            .ContainSubtree(
                @" { ""type"": ""Invoke"", ""serviceName"": ""testService"", ""payload"": ""xyz"" } ");

        var requestId = request.Value<string>("requestId");
        await service.SendUtf8BytesAsync(
            $@" {{ ""type"": ""InvokeResponse"", ""requestId"": ""{requestId}"", ""payload"": ""abc"" }} ");
        await Task.Delay(100);
        var response = await client.ReceiveJsonAsync();

        response.Should()
            .ContainSubtree(
                @" { ""type"": ""InvokeResponse"", ""requestId"": ""1"", ""payload"": ""abc"" } ");
    }

    private async Task<string> ConnectAndWaitForResponse(WebSocket webSocket)
    {
        await webSocket.SendUtf8BytesAsync(@" { ""type"": ""Connect"" }");
        var response = await webSocket.ReceiveJsonAsync();
        response.Should().ContainSubtree(@" { ""type"": ""ConnectResponse"" } ");
        return response.Value<string>("clientId")!;
    }

    private async Task<WebSocket> ConnectAsync()
    {
        var webSocket = await App.Server.CreateWebSocketClient().ConnectAsync(WebSocketUri, CancellationToken.None);
        await ConnectAndWaitForResponse(webSocket);
        return webSocket;
    }
}