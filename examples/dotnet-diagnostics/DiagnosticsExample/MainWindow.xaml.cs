﻿// Morgan Stanley makes this available to you under the Apache License,
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
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Protocol;
using Finos.Fdc3.Context;
using System.CodeDom;

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
        var diag = await _messaging!.InvokeJsonServiceAsync<DiagnosticInfo>("Diagnostics", new JsonSerializerOptions { WriteIndented = true });
        
        if (diag == null)
        {
            return;
        }
        
        await Dispatcher.InvokeAsync(() => DiagnosticsText += diag.ToString());

        var result =
            await _desktopAgent.GetAppMetadata(new MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Protocol.AppIdentifier()
                { AppId = "WPFExample" });

        await Dispatcher.InvokeAsync(() => DiagnosticsText += "\n" + result.Description);
    }

    private async void SubscribeButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Dispatcher.Invoke(() => DiagnosticsText += "\n" + "Subscription is in working progress");

            await Task.Run(async () =>
            {
                if (await _desktopAgent.GetCurrentChannel() == null)
                {
                    await _desktopAgent.JoinUserChannel("fdc3.channel.1");
                }

                _subscription = await _desktopAgent.AddContextListener<Instrument>("fdc3.instrument", (context, contextMetadata) =>
                {
                    Dispatcher.Invoke(() => DiagnosticsText += "\n" + "Context received: " + context.Name + "; type: " + context.Type);
                });
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
                if (await _desktopAgent.GetCurrentChannel() == null)
                {
                    await _desktopAgent.JoinUserChannel("fdc3.channel.1");
                }

                var instrument = new Instrument(new InstrumentID() { BBG = "test" }, $"{Guid.NewGuid().ToString()}");
                await _desktopAgent.Broadcast(instrument);
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
}
