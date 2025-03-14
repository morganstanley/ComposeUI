import * as AdaptiveCards from "adaptivecards";
import { Types } from "./constants";
import { CustomTemplate } from "./customElement";
import { Notification } from "./notification";
import defaultTemplateStyles from "./styles/defaultTemplateStyles.css";

export default class CustomHtmlNotification extends Notification {
  adaptiveTemplate: object;
  htmlTemplate: string;

  constructor(type: Types, adaptiveTemplate: object) {
    super(type);
    this.adaptiveTemplate = adaptiveTemplate;

    const serializationContext = this.getSerializationContext();
    this.card.parse(this.adaptiveTemplate, serializationContext);
  }

  getSerializationContext() {
    // Create a custom registry for elements
    let elementRegistry =
      new AdaptiveCards.CardObjectRegistry<AdaptiveCards.CardElement>();
    // Populate it with the default set of elements
    AdaptiveCards.GlobalRegistry.populateWithDefaultElements(elementRegistry);
    // Register the custom ProgressBar element
    elementRegistry.register(CustomTemplate.JsonTypeName, CustomTemplate);
    // Parse a card payload using the custom registry
    let serializationContext = new AdaptiveCards.SerializationContext();
    serializationContext.setElementRegistry(elementRegistry);

    return serializationContext;
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
    style.innerText = defaultTemplateStyles;
    doc.head.appendChild(style);
  }
}
