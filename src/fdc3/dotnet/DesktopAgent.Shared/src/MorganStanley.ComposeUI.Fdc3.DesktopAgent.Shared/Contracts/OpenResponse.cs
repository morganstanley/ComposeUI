﻿/*
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

using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Protocol;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Contracts;

/// <summary>
/// Response for the `fdc3.open()` call.
/// </summary>
internal sealed class OpenResponse
{
    /// <summary>
    /// Result app details when the open request was successful, returning the app id and FDC3 instance id.
    /// </summary>
    public AppIdentifier? AppIdentifier { get; set; }

    /// <summary>
    /// Error message indicating that the `fdc3.open()` wasn't executed successfully.
    /// </summary>
    public string? Error { get; set; }

    public static OpenResponse Success(AppIdentifier appIdentifier) => new() { AppIdentifier = appIdentifier };
    public static OpenResponse Failure(string error) => new() { Error = error };
}