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

using System.Text;
using System.Text.Json;
using MorganStanley.ComposeUI.LayoutPersistence.Abstractions;

namespace MorganStanley.ComposeUI.LayoutPersistence.Serializers;

public class JsonLayoutSerializer<T> : ILayoutSerializer<T>
{
    private readonly JsonSerializerOptions? _jsonSerializerOptions;

    public JsonLayoutSerializer(JsonSerializerOptions? jsonSerializerOptions = null)
    {
        _jsonSerializerOptions = jsonSerializerOptions;
    }

    public async Task<string> SerializeAsync(T layoutObject, CancellationToken cancellationToken = default)
    {
        if (layoutObject == null)
        {
            throw new ArgumentNullException(nameof(layoutObject), "The layout object to serialize cannot be null.");
        }

        using var stream = new MemoryStream();
        await JsonSerializer.SerializeAsync(stream, layoutObject, _jsonSerializerOptions, cancellationToken);

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    public async Task<T?> DeserializeAsync(string layoutData, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(layoutData))
        {
            throw new ArgumentException("The layout data cannot be null or empty.", nameof(layoutData));
        }

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(layoutData));
        return await JsonSerializer.DeserializeAsync<T>(stream, _jsonSerializerOptions, cancellationToken);
    }
}
