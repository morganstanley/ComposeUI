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

export class MessageScope {

    asString(): string {
        return this.value ?? "";
    }

    equals(other: MessageScope): boolean {
        return other.value === this.value;
    }

    isClientId(): boolean {
        return !!this.value && this.value.startsWith('@');
    }

    getClientId(): string | undefined {
        return this.isClientId() ? this.value?.slice(1) : undefined;
    }

    static readonly default = new MessageScope(null);
    static readonly application = this.default;

    static fromClientId(clientId: string): MessageScope {
        return new MessageScope("@" + clientId);
    }

    static parse(value: string): MessageScope {
        return new MessageScope(value);
    }

    private value: string | null = null;

    private constructor(value: string | null) {
        this.value = !!value ? value : null;
    }
}
