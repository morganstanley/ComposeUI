/* 
 *  Morgan Stanley makes this available to you under the Apache License,
 *  Version 2.0 (the "License"). You may obtain a copy of the License at
 *       http://www.apache.org/licenses/LICENSE-2.0.
 *  See the NOTICE file distributed with this work for additional information
 *  regarding copyright ownership. Unless required by applicable law or agreed
 *  to in writing, software distributed under the License is distributed on an
 *  "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
 *  or implied. See the License for the specific language governing permissions
 *  and limitations under the License.
 *  
 */

/**
 * Handles a topic message already serialized as a string.
 * @param payload Serialized message string.
 * @returns A promise that resolves when processing is complete.
 */
export type TopicMessageHandler = (payload: string) => Promise<void>;

/**
 * Handles a service request where the request and response are JSON-serialized strings.
 * @param request Optional serialized request string or null.
 * @returns A promise resolving to a serialized response string or null.
 */
export type ServiceHandler = (request?: string | null) => Promise<string | null>;

/**
 * Handles a typed service request/response with generic payloads.
 * @typeParam TRequest The request type after deserialization.
 * @typeParam TResponse The response type before serialization.
 * @param request Optional typed request or null.
 * @returns A promise resolving to a typed response or null.
 */
export type TypedServiceHandler<TRequest, TResponse> = (request?: TRequest | null) => Promise<TResponse | null>;