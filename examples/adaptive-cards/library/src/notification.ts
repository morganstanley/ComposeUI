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
    template:object=notificationTemplate;
    type!: string;
    icon?:string;
    card = new AdaptiveCards.AdaptiveCard();
    
    
    /*constructor(type:string, template?:string, icon?:string) {

        if(template) {
            //this.template = template;
            AdaptiveCards.AdaptiveCard.elementTypeRegistry.registerType("templateElement", () => { return new customTemplate(); });
            console.log(template);
        }
        this.type = type;
        this.icon = icon;
    }*/

    getCard() {
        return this.card;
    }
  
  //let adaptiveCard = new AdaptiveCards.AdaptiveCard();

    
  }