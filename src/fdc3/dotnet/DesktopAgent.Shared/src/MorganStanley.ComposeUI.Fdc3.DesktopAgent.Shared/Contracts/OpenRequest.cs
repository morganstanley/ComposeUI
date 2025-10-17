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

using System.Text.Json.Serialization;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Converters;
using AppIdentifier = MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Protocol.AppIdentifier;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Contracts;

/// <summary>
/// Request opening an app.
/// </summary>
internal sealed class OpenRequest
{
    /// <summary>
    /// FDC3 instance id of the app that requested to open a target app.
    /// </summary>
    public string InstanceId { get; set; }

    /// <summary>
    /// The details of the app that should be opened.
    /// </summary>
    public AppIdentifier AppIdentifier { get; set; }

    /// <summary>
    /// Context meant to be passed to the opened application.
    /// </summary>
    [JsonConverter(typeof(ContextJsonConverter))]
    public string? Context { get; set; }

    /// <summary>
    /// Channel id, where the target app should connect and register its context listener if the context argument was passed to the `fdc3.open` function.
    /// </summary>
    public string? ChannelId { get; set; }
}