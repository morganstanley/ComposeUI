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

using Finos.Fdc3.AppDirectory;
using Finos.Fdc3.Context;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Tests;

public class Fdc3AppExtensionsTests
{
    [Fact]
    public void CanRaiseIntent_returns_false_when_Fdc3App_is_null()
    {
        Fdc3App? app = null;
        var result = app.CanRaiseIntent("testIntent", "testContextType");

        result.Should().BeFalse();
    }

    [Fact]
    public void CanRaiseIntent_returns_false_when_Interop_section_is_null()
    {
        var app = new Fdc3App("testAppId", "testAppName", AppType.Web, new WebAppDetails("https://www.myApp.com"))
        {
            Interop = null
        };

        var result = app.CanRaiseIntent("testIntent", "testContextType");

        result.Should().BeFalse();
    }

    [Fact]
    public void CanRaiseIntent_returns_false_when_Intents_section_is_null()
    {
        var app = new Fdc3App("testAppId", "testAppName", AppType.Web, new WebAppDetails("https://www.myApp.com"))
        {
            Interop = new Interop()
            {
                Intents = null
            }
        };

        var result = app.CanRaiseIntent("testIntent", "testContextType");

        result.Should().BeFalse();
    }

    [Fact]
    public void CanRaiseIntent_returns_false_when_Raises_section_is_null()
    {
        var app = new Fdc3App("testAppId", "testAppName", AppType.Web, new WebAppDetails("https://www.myApp.com"))
        {
            Interop = new Interop()
            {
                Intents = new Intents()
                {
                    Raises = null
                }
            }
        };

        var result = app.CanRaiseIntent("testIntent", "testContextType");

        result.Should().BeFalse();
    }

    [Fact]
    public void CanRaiseIntent_returns_false_when_context_type_is_null()
    {
        var app = new Fdc3App("testAppId", "testAppName", AppType.Web, new WebAppDetails("https://www.myApp.com"))
        {
            Interop = new Interop()
            {
                Intents = new Intents()
                {
                    Raises = new Dictionary<string, IEnumerable<string>>()
                    {
                        { "myIntent1", new string[] { "myContextType" } },
                        { "myIntent2", new string[] { "myContextType", "myContextType1" } }
                    }
                }
            }
        };

        var result = app.CanRaiseIntent("testIntent");

        result.Should().BeFalse();
    }

    [Fact]
    public void CanRaiseIntent_returns_false_when_intent_is_null_and_context_type_is_not_found()
    {
        var app = new Fdc3App("testAppId", "testAppName", AppType.Web, new WebAppDetails("https://www.myApp.com"))
        {
            Interop = new Interop()
            {
                Intents = new Intents()
                {
                    Raises = new Dictionary<string, IEnumerable<string>>()
                    {
                        { "myIntent1", new string[] { "myContextType" } },
                        { "myIntent2", new string[] { "myContextType1", "myContextType2" } }
                    }
                }
            }
        };

        var result = app.CanRaiseIntent(contextType: "myContextType3");

        result.Should().BeFalse();
    }

    [Fact]
    public void CanRaiseIntent_returns_false_when_intent_is_not_found()
    {
        var app = new Fdc3App("testAppId", "testAppName", AppType.Web, new WebAppDetails("https://www.myApp.com"))
        {
            Interop = new Interop()
            {
                Intents = new Intents()
                {
                    Raises = new Dictionary<string, IEnumerable<string>>()
                    {
                        { "myIntent1", new string[] { "myContextType" } },
                        { "myIntent2", new string[] { "myContextType1", "myContextType2" } }
                    }
                }
            }
        };

        var result = app.CanRaiseIntent("notExistentIntent", "myContextType");

        result.Should().BeFalse();
    }

    [Fact]
    public void CanRaiseIntent_returns_true_when_intent_is_null_and_context_type_is_found()
    {
        var app = new Fdc3App("testAppId", "testAppName", AppType.Web, new WebAppDetails("https://www.myApp.com"))
        {
            Interop = new Interop()
            {
                Intents = new Intents()
                {
                    Raises = new Dictionary<string, IEnumerable<string>>()
                    {
                        { "myIntent1", new string[] { "myContextType" } },
                        { "myIntent2", new string[] { "myContextType1", "myContextType2" } }
                    }
                }
            }
        };

        var result = app.CanRaiseIntent(contextType: "myContextType2");

        result.Should().BeTrue();
    }

    [Fact]
    public void CanRaiseIntent_returns_true_when_context_type_is_fdc3_nothing_and_intent_found_with_empty_array_of_context_types()
    {
        var app = new Fdc3App("testAppId", "testAppName", AppType.Web, new WebAppDetails("https://www.myApp.com"))
        {
            Interop = new Interop()
            {
                Intents = new Intents()
                {
                    Raises = new Dictionary<string, IEnumerable<string>>()
                    {
                        { "myIntent0", new string[] { } },
                        { "myIntent1", new string[] { "myContextType" } },
                        { "myIntent2", new string[] { "myContextType1", "myContextType2" } }
                    }
                }
            }
        };

        var result = app.CanRaiseIntent("myIntent0", ContextTypes.Nothing);

        result.Should().BeTrue();
    }

    [Fact]
    public void CanRaiseIntent_returns_true_when_context_type_is_fdc3_nothing_and_intent_found()
    {
        var app = new Fdc3App("testAppId", "testAppName", AppType.Web, new WebAppDetails("https://www.myApp.com"))
        {
            Interop = new Interop()
            {
                Intents = new Intents()
                {
                    Raises = new Dictionary<string, IEnumerable<string>>()
                    {
                        { "myIntent0", new string[] { } },
                        { "myIntent1", new string[] { "myContextType" } },
                        { "myIntent2", new string[] { "myContextType1", "myContextType2" } }
                    }
                }
            }
        };

        var result = app.CanRaiseIntent("myIntent1", ContextTypes.Nothing);

        result.Should().BeTrue();
    }

    [Fact]
    public void CanRaiseIntent_returns_false_when_context_type_is_not_null_and_intent_found()
    {
        var app = new Fdc3App("testAppId", "testAppName", AppType.Web, new WebAppDetails("https://www.myApp.com"))
        {
            Interop = new Interop()
            {
                Intents = new Intents()
                {
                    Raises = new Dictionary<string, IEnumerable<string>>()
                    {
                        { "myIntent0", new string[] { } },
                        { "myIntent1", new string[] { "myContextType" } },
                        { "myIntent2", new string[] { "myContextType1", "myContextType2" } }
                    }
                }
            }
        };

        var result = app.CanRaiseIntent("myIntent0", "myContextType3");

        result.Should().BeFalse();
    }

    [Fact]
    public void CanRaiseIntent_returns_true_when_context_type_is_not_null_and_intent_found()
    {
        var app = new Fdc3App("testAppId", "testAppName", AppType.Web, new WebAppDetails("https://www.myApp.com"))
        {
            Interop = new Interop()
            {
                Intents = new Intents()
                {
                    Raises = new Dictionary<string, IEnumerable<string>>()
                    {
                        { "myIntent0", new string[] { } },
                        { "myIntent1", new string[] { "myContextType" } },
                        { "myIntent2", new string[] { "myContextType1", "myContextType2" } }
                    }
                }
            }
        };

        var result = app.CanRaiseIntent("myIntent1", "myContextType");

        result.Should().BeTrue();
    }
}
