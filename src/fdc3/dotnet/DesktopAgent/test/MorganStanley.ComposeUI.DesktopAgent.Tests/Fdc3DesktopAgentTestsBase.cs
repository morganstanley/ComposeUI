using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Finos.Fdc3.AppDirectory;
using Microsoft.Extensions.Logging.Abstractions;
using MorganStanley.ComposeUI.Fdc3.AppDirectory;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.DependencyInjection;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Infrastructure.Internal;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Tests.TestUtils;
using MorganStanley.ComposeUI.ModuleLoader;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Tests
{
    public abstract class Fdc3DesktopAgentTestsBase : IAsyncLifetime
    {
        protected IAppDirectory AppDirectory { get; }

        internal IFdc3DesktopAgentBridge Fdc3 { get; }
        protected MockModuleLoader ModuleLoader { get; } = new();
        protected Mock<IResolverUICommunicator> ResolverUICommunicator { get; } = new();
        private readonly ConcurrentDictionary<Guid, IModuleInstance> _modules = new();
        private IDisposable? _disposable;

        public Fdc3DesktopAgentTestsBase(string appDirectorySource)
        {
            AppDirectory = new AppDirectory.AppDirectory(
        new AppDirectoryOptions
        {
            Source = new Uri(appDirectorySource)
        });

            var options = new Fdc3DesktopAgentOptions();

            Fdc3 = new Fdc3DesktopAgent(
                AppDirectory,
                ModuleLoader.Object,
                options,
                ResolverUICommunicator.Object,
                new UserChannelSetReader(options),
                NullLoggerFactory.Instance);
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
}
