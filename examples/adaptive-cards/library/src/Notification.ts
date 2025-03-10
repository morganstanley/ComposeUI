// Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License.
import * as AdaptiveCards from "adaptivecards";
import markdownit from "markdown-it";
import { Types, checkType, INotification, INotificationCollection } from "./Shared";

const md = markdownit("commonmark");
AdaptiveCards.AdaptiveCard.onProcessMarkdown = function (text, result) {
  result.outputHtml = md.render(text);
  result.didProcess = true;
};

export class Notification implements INotification {
  templatePromise: Promise<object | string>;
  card: AdaptiveCards.AdaptiveCard;
  type: Types;
  defaultTimer: number = 6000;
  adaptiveTemplate: object;
  static notifications: INotificationCollection[] = [];
  count: number = Notification.notifications.length;

  constructor(type: string) {
    const notificationType = checkType(type);
    this.type = notificationType;
    this.card = new AdaptiveCards.AdaptiveCard();
    this.addStyles(notificationType);

    Notification.notifications.push({
      count: this.count,
      card: this.card,
    });
  }

  private addStyles(type: Types): void {
    this.card.hostConfig = new AdaptiveCards.HostConfig(
      this.hostConfig(this.typebgColor(type))
    );
  }

  private typebgColor(type: Types): string {
    switch (type as Types) {
      case Types.Success:
        return "rgb(201 245 212 / 0.3)";
      case Types.Err:
        return "rgb(237 140 140 / 0.3)";
      case Types.Info:
        return "rgb(165 176 250 / 0.3)";
    }
  }

  public getCard(): AdaptiveCards.AdaptiveCard {
    return this.card;
  }

  public get AdaptiveTemplate(): object {
    return this.adaptiveTemplate;
  }

  public set AdaptiveTemplate(acTemplate: object) {
    this.adaptiveTemplate = { ...acTemplate };
  }

  public render(): void {}

  // eslint-disable-next-line @typescript-eslint/no-unused-vars
  public renderStyles(doc: Document): void {}

  private hostConfig(bgColor: string): object {
    return {
      fontFamily: "Segoe UI, Helvetica Neue, sans-serif",
      containerStyles: {
        default: {
          backgroundColor: `${bgColor}`,
        },
      },
    };
  };
}
