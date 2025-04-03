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

using MorganStanley.ComposeUI.Shell.Interaction.Abstraction.Contracts;

namespace MorganStanley.ComposeUI.Shell.Interaction.Abstraction.Interfaces;

/// <summary>
/// Defines the interface for showing notifications.
/// </summary>
public interface INotification
{
    /// <summary>
    /// Shows a notification with the specified window ID, title, and options.
    /// </summary>
    /// <param name="windowId">The ID of the window where the notification will be shown.</param>
    /// <param name="title">The title of the notification.</param>
    /// <param name="options">The options for the notification, such as URL and body content.</param>
    void ShowNotification(string windowId, string title, NotificationOptions options);
}