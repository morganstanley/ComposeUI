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

using System;
using System.Windows.Controls;
using Microsoft.Extensions.Logging;

namespace WpfHost;

public class MainWindowLoggerProvider : ILoggerProvider, ISupportExternalScope
{
    private readonly App _app;
    private IExternalScopeProvider? _scopeProvider;

    public MainWindowLoggerProvider(App app)
    {
        _app = app;
    }

    public void Dispose()
    {
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new Logger(categoryName, _app, _scopeProvider!);
    }

    private class Logger : ILogger
    {
        private readonly string _category;
        private readonly App _app;
        private readonly IExternalScopeProvider _scopeProvider;

        public Logger(string category, App app, IExternalScopeProvider scopeProvider)
        {
            _category = category;
            _app = app;
            _scopeProvider = scopeProvider;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            _app.Dispatcher.InvokeAsync(
                () =>
                {
                    var textBox = GetTextBox();

                    if (textBox == null)
                        return;

                    textBox.Text += $"[{logLevel}] [{_category}] {formatter(state, exception)}\r\n";
                });
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return _scopeProvider.Push(state);
        }

        private TextBox? GetTextBox()
        {
            return _app.MainWindow?.ConsoleOutputTextBox;
        }
    }

    public void SetScopeProvider(IExternalScopeProvider scopeProvider)
    {
        _scopeProvider = scopeProvider;
    }
}
