import * as AdaptiveCards from "adaptivecards";
import  notificationTemplate  from "./defaultTemplate.json";

export class ToastNotification {
    template:object=notificationTemplate;
    type:string;
    icon?:string;
    
    
    constructor(type:string, template?:object, icon?:string) {
        if(template) {
            this.template = template;
        }
        this.type = type;
        this.icon = icon;
    }
  
  //let adaptiveCard = new AdaptiveCards.AdaptiveCard();

    
  }