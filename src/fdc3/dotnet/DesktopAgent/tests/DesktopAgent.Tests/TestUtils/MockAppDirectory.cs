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

using MorganStanley.Fdc3.AppDirectory;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Tests.TestUtils;

public class MockAppDirectory : IAppDirectory
{
    public IEnumerable<Fdc3App> Apps => _sampleAppDirectory;

    private IEnumerable<Fdc3App> _sampleAppDirectory = new List<Fdc3App>()
    {
        new Fdc3App("appId1", "app1", AppType.Web, new WebAppDetails("customurl"))
        {
            Interop = new Interop()
            {
                Intents = new Intents()
                {
                    ListensFor = new Dictionary<string, IntentMetadata>()
                    {
                        {
                            "intentMetadata1",
                            new IntentMetadata("intentMetadata1", "displayName1", new string[] {"context1"})
                        }
                    }
                }
            }
        },

        new Fdc3App("appId2", "app2", AppType.Web, new WebAppDetails("customurl"))
        {
            Interop = new Interop()
            {
                Intents = new Intents()
                {
                    ListensFor = new Dictionary<string, IntentMetadata>()
                    {
                        {
                            "intentMetadata2", 
                            new IntentMetadata("intentMetadata2", "displayName2", new string[] { "dummyContext" })
                        }
                    }
                }
            }
        },

        new Fdc3App("appId3", "app3", AppType.Web, new WebAppDetails("customurl"))
        {
            Interop = new Interop()
            {
                Intents = new Intents()
                {
                }
            }
        },

        new Fdc3App("appId4", "app4", AppType.Web, new WebAppDetails("customurl"))
        {
            Interop = new Interop()
            {
                Intents = new Intents()
                {
                    ListensFor = new Dictionary<string, IntentMetadata>()
                    {
                        {
                            "intentMetadata4",
                            new IntentMetadata("intentMetadata4", "displayName4", new string[] {"context2"})
                        },
                        {
                            "intentMetadataCustom",
                            new IntentMetadata("intentMetadataCustom", "intentMetadataCustom", new string[] {"contextCustom"})
                        }
                    }
                }
            }
        },

        new Fdc3App("appId5", "app5", AppType.Web, new WebAppDetails("customurl"))
        {
            Interop = new Interop()
            {
                Intents = new Intents()
                {
                    ListensFor = new Dictionary<string, IntentMetadata>()
                    {
                        {
                            "intentMetadata4",
                            new IntentMetadata("intentMetadata4", "displayName4", new string[] { "context2", "context5" })
                            {
                                ResultType = "resultType<specified>"
                            }
                        }
                    }
                }
            }
        },

        new Fdc3App("appId6", "app6", AppType.Web, new WebAppDetails("customurl"))
        {
            Interop = new Interop()
            {
                Intents = new Intents()
                {
                    ListensFor = new Dictionary<string, IntentMetadata>()
                    {
                        {
                            "intentMetadata1",
                            new IntentMetadata("intentMetadata1", "displayName1", new string[] {"context6"})
                            {
                                ResultType = "res66"
                            }
                        },
                        {
                            "intentMetadata4",
                            new IntentMetadata("intentMetadata4", "displayName4", new string[] {"context2"})
                            {
                                ResultType = "resultType"
                            }
                        }
                    }
                }
            }
        },

        new Fdc3App("appId7", "app7", AppType.Web, new WebAppDetails("customurl"))
        {
            Interop = new Interop()
            {
                Intents = new Intents()
                {
                    ListensFor = new Dictionary<string, IntentMetadata>()
                    {
                        {
                            "intentMetadata7",
                            new IntentMetadata("intentMetadat7", "displayName7", new string[] {"context8"})
                            {
                                ResultType = "resultType2<specified2>"
                            }
                        },
                        {
                            "intentMetadata8",
                            new IntentMetadata("intentMetadata8", "displayName8", new string[] {"context7"})
                            {
                                ResultType = "resultType2<specified>"
                            }
                        }
                    }
                }
            }
        },

        new Fdc3App("appId8", "app8", AppType.Web, new WebAppDetails("customurl"))
        {
            Interop = new Interop()
            {
                Intents = new Intents()
                {
                    ListensFor = new Dictionary<string, IntentMetadata>()
                    {
                        {
                            "intentMetadata8",
                            //typo in purpose!
                            new IntentMetadata("intentMetadat8", "displayName8", new string[] {"context7"})
                            {
                                ResultType = "resultType2<specified>"
                            }
                        }
                    }
                }
            }
        },

        new Fdc3App("wrongappId9", "app9", AppType.Web, new WebAppDetails("customurl"))
        {
            Interop = new Interop()
            {
                Intents = new Intents()
                {
                    ListensFor = new Dictionary<string, IntentMetadata>()
                    {
                        {
                            "intentMetadata9",
                            new IntentMetadata("intentMetadata9", "displayName9", new string[] {"context9"})
                            {
                                ResultType = "resultWrongApp"
                            }
                        }
                    }
                }
            }
        },

        new Fdc3App("wrongappId9", "app10", AppType.Web, new WebAppDetails("customurl"))
        {
            Interop = new Interop()
            {
                Intents = new Intents()
                {
                    ListensFor = new Dictionary<string, IntentMetadata>()
                    {
                        {
                            "intentMetadata10",
                            new IntentMetadata("intentMetadata10", "displayName10", new string[] {"context9"})
                            {
                                ResultType = "resultWrongApp"
                            }
                        },
                        {
                            "intentMetadata11",
                            new IntentMetadata("intentMetadata11", "displayName11", new string[] {"context10"})
                            {
                                ResultType = "resultWrongApp2"
                            }
                        }
                    }
                }
            }
        },

        new Fdc3App("appId11", "app11", AppType.Web, new WebAppDetails("customurl"))
        {
            Interop = new Interop()
            {
                Intents = new Intents()
                {
                    ListensFor = new Dictionary<string, IntentMetadata>()
                    {
                        {
                            "intentMetadata10",
                            new IntentMetadata("intentMetadata10", "displayName10", new string[] {"context9"})
                            {
                                ResultType = "channel<specified>"
                            }
                        }
                    }
                }
            }
        },
    };

    public Task<IEnumerable<Fdc3App>> GetApps()
    {
        return Task.FromResult(_sampleAppDirectory);
    }

    public Task<Fdc3App?> GetApp(string appId)
    {
        var app = _sampleAppDirectory.FirstOrDefault(app => app.AppId == appId);
        return Task.FromResult(app);
    }
}