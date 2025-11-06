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

internal delegate void PrivateChannelEventHandler(string? contextType);
internal delegate void PrivateChannelOnUnsubscribeHandler(PrivateChannelContextListenerEventListener privateChannelContextListener);

internal class PrivateChannelContextListenerEventListener : IListener
{
    private PrivateChannelEventHandler _handler;
    private PrivateChannelOnUnsubscribeHandler _unsubscribeCallback;
    private readonly ILogger<PrivateChannelContextListenerEventListener> _logger;
    private readonly SemaphoreSlim _semaphoreSlim = new(1, 1);
    private bool _subscribed;

    public PrivateChannelContextListenerEventListener(
        PrivateChannelEventHandler onContext,
        PrivateChannelOnUnsubscribeHandler onUnsubscribe,
        ILogger<PrivateChannelContextListenerEventListener>? logger = null)
    {
        _handler = onContext;
        _unsubscribeCallback = onUnsubscribe;
        _subscribed = true;
        _logger = logger ?? NullLogger<PrivateChannelContextListenerEventListener>.Instance;
    }

    public void Execute(string? contextType)
    {
        try
        {
            _semaphoreSlim.Wait();

            if (_subscribed)
            {
                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.LogDebug("Invoking context listener for context type '{ContextType}'.", contextType);
                }

                _handler(contextType);
            }
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
                    _logger.LogDebug("Unsubscribing {NameOfPrivateChannelContextListenerEventListener}.", nameof(PrivateChannelContextListenerEventListener));
                }

                _unsubscribeCallback(this);
            }
        }
        finally
        {
            _semaphoreSlim.Release();
        }
    }
}
