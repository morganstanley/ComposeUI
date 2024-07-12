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
using Moq;

namespace MorganStanley.ComposeUI.ModuleLoader.Abstractions.Tests;

public class StartupContextTests
{
    [Fact]
    public void WhenAdd_AddedValuesCanBeRetrieved()
    {
        var expected = new[]
        {
            new MyContextInfo { Name = "Test1" },
            new MyContextInfo { Name = "Test2" }
        };

        var context = new StartupContext(new StartRequest("test"), Mock.Of<IModuleInstance>());
        context.AddProperty(expected[0]);
        context.AddProperty(expected[1]);

        var result = context.GetProperties();
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void GivenNullArgument_WhenAdd_ThrowsArgumentNullException()
    {
        var context = new StartupContext(new StartRequest("test"), Mock.Of<IModuleInstance>());
        var action = () => context.AddProperty<object>(null!);
        action.Should().Throw<ArgumentNullException>();
    }

    private class MyContextInfo
    {
        public string? Name { get; set; }
    }
}