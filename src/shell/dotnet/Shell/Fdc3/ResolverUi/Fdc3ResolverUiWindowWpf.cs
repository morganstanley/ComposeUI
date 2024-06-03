﻿/*
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
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Finos.Fdc3;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Contracts;

namespace MorganStanley.ComposeUI.Shell.Fdc3.ResolverUi;

internal class Fdc3ResolverUiWindowWpf : IResolverUiWindow
{
    private readonly ILogger<Fdc3ResolverUiWindowWpf> _logger;
    private readonly ILoggerFactory _loggerFactory;

    public Fdc3ResolverUiWindowWpf(
        ILogger<Fdc3ResolverUiWindowWpf>? logger = null,
        ILoggerFactory? loggerFactory = null)
    {
        _logger = logger ?? NullLogger<Fdc3ResolverUiWindowWpf>.Instance;
        _loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
    }

    public ValueTask<ResolverUiResponse> ShowResolverUi(IEnumerable<IAppMetadata> apps, TimeSpan timeout)
    {
        try
        {
            var dispatcher = GetDispatcher();

            Fdc3ResolverUi? resolverUi = null;
            Task? timeoutTask = null;

            dispatcher.Invoke(() =>
            {
                if (Application.Current.Dispatcher == null
                    || Application.Current.Dispatcher.HasShutdownStarted
                    || Application.Current.Dispatcher.HasShutdownFinished)
                {
                    return;
                }

                resolverUi = new Fdc3ResolverUi(apps, _loggerFactory.CreateLogger<Fdc3ResolverUi>());

                timeoutTask = Task.Delay(timeout)
                    .ContinueWith((task) => resolverUi?.Close(), TaskScheduler.FromCurrentSynchronizationContext());

                resolverUi.ShowDialog();
            });

            //First we need to check if the timeout happened
            if (timeoutTask != null
                && timeoutTask.IsCompletedSuccessfully)
            {
                return ValueTask.FromResult(
                    new ResolverUiResponse()
                    {
                        Error = ResolveError.ResolverTimeout,
                    });
            }

            if (resolverUi?.UserCancellationToken != null
                && resolverUi.UserCancellationToken.IsCancellationRequested)
            {
                return ValueTask.FromResult(
                    new ResolverUiResponse()
                    {
                        Error = ResolveError.UserCancelledResolution,
                    });
            }

            return ValueTask.FromResult(
                new ResolverUiResponse()
                {
                    AppMetadata = resolverUi?.AppMetadata
                });
        }
        catch (TimeoutException)
        {
            return ValueTask.FromResult(
                    new ResolverUiResponse()
                    {
                        Error = ResolveError.ResolverTimeout,
                    });
        }
        catch (Exception exception)
        {
            if (_logger.IsEnabled(LogLevel.Error))
            {
                _logger.LogError(exception, $"Exception thrown while showing ResolverUi.");
            }

            return ValueTask.FromResult(
                new ResolverUiResponse()
                {
                    Error = ResolveError.ResolverUnavailable
                });
        }
    }

    private static Dispatcher GetDispatcher()
    {
        return Application.Current.Dispatcher.Invoke(() =>
        {
            return
                //First window which is active
                Application.Current.Windows
                    .Cast<Window>()
                    .FirstOrDefault(window => window.IsActive) ??
                //Or the first window which is visible
                Application.Current.Windows
                    .Cast<Window>()
                    .FirstOrDefault(window => window.Visibility == Visibility.Visible);
        })?.Dispatcher ??
        Application.Current.Dispatcher;
    }
}