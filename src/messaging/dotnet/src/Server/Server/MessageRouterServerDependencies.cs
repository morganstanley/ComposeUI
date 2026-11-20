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

namespace MorganStanley.ComposeUI.Messaging.Server;

internal class MessageRouterServerDependencies
{
    public MessageRouterServerDependencies(
        IAccessTokenValidator? accessTokenValidator = null,
        TimeSpan? connectTimeout = null)
    {
        AccessTokenValidator = accessTokenValidator;
        ConnectTimeout = connectTimeout ?? DefaultConnectTimeout;
    }

    public IAccessTokenValidator? AccessTokenValidator { get; }

    /// <summary>
    /// How long a connection may stay open without sending a Connect request.
    /// </summary>
    public TimeSpan ConnectTimeout { get; }

    private static readonly TimeSpan DefaultConnectTimeout = TimeSpan.FromSeconds(10);
}
