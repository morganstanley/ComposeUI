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
