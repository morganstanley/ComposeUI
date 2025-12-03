/*
 * Morgan Stanley makes this available to you under the Apache License,
 * Version 2.0 (the "License"). You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0.
 *
 * See the NOTICE file distributed with this work for additional information
 * regarding copyright ownership. Unless required by applicable law or agreed
 * to in writing, software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
 * or implied. See the License for the specific language governing permissions
 * and limitations under the License.
 */

using Finos.Fdc3;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Client.Infrastructure.Internal;

internal delegate void PrivateChannelDisconnectEventHandler();
internal delegate void PrivateChannelUnsubscribeHandler(PrivateChannelDisconnectEventListener listener);

internal class PrivateChannelDisconnectEventListener : IListener
{
    private readonly PrivateChannelDisconnectEventHandler _onDisconnect;
    private readonly PrivateChannelUnsubscribeHandler _onUnsubscribe;
    private readonly ILogger<PrivateChannelDisconnectEventListener> _logger;
    private readonly SemaphoreSlim _semaphoreSlim = new(1, 1);
    private bool _subscribed;

    public PrivateChannelDisconnectEventListener(
        PrivateChannelDisconnectEventHandler onDisconnectEventHandler,
        PrivateChannelUnsubscribeHandler onUnsubscribeHandler,
        ILogger<PrivateChannelDisconnectEventListener>? logger = null)
    {
        _onDisconnect = onDisconnectEventHandler;
        _onUnsubscribe = onUnsubscribeHandler;
        _subscribed = true;
        _logger = logger ?? NullLogger<PrivateChannelDisconnectEventListener>.Instance;
    }

    public void Execute()
    {
        try
        {
            _semaphoreSlim.Wait();

            if (!_subscribed)
            {
                return;
            }

            _onDisconnect();
        }
        finally
        {
            _semaphoreSlim.Release();
        }
    }

    public void Unsubscribe()
    {
        UnsubscribeCore(true);
    }

    internal void UnsubscribeCore(bool doCallback)
    {
        try
        {
            _semaphoreSlim.Wait();

            _subscribed = false;

            if (doCallback)
            {
                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.LogDebug("Unsubscribing {NameOfPrivateChannelDisconnectEventHandler}.", nameof(PrivateChannelDisconnectEventHandler));
                }

                _onUnsubscribe(this);
            }
        }
        finally
        {
            _semaphoreSlim.Release();
        }
    }
}
