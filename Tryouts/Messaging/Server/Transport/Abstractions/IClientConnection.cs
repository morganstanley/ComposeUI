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

using MorganStanley.ComposeUI.Tryouts.Messaging.Core.Messages;

namespace MorganStanley.ComposeUI.Tryouts.Messaging.Server.Transport.Abstractions;

/// <summary>
/// Abstraction of a client connected to the Message Router server.
/// </summary>
public interface IClientConnection : IAsyncDisposable
{
    /// <summary>
    /// Sends a message to the client.
    /// </summary>
    /// <param name="message"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    ValueTask SendAsync(Message message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the async stream of messages received from the client.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    IAsyncEnumerable<Message> ReceiveAsync(CancellationToken cancellationToken = default);
}