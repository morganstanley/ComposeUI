using Shell.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShellTests
{
    public class CommandLineParserAttributedPropertyTests
    {
        public class AttributedOptions
        {
            [Display(Name = "opt", Description = "Option with overwritten name")]
            public string RenamedOption { get; set; }

            [Display(Description = "Just a description")]
            public string Option { get; set; }
        }

        [Fact]
        public void TestParsingRenamedOption()
        {
            var testValue = Guid.NewGuid().ToString();
            var options = CommandLineParser.Parse<AttributedOptions>(new[] { "--opt", testValue });
            Assert.NotNull(options);
            Assert.Equal(testValue.ToString(), options.RenamedOption);
        }

        [Fact]
        public void TestParsingRenamedOptionWithOriginalName()
        {
            var testValue = Guid.NewGuid().ToString();
            var options = CommandLineParser.Parse<AttributedOptions>(new[] { "--renamedOption", testValue });
            Assert.NotNull(options);
            Assert.Null(options.RenamedOption);
        }

        [Fact]
        public void TestParsingOptionWithDescription()
        {
            var testValue = Guid.NewGuid().ToString();
            var options = CommandLineParser.Parse<AttributedOptions>(new[] { "--option", testValue });
            Assert.NotNull(options);
            Assert.Equal(testValue.ToString(), options.Option);
        }
    }
}
