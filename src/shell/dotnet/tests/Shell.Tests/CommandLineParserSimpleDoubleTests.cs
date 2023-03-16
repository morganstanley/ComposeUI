using Shell.Utilities;
using System.Globalization;

namespace ShellTests
{
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
}