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
    private IListener _subscription;
    private IChannel _appChannel;
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
        var diag = await _messaging!.InvokeJsonServiceAsync<DiagnosticInfo>("Diagnostics", new JsonSerializerOptions { WriteIndented = true }).ConfigureAwait(false);

        if (diag == null)
        {
            return;
        }

        await Dispatcher.InvokeAsync(() => DiagnosticsText += diag.ToString());

        var result =
            await _desktopAgent.GetAppMetadata(new MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Protocol.AppIdentifier()
            { AppId = "WPFExample" }).ConfigureAwait(false);

        await Dispatcher.InvokeAsync(() => DiagnosticsText += "\n" + result.Description);
    }

    private async void SubscribeButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Dispatcher.Invoke(() => DiagnosticsText += "\n" + "Subscription is in working progress");

            await Task.Run(async () =>
            {
                await JoinToUserChannel().ConfigureAwait(false);
                _subscription = await _desktopAgent.AddContextListener<Instrument>("fdc3.instrument", (context, contextMetadata) =>
                {
                    Dispatcher.Invoke(() => DiagnosticsText += "\n" + "Context received: " + context.Name + "; type: " + context.Type);
                }).ConfigureAwait(false);
            });

            DiagnosticsText += "\n" + "Subscription is done.";
        }
        catch (Exception ex)
        {
            await Dispatcher.InvokeAsync(() =>
            {
                DiagnosticsText += $"\nAddContextListener failed: {ex.Message}, {ex.ToString()}";
            });
        }
    }

    private async void BroadcastButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Dispatcher.Invoke(() =>
            {
                DiagnosticsText += "\nBroadcasting is in working progress";
            });

            await Task.Run(async () =>
            {
                await JoinToUserChannel().ConfigureAwait(false);

                var instrument = new Instrument(new InstrumentID() { BBG = "test" }, $"{Guid.NewGuid().ToString()}");
                await _desktopAgent.Broadcast(instrument).ConfigureAwait(false);
            });

            Dispatcher.Invoke(() => DiagnosticsText += "\nContext broadcasted");
        }
        catch (Exception ex)
        {
            await Dispatcher.InvokeAsync(() =>
            {
                DiagnosticsText += $"\nBroadcast failed: {ex.Message}";
            });
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
                    DiagnosticsText += "\n" + "Context received from AppChannel: " + context.Name + "; type: " + context.Type;
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
            var context = new Instrument();

            Dispatcher.Invoke(() =>
            {
                DiagnosticsText += $"\nRaising an intent for context: {context.Type}...";
            });

            var result = await _desktopAgent.RaiseIntentForContext(context).ConfigureAwait(false);

            Dispatcher.Invoke(() =>
            {
                DiagnosticsText += $"\nRaiseIntentForContext is completed. Intent name: {result.Intent} for app: {result.Source.AppId}. Awaiting for IntentResolution...";
            });

            var intentResolution = await result.GetResult().ConfigureAwait(false);

            if (intentResolution != null
                && intentResolution is IChannel channel)
            {
                Dispatcher.Invoke(() =>
                {
                    DiagnosticsText += $"\nIntentResolution is completed. Channel returned: {channel.Id}...";
                });
            }
            else if (intentResolution != null
                && intentResolution is IContext returnedContext)
            {
                Dispatcher.Invoke(() =>
                {
                    DiagnosticsText += $"\nIntentResolution is completed. Context returned: {returnedContext.Type}...";
                });
            }
            else
            {
                Dispatcher.Invoke(() =>
                {
                    DiagnosticsText += $"\nIntentResolution is completed. It was handled by the app ...";
                });
            }
        });
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
            if (channels.Count() > 0)
            {
                await _desktopAgent.JoinUserChannel(channels.ElementAt(0).Id);
            }
            else
            {
                await _desktopAgent.JoinUserChannel("fdc3.channel.1");
            }
        }
    }
}
