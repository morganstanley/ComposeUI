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

using MorganStanley.Fdc3;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Contracts;

/// <summary>
/// Response, for handling <see cref="RaiseIntentRequest"/> originated via fdc.raiseIntent by clients.
/// </summary>
public class RaiseIntentResponse
{
    /// <summary>
    /// Intent, for which the raiseIntent was executed.
    /// </summary>
    public string Intent { get; set; }

    /// <summary>
    /// Apps, that could handle the raiseIntent.
    /// </summary>
    public IEnumerable<AppMetadata>? AppMetadatas { get; set; }

    /// <summary>
    /// Error, which indicates that some error has happened during the raiseIntent's execution.
    /// </summary>
    public string? Error { get; set; }

    public static RaiseIntentResponse Success(string intent, IEnumerable<IAppMetadata>? appMetadatas = null)
    {
        var raiseIntentResponse = new RaiseIntentResponse()
        {
            Intent = intent
        };

        if (appMetadatas == null) return raiseIntentResponse;
        
        //Could not handle the IAppMetadata interface when deserializing/serializing, that's why we would need a casting. 
        var castedAppMetadata = appMetadatas.Select(
                appMetadata => new AppMetadata(
                    appId: appMetadata.AppId,
                    instanceId: appMetadata.InstanceId,
                    name: appMetadata.Name,
                    version: appMetadata.Version,
                    title: appMetadata.Title,
                    tooltip: appMetadata.Tooltip,
                    description: appMetadata.Description,
                    icons: appMetadata.Icons,
                    images: appMetadata.Screenshots,
                    resultType: appMetadata.ResultType))
            .ToList();

        raiseIntentResponse.AppMetadatas = castedAppMetadata;

        return raiseIntentResponse;
    }

    public static RaiseIntentResponse Failure(string error) => new() { Error = error };
}
