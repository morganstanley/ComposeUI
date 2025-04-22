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
/// Provides constants for interaction topics used in the ComposeUI system.
/// </summary>
public static class InteractionTopic
{
    /// <summary>
    /// The root topic for all interaction-related messages in ComposeUI.
    /// </summary>
    public static readonly string TopicRoot = "ComposeUI/interaction/v1.0/";

    /// <summary>
    /// The topic used for sending result of notifications in the ComposeUI interaction system.
    /// </summary>
    public static readonly string NotificationResult = TopicRoot + "NotificationResult";

    /// <summary>
    /// The topic used for signalling of accessing shortcuts in the ComposeUI interaction system.
    /// </summary>
    public static readonly string ShortcutUsed = TopicRoot + "ShortcutUsed";
}