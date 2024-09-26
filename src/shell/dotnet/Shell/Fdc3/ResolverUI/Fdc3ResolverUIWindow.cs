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

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using Finos.Fdc3;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Contracts;

namespace MorganStanley.ComposeUI.Shell.Fdc3.ResolverUI;

internal class Fdc3ResolverUIWindow(ILogger<Fdc3ResolverUIWindow>? logger = null) : IResolverUIProjector
{
    private readonly ILogger<Fdc3ResolverUIWindow> _logger = logger ?? NullLogger<Fdc3ResolverUIWindow>.Instance;

    public ValueTask<ResolverUIResponse> ShowResolverUI(IEnumerable<IAppMetadata> apps, TimeSpan timeout)
    {
        try
        {
            var dispatcher = Application.Current.Dispatcher;

            ResolverUI? resolverUI = null;
            Task? timeoutTask = null;

            dispatcher.Invoke(
                () =>
                {
                    if (dispatcher.HasShutdownStarted
                        || dispatcher.HasShutdownFinished)
                    {
                        return;
                    }

                    resolverUI = new ResolverUI(apps);

                    timeoutTask = Task.Delay(timeout)
                        .ContinueWith(task => resolverUI?.Close(), TaskScheduler.FromCurrentSynchronizationContext());

                    resolverUI.ShowDialog();
                });

            //First we need to check if the timeout happened
            if (timeoutTask != null
                && timeoutTask.IsCompletedSuccessfully)
            {
                return ValueTask.FromResult(
                    new ResolverUIResponse
                    {
                        Error = ResolveError.ResolverTimeout
                    });
            }

            if (resolverUI?.UserCancellationToken != null
                && resolverUI.UserCancellationToken.IsCancellationRequested)
            {
                return ValueTask.FromResult(
                    new ResolverUIResponse
                    {
                        Error = ResolveError.UserCancelledResolution
                    });
            }

            return ValueTask.FromResult(
                new ResolverUIResponse
                {
                    AppMetadata = resolverUI?.AppMetadata
                });
        }
        catch (TimeoutException)
        {
            return ValueTask.FromResult(
                new ResolverUIResponse
                {
                    Error = ResolveError.ResolverTimeout
                });
        }
        catch (Exception exception)
        {
            if (_logger.IsEnabled(LogLevel.Error))
            {
                _logger.LogError(exception, message: "Exception thrown while showing ResolverUi.");
            }

            return ValueTask.FromResult(
                new ResolverUIResponse
                {
                    Error = ResolveError.ResolverUnavailable
                });
        }
    }

    //TODO: Refactor this
    public ValueTask<ResolverUIIntentResponse> ShowResolverUI(IEnumerable<string> intents, TimeSpan timeout)
    {
        try
        {
            var dispatcher = Application.Current.Dispatcher;

            ResolverUIIntent? resolverUI = null;
            Task? timeoutTask = null;

            dispatcher.Invoke(
                () =>
                {
                    if (dispatcher.HasShutdownStarted
                        || dispatcher.HasShutdownFinished)
                    {
                        return;
                    }

                    resolverUI = new ResolverUIIntent(intents);

                    timeoutTask = Task.Delay(timeout)
                        .ContinueWith(task => resolverUI?.Close(), TaskScheduler.FromCurrentSynchronizationContext());

                    resolverUI.ShowDialog();
                });

            //First we need to check if the timeout happened
            if (timeoutTask != null
                && timeoutTask.IsCompletedSuccessfully)
            {
                return ValueTask.FromResult(
                    new ResolverUIIntentResponse
                    {
                        Error = ResolveError.ResolverTimeout
                    });
            }

            if (resolverUI?.UserCancellationToken != null
                && resolverUI.UserCancellationToken.IsCancellationRequested)
            {
                return ValueTask.FromResult(
                    new ResolverUIIntentResponse
                    {
                        Error = ResolveError.UserCancelledResolution
                    });
            }

            return ValueTask.FromResult(
                new ResolverUIIntentResponse
                {
                    SelectedIntent = resolverUI?.Intent
                });
        }
        catch (TimeoutException)
        {
            return ValueTask.FromResult(
                new ResolverUIIntentResponse
                {
                    Error = ResolveError.ResolverTimeout
                });
        }
        catch (Exception exception)
        {
            if (_logger.IsEnabled(LogLevel.Error))
            {
                _logger.LogError(exception, message: "Exception thrown while showing ResolverUi.");
            }

            return ValueTask.FromResult(
                new ResolverUIIntentResponse
                {
                    Error = ResolveError.ResolverUnavailable
                });
        }
    }
}