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

namespace MorganStanley.ComposeUI.ModuleLoader.Runners;

internal class WebModuleRunner : IModuleRunner
{
    public string ModuleType => ComposeUI.ModuleLoader.ModuleType.Web;

    public async Task Start(IModuleInstance moduleInstance, StartupContext startupContext, Func<Task> pipeline)
    {
        if (moduleInstance.Manifest.TryGetDetails(out WebManifestDetails details))
        {
            startupContext.AddProperty(new WebStartupProperties { IconUrl = details.IconUrl, Url = details.Url });
        }

        await pipeline();
    }

    public Task Stop(IModuleInstance moduleInstance, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
