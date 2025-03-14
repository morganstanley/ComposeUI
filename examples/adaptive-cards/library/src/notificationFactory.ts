import * as ACData from "adaptivecards-templating";
import cancel from "./img/cancel.png";
import { Icons, checkType, getIcon } from "./constants";
import { notification } from "./utils";
import CustomHtmlNotification from "./customNotification";
import DefaultNotification from "./defaultNotification";
import { INotification } from "./notification";

export interface INotificationOptions {
  type?: string;
  templateID?: string;
  htmlTemplate?: string;
  icon?: string;
  data?: object;
}

export class ToastNotification {
  static notifications: notification[] = [];
  count: number = ToastNotification.notifications.length;

  static async createNotification(
    options: INotificationOptions
  ): Promise<INotification> {
    const { type, templateID, htmlTemplate, icon, data } = options;

    const notificationType = checkType(type);
    const notificationIcon = icon ? icon : getIcon(notificationType);

    let expansionTemplate: ACData.IEvaluationContext = data
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
      let templ = new ACData.Template(cardTemplate);

      const adaptiveTemplate = templ.expand(expansionTemplate);

      return new CustomHtmlNotification(notificationType, adaptiveTemplate);
    } else {
      const fetchId = templateID ?? "default";
      const cardTemplate = await this.getCardTemplate(fetchId); // get template for given templateID from api in a promise
      let templ = new ACData.Template(cardTemplate);
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
  ) {
    if (!(key in expansionTemplate.$root)) {
      expansionTemplate.$root[key] = value;
    }

    return expansionTemplate;
  }
}
