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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MorganStanley.ComposeUI.ModuleLoader;

namespace MorganStanley.ComposeUI.Shell.Preloading;

internal sealed class DefaultPreloadScriptPolicy : IPreloadScriptPolicy
{
    private readonly IEnumerable<Uri> _allowedOrigins = Enumerable.Empty<Uri>();

    public DefaultPreloadScriptPolicy(IModuleInstance? moduleInstance)
    {
        if (moduleInstance == null)
            return;

        var webProperties = moduleInstance.GetProperties().OfType<WebStartupProperties>().FirstOrDefault();
        if (webProperties != null)
        {
            _allowedOrigins = webProperties.AllowedOrigins ?? Enumerable.Empty<Uri>();
        }
    }

    public Task<bool> IsPreloadingScriptsAllowedAsync(Uri uri, Uri appUri)
    {
        if (uri.IsBaseOf(appUri))
        {
            return Task.FromResult(true);
        }

        if (appUri.GetLeftPart(UriPartial.Authority) == uri.GetLeftPart(UriPartial.Authority))
        {
            return Task.FromResult(true);
        }

        if (_allowedOrigins.Contains(appUri))
        {
            return Task.FromResult(true);
        }

        return Task.FromResult(false);
    }
}
