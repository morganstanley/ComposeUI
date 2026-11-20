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

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Infrastructure.Internal;

/// <summary>
/// Checks the values that end up in the startup script of a module.
/// </summary>
internal static class Fdc3StartupPropertiesValidator
{
    internal const int MaxValueLength = 512;

    /// <exception cref="ArgumentException">A value is empty, too long or contains control characters.</exception>
    public static void Validate(Fdc3StartupProperties properties)
    {
        ValidateValue(properties.AppId, nameof(properties.AppId));
        ValidateOptionalValue(properties.ChannelId, nameof(properties.ChannelId));
        ValidateOptionalValue(properties.OpenedAppContextId, nameof(properties.OpenedAppContextId));
    }

    private static void ValidateOptionalValue(string? value, string name)
    {
        if (value != null)
        {
            ValidateValue(value, name);
        }
    }

    // The rejected value is left out of the message, as it comes from outside and may be hostile.
    private static void ValidateValue(string value, string name)
    {
        if (string.IsNullOrEmpty(value) || value.Length > MaxValueLength || value.Any(char.IsControl))
        {
            throw new ArgumentException(
                $"{name} must be 1-{MaxValueLength} characters long and must not contain control characters.",
                name);
        }
    }
}
