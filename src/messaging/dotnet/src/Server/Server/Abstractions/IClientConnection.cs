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

using MorganStanley.ComposeUI.Messaging.Protocol.Messages;

namespace MorganStanley.ComposeUI.Messaging.Server.Abstractions;

/// <summary>
/// Abstraction of a client connected to the Message Router server.
/// </summary>
public interface IClientConnection
{
    /// <summary>
    /// Sends a message to the client.
    /// </summary>
    /// <param name="message"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="MessageRouterException">With name <see cref="MessageRouterErrors.ConnectionClosed"/> if the connection was closed by either party while sending the request</exception>
    /// <exception cref="MessageRouterException">With name <see cref="MessageRouterErrors.ConnectionAborted"/> if the connection was closed due to an unexpected error</exception>
    public ValueTask SendAsync(Message message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the next message received from the client.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="MessageRouterException">With name <see cref="MessageRouterErrors.ConnectionClosed"/> if the connection was closed by either party while sending the request</exception>
    /// <exception cref="MessageRouterException">With name <see cref="MessageRouterErrors.ConnectionAborted"/> if the connection was closed due to an unexpected error</exception>
    public ValueTask<Message> ReceiveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Invoked by the server to notify the object of a disconnection.
    /// </summary>
    /// <returns></returns>
    public ValueTask CloseAsync();
}