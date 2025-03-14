// Morgan Stanley makes this available to you under the Apache License,
// Version 2.0 (the "License"). You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0.
// 
// See the NOTICE file distributed with this work for additional information
// regarding copyright ownership. Unless required by applicable law or agreed
// to in writing, software distributed under the License is distributed on an
// "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
// or implied. See the License for the specific language governing permissions
// and limitations under the License.

using FluentAssertions;

namespace MorganStanley.ComposeUI.ModuleLoader.Tests;

public class AggregateModuleCatalogTests
{
    private readonly IModuleCatalog _moduleCatalog = new AggregateModuleCatalog(
        new[]
        {
            new MockModuleCatalog(new[]
            {
                new MockModuleManifest("testModuleId", "testModuleName"),
                new MockModuleManifest("testModuleId1", "testModuleName1"),
                new MockModuleManifest("testModuleId2", "testModuleName2"),
                new MockModuleManifest("testModuleId3", "testModuleName3"),
                new MockModuleManifest("testModuleId4", "testModuleName4")
            }),
            new MockModuleCatalog(new[]
            {
                new MockModuleManifest("testModuleId5", "testModuleName5"),
                new MockModuleManifest("testModuleId", "testModuleName8"),
                new MockModuleManifest("testModuleId6", "testModuleName6"),
            }),
            new MockModuleCatalog(new[]
            {
                new MockModuleManifest("testModuleId", "testModuleName9"),
                new MockModuleManifest("testModuleId5", "testModuleName5X"),
            }),
            new MockModuleCatalog(new[]
            {
                new MockModuleManifest("testModuleId7", "testModuleName7"),
            }),
            new MockModuleCatalog(Enumerable.Empty<MockModuleManifest>()),
        });

    [Theory]
    [ClassData(typeof(AggregateModuleCatalogReturnManifestTheoryData))]
    public async Task GetManifest_ReturnsModuleManifest_WhenModuleIdPassed(string moduleId, IModuleManifest expectedModuleManifest)
    {
        var result = await _moduleCatalog.GetManifest(moduleId);
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(expectedModuleManifest);
    }

    [Theory]
    [InlineData("noModuleId")]
    [InlineData("testModuleId8")]
    public async Task GetManifest_ThrowsModuleNotFoundException_WhenNoModuleIdFound(string moduleId)
    {
        var action = () => _moduleCatalog.GetManifest(moduleId);
        await action.Should().ThrowAsync<ModuleNotFoundException>();
    }

    [Theory]
    [InlineData("testModuleId")]
    public async Task GetManifest_ReturnsTheFirstOccurrence_WhenMultipleCatalogContainsTheSameModuleId(string moduleId)
    {
        var result = await _moduleCatalog.GetManifest(moduleId);
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(new MockModuleManifest("testModuleId", "testModuleName"));
    }

    [Fact]
    public async Task GetModuleIds_ReturnsTheUniqueModules()
    {
        var result = await _moduleCatalog.GetModuleIds();
        result.Should().NotBeNull();
        result.Should().HaveCount(8);
        result.Should().BeEquivalentTo(
            new[]
            {
                "testModuleId",
                "testModuleId1",
                "testModuleId2",
                "testModuleId3",
                "testModuleId4",
                "testModuleId5",
                "testModuleId6",
                "testModuleId7",
            });
    }

    // GetAllManifests returns unique modules
    [Fact]
    public async Task GetAllManifests_ReturnsTheUniqueModules()
    {
        var result = await _moduleCatalog.GetAllManifests();
        result.Should().NotBeNull();
        result.Should().HaveCount(8);
        result.Select(module => module.Id).Should().BeEquivalentTo(
            new[]
            {
                "testModuleId",
                "testModuleId1",
                "testModuleId2",
                "testModuleId3",
                "testModuleId4",
                "testModuleId5",
                "testModuleId6",
                "testModuleId7",
            });
    }

    private class MockModuleCatalog : IModuleCatalog
    {
        private readonly IEnumerable<IModuleManifest> _modules;

        public MockModuleCatalog(IEnumerable<IModuleManifest> modules)
        {
            _modules = modules;
        }
        
        public Task<IEnumerable<IModuleManifest>> GetAllManifests()
        {
            return Task.FromResult(_modules);
        }

        public Task<IModuleManifest> GetManifest(string moduleId)
        {
            var module = _modules.FirstOrDefault(module => module.Id == moduleId);
            if (module == null)
            {
                throw new NullReferenceException(moduleId);
            }

            return Task.FromResult(module);
        }

        public Task<IEnumerable<string>> GetModuleIds()
        {
            return Task.FromResult(_modules.Select(module => module.Id));
        }
    }

    private class MockModuleManifest : IModuleManifest
    {
        private string _id;
        private string _name;

        public MockModuleManifest(string id, string name)
        {
            _id = id;
            _name = name;
        }

        public string Id => _id;

        public string Name => _name;

        public string ModuleType => "dummy";

        public string[] Tags => ["tag1", "tag2"];

        public Dictionary<string, string> AdditionalProperties => new() { { "color", "blue" } };
    }

    private class AggregateModuleCatalogReturnManifestTheoryData : TheoryData
    {
        public AggregateModuleCatalogReturnManifestTheoryData()
        {
            AddRow("testModuleId1", new MockModuleManifest("testModuleId1", "testModuleName1"));
            AddRow("testModuleId2", new MockModuleManifest("testModuleId2", "testModuleName2"));
            AddRow("testModuleId5", new MockModuleManifest("testModuleId5", "testModuleName5"));
        }
    }

    [Fact]
    public void ModuleManifestIdComparer_Equals_ReturnsTrue_ForSameId()
    {
        var comparer = new AggregateModuleCatalog.ModuleManifestIdComparer();
        var manifest1 = new MockModuleManifest("testId", "testName1");
        var manifest2 = new MockModuleManifest("testId", "testName2");

        var result = comparer.Equals(manifest1, manifest2);

        result.Should().BeTrue();
    }

    [Fact]
    public void ModuleManifestIdComparer_Equals_ReturnsFalse_ForDifferentId()
    {
        var comparer = new AggregateModuleCatalog.ModuleManifestIdComparer();
        var manifest1 = new MockModuleManifest("testId1", "testName1");
        var manifest2 = new MockModuleManifest("testId2", "testName2");

        var result = comparer.Equals(manifest1, manifest2);

        result.Should().BeFalse();
    }

    [Fact]
    public void ModuleManifestIdComparer_GetHashCode_ReturnsSameHashCode_ForSameId()
    {
        var comparer = new AggregateModuleCatalog.ModuleManifestIdComparer();
        var manifest1 = new MockModuleManifest("testId", "testName1");
        var manifest2 = new MockModuleManifest("testId", "testName2");

        var hashCode1 = comparer.GetHashCode(manifest1);
        var hashCode2 = comparer.GetHashCode(manifest2);

        hashCode1.Should().Be(hashCode2);
    }

    [Fact]
    public void ModuleManifestIdComparer_GetHashCode_ReturnsDifferentHashCode_ForDifferentId()
    {
        var comparer = new AggregateModuleCatalog.ModuleManifestIdComparer();
        var manifest1 = new MockModuleManifest("testId1", "testName1");
        var manifest2 = new MockModuleManifest("testId2", "testName2");

        var hashCode1 = comparer.GetHashCode(manifest1);
        var hashCode2 = comparer.GetHashCode(manifest2);

        hashCode1.Should().NotBe(hashCode2);
    }

    [Fact]
    public void ModuleManifestIdComparer_Equals_ReturnsFalse_WhenFirstIsNull()
    {
        var comparer = new AggregateModuleCatalog.ModuleManifestIdComparer();
        var manifest1 = (IModuleManifest) null!;
        var manifest2 = new MockModuleManifest("testId", "testName");

        var result = comparer.Equals(manifest1, manifest2);

        result.Should().BeFalse();
    }

    [Fact]
    public void ModuleManifestIdComparer_Equals_ReturnsFalse_WhenSecondIsNull()
    {
        var comparer = new AggregateModuleCatalog.ModuleManifestIdComparer();
        var manifest1 = new MockModuleManifest("testId", "testName");
        var manifest2 = (IModuleManifest) null!;

        var result = comparer.Equals(manifest1, manifest2);

        result.Should().BeFalse();
    }

    [Fact]
    public void ModuleManifestIdComparer_Equals_ReturnsTrue_WhenBothAreNull()
    {
        var comparer = new AggregateModuleCatalog.ModuleManifestIdComparer();
        var manifest1 = (IModuleManifest) null!;
        var manifest2 = (IModuleManifest) null!;

        var result = comparer.Equals(manifest1, manifest2);

        result.Should().BeTrue();
    }

    [Fact]
    public void ModuleManifestIdComparer_GetHashCode_ThrowsException_WhenNull()
    {
        var comparer = new AggregateModuleCatalog.ModuleManifestIdComparer();
        var manifest = (IModuleManifest) null!;

        Action act = () => comparer.GetHashCode(manifest);

        act.Should().Throw<NullReferenceException>();
    }
}