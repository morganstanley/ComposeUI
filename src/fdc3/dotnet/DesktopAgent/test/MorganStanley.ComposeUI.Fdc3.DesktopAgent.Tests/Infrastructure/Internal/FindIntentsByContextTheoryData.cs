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

using Finos.Fdc3;
using Finos.Fdc3.Context;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Contracts;
using AppIntent = MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Protocol.AppIntent;
using AppMetadata = MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Protocol.AppMetadata;
using static MorganStanley.ComposeUI.Fdc3.DesktopAgent.Tests.TestData.TestAppDirectoryData;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Tests.Infrastructure.Internal;

public partial class Fdc3DesktopAgentMessagingServiceTests
{
    public class FindIntentsByContextTestCase
    {
        internal string Name { get; set; }
        internal FindIntentsByContextRequest Request { get; set; }
        internal FindIntentsByContextResponse ExpectedResponse { get; set; }
    }

    public class FindIntentsByContextTheoryData : TheoryData
    {
        public FindIntentsByContextTheoryData()
        {

            AddRow(
                new FindIntentsByContextTestCase
                {
                    Name = "Returning one AppIntent with one app by just passing Context",
                    Request = new FindIntentsByContextRequest
                    {
                        Fdc3InstanceId = Guid.NewGuid().ToString(),
                        Context = SingleContext.AsJson()
                    },
                    ExpectedResponse = new FindIntentsByContextResponse
                    {
                        AppIntents = new[]
                        {
                            new AppIntent
                            {
                                Intent = Intent1,
                                Apps = new[]
                                {
                                    App1
                                }
                            }
                        }
                    }
                });

            AddRow(
                new FindIntentsByContextTestCase
                {
                    Name = "Returning one AppIntent with multiple app by just passing Context",
                    Request = new FindIntentsByContextRequest
                    {
                        Fdc3InstanceId = Guid.NewGuid().ToString(),
                        Context = Intent2Context.AsJson()
                    },
                    ExpectedResponse = new FindIntentsByContextResponse
                    {
                        AppIntents = new[]
                        {
                            new AppIntent
                            {
                                Intent = Intent2,
                                Apps = new AppMetadata[]
                                {
                                  App2,
                                  App3ForIntent2
                                }
                            }
                        }
                    }
                });


            AddRow(
                new FindIntentsByContextTestCase
                {
                    Name = "Returning multiple appIntents by just passing Context",
                    Request = new FindIntentsByContextRequest
                    {
                        Fdc3InstanceId = Guid.NewGuid().ToString(),
                        Context = MultipleContext.AsJson()
                    },
                    ExpectedResponse = new FindIntentsByContextResponse
                    {
                        AppIntents = new[]
                        {
                            new AppIntent
                            {
                                Intent = Intent2,
                                Apps = new[]
                                {
                                    App2,
                                    App3ForIntent2
                                }
                            },
                            new AppIntent
                            {
                                Intent = Intent3,
                                Apps = new[]
                                {
                                    App3ForIntent3
                                }
                            }
                        }
                    }
                });

            AddRow(
                new FindIntentsByContextTestCase
                {
                    Name = "Returning error no apps found by just passing Context",
                    Request = new FindIntentsByContextRequest
                    {
                        Fdc3InstanceId = Guid.NewGuid().ToString(),
                        Context = new Context("noAppShouldReturn").AsJson()
                    },
                    ExpectedResponse = new FindIntentsByContextResponse
                    {
                        Error = ResolveError.NoAppsFound
                    }
                });

            AddRow(
                new FindIntentsByContextTestCase
                {
                    Name = "Returning one AppIntent with one app by ResultType",
                    Request = new FindIntentsByContextRequest
                    {
                        Fdc3InstanceId = Guid.NewGuid().ToString(),
                        Context = MultipleContext.AsJson(),
                        ResultType = ResultType1
                    },
                    ExpectedResponse = new FindIntentsByContextResponse
                    {
                        AppIntents = new[]
                        {
                            new AppIntent
                            {
                                Intent = Intent2,
                                Apps = new[]
                                {
                                    App3ForIntent2
                                }
                            }
                        }
                    }
                });

            AddRow(
                new FindIntentsByContextTestCase
                {
                    Name = "Returning one AppIntent with multiple apps by ResultType",
                    Request = new FindIntentsByContextRequest
                    {
                        Fdc3InstanceId = Guid.NewGuid().ToString(),
                        Context = ContextType.Nothing.AsJson(),
                        ResultType = ContextTypes.Nothing
                    },
                    ExpectedResponse = new FindIntentsByContextResponse
                    {
                        AppIntents = new[]
                        {
                            new AppIntent
                            {
                                Intent = IntentWithNoResult,
                                Apps = new[]
                                {
                                    App4,
                                    App5
                                }
                            }
                        }
                    }
                });

            AddRow(
                new FindIntentsByContextTestCase
                {
                    Name = "Returning multiple AppIntents by ResultType",
                    Request = new FindIntentsByContextRequest
                    {
                        Fdc3InstanceId = Guid.NewGuid().ToString(),
                        Context = MultipleContext.AsJson(),
                        ResultType = ResultType2
                    },
                    ExpectedResponse = new FindIntentsByContextResponse
                    {
                        AppIntents = new[]
                        {
                            new AppIntent
                            {
                                Intent = Intent2,
                                Apps = new[]
                                {
                                    App2
                                }
                            },
                            new AppIntent
                            {
                                Intent = Intent3,
                                Apps = new[]
                                {
                                    App3ForIntent3
                                }
                            }
                        }
                    }
                });

            AddRow(
                new FindIntentsByContextTestCase
                {
                    Name = "Returning no apps found error by using ResultType",
                    Request = new FindIntentsByContextRequest
                    {
                        Fdc3InstanceId = Guid.NewGuid().ToString(),
                        Context = MultipleContext.AsJson(),
                        ResultType = "noAppShouldReturn"
                    },
                    ExpectedResponse = new FindIntentsByContextResponse
                    {
                        Error = ResolveError.NoAppsFound
                    }
                });

            AddRow(
                new FindIntentsByContextTestCase
                {
                    Name = "Returning intent delivery error",
                    Request = null,
                    ExpectedResponse = new FindIntentsByContextResponse
                    {
                        Error = ResolveError.IntentDeliveryFailed
                    }
                });

            AddRow(
                new FindIntentsByContextTestCase
                {
                    Name = "Result type channel finds specific channels as well",
                    Request = new FindIntentsByContextRequest
                    {
                        Fdc3InstanceId = Guid.NewGuid().ToString(),
                        Context = ChannelContext.AsJson(),
                        ResultType = ChannelResult
                    },
                    ExpectedResponse = new FindIntentsByContextResponse
                    {
                        AppIntents = new[]
                        {
                            new AppIntent{
                                Intent = IntentWithChannelResult,
                                Apps = new[] {
                                    App6,
                                    App7
                                }
                            }
                        }
                    }
                });

            AddRow(
                new FindIntentsByContextTestCase
                {
                    Name = "Specific channel result type matched",
                    Request = new FindIntentsByContextRequest
                    {
                        Fdc3InstanceId = Guid.NewGuid().ToString(),
                        Context = ChannelContext.AsJson(),
                        ResultType = SpecificChannelResult
                    },
                    ExpectedResponse = new FindIntentsByContextResponse
                    {
                        AppIntents = new[]
                        {
                            new AppIntent{
                                Intent = IntentWithChannelResult,
                                Apps = new[] {
                                    App7
                                }
                            }
                        }
                    }
                });

        }

    }
}