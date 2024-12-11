import * as AdaptiveCards from "adaptivecards";
import  notificationTemplate  from "./defaultTemplate.json";
import markdownit from 'markdown-it';
import { customTemplate } from "./customElement";

const md = markdownit('commonmark');
AdaptiveCards.AdaptiveCard.onProcessMarkdown = function(text, result) { 
    result.outputHtml = md.render(text);
	result.didProcess = true;
 }

export class ToastNotification {
    template?:string;
    type!: string;
    icon?:string;
    card = new AdaptiveCards.AdaptiveCard();
    adaptiveTemplate: object = notificationTemplate;
    
    
    constructor(type?:string, template?:string, icon?:string) {

        if(template) {
            //this.template = template;
            AdaptiveCards.AdaptiveCard.elementTypeRegistry.registerType("templateElement", () => { return new customTemplate(); });

            let mew = new customTemplate();
            mew.templateString = '<p>I am a code cat </p>';
            console.log(mew);

            console.log(this.adaptiveTemplate);
        }
        this.type = type ? type : 'info';
        this.icon = icon;

    this.card.parse(this.adaptiveTemplate);


    }

    getCard() {
        return this.card;
    }

    get AdaptiveTemplate() : object {
        return this.adaptiveTemplate;
    }

    set AdaptiveTemplate(acTemplate: object) {
        this.adaptiveTemplate = { ...acTemplate };
    }

    
  }