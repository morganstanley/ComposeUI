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

// ReSharper disable once CheckNamespace
using MorganStanley.ComposeUI.Messaging;

namespace Microsoft.Extensions.DependencyInjection;

public sealed class MessageRouterBuilder
{
    public MessageRouterBuilder UseAccessToken(string accessToken)
    {
        AccessToken = accessToken;
        return this;
    }

    public MessageRouterBuilder UseAccessTokenFromEnvironment()
    {
        AccessToken = Environment.GetEnvironmentVariable(EnvironmentVariables.AccessTokenEnvironmentVariableName);
        return this;
    }

    internal MessageRouterBuilder(IServiceCollection serviceCollection)
    {
        ServiceCollection = serviceCollection;
    }

    public IServiceCollection ServiceCollection { get; }

    internal string? AccessToken { get; set; }
}