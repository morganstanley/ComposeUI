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

using Finos.Fdc3.AppDirectory;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Infrastructure.Internal;

internal static class IntentExtensions
{
    public static IEnumerable<FlatAppIntent> AsFlatAppIntents(this Fdc3App app, Guid? instanceId = null)
    {
        foreach (var intent in app.Interop?.Intents?.ListensFor ?? [])
        {
            // We need a new IntentMetadata where the Name is the Key as it is not guaranteed to have a Name property (e.g. in the Conformance Framework)
            var newIntent = new IntentMetadata(intent.Key, intent.Value.DisplayName, intent.Value.Contexts)
            {
                CustomConfig = intent.Value.CustomConfig,
                ResultType = intent.Value.ResultType
            };

            var appIntent= new FlatAppIntent()
            {
                InstanceId = instanceId,
                Intent = newIntent,
                App = app
            };
            yield return appIntent;
        }
    }

    public static IEnumerable<FlatAppIntent> AsFlatAppIntents(this IEnumerable<Fdc3App> apps)
    {
        foreach (var app in apps)
        {
            foreach (var intent in app.Interop?.Intents?.ListensFor ?? [])
            {
                // We need a new IntentMetadata where the Name is the Key as it is not guaranteed to have a Name property (e.g. in the Conformance Framework)
                var newIntent = new IntentMetadata(intent.Key, intent.Value.DisplayName, intent.Value.Contexts)
                {
                    CustomConfig = intent.Value.CustomConfig,
                    ResultType = intent.Value.ResultType
                };
                var appIntent = new FlatAppIntent()
                {
                    Intent = newIntent,
                    App = app
                };
                yield return appIntent;
            }
        }
    }
}
