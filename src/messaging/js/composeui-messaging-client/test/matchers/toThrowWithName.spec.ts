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

import jestExpect, { JestAssertionError } from "expect";

describe("toThrowWithName matcher", () => {

    class CustomError extends Error {
        constructor(name?: string) {
            super("Custom error");
            this.name = name ?? "CustomError";
        }
    }

    class SpecialError extends CustomError {
        constructor(name?: string) {
            super("Special error");
            this.name = name ?? "SpecialError";
        }
    }

    class OtherError extends Error {
        constructor(name?: string) {
            super("Other error");
            this.name = name ?? "CustomError";
        }
    }

    function syncTest(actualType: jest.Constructable, actualName: string | undefined, expectedType: jest.Constructable, expectedName: string): void {
        const fn = () => {
            throw new actualType(actualName);
        };

        expect(fn).toThrowWithName(expectedType, expectedName);
    }

    async function asyncTest(actualType: jest.Constructable, actualName: string | undefined, expectedType: jest.Constructable, expectedName: string): Promise<void> {
        const fn = async (): Promise<void> => {
            throw new actualType(actualName);
        };

        await expect(fn()).rejects.toThrowWithName(expectedType, expectedName);
    }

    async function mustPass(actualType: jest.Constructable, actualName: string | undefined, expectedType: jest.Constructable, expectedName: string): Promise<void> {
        expect(() => syncTest(actualType, actualName, expectedType, expectedName)).not.toThrow();
        await expect(asyncTest(actualType, actualName, expectedType, expectedName)).resolves.not.toThrow();
    }

    async function mustFail(actualType: jest.Constructable, actualName: string | undefined, expectedType: jest.Constructable, expectedName: string): Promise<void> {
        expect(() => syncTest(actualType, actualName, expectedType, expectedName)).toThrow();
        await expect(asyncTest(actualType, actualName, expectedType, expectedName)).rejects.toThrow();
    }

    it("passes if error type and name match", async () => {

        await mustPass(Error, "Error", Error, "Error");
        await mustPass(CustomError, "CustomError", Error, "CustomError");
        await mustPass(CustomError, "CustomError", CustomError, "CustomError");
        await mustPass(SpecialError, "CustomError", CustomError, "CustomError");
        await mustPass(SpecialError, "SpecialError", CustomError, "SpecialError");

    });

    it("fails if error type does not match", async () => {

        await mustFail(Error, "Error", CustomError, "Error");
        await mustFail(OtherError, "Error", CustomError, "Error");
        await mustFail(CustomError, "Error", SpecialError, "Error");
        await mustFail(OtherError, "Error", SpecialError, "Error");
        
    });

    it("fails if error type matches but the name is different", async () => {

        await mustFail(Error, "CustomError", Error, "OtherError");
        await mustFail(CustomError, "CustomError", Error, "OtherError");
        await mustFail(SpecialError, "CustomError", CustomError, "OtherError");
        await mustFail(CustomError, "CustomError", CustomError, "OtherError");

    });

    
});
