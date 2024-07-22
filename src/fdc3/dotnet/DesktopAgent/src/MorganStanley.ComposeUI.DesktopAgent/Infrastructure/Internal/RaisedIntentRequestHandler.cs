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

using Finos.Fdc3.Context;
using Finos.Fdc3;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Infrastructure.Internal;

internal class RaisedIntentRequestHandler
{
    private readonly object _intentListenersLock = new();
    private readonly object _raiseIntentInvocationsLock = new();
    private readonly List<string> _registeredIntentListeners = new();
    private readonly List<RaiseIntentResolutionInvocation> _raiseIntentResolutions = new();
    private readonly ILogger<RaisedIntentRequestHandler> _logger;

    public IEnumerable<string> IntentListeners => _registeredIntentListeners;
    public IEnumerable<RaiseIntentResolutionInvocation> RaiseIntentResolutions => _raiseIntentResolutions;

    public RaisedIntentRequestHandler(ILogger<RaisedIntentRequestHandler>? logger = null)
    {
        _logger = logger ?? NullLogger<RaisedIntentRequestHandler>.Instance;
    }

    public RaisedIntentRequestHandler AddIntentListener(string intent)
    {
        lock (_intentListenersLock)
        {
            if (_registeredIntentListeners.Contains(intent))
            {
                if (_logger.IsEnabled(LogLevel.Warning))
                {
                    _logger.LogWarning($"Multiple IntentHandlers are registered to the intent: {intent}.");
                }
            }

            _registeredIntentListeners.Add(intent);
            return this;
        }
    }

    public RaisedIntentRequestHandler RemoveIntentListener(string intent)
    {
        lock (_intentListenersLock)
        {
            _registeredIntentListeners.Remove(intent);
            return this;
        }
    }

    public bool IsIntentListenerRegistered(string intent)
    {
        lock (_intentListenersLock)
        {
            return _registeredIntentListeners.Contains(intent);
        }
    }

    public RaisedIntentRequestHandler AddRaiseIntentToHandle(RaiseIntentResolutionInvocation raiseIntentInvocation)
    {
        lock (_raiseIntentInvocationsLock)
        {
            _raiseIntentResolutions.Add(raiseIntentInvocation);
            return this;
        }
    }

    public RaisedIntentRequestHandler AddIntentResult(
        string messageId,
        string intent,
        string? channelId = null,
        ChannelType? channelType = null,
        Context? context = null,
        bool? voidResult = false,
        string? error = null)
    {
        lock (_raiseIntentInvocationsLock)
        {
            var raisedIntentInvocations = _raiseIntentResolutions.Where(
                raisedIntentToHandle => raisedIntentToHandle.RaiseIntentMessageId == messageId && raisedIntentToHandle.Intent == intent);

            if (raisedIntentInvocations.Count() == 1)
            {
                var raisedIntentInvocation = raisedIntentInvocations.First();
                raisedIntentInvocation.ResultChannelId = channelId;
                raisedIntentInvocation.ResultChannelType = channelType;
                raisedIntentInvocation.ResultContext = context;
                raisedIntentInvocation.ResultVoid = voidResult;
                raisedIntentInvocation.ResultError = error;
                raisedIntentInvocation.IsResolved = true;
            }
            else if (raisedIntentInvocations.Count() > 1)
            {
                throw ThrowHelper.MultipleIntentRegisteredToAnAppInstance(intent);
            }

            return this;
        }
    }

    public bool TryGetRaisedIntentResult(string messageId, string intent, out RaiseIntentResolutionInvocation raiseIntentInvocation)
    {
        lock (_raiseIntentInvocationsLock)
        {
            var raiseIntentInvocations = _raiseIntentResolutions.Where(raiseIntentInvocation => 
                raiseIntentInvocation.RaiseIntentMessageId == messageId 
                && raiseIntentInvocation.Intent == intent 
                && raiseIntentInvocation.IsResolved);

            if (raiseIntentInvocations.Any())
            {
                if (raiseIntentInvocations.Count() > 1)
                {
                    throw ThrowHelper.MultipleIntentRegisteredToAnAppInstance(intent);
                }
                raiseIntentInvocation = raiseIntentInvocations.First();
                return true;
            }

            raiseIntentInvocation = null;
            return false;
        }
    }
}