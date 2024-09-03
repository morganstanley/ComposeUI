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

using MorganStanley.ComposeUI.LayoutPersistence.Abstractions;
using System.Text;
using System.Xml.Serialization;

namespace MorganStanley.ComposeUI.LayoutPersistence.Serializers;

public class XmlLayoutSerializer<T> : ILayoutSerializer<T>
{
    public async Task<string> SerializeAsync(T layoutObject, CancellationToken cancellationToken = default)
    {
        if (layoutObject == null)
        {
            throw new ArgumentNullException(nameof(layoutObject), "The layout object to serialize cannot be null.");
        }

        using var stream = new MemoryStream();
        var serializer = new XmlSerializer(typeof(T));

        using (var writer = new StreamWriter(stream, Encoding.UTF8, leaveOpen: true))
        {
            serializer.Serialize(writer, layoutObject);
            await writer.FlushAsync();  
        }

        cancellationToken.ThrowIfCancellationRequested();

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    public async Task<T?> DeserializeAsync(string layoutData, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(layoutData))
        {
            throw new ArgumentException("The layout data cannot be null or empty.", nameof(layoutData));
        }

        var dataBytes = Encoding.UTF8.GetBytes(layoutData);

        using var stream = new MemoryStream(dataBytes);

        cancellationToken.ThrowIfCancellationRequested();

        var serializer = new XmlSerializer(typeof(T));

        return await Task.FromResult((T?)serializer.Deserialize(stream));
    }
}
