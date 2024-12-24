import * as AdaptiveCards from "adaptivecards";
import { defaultCard, defaultCardTemplate } from "./defaultTemplates";
import markdownit from "markdown-it";
import { CustomTemplate } from "./customElement";
import { hostConfig, Icons, Types, checkType, getIcon } from "./constants";
import defaultCardStyles from './styles/defaultStyles.css';

const md = markdownit("commonmark");
AdaptiveCards.AdaptiveCard.onProcessMarkdown = function (text, result) {
  result.outputHtml = md.render(text);
  result.didProcess = true;
};

export class ToastNotification {
  template?: string;
  type: Types;
  icon: string | Icons = Icons.Info;
  card;
  defaultTimer: number = 9000;
  adaptiveTemplate: object;

  constructor(type: string = "info", template: string, icon?: string, message?:string) {
    console.log(defaultCardStyles)
    this.type = checkType(type);
    if (icon) {
      this.icon = icon;
    } else {
      this.icon = getIcon(this.type);
    }

    if (template) {
      console.log(template);
      //this.template = template;
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
      this.card = new AdaptiveCards.AdaptiveCard();

      this.adaptiveTemplate = defaultCardTemplate({
        title: `${this.type} Notification`,
        icon: this.icon,
        template: template,
      });

      this.addStyles();

      this.card.parse(this.adaptiveTemplate, serializationContext);
    } else {
      this.adaptiveTemplate = defaultCard({
        title: `${this.type} Notification`,
        icon: this.icon,
        message: message ? message : "This is a placeholder for the notification message."
      });
      this.card = new AdaptiveCards.AdaptiveCard();

      this.addStyles();

      this.card.parse(this.adaptiveTemplate);
    }
  }

  addStyles() {
    this.card.hostConfig = new AdaptiveCards.HostConfig(
      hostConfig(this.typebgColor())
    );
  }

  typebgColor() {
    switch (this.type) {
      case Types.Success:
        return "rgb(201 245 212 / 0.3)";
      case Types.Err:
        return "rgb(237 140 140 / 0.3)";
      default:
        return "rgb(165 176 250 / 0.3)";
    }
  }

  getCard() {
    return this.card;
  }

  get AdaptiveTemplate(): object {
    return this.adaptiveTemplate;
  }

  set AdaptiveTemplate(acTemplate: object) {
    this.adaptiveTemplate = { ...acTemplate };
  }

  renderCard(document: Document) {
    let result = this.card.render(document.body);

    //this.renderStyles(document);

    this.card.onExecuteAction = function (action) {
      if (result) {
        result.remove();
      }
    };

    setTimeout(() => {
      if (result) {
        result.remove();
      }
    }, this.defaultTimer);
  }

  renderStyles(document:Document) {

    let styles = new CSSStyleSheet();

    styles.replaceSync(defaultCardStyles);

    document.adoptedStyleSheets = [styles];
  }
}


