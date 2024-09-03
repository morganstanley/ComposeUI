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

using FluentAssertions;
using MorganStanley.ComposeUI.LayoutPersistence.Serializers;

namespace MorganStanley.ComposeUI.LayoutPersistence.Tests;

public class JsonLayoutSerializerTests
{
    private readonly JsonLayoutSerializer<LayoutData> _serializer = new();

    [Fact]
    public async Task Serialize_Then_Deserialize_ShouldReturnTheOriginalObject()
    {
        var layoutObject = new LayoutData
        {
            Windows = new List<Window>
        {
            new() { Id = "1", X = 10, Y = 20, Width = 300, Height = 200 },
            new() { Id = "2", X = 30, Y = 40, Width = 500, Height = 400 }
        }
        };

        var json = await _serializer.SerializeAsync(layoutObject);
        
        var loadedData = await _serializer.DeserializeAsync(json);

        loadedData.Should().Be(layoutObject);
    }
}