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
using System.ComponentModel.DataAnnotations;

namespace MorganStanley.ComposeUI.Shell.Tests
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
