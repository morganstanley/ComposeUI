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

using MorganStanley.ComposeUI.Tryouts.Messaging.Server;
using MorganStanley.ComposeUI.Tryouts.Messaging.Server.Internal;

namespace Microsoft.Extensions.DependencyInjection;

public sealed class MessageRouterBuilder
{
    public MessageRouterBuilder(IServiceCollection serviceCollection)
    {
        ServiceCollection = serviceCollection;
    }

    public MessageRouterBuilder UseAccessTokenValidator(IAccessTokenValidator validator)
    {
        ServiceCollection.AddSingleton(validator);

        return this;
    }

    public MessageRouterBuilder UseAccessTokenValidator(Func<IServiceProvider, IAccessTokenValidator> factory)
    {
        ServiceCollection.AddSingleton<IAccessTokenValidator>(factory);

        return this;
    }

    public MessageRouterBuilder UseAccessTokenValidator(Action<string, string?> validatorCallback)
    {
        ServiceCollection.AddSingleton<IAccessTokenValidator>(
            new AccessTokenValidator(
                (id, token) =>
                {
                    validatorCallback(id, token);

                    return default(ValueTask);
                }));

        return this;
    }

    public MessageRouterBuilder UseAccessTokenValidator(Func<string, string?, ValueTask> validatorCallback)
    {
        ServiceCollection.AddSingleton<IAccessTokenValidator>(new AccessTokenValidator(validatorCallback));

        return this;
    }

    public MessageRouterBuilder UseAccessTokenValidator(Func<string, string?, Task> validatorCallback)
    {
        ServiceCollection.AddSingleton<IAccessTokenValidator>(
            new AccessTokenValidator(
                (id, token) => new ValueTask(validatorCallback(id, token))));

        return this;
    }


    internal IServiceCollection ServiceCollection { get; }
}
