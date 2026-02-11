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

using System.Collections.Concurrent;
using Finos.Fdc3.AppDirectory;
using Microsoft.Extensions.Logging;
using MorganStanley.ComposeUI.Fdc3.AppDirectory;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.DependencyInjection;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Infrastructure.Internal;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Tests.TestUtils;
using MorganStanley.ComposeUI.ModuleLoader;
using ILoggerFactory = Microsoft.Extensions.Logging.ILoggerFactory;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Tests;

public abstract class Fdc3DesktopAgentServiceTestsBase : IAsyncLifetime
{
    protected IAppDirectory AppDirectory { get; }

    internal IFdc3DesktopAgentService Fdc3 { get; }
    protected MockModuleLoader ModuleLoader { get; } = new();
    protected Mock<IResolverUICommunicator> ResolverUICommunicator { get; } = new();
    internal Mock<ILogger<It.IsAnyType>> Logger { get; } = new();
    protected Mock<ILoggerFactory> LoggerFactory { get; } = new();
    protected Mock<IChannelSelector> ChannelSelector { get; } = new();

    private readonly ConcurrentDictionary<Guid, IModuleInstance> _modules = new();
    private IDisposable? _disposable;

    public Fdc3DesktopAgentServiceTestsBase(string appDirectorySource)
    {
        AppDirectory = new AppDirectory.AppDirectory(
            new AppDirectoryOptions
            {
                Source = new Uri(appDirectorySource)
            });

        var options = new Fdc3DesktopAgentOptions();

        var loggerFilterOptions = new LoggerFilterOptions();

        loggerFilterOptions.AddFilter("", LogLevel.Warning);

        Logger
            .Setup(x => x.IsEnabled(It.IsAny<LogLevel>()))
            .Returns<LogLevel>(level => loggerFilterOptions.MinLevel <= level);

        LoggerFactory
            .Setup(_ => _.CreateLogger(It.IsAny<string>()))
            .Returns(Logger.Object);

        Fdc3 = new Fdc3DesktopAgentService(
            AppDirectory,
            ModuleLoader.Object,
            options,
            ResolverUICommunicator.Object,
            new UserChannelSetReader(options),
            ChannelSelector.Object,
            LoggerFactory.Object);
    }

    public async Task InitializeAsync()
    {
        await Fdc3.StartAsync(CancellationToken.None);

        _disposable = ModuleLoader.Object.LifetimeEvents.Subscribe(x =>
        {
            switch (x.EventType)
            {
                case LifetimeEventType.Started:
                    _modules.TryAdd(x.Instance.InstanceId, x.Instance);
                    break;

                case LifetimeEventType.Stopped:
                    _modules.TryRemove(x.Instance.InstanceId, out _);
                    break;
            }
        });
    }

    public async Task DisposeAsync()
    {
        await Fdc3.StopAsync(CancellationToken.None);

        foreach (var module in _modules)
        {
            await ModuleLoader.Object.StopModule(new(module.Key));
        }

        _disposable?.Dispose();
    }
}
