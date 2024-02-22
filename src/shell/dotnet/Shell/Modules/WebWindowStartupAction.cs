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
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MorganStanley.ComposeUI.ModuleLoader;

namespace MorganStanley.ComposeUI.Shell.Modules;

internal class WebWindowStartupAction : IStartupAction
{
    private readonly App _app;
    private readonly ILogger<WebWindowStartupAction> _logger;

    public WebWindowStartupAction(App app, ILogger<WebWindowStartupAction> logger)
    {
        _app = app;
        _logger = logger;
    }

    public async Task InvokeAsync(StartupContext startupContext, Func<Task> next)
    {
        if (startupContext.ModuleInstance.Manifest.ModuleType != ModuleType.Web)
        {
            await next();

            return;
        }

        var webWindowOptionsParameter = startupContext.StartRequest.Parameters
            .FirstOrDefault(p => p.Key == WebWindowOptions.ParameterName)
            .Value;

        if (webWindowOptionsParameter != null)
        {
            var webWindowOptions = JsonSerializer.Deserialize<WebWindowOptions>(webWindowOptionsParameter);
            startupContext.AddProperty(webWindowOptions);
        }

        await next();

        try
        {
            var webWindow = await _app.Dispatcher.InvokeAsync(
                () =>
                {
                    var webProperties = startupContext.GetProperties<WebStartupProperties>().First();
                    var webWindowOptions = startupContext.GetProperties<WebWindowOptions>().FirstOrDefault();

                    var window = _app.CreateWindow<WebWindow>(
                        startupContext.ModuleInstance,
                        webWindowOptions
                        ?? new WebWindowOptions
                        {
                            Url = webProperties.Url.ToString(),
                            IconUrl = webProperties.IconUrl?.ToString()
                        });

                    return window;
                });

            startupContext.AddProperty(webWindow);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Exception thrown when trying to create a web window: {ExceptionType}: {ExceptionMessage}",
                ex.GetType().FullName,
                ex.Message);
        }
    }
}