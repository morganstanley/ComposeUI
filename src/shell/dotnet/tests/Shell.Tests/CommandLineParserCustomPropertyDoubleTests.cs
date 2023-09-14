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
    public class CommandLineParserCustomPropertyDoubleTests
    {
        private static readonly Random Random = new Random();
        private static readonly double Default = Random.NextDouble();

        /// <summary>
        /// Get a test value that's different from the default value
        /// </summary>
        /// <returns></returns>
        private static double GetTestValue()
        {
            var x = Random.NextDouble();
            while (x == Default) { x = Random.NextDouble(); }
            return x;
        }

        public class CustomPropertyDoubleOptions
        {
            private double _option = Default;
            public double? Option
            {
                get { return _option; }
                set { _option = value ?? Default; }
            }
        }

        [Fact]
        public void TestParseCustomPropertyDoubleWithEmptyParameterList()
        {
            var options = CommandLineParser.Parse<CustomPropertyDoubleOptions>(new string[0]);
            Assert.NotNull(options);
            Assert.Equal(Default, options.Option);
        }

        [Fact]
        public void TestParseCustomPropertyDoubleWithValueProvided()
        {
            var testValue = GetTestValue();
            var options = CommandLineParser.Parse<CustomPropertyDoubleOptions>(new[] { "--option", testValue.ToString() });
            Assert.NotNull(options);
            Assert.Equal(testValue, options.Option);
        }

        [Fact]
        public void TestParseCustomPropertyDoubleWithoutValue()
        {
            Assert.Throws<InvalidOperationException>(() => CommandLineParser.Parse<CustomPropertyDoubleOptions>(new[] { "--option" }));
        }

        [Fact]
        public void TestParseCustomPropertyDoubleWithOnlyDifferentParameter()
        {
            var options = CommandLineParser.Parse<CustomPropertyDoubleOptions>(new[] { "--stuff", GetTestValue().ToString() });
            Assert.NotNull(options);
            Assert.Equal(Default, options.Option);
        }

        [Fact]
        public void TestParseCustomPropertyDoubleWithOtherParameters()
        {
            var testValue = GetTestValue();
            var options = CommandLineParser.Parse<CustomPropertyDoubleOptions>(new[] { "--firstParam", GetTestValue().ToString(), "--option", testValue.ToString(), "--lastParam", GetTestValue().ToString() });
            Assert.NotNull(options);
            Assert.Equal(testValue, options.Option);
        }
    }
}