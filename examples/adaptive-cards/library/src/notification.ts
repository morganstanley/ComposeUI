import * as AdaptiveCards from "adaptivecards";
import { defaultCard,defaultCardTemplate } from "./defaultTemplates";
import markdownit from "markdown-it";
import { CustomTemplate, customInterface } from "./customElement";
import { hostConfig, images } from "./constants";

const md = markdownit("commonmark");
AdaptiveCards.AdaptiveCard.onProcessMarkdown = function (text, result) {
  result.outputHtml = md.render(text);
  result.didProcess = true;
};

export class ToastNotification {
  template?: string;
  type: string;
  icon: string;
  card;
  adaptiveTemplate: object;

  constructor(type:string="info", template: string, icon: string = "img/info.png") {
    this.type = type
    this.icon = icon;

    //console.log(path);
    console.log(images);

    if (template) {
        console.log(template);
      //this.template = template;
      // Create a custom registry for elements
      let elementRegistry = new AdaptiveCards.CardObjectRegistry<AdaptiveCards.CardElement>();

      // Populate it with the default set of elements
      AdaptiveCards.GlobalRegistry.populateWithDefaultElements(elementRegistry);

      // Register the custom ProgressBar element
      elementRegistry.register(CustomTemplate.JsonTypeName, CustomTemplate);

      // Parse a card payload using the custom registry
      let serializationContext = new AdaptiveCards.SerializationContext();
      serializationContext.setElementRegistry(elementRegistry);
      this.card = new AdaptiveCards.AdaptiveCard();

      this.adaptiveTemplate = defaultCardTemplate;

      this.addStyles()

      this.card.parse(this.adaptiveTemplate,serializationContext);
    } else {
        this.adaptiveTemplate = defaultCard(`${this.type} Notification`,images[1]);
      console.log(this.adaptiveTemplate);
      
      this.card = new AdaptiveCards.AdaptiveCard();

      this.addStyles();

      this.card.parse(this.adaptiveTemplate);
    }
    

    //this.card.parse(this.adaptiveTemplate);
  }

  addStyles() {
    this.card.hostConfig = new AdaptiveCards.HostConfig(
        hostConfig(this.typebgColor())
    );

  }

  typebgColor() {
    switch (this.type) {
        case "success":
            return "rgb(201 245 212 / 0.3)";
        case "error":
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
}
