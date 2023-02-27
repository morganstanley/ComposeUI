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

import { isError } from "@jest/expect-utils";
import { ExpectationResult } from "expect";
import { isRegExp } from "util/types";

declare global {
    namespace jest {
        interface Matchers<R, T extends {}> {
            toThrowWithName(type: Function, name: string | RegExp): R;
        }
    }
}

expect.extend({

    toThrowWithName(this: jest.MatcherContext, received: any, type: Function, name: string | RegExp): ExpectationResult {

        const fromPromise = this.promise;

        let error: any;

        if (fromPromise) {
            error = received;
        }
        else if (typeof received === "function") {
            try {
                received();
            }
            catch (e)
            {
                error = e;
            }
        }
        
        if (!isError(error)) {
            return {
                pass: false,
                message: () => `Expected the function to throw ${type.name} with name '${name}', but no Error was thrown.`
            };
        }

        if (!(error instanceof type)) {
            const receivedConstructorName = Object.getPrototypeOf(error)?.constructor?.name ?? "<unknown type>";

            return {
                pass: false,
                message: () => `Expected the function to throw ${type.name} with name '${name}', but ${receivedConstructorName} is not assignable to ${type.name}.`
            };
        }

        if (isRegExp(name)) {
            if (!name.test(error.name)) {
                return {
                    pass: false,
                    message: () => `Expected the function to throw ${type.name} with name matching '${name}', but the name was '${error.name}.'`
                };    
            }
        }
        else {
            if (name != error.name) {
                return {
                    pass: false,
                    message: () => `Expected the function to throw ${type.name} with name '${name}', but the name was '${error.name}.'`
                };    
            }
        }

        return {
            pass: true,
            message: () => `Expected received to throw ${type.name} with name '${name}'.`
        }
    }
});
