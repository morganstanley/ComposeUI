using Shell.Utilities;

namespace ShellTests
{
    public class CommandLineParserCustomPropertyStringTests
    {
        private const string Default = "default";
        public class CustomPropertyStringOptions
        {
            private string? _option = null;
            public string? Option
            {
                get { return _option ?? Default; }
                set { _option = value; }
            }
        }

        [Fact]
        public void TestParseSimpleStringWithEmptyParameterList()
        {
            var options = CommandLineParser.Parse<CustomPropertyStringOptions>(new string[0]);
            Assert.NotNull(options);
            Assert.Equal(Default, options.Option);
        }

        [Fact]
        public void TestParseSimpleStringWithValueProvided()
        {
            var testValue = Guid.NewGuid();
            var options = CommandLineParser.Parse<CustomPropertyStringOptions>(new[] { "--option", testValue.ToString() });
            Assert.NotNull(options);
            Assert.Equal(testValue.ToString(), options.Option);
        }

        [Fact]
        public void TestParseSimpleStringWithoutValue()
        {
            var testValue = Guid.NewGuid();
            Assert.Throws<InvalidOperationException>(() => CommandLineParser.Parse<CustomPropertyStringOptions>(new[] { "--option" }));
        }

        [Fact]
        public void TestParseSimpleStringWithOnlyDifferentParameter()
        {
            var options = CommandLineParser.Parse<CustomPropertyStringOptions>(new[] { "--stuff", "irrelevant" });
            Assert.NotNull(options);
            Assert.Equal(Default, options.Option);
        }

        [Fact]
        public void TestParseSimpleStringWithOtherParameters()
        {
            var testValue = Guid.NewGuid();
            var options = CommandLineParser.Parse<CustomPropertyStringOptions>(new[] { "--firstParam", "irrelevant", "--option", testValue.ToString(), "--lastParam", "irrelevant" });
            Assert.NotNull(options);
            Assert.Equal(testValue.ToString(), options.Option);
        }
    }
}