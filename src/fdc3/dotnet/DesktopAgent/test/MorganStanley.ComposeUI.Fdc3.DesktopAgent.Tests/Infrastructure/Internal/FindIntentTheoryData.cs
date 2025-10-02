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

public class FindIntentTestCase
{
    internal string Name { get; set; }
    internal FindIntentRequest Request { get; set; }
    internal FindIntentResponse ExpectedResponse { get; set; }
}

public partial class Fdc3DesktopAgentMessagingServiceTests
{
    private class FindIntentTheoryData : TheoryData
    {
        public FindIntentTheoryData()
        {
            AddRow(
                new FindIntentTestCase
                {
                    Name = "No apps match the accepted context",
                    ExpectedResponse = new FindIntentResponse
                    {
                        Error = ResolveError.NoAppsFound
                    },
                    Request = new FindIntentRequest
                    {
                        Fdc3InstanceId = Guid.NewGuid().ToString(),
                        Intent = Intent2.Name,
                        Context = new Context("noAppShouldBeReturned").AsJson()
                    }
                });

            AddRow(
                new FindIntentTestCase
                {
                    Name = "Null request",
                    ExpectedResponse = new FindIntentResponse
                    {
                        Error = ResolveError.IntentDeliveryFailed
                    },
                    Request = null
                });

            AddRow(
                new FindIntentTestCase
                {
                    Name = "Match single intent, context and result type",
                    Request = new FindIntentRequest
                    {
                        Fdc3InstanceId = Guid.NewGuid().ToString(),
                        Intent = Intent1.Name,
                        Context = SingleContext.AsJson(),
                        ResultType = ResultType1
                    },
                    ExpectedResponse = new FindIntentResponse
                    {
                        AppIntent = new AppIntent
                        {
                            Intent = Intent1,
                            Apps = new[] { App1 }
                        }
                    }
                });

            AddRow(
                new FindIntentTestCase
                {
                    Name = "Match single app by intent and result type",
                    Request = new FindIntentRequest
                    {
                        Fdc3InstanceId = Guid.NewGuid().ToString(),
                        Intent = Intent1.Name,
                        ResultType = ResultType1
                    },
                    ExpectedResponse = new FindIntentResponse
                    {
                        AppIntent = new AppIntent
                        {
                            Intent = Intent1,
                            Apps = new[]
                            {
                                App1
                            }
                        }
                    }
                });

            AddRow(
                new FindIntentTestCase
                {
                    Name = "Match single app by intent and context",
                    Request = new FindIntentRequest
                    {
                        Fdc3InstanceId = Guid.NewGuid().ToString(),
                        Intent = Intent1.Name,
                        Context = SingleContext.AsJson()
                    },
                    ExpectedResponse = new FindIntentResponse
                    {
                        AppIntent = new AppIntent
                        {
                            Intent = Intent1,
                            Apps = new[]
                            {
                                App1
                            }
                        }
                    }
                });

            AddRow(
              new FindIntentTestCase
              {
                  Name = "Match single app by intent",
                  Request = new FindIntentRequest
                  {
                      Fdc3InstanceId = Guid.NewGuid().ToString(),
                      Intent = Intent1.Name
                  },
                  ExpectedResponse = new FindIntentResponse
                  {
                      AppIntent = new AppIntent
                      {
                          Intent = Intent1,
                          Apps = new[]
                          {
                                App1
                          }
                      }
                  }
              });

            AddRow(
                new FindIntentTestCase
                {
                    Name = "Match multiple apps by intent and context",
                    Request = new FindIntentRequest
                    {
                        Fdc3InstanceId = Guid.NewGuid().ToString(),
                        Intent = Intent2.Name,
                        Context = Intent2Context.AsJson()
                    },
                    ExpectedResponse = new FindIntentResponse
                    {
                        AppIntent = new AppIntent
                        {
                            Intent = Intent2,
                            Apps = new[]
                            {
                                App2,
                                App3ForIntent2
                            }
                        }
                    }
                });

            AddRow(
                new FindIntentTestCase
                {
                    Name = "Match mutliple apps by intent",
                    Request = new FindIntentRequest
                    {
                        Fdc3InstanceId = Guid.NewGuid().ToString(),
                        Intent = Intent2.Name
                    },
                    ExpectedResponse = new FindIntentResponse
                    {
                        AppIntent = new AppIntent
                        {
                            Intent = Intent2,
                            Apps = new AppMetadata[]
                            {
                                App2,
                                App3ForIntent2
                            }
                        }
                    }
                });

            AddRow(
                new FindIntentTestCase
                {
                    Name = "Result type channel finds specific channels as well",
                    Request = new FindIntentRequest
                    {
                        Fdc3InstanceId = Guid.NewGuid().ToString(),
                        Intent = IntentWithChannelResult.Name,
                        ResultType = ChannelResult
                    },
                    ExpectedResponse = new FindIntentResponse
                    {
                        AppIntent = new AppIntent
                        {
                            Intent = IntentWithChannelResult,
                            Apps = new[] {
                                App6,
                                App7
                            }
                        }
                    }
                });

            AddRow(
                new FindIntentTestCase
                {
                    Name = "Specific channel result type matched",
                    Request = new FindIntentRequest
                    {
                        Fdc3InstanceId = Guid.NewGuid().ToString(),
                        Intent = IntentWithChannelResult.Name,
                        ResultType = SpecificChannelResult
                    },
                    ExpectedResponse = new FindIntentResponse
                    {
                        AppIntent = new AppIntent
                        {
                            Intent = IntentWithChannelResult,
                            Apps = new[] {
                                    App7

                            }
                        }
                    }
                });
        }
    }
}