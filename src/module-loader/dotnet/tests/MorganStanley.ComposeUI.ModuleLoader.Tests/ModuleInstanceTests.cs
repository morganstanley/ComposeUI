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


using Moq;

namespace MorganStanley.ComposeUI.ModuleLoader.Tests;

public class ModuleInstanceTests
{
    private readonly Guid _testGuid = Guid.NewGuid();

    [Fact]
    public void GivenNullArguments_WhenCtor_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new ModuleInstance(_testGuid, null!, new StartRequest("test")));
        Assert.Throws<ArgumentNullException>(() => new ModuleInstance(_testGuid, new Mock<IModuleManifest>().Object, null!));
    }

    [Fact]
    public void WhenNewModuleInstance_GetProperties_ReturnsEmptyCollection()
    {
        var moduleInstance = new ModuleInstance(_testGuid, new Mock<IModuleManifest>().Object, new StartRequest("test"));

        var properties = moduleInstance.GetProperties();
        Assert.Empty(properties);
    }

    [Fact]
    public void WhenSetProperties_GetPropertiesReturnThePropertiesAdded()
    {
        var moduleInstance = new ModuleInstance(_testGuid, new Mock<IModuleManifest>().Object, new StartRequest("test"));
        string[] testProperties = new[] { "test", "test2" };

        moduleInstance.AddProperties(testProperties);
        var properties = moduleInstance.GetProperties();

        Assert.Equal(testProperties, properties);
    }
}
