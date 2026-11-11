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

using Microsoft.Extensions.Hosting;
using MorganStanley.ComposeUI.ModuleLoader;

namespace MorganStanley.ComposeUI.Shell.Messaging;

/// <summary>
/// Revokes the message router access token of a module instance when the module stops.
/// </summary>
/// <remarks>
/// The subscription is made in the constructor, not in <see cref="StartAsync"/>, because the host
/// constructs every hosted service before it starts any of them. This way no event is missed,
/// even if another hosted service starts modules.
/// </remarks>
internal sealed class ModuleTokenRevocationService : IHostedService, IObserver<LifetimeEvent>, IDisposable
{
    public ModuleTokenRevocationService(IModuleLoader moduleLoader, IModuleTokenStore tokenStore)
    {
        _tokenStore = tokenStore;
        _subscription = moduleLoader.LifetimeEvents.Subscribe(this);
    }

    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public void OnNext(LifetimeEvent value)
    {
        if (value is LifetimeEvent.Stopped)
        {
            _tokenStore.Revoke(value.Instance.InstanceId);
        }
    }

    public void OnError(Exception error)
    {
    }

    public void OnCompleted()
    {
    }

    public void Dispose()
    {
        _subscription.Dispose();
    }

    private readonly IModuleTokenStore _tokenStore;
    private readonly IDisposable _subscription;
}
