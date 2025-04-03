// Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License.
import * as AdaptiveCards from "adaptivecards";

export enum Types {
  Info = "Information",
  Err = "Error",
  Success = "Success",
}

export interface INotification {
  adaptiveTemplate: object;
  type: Types;
  card: AdaptiveCards.AdaptiveCard;
  render(): void;
  renderStyles(doc: Document): void;
}

export function checkType(type: string): Types {
  switch (type.toLocaleLowerCase()) {
    case "error":
      return Types.Err;
    case "success":
      return Types.Success;
    default:
      return Types.Info;
  }
}

export interface INotificationCollection {
  count: number;
  card: AdaptiveCards.AdaptiveCard;
}