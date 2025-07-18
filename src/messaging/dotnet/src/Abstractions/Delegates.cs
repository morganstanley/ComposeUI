// Morgan Stanley makes this available to you under the Apache License,
// Version 2.0 (the "License"). You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0.
// 
// See the NOTICE file distributed with this work for additional information
// regarding copyright ownership. Unless required by applicable law or agreed
// to in writing, software distributed under the License is distributed on an
// "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
// or implied. See the License for the specific language governing permissions
// and limitations under the License.

namespace MorganStanley.ComposeUI.Messaging.Abstractions;

/// <summary>
/// The delegate type that gets called when an subscription receives a message.
/// </summary>
public delegate ValueTask TopicMessageHandler(string payload);

/// <summary>
/// The delegate type that gets called when a service is invoked
/// </summary>
/// <param name="request"></param>
/// <returns></returns>
public delegate ValueTask<string?> ServiceHandler(string? request);

/// <summary>
/// A generic delegate type to be used with typed service registrations e.g. <see cref="MessagingServiceJsonExtensions.RegisterJsonServiceAsync{TRequest, TResult}(IMessaging, string, Func{TRequest?, ValueTask{TResult?}}, System.Text.Json.JsonSerializerOptions, CancellationToken)"/>
/// </summary>
/// <typeparam name="TRequest"></typeparam>
/// <typeparam name="TResponse"></typeparam>
/// <param name="request"></param>
/// <returns></returns>
public delegate ValueTask<TResponse?> ServiceHandler<TRequest, TResponse>(TRequest? request);