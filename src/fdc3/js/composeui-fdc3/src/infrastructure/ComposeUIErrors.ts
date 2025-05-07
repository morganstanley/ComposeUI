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

export enum ComposeUIErrors {
    NoAnswerWasProvided = 'No answer was provided by the DesktopAgent backend.',
    InstanceIdNotFound = 'InstanceId was not found on window object. To run Fdc3\'s ComposeUI implementation instance config should be set on window config.',
    CurrentChannelNotSet = 'The current channel has not been set.',
    UnsubscribeFailure = 'The Listener could not unsubscribe.',
    SubscribeFailure = 'The Listener could not subscribe.',
    AppIdentifierTypeFailure = 'Using string type for app argument is not supported. Please use undefined | AppIdentifier types!'
}