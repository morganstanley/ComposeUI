// ********************************************************************************************************
//
// Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License").
// You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0.
// See the NOTICE file distributed with this work for additional information regarding copyright ownership.
// Unless required by applicable law or agreed to in writing, software distributed under the License
// is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and limitations under the License.
// 
// ********************************************************************************************************


using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using ModulesPrototype.Infrastructure;
using ModulesPrototype.Infrastructure.Messages;
using MorganStanley.ComposeUI.Messaging;
using MorganStanley.ComposeUI.Tryouts.Core.Abstractions.Modules;

namespace ModulesPrototype;

internal class SubsystemLauncherBuilder
{
    private IServiceCollection _serviceCollection;
    private ILoggerFactory? _loggerFactory;

    public SubsystemLauncherBuilder(IServiceCollection serviceCollection)
    {
        this._serviceCollection = serviceCollection;
    }

    public SubsystemLauncherBuilder Configure(ILoggerFactory? loggerFactory,
        IMessageRouter messageRouter,
        IModuleLoader moduleLoader)
    {
        if (loggerFactory != null)
        {
            _serviceCollection.RemoveAll<ILoggerFactory>();
            _serviceCollection.AddSingleton<ILoggerFactory>(loggerFactory);
            _loggerFactory = loggerFactory;

            //_serviceCollection.RemoveAll<ILogger<SubsystemLauncher>>();
            //_serviceCollection.AddSingleton<ILogger<SubsystemLauncher>>(_loggerFactory.CreateLogger<SubsystemLauncher>());

            _serviceCollection.RemoveAll<ILogger<SubsystemHandlerRouterMessage>>();
            _serviceCollection.AddSingleton<ILogger<SubsystemHandlerRouterMessage>>(_loggerFactory.CreateLogger<SubsystemHandlerRouterMessage>());

            _serviceCollection.RemoveAll<ILogger<SubsystemControllerCommunicator>>();
            _serviceCollection.AddSingleton<ILogger<SubsystemControllerCommunicator>>(_loggerFactory.CreateLogger<SubsystemControllerCommunicator>());

        }

        _serviceCollection.RemoveAll<IMessageRouter>();
        _serviceCollection.AddSingleton<IMessageRouter>(messageRouter);

        _serviceCollection.RemoveAll<IModuleLoader>();
        _serviceCollection.AddSingleton<IModuleLoader>(moduleLoader);
      
        return this;
    }

    internal ServiceLifetime ServiceLifetime { get; }
}
