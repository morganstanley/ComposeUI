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

import { Error, isProtocolError } from "./protocol/Error";

export class MessageRouterError extends Error {
    
    constructor(err: string | Error, message?: string, stack?: string) {
        let [name, msg] = isProtocolError(err) ? [err.name, err.message] : [err, message];
        super(msg);
        this.name = name;
        if (stack) {
            this.stack = stack;
        }
    }
}

export function createProtocolError(err: any): Error {
    if (typeof err === "string")
        return {
            name: "Error",
            message: err
        };

    return {
        name: err.name ?? "Error",
        message: err.message ?? `Unknown error (${err})`
    }
}
