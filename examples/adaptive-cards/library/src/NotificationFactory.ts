// Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License.
import * as ACData from "adaptivecards-templating";
import cancel from "./img/cancel.png";
import error from "./img/error.png";
import info from "./img/info.png";
import success from "./img/success.png";
import { checkType, Types, INotificationCollection } from "./Shared";
import CustomHtmlNotification from "./CustomNotification";
import DefaultNotification from "./DefaultNotification";
import { INotification } from "./Shared";

enum Icons {
  Info = info,
  Err = error,
  Success = success,
}

interface INotificationOptions {
  type?: string;
  templateID?: string;
  htmlTemplate?: string;
  icon?: string;
  data?: object;
}

export class ToastNotification {
  static notifications: INotificationCollection[] = [];
  count: number = ToastNotification.notifications.length;

  public static async createNotification(
    options: INotificationOptions
  ): Promise<INotification> {
    const { type, templateID, htmlTemplate, icon, data } = options;

    const notificationType = checkType(type);
    const notificationIcon = icon ? icon : this.getIcon(notificationType);

    const expansionTemplate: ACData.IEvaluationContext = data
      ? data
      : {
          $root: {
            title: `${notificationType} Notification`,
            icon: notificationIcon,
          },
        };

    this.checkProperty(
      expansionTemplate,
      "title",
      `${notificationType} Notification`
    );
    this.checkProperty(expansionTemplate, "icon", notificationIcon);

    if (htmlTemplate) {
      expansionTemplate.$root.template = htmlTemplate;
      const cardTemplate = await this.getCardTemplate("customHtml"); // get template for customHTML from api in a promise
      const templ = new ACData.Template(cardTemplate);

      const adaptiveTemplate = templ.expand(expansionTemplate);

      return new CustomHtmlNotification(notificationType, adaptiveTemplate);
    } else {
      const fetchId = templateID ?? "default";
      const cardTemplate = await this.getCardTemplate(fetchId); // get template for given templateID from api in a promise
      const templ = new ACData.Template(cardTemplate);
      expansionTemplate.$root.cancel = cancel;
      const adaptiveTemplate = templ.expand(expansionTemplate);

      return new DefaultNotification(notificationType, adaptiveTemplate);
    }
  }

  private static async getCardTemplate(templateID: string): Promise<object> {
    const api = `http://localhost:3000/template/name/${templateID}`;
    try {
      const response = await fetch(api, {
        method: "GET",
        headers: {
          "Content-Type": "application/json",
        },
      });
      const data = await response.json();
      return data;
    } catch (error) {
      if (error) {
        console.log(error.message);
      }
    }
  }

  private static checkProperty(
    expansionTemplate: ACData.IEvaluationContext,
    key: string,
    value: string | Icons
  ): ACData.IEvaluationContext {
    if (!(key in expansionTemplate.$root)) {
      expansionTemplate.$root[key] = value;
    }

    return expansionTemplate;
  }

  private static getIcon(type: Types): Icons {
    switch (type) {
      case Types.Err:
        return Icons.Err;
      case Types.Success:
        return Icons.Success;
      case Types.Info:
        return Icons.Info;
    }
  }
}
