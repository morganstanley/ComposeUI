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

using MorganStanley.ComposeUI.Shell.Utilities;

namespace MorganStanley.ComposeUI.Shell.Tests
{
    public class CommandLineParserSimpleStringTests
    {
        public class SimpleStringOptions
        {
            public string? Option { get; set; }
        }

        [Fact]
        public void TestParseSimpleStringWithEmptyParameterList()
        {
            var options = CommandLineParser.Parse<SimpleStringOptions>(new string[0]);
            Assert.NotNull(options);
            Assert.Null(options.Option);
        }

        [Fact]
        public void TestParseSimpleStringWithValueProvided()
        {
            var testValue = Guid.NewGuid();
            var options = CommandLineParser.Parse<SimpleStringOptions>(new[] { "--option", testValue.ToString() });
            Assert.NotNull(options);
            Assert.Equal(testValue.ToString(), options.Option);
        }

        [Fact]
        public void TestParseSimpleStringWithoutValue()
        {
            var testValue = Guid.NewGuid();
            Assert.Throws<InvalidOperationException>(() => CommandLineParser.Parse<SimpleStringOptions>(new[] { "--option" }));
        }

        [Fact]
        public void TestParseSimpleStringWithOnlyDifferentParameter()
        {
            var options = CommandLineParser.Parse<SimpleStringOptions>(new[] { "--stuff", "irrelevant" });
            Assert.NotNull(options);
            Assert.Null(options.Option);
        }

        [Fact]
        public void TestParseSimpleStringWithOtherParameters()
        {
            var testValue = Guid.NewGuid();
            var options = CommandLineParser.Parse<SimpleStringOptions>(new[] { "--firstParam", "irrelevant", "--option", testValue.ToString(), "--lastParam", "irrelevant" });
            Assert.NotNull(options);
            Assert.Equal(testValue.ToString(), options.Option);
        }
    }
}