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
using System;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace DiagnosticsExample;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private IMessageRouter? _messageRouter;



    public string DiagnosticsText
    {
        get { return (string)GetValue(DiagnosticsTextProperty); }
        set { SetValue(DiagnosticsTextProperty, value); }
    }

    // Using a DependencyProperty as the backing store for DiagnosticsText.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty DiagnosticsTextProperty =
        DependencyProperty.Register("DiagnosticsText", typeof(string), typeof(MainWindow), new PropertyMetadata(string.Empty));



    public MainWindow()
    {
        InitializeComponent();
        _messageRouter = ((App)Application.Current).ServiceProvider.GetService<IMessageRouter>();
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
            if (_messageRouter != null)
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
        var diag = await _messageRouter!.InvokeAsync("Diagnostics");
        await Dispatcher.InvokeAsync(() => DiagnosticsText += JsonSerializer.Serialize(diag.ReadJson<DiagnosticInfo>(), new JsonSerializerOptions { WriteIndented = true }));
    }
}
