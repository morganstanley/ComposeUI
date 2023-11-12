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

using MorganStanley.ComposeUI.Utilities;
using System.Globalization;

namespace MorganStanley.ComposeUI.Utilities.Tests;

public class CommandLineParserSimpleDoubleTests
{
    private static readonly Random Random = new Random();

    public class SimpleDoubleOptions
    {
        public double? Option { get; set; }
    }

    [Fact]
    public void TestParseSimpleDoubleWithEmptyParameterList()
    {
        var options = CommandLineParser.Parse<SimpleDoubleOptions>(new string[0]);
        Assert.NotNull(options);
        Assert.Null(options.Option);
    }

    [Fact]
    public void TestParseSimpleDoubleWithValueProvided()
    {
        var testValue = Random.NextDouble();
        var options = CommandLineParser.Parse<SimpleDoubleOptions>(new[] { "--option", testValue.ToString() });
        Assert.NotNull(options);
        Assert.Equal(testValue, options.Option);
    }

    [Fact]
    public void TestParseSimpleDoubleWithoutValue()
    {
        Assert.Throws<InvalidOperationException>(() => CommandLineParser.Parse<SimpleDoubleOptions>(new[] { "--option" }));
    }

    [Fact]
    public void TestParseSimpleDoubleWithOnlyDifferentParameter()
    {
        var options = CommandLineParser.Parse<SimpleDoubleOptions>(new[] { "--stuff", Random.NextDouble().ToString() });
        Assert.NotNull(options);
        Assert.Null(options.Option);
    }

    [Fact]
    public void TestParseSimpleDoubleWithOtherParameters()
    {
        var testValue = Random.NextDouble();
        var options = CommandLineParser.Parse<SimpleDoubleOptions>(new[] { "--firstParam", Random.NextDouble().ToString(), "--option", testValue.ToString(), "--lastParam", Random.NextDouble().ToString() });
        Assert.NotNull(options);
        Assert.Equal(testValue, options.Option);
    }
}