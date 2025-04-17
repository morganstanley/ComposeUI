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

namespace MorganStanley.ComposeUI.Shell.Interaction.Abstraction.Contracts;

/// <summary>
/// Represents the options for a notification, including the URL and body content.
/// </summary>
public class NotificationOptions
{
    /// <summary>
    /// Gets or sets the ID of the window associated with the notification.
    /// </summary>
    public string? WindowId { get; set; }

    /// <summary>
    /// Gets or sets the title of the notification.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Optionally, a notification can be configured to send a message through the message router upon
    /// pressing e.g. a button.
    /// The message router is configured with a topic name and a NotificationId as payload.
    /// </summary>
    public string? NotificationId { get; set; }

    /// <summary>
    /// Gets or sets the body content of the notification.
    /// The body can contain elements like buttons, links, etc. that are forwarded to the messagerouter if configured.
    /// </summary>
    public string? Body { get; set; }
}