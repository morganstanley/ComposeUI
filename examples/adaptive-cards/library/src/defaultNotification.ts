import { Types } from "./constants";
import { Notification } from "./notification";
import defaultStyles from "./styles/defaultStyles.css";

export default class DefaultNotification extends Notification {
  adaptiveTemplate: object;

  constructor(type: Types, adaptiveTemplate: object) {
    super(type);
    this.adaptiveTemplate = adaptiveTemplate;
    this.card.parse(this.adaptiveTemplate);
  }

  render() {
    let newTab: WindowProxy;
    let result: HTMLElement | undefined;

    if (!window.opener) {
      let otherTab = window.open(document.URL, "_blank", "popup");

      if (otherTab) {
        newTab = otherTab;
        otherTab.name = `${this.count}`;
      }
    }

    newTab!.addEventListener("load", (event) => {
      const element = newTab.document.getElementById("container");
      if (element !== null) {
        while (element!.firstChild) {
          element.removeChild(element.firstChild);
        }
      }

      result = this.card.render(newTab.document.body);
      newTab.setTimeout(() => {
        if (result) {
          result.remove();
          newTab.close();
        }
      }, this.defaultTimer);

      this.renderStyles(newTab!.document);
    });

    this.card.onExecuteAction = function (action) {
      newTab.close();
    };
  }

  renderStyles(doc: Document) {
    const style = document.createElement("style");
    style.innerText = defaultStyles;
    doc.head.appendChild(style);
  }
}
