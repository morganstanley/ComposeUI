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
using System;
using System.Windows;

namespace DiagnosticsExample;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private IServiceProvider? _serviceProvider;
    internal IServiceProvider ServiceProvider => _serviceProvider ?? throw new ApplicationException("ServiceProvider not yet initialized");


    private void Application_Startup(object sender, StartupEventArgs e)
    {
        IServiceCollection serviceCollection = new ServiceCollection();

        try
        {
            serviceCollection
                .AddMessageRouter(m =>
                {
                    m.UseWebSocketFromEnvironment();
                    m.UseAccessTokenFromEnvironment();
                });

            serviceCollection.AddMessageRouterMessagingAdapter();
        }
        catch
        {
            // MessageRouter couldn't be initialized, text will be displayed
        }


        _serviceProvider = serviceCollection.BuildServiceProvider();
    }
}
