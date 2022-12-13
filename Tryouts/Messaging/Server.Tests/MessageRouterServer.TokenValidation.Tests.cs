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

using Microsoft.Extensions.DependencyInjection;
using MorganStanley.ComposeUI.Messaging.Protocol.Messages;
using MorganStanley.ComposeUI.Messaging.Server;
using MorganStanley.ComposeUI.Messaging.TestUtils;

namespace MorganStanley.ComposeUI.Messaging;

public class MessageRouterServerTokenValidationTests
{
    [Fact]
    public async Task It_accepts_connection_with_valid_token()
    {
        var validator = new Mock<IAccessTokenValidator>();

        var server = CreateServer(
            mr => mr.UseAccessTokenValidator(validator.Object));

        var connection = new MockClientConnection();

        await server.ClientConnected(connection.Object);
        await connection.Sent.Writer.WriteAsync(new ConnectRequest { AccessToken = "token" });
        var connectResponse = await connection.Received.Reader.ReadAsync();
        connectResponse.Should().BeOfType<ConnectResponse>();
        ((ConnectResponse)connectResponse).ClientId.Should().NotBeNull();
        ((ConnectResponse)connectResponse).Error.Should().BeNull();
    }

    [Fact]
    public async Task It_accepts_connection_without_token_if_validator_is_not_registered()
    {
        var server = CreateServer(_ => { });

        var connection = new MockClientConnection();

        await server.ClientConnected(connection.Object);
        await connection.Sent.Writer.WriteAsync(new ConnectRequest());
        var connectResponse = await connection.Received.Reader.ReadAsync();
        connectResponse.Should().BeOfType<ConnectResponse>();
        ((ConnectResponse)connectResponse).ClientId.Should().NotBeNull();
        ((ConnectResponse)connectResponse).Error.Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("invalid-token")]
    public async Task It_rejects_connections_with_invalid_token(string? token)
    {
        var validator = new Mock<IAccessTokenValidator>();

        validator.Setup(_ => _.Validate(It.IsAny<string>(), It.IsAny<string?>()))
            .Throws(new InvalidOperationException("Invalid token"));

        var server = CreateServer(
            mr => mr.UseAccessTokenValidator(validator.Object));

        var connection = new MockClientConnection();
        

        await server.ClientConnected(connection.Object);
        await connection.Sent.Writer.WriteAsync(new ConnectRequest{AccessToken = token});

        var connectResponse = await connection.Received.Reader.ReadAsync();
        connectResponse.Should().BeOfType<ConnectResponse>();
        ((ConnectResponse)connectResponse).Error.Should().Be("Invalid token");
    }

    private static IMessageRouterServer CreateServer(Action<MessageRouterBuilder> builderAction)
    {
        var services = new ServiceCollection()
            .AddMessageRouterServer(builderAction)
            .BuildServiceProvider();

        return services.GetRequiredService<IMessageRouterServer>();
    }
}
