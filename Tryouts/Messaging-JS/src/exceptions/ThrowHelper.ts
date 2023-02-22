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

import { MessageRouterError } from "../MessageRouterError";
import { ErrorNames } from "../ErrorNames";

export class ThrowHelper {

    static duplicateEndpoint(endpoint: string): MessageRouterError {
        return new MessageRouterError({ name: ErrorNames.duplicateEndpoint, message: `Duplicate endpoint registration: '${endpoint}'`});
    }

    static duplicateRequestId(): MessageRouterError {
        return new MessageRouterError({ name: ErrorNames.duplicateRequestId, message: "Duplicate request ID"});
    }

    static invalidEndpoint(endpoint: string): MessageRouterError {
        return new MessageRouterError({ name: ErrorNames.invalidEndpoint, message: `Invalid endpoint: '${endpoint}'`});
    }

    static invalidTopic(topic: string): MessageRouterError {
        return new MessageRouterError({ name: ErrorNames.invalidTopic, message: `Invalid topic: '${topic}'`});
    }

    static unknownEndpoint(endpoint: string): MessageRouterError {
        return new MessageRouterError({ name: ErrorNames.unknownEndpoint, message: `Unknown endpoint: ${endpoint}`});
    }

    static connectionClosed(): MessageRouterError {
        return new MessageRouterError({ name: ErrorNames.connectionClosed, message: "The connection has been closed"});
    }

    static connectionFailed(): MessageRouterError {
        return new MessageRouterError({ name: ErrorNames.connectionFailed, message: "Connection failed"});
    }

    static connectionAborted(): MessageRouterError {
        return new MessageRouterError({ name: ErrorNames.connectionAborted, message: "The connection dropped unexpectedly"});
    }

}
