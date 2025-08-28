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

using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Exceptions;
using MorganStanley.ComposeUI.ModuleLoader;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Tests.Helpers;

internal static class Fdc3InstanceIdRetriever
{
    public static string GetFdc3InstanceId(this IModuleInstance instance)
    {
        var fdc3InstanceId = instance.StartRequest.Parameters.FirstOrDefault(parameter => parameter.Key == Fdc3StartupParameters.Fdc3InstanceId);
        if (!string.IsNullOrEmpty(fdc3InstanceId.Value))
        {
            return fdc3InstanceId.Value;
        }

        if (instance.GetProperties().FirstOrDefault(property => property is Fdc3StartupProperties) is not Fdc3StartupProperties fdc3StartupProperties)
        {
            throw ThrowHelper.MissingFdc3InstanceId(instance.Manifest.Id);
        }
        else
        {
            return fdc3StartupProperties.InstanceId;
        }
    }
}
