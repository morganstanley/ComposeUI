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

using Finos.Fdc3.AppDirectory;

namespace MorganStanley.ComposeUI.Fdc3.AppDirectory;

public partial class Fdc3ModuleCatalogTests
{
    [Fact]
    public async Task GetModuleIds_returns_available_appIds()
    {
        var moduleIds = await _catalog.GetModuleIds();

        moduleIds.Should().HaveCount(2).And.Contain(new[] { "app1", "app2" });
    }

    [Fact]
    public async Task GetModuleIds_returns_empty_collection_on_empty_directory()
    {
        var directory = new Mock<IAppDirectory>();
        directory.Setup(x => x.GetApps()).Returns(Task.FromResult(Enumerable.Empty<Fdc3App>()));

        var catalog = new Fdc3ModuleCatalog(directory.Object);

        var moduleIds = await catalog.GetModuleIds();
        moduleIds.Should().NotBeNull().And.BeEmpty();
    }
}
