using Shell.Utilities;

namespace ShellTests
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