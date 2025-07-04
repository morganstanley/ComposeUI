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

using MorganStanley.ComposeUI.MessagingAdapter;
using MorganStanley.ComposeUI.MessagingAdapter.Abstractions;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionMessagingadapterExtensions
{
    /// <summary>
    /// Adds the ComposeUI messaging adapter to the service collection.
    /// </summary>
    /// <param name="services">The service collection to add the messaging adapter to.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddComposeUIMessagingAdapter(this IServiceCollection services)
    {
        services.AddSingleton<IComposeUIMessaging, ComposeUIMessaging>();
        return services;
    }
}