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

using Microsoft.Extensions.DependencyInjection;
using MorganStanley.ComposeUI.Messaging;
using MorganStanley.ComposeUI.Messaging.Abstractions;
using MorganStanley.ComposeUI.Messaging.Client.WebSocket;
using System.Text;
using System.Text.Json;
using System.Windows;
using Finos.Fdc3;
using Finos.Fdc3.Context;
using AppIdentifier = MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Protocol.AppIdentifier;

namespace DiagnosticsExample;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly IMessaging? _messaging;

    public string DiagnosticsText
    {
        get { return (string)GetValue(DiagnosticsTextProperty); }
        set { SetValue(DiagnosticsTextProperty, value); }
    }

    // Using a DependencyProperty as the backing store for DiagnosticsText.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty DiagnosticsTextProperty =
        DependencyProperty.Register("DiagnosticsText", typeof(string), typeof(MainWindow), new PropertyMetadata(string.Empty));

    private readonly IDesktopAgent _desktopAgent;
    private IChannel _appChannel;
    private IPrivateChannel? _privateChannel;
    private IListener? _privateChannelContextListener;
    private IListener? _subscription;
    private IListener _listener;

    public MainWindow()
    {
        InitializeComponent();

        _messaging = ((App)Application.Current).ServiceProvider.GetService<IMessaging>();
        _desktopAgent = ((App)Application.Current).ServiceProvider.GetService<IDesktopAgent>() ?? throw new NullReferenceException();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        var text = new StringBuilder();
        text.AppendLine(Environment.GetEnvironmentVariable("CUSTOM_GREETINGS"));
        text.Append("MessageRouter WebSocket URL: ");
        text.AppendLine(Environment.GetEnvironmentVariable(WebSocketEnvironmentVariableNames.Uri));
        text.Append("MessageRouter AccessToken: ");
        text.AppendLine(Environment.GetEnvironmentVariable(EnvironmentVariableNames.AccessToken));
        DiagnosticsText = text.ToString();

        try
        {
            if (_messaging != null)
            {
                Task.Run(LogDiagnostics);
            }
            else
            {
                DiagnosticsText += "No Message Router registered";
            }
        }
        catch (Exception ex)
        {
            Diagnostics.Text = ex.Message;
        }
    }

    private async Task LogDiagnostics()
    {
        var textBuilder = new StringBuilder();

        try
        {
            var diag = await _messaging!.InvokeJsonServiceAsync<DiagnosticInfo>("Diagnostics", new JsonSerializerOptions { WriteIndented = true }).ConfigureAwait(false);
            if (diag != null)
            {
                textBuilder.AppendLine(diag.ToString());
            }

            var result = await _desktopAgent.GetAppMetadata(new AppIdentifier() { AppId = "WPFExample" }).ConfigureAwait(false);
            textBuilder.AppendLine(result.Description);
        }
        catch (Exception ex)
        {
            textBuilder.AppendLine(ex.Message);
        }
        finally
        {
            await Dispatcher.InvokeAsync(() => DiagnosticsText += textBuilder.ToString());
        }
    }

    private async void SubscribeButton_Click(object sender, RoutedEventArgs e)
    {
        var textBuilder = new StringBuilder();
        DiagnosticsText = string.Empty;

        try
        {
            await Task.Run(async () =>
            {
                textBuilder.AppendLine("Subscription is in working progress");

                await JoinToUserChannel().ConfigureAwait(false);

                _subscription = await _desktopAgent.AddContextListener<Instrument>("fdc3.instrument",
                    (context, contextMetadata) =>
                    {
                        Dispatcher.Invoke(() =>
                            DiagnosticsText += "\nContext received: " + context.Name + "; type: " + context.Type);
                    }).ConfigureAwait(false);

                textBuilder.AppendLine("Subscription is done.");
            });
        }
        catch (Exception ex)
        {
            textBuilder.AppendLine($"AddContextListener failed: {ex.Message}, {ex}");
        }
        finally
        {
            await Dispatcher.InvokeAsync(() => DiagnosticsText = textBuilder.ToString());
        }
    }

    private async void BroadcastButton_Click(object sender, RoutedEventArgs e)
    {
        var textBuilder = new StringBuilder();
        DiagnosticsText = string.Empty;

        try
        {
            await Task.Run(async () =>
            {
                textBuilder.AppendLine("Broadcasting is in working progress");
                await JoinToUserChannel().ConfigureAwait(false);

                var instrument = new Instrument(new InstrumentID() { BBG = "test" }, $"{Guid.NewGuid()}");
                await _desktopAgent.Broadcast(instrument).ConfigureAwait(false);

                textBuilder.AppendLine("Context broadcasted");
            });
        }
        catch (Exception ex)
        {
            textBuilder.AppendLine($"Broadcast failed: {ex.Message}");
        }
        finally
        {
            await Dispatcher.InvokeAsync(() => DiagnosticsText = textBuilder.ToString());
        }
    }

    private async void AppChannelBroadcastButton_Click(object sender, RoutedEventArgs e)
    {
        await Task.Run(async () =>
        {
            Dispatcher.Invoke(() =>
            {
                DiagnosticsText += "\nChecking if app is already joined to an app channel...";
            });

            await JoinToAppChannel().ConfigureAwait(false);

            var instrument = new Instrument(new InstrumentID() { BBG = "app-channel-test" }, $"{Guid.NewGuid().ToString()}");
            await _appChannel.Broadcast(instrument).ConfigureAwait(false);

            Dispatcher.Invoke(() =>
            {
                DiagnosticsText += $"\nContext broadcasted to AppChannel: instrument: {instrument.ID}; {instrument.Name}";
            });
        });
    }

    private async void AppChannelAddContextListenerButton_Click(object sender, RoutedEventArgs e)
    {
        await Task.Run(async () =>
        {
            Dispatcher.Invoke(() =>
            {
                DiagnosticsText += "\nChecking if app is already joined to an app channel...";
            });

            await JoinToAppChannel().ConfigureAwait(false);

            var instrument = new Instrument(new InstrumentID() { BBG = "app-channel-test" }, $"{Guid.NewGuid().ToString()}");
            _listener = await _appChannel.AddContextListener<Instrument>("fdc3.instrument", (context, contextMetadata) =>
            {
                Dispatcher.Invoke(() =>
                {
                    DiagnosticsText += "\nContext received from AppChannel: " + context.Name + "; type: " + context.Type;
                });
            }).ConfigureAwait(false);

            Dispatcher.Invoke(() =>
            {
                DiagnosticsText += $"\nContext listener is added to AppChannel: instrument: {instrument.ID}; {instrument.Name}";
            });
        });
    }

    private async void FindIntentButton_Click(object sender, RoutedEventArgs e)
    {
        await Task.Run(async () =>
        {
            Dispatcher.Invoke(() =>
            {
                DiagnosticsText += "\nFinding intent for ViewChart...";
            });

            var result = await _desktopAgent.FindIntent("ViewChart").ConfigureAwait(false);

            Dispatcher.Invoke(() => DiagnosticsText += $"\nFindIntent is completed. Intent name: {result.Intent.Name}");

            foreach (var app in result.Apps)
            {
                Dispatcher.Invoke(() =>
                {
                    DiagnosticsText += $"\nIntent: {result.Intent.Name} is found for app: {app.AppId}";
                });
            }
        });
    }

    private async void FindInstancesButton_Click(object sender, RoutedEventArgs e)
    {
        await Task.Run(async () =>
        {
            var result = await _desktopAgent.GetAppMetadata(new AppIdentifier() { AppId = "WPFExample" }).ConfigureAwait(false);

            Dispatcher.Invoke(() =>
            {
                DiagnosticsText += $"\nFinding instances for {result.AppId}...";
            });

            var instances = await _desktopAgent.FindInstances(result).ConfigureAwait(false);

            foreach (var app in instances)
            {
                Dispatcher.Invoke(() =>
                {
                    DiagnosticsText += $"\nInstance found: app: {app.AppId}; FDC3 instanceId: {app.InstanceId}";
                });
            }
        });
    }

    private async void FindIntentsByContextButton_Click(object sender, RoutedEventArgs e)
    {
        await Task.Run(async () =>
        {
            var context = new Instrument();
            Dispatcher.Invoke(() =>
            {
                DiagnosticsText += $"\nFinding intents by context: {context.Type}...";
            });

            var appIntents = await _desktopAgent.FindIntentsByContext(context).ConfigureAwait(false);

            foreach (var appIntent in appIntents)
            {
                foreach (var app in appIntent.Apps)
                {
                    Dispatcher.Invoke(() =>
                    {
                        DiagnosticsText += $"\nIntent found: {appIntent.Intent.Name} for app: {app.AppId}";
                    });
                }
            }
        });
    }
    private async void RaiseIntentForContextButton_Click(object sender, RoutedEventArgs e)
    {
        await Task.Run(async () =>
        {
            var context = new Nothing();

            Dispatcher.Invoke(() =>
            {
                DiagnosticsText += $"\nRaising an intent for context: {context.Type}...";
            });

            var intentResolution = await _desktopAgent.RaiseIntentForContext(context).ConfigureAwait(false);

            Dispatcher.Invoke(() =>
            {
                DiagnosticsText += $"\nRaiseIntentForContext is completed. Intent name: {intentResolution.Intent} for app: {intentResolution.Source.AppId}. Awaiting for IntentResolution...";
            });

            var intentResult = await intentResolution.GetResult().ConfigureAwait(false);

            if (intentResult != null
                && intentResult is IChannel channel)
            {
                Dispatcher.Invoke(() =>
                {
                    DiagnosticsText += $"\nIntentResolution is completed. Channel returned: {channel.Id}...";
                });

                if (channel is IPrivateChannel privateChannel)
                {
                    Dispatcher.Invoke(() =>
                    {
                        DiagnosticsText += $"\n It is a private channel with id: {privateChannel.Id}...";
                    });

                    _privateChannel = privateChannel;
                    _privateChannel.OnAddContextListener((ctx) =>
                    {
                        Dispatcher.Invoke(() => DiagnosticsText += $"\nContext listener was added in private channel: {privateChannel.Id} for context: {ctx}...");
                    });

                    _privateChannel.OnDisconnect(() =>
                    {
                        Dispatcher.Invoke(() => DiagnosticsText += $"\nDisconnected from private channel: {_privateChannel.Id}...");
                    });

                    _privateChannel.OnUnsubscribe((ctx) =>
                    {
                        Dispatcher.Invoke(() => DiagnosticsText += $"\nUnsubscribed from private channel: {_privateChannel.Id} for context: {ctx}...");
                    });
                }
            }
            else if (intentResult != null
                && intentResult is IContext returnedContext)
            {
                Dispatcher.Invoke(() =>
                {
                    DiagnosticsText += $"\nIntentResult is completed. Context returned: {returnedContext.Type}...";
                });
            }
            else
            {
                Dispatcher.Invoke(() =>
                {
                    DiagnosticsText += $"\nIntentResult is completed. It was handled by the app ...";
                });
            }
        });
    }

    private async void AddIntentListenerButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await Task.Run(async () =>
            {
                Dispatcher.Invoke(() => DiagnosticsText += "\nAdding intent listener for OpenDiagnostics...");

                _listener = await _desktopAgent.AddIntentListener<Nothing>("OpenDiagnostics", async (context, contextMetadata) =>
                {
                    Dispatcher.Invoke(() => DiagnosticsText += "\n" + "Intent received: " + context.Name + "; type: " + context.Type);
                    await CreatePrivateChannelAsync();

                    return _privateChannel;
                }).ConfigureAwait(false);
            });
        }
        catch (Exception exception)
        {
            Dispatcher.Invoke(() => DiagnosticsText += $"\nException was thrown: {exception.ToString()}");
        }
    }

    private async Task CreatePrivateChannelAsync()
    {
        _privateChannel = await _desktopAgent.CreatePrivateChannel().ConfigureAwait(false);
        Dispatcher.Invoke(() => DiagnosticsText += $"\nCreated private channel with id: {_privateChannel.Id}...");

        _privateChannel.OnAddContextListener((ctx) =>
        {
            Dispatcher.Invoke(() => DiagnosticsText += $"\nContextListener added in private channel: {_privateChannel.Id} for context: {ctx}...");
        });

        _privateChannel.OnDisconnect(() =>
        {
            Dispatcher.Invoke(() => DiagnosticsText += $"\nDisconnected from private channel: {_privateChannel.Id}...");
        });

        _privateChannel.OnUnsubscribe((ctx) =>
        {
            Dispatcher.Invoke(() => DiagnosticsText += $"\nUnsubscribed from private channel: {_privateChannel.Id} for context: {ctx}...");
        });
    }

    private async void RaiseIntentButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await Task.Run(async () =>
            {
                Dispatcher.Invoke(() => DiagnosticsText += "\nRaising intent for fdc3.instrument...");

                var resolution = await _desktopAgent.RaiseIntent("OpenDiagnostics", new Instrument(new InstrumentID() { BBG = "raise-intent-test" }, "Raise Intent Test")).ConfigureAwait(false);

                var result = await resolution.GetResult().ConfigureAwait(false);

                if (result == null)
                {
                    Dispatcher.Invoke(() => DiagnosticsText += "\nIntent was handled by the app, no result returned.");
                    return;
                }
                else if (result is IChannel channel)
                {
                    Dispatcher.Invoke(() => DiagnosticsText += $"\nIntentResolution is completed. Channel returned: {channel.Id}...");

                    if (channel is IPrivateChannel privateChannel)
                    {
                        Dispatcher.Invoke(() => DiagnosticsText += $"\n It is a private channel with id: {privateChannel.Id}...");
                    }
                }
                else if (result is IContext context)
                {
                    Dispatcher.Invoke(() => DiagnosticsText += $"\n IntentResolution is completed. Context returned: {context.Type}...");
                }
            });
        }
        catch (Exception exception)
        {
            Dispatcher.Invoke(() => DiagnosticsText += $"\nRaiseIntent failed: {exception.ToString()}");
        }
    }

    private async void OpenButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await Task.Run(async () =>
            {
                Dispatcher.Invoke(() => DiagnosticsText += "\nOpening app with fdc3.instrument...");

                var appIdentifier = await _desktopAgent.Open(new AppIdentifier() { AppId = "WPFExample" }, new Instrument(new InstrumentID() { BBG = "open-test" }, "Open Test")).ConfigureAwait(false);

                Dispatcher.Invoke(() => DiagnosticsText += $"\nOpen is completed. AppId: {appIdentifier.AppId}, instanceId: {appIdentifier.InstanceId}...");
            });
        }
        catch (Exception exception)
        {
            Dispatcher.Invoke(() => DiagnosticsText += $"\nOpen failed: {exception.ToString()}");
        }
    }

    private async void PrivateChannelBroadcastButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await Task.Run(async () =>
            {
                if (_privateChannel == null)
                {
                    Dispatcher.Invoke(() => DiagnosticsText += "\nNo private channel to broadcast to. You should RaiseIntentForContext first and the new app should add its context listener to the private channel!");
                    return;
                }

                Dispatcher.Invoke(() =>
                {
                    DiagnosticsText += $"\nBroadcasting to a private channel: {_privateChannel?.Id}";
                });

                var instrument = new Instrument(new InstrumentID() { BBG = "private-channel-test" }, $"{Guid.NewGuid().ToString()}");
                await _privateChannel.Broadcast(instrument).ConfigureAwait(false);
            });
        }
        catch (Exception exception)
        {
            Dispatcher.Invoke(() => DiagnosticsText += $"\nPrivate channel broadcast failed: {exception.ToString()}");
        }
    }

    private async void PrivateChannelAddContextListenerButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await Task.Run(async () =>
            {
                if (_privateChannel == null)
                {
                    Dispatcher.Invoke(() => DiagnosticsText += "\nNo private channel to broadcast to. You should RaiseIntentForContext first and the new app should add its context listener to the private channel!");
                    return;
                }

                Dispatcher.Invoke(() =>
                {
                    DiagnosticsText += $"\nAdding context Listener to a private channel: {_privateChannel?.Id}";
                });

                _privateChannelContextListener = await _privateChannel.AddContextListener<Instrument>("fdc3.instrument", (context, contextMetadata) =>
                {
                    Dispatcher.Invoke(() => DiagnosticsText += $"\nContext received from private channel for Context: {context}...");
                }).ConfigureAwait(false);
            });
        }
        catch (Exception exception)
        {
            Dispatcher.Invoke(() => DiagnosticsText += $"\nPrivate channel broadcast failed: {exception.ToString()}");
        }
    }

    private async void PrivateChannelDisconnectButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await Task.Run(() =>
            {
                if (_privateChannel == null)
                {
                    Dispatcher.Invoke(() => DiagnosticsText += "\nNo private channel to disconnect from. You should RaiseIntentForContext first and the new app should add its context listener to the private channel!");
                    return;
                }

                Dispatcher.Invoke(() =>
                {
                    DiagnosticsText += $"\nDisconnecting from a private channel: {_privateChannel?.Id}";
                });

                _privateChannel.Disconnect();
            });
        }
        catch (Exception exception)
        {
            Dispatcher.Invoke(() => $"\nException was thrown: {exception.ToString()}");
        }
    }

    private async void PrivateChannelUnsubscribeButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await Task.Run(async () =>
            {
                if (_privateChannel == null)
                {
                    Dispatcher.Invoke(() => DiagnosticsText += "\nNo private channel to unsubscribe. You should RaiseIntentForContext first and the new app should add its context listener to the private channel!");
                    return;
                }

                if (_privateChannelContextListener == null)
                {
                    Dispatcher.Invoke(() => DiagnosticsText += "\nNo context listener is registered on the private channel to unsubscribe. You should RaiseIntentForContext first and the new app should add its context listener to the private channel!");
                    return;
                }

                Dispatcher.Invoke(() =>
                {
                    DiagnosticsText += $"\nUnsubscribing from a private channel: {_privateChannel?.Id} with private channel context listener";
                });

                _privateChannelContextListener.Unsubscribe();
            });
        }
        catch (Exception exception)
        {
            Dispatcher.Invoke(() => $"\nException was thrown: {exception.ToString()}");
        }
    }

    private async Task JoinToAppChannel()
    {
        if (_appChannel == null)
        {
            _appChannel = await _desktopAgent.GetOrCreateChannel("app-channel-1").ConfigureAwait(false);

            Dispatcher.Invoke(() =>
            {
                DiagnosticsText += "\nJoined to AppChannel: app-channel-1...";
            });
        }
    }

    private async Task JoinToUserChannel()
    {
        if (await _desktopAgent.GetCurrentChannel() == null)
        {
            var channels = await _desktopAgent.GetUserChannels();
            var firstChannel = channels.FirstOrDefault();
            if (firstChannel == null)
            {
                await _desktopAgent.JoinUserChannel("fdc3.channel.1");
                return;
            }

            await _desktopAgent.JoinUserChannel(firstChannel.Id);
        }
    }
}
