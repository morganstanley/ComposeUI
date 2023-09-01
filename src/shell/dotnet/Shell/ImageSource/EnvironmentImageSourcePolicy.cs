// /*
//  * Morgan Stanley makes this available to you under the Apache License,
//  * Version 2.0 (the "License"). You may obtain a copy of the License at
//  *
//  *      http://www.apache.org/licenses/LICENSE-2.0.
//  *
//  * See the NOTICE file distributed with this work for additional information
//  * regarding copyright ownership. Unless required by applicable law or agreed
//  * to in writing, software distributed under the License is distributed on an
//  * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
//  * or implied. See the License for the specific language governing permissions
//  * and limitations under the License.
//  */

using System;
using System.Linq;

namespace MorganStanley.ComposeUI.Shell.ImageSource;

public sealed class EnvironmentImageSourcePolicy : IImageSourcePolicy
{
    private const string AllowListEnvVar = "COMPOSE_ALLOWED_IMAGE_SOURCES";
    public bool IsAllowed(Uri uri, Uri appUri)
    {
        var allowListString = Environment.GetEnvironmentVariable(AllowListEnvVar);

        // Only allow http or https sources. If no sources are allowed, 
        if (!uri.Scheme.StartsWith("http"))
        {
            return false;
        }
        // If the source host is the same as the app host, allow it.            
        if (uri.Host == appUri.Host)
        {
            return true;
        }
        if (string.IsNullOrEmpty(allowListString))
        {
            return false;
        }

        var allowedSources = allowListString.Split(';');
        return allowedSources.Contains(uri.Host);
    }
}
