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

using MorganStanley.ComposeUI.Messaging.Server.Abstractions;

namespace MorganStanley.ComposeUI.Messaging.Server;

/// <summary>
/// Provides management and hosting features of the Message Router server.
/// </summary>
public interface IMessageRouterServer : IAsyncDisposable
{
    /// <summary>
    /// Notifies the Message Router of a new client.
    /// </summary>
    /// <param name="connection"></param>
    /// <returns></returns>
    public ValueTask ClientConnected(IClientConnection connection);

    /// <summary>
    /// Notifies the Message Router of a disconnected client.
    /// </summary>
    /// <param name="connection"></param>
    /// <returns></returns>
    public ValueTask ClientDisconnected(IClientConnection connection);
}