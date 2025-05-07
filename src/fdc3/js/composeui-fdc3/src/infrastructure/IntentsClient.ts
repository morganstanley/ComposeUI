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

import { AppIdentifier, AppIntent, AppMetadata, Context, IntentResolution } from "@finos/fdc3";

export interface IntentsClient {
    findIntent(intent: string, context?: Context, resultType?: string): Promise<AppIntent>;
    findIntentsByContext(context: Context, resultType?: string): Promise<Array<AppIntent>>;
    raiseIntent(intent: string, context: Context, app?: string | AppIdentifier): Promise<IntentResolution>
    raiseIntentForContext(context: Context, app?: string | AppIdentifier): Promise<IntentResolution>;
    getIntentResolution(messageId: string, intent: string, source: AppMetadata): Promise<IntentResolution>;
}