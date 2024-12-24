import * as AdaptiveCards from "adaptivecards";
import sanitizeHtml from 'sanitize-html';

export interface customInterface {
    type:string
    templateString:string,
}


export class CustomTemplate extends AdaptiveCards.CardElement {
    private parser = new DOMParser();
    static readonly JsonTypeName = "AdaptiveHTML";

    //#region Schema
    static readonly templateStringProperty = new AdaptiveCards.StringProperty(AdaptiveCards.Versions.v1_0, "templateString");

    @AdaptiveCards.property(CustomTemplate.idProperty)
    get templateString(): string {
        return this.getValue(CustomTemplate.templateStringProperty);
    }

    set templateString(value: string) {
        let santitizedString = sanitizeHtml(value);

        if(this.templateString !== santitizedString) {
            this.setValue(CustomTemplate.templateStringProperty, santitizedString);

            this.updateLayout();
        }
    }

    //static readonly idProperty: AdaptiveCards.StringProperty = new AdaptiveCards.StringProperty(AdaptiveCards.Versions.v1_0, "id");

    //static readonly titleProperty = new AdaptiveCards.StringProperty(AdaptiveCards.Versions.v1_0, "title", true);
    //static readonly valueProperty = new AdaptiveCards.NumProperty(AdaptiveCards.Versions.v1_0, "value");

    /*
    

    @AdaptiveCards.property(CustomTemplate.valueProperty)
    get value(): number {
        return this.getValue(CustomTemplate.valueProperty);
    }

    set value(value: number) {
        let adjustedValue = value;

        if (adjustedValue < 0) {
            adjustedValue = 0;
        }
        else if (adjustedValue > 100) {
            adjustedValue = 100;
        }

        if (this.value !== adjustedValue) {
            this.setValue(customTemplate.valueProperty, adjustedValue);

            this.updateLayout();
        }
    }
        */

    //#endregion


    private _templateElement?: HTMLElement;


    protected internalRender(): HTMLElement {

        let element = document.createElement("div");
        
        this._templateElement = document.createElement("div");

        this._templateElement.insertAdjacentHTML("afterbegin",this.templateString);

        //let template = this.parser.parseFromString(this.templateString,"text/html");

        element.append(this._templateElement);

        return element;
    }

    getJsonTypeName(): string {
        return CustomTemplate.JsonTypeName;
    }

    updateLayout(processChildren: boolean = true) {
        super.updateLayout(processChildren);

        this._templateElement = document.createElement("div");

        this._templateElement.insertAdjacentHTML("afterbegin",this.templateString);
        
    }


    /*
    protected internalRender(): HTMLElement {
        let element = document.createElement("div");
        //parse html from string and append to div
        //element.append(this._titleElement, progressBarElement);

        return element;
    }

    getJsonTypeName(): string {
        return "templateElement";
    }

    toJSON(): any {
        let result = super.toJSON();

        AdaptiveCards.setProperty(result, "id", this._id);
        return result;
    }

    parse(json: any, errors?: Array<AdaptiveCards.IValidationError>) {
        super.parse(json, errors);

        this.id = AdaptiveCards.getValueOrDefault(json["id"], "templateId");
    }

    updateLayout(processChildren: boolean = true) {
        super.updateLayout(processChildren);

        if (this.renderedElement) {
            //need to figure this
        }
    }

    renderSpeech(): string {
        return (AdaptiveCards.isNullOrEmpty(this.id) ? "Progress" : "whoAmi");
    }


    get templateString(): string {
        return this._templateString;
    }

    public set templateString(value: string) {
        let santitizedString = sanitizeHtml(value);

        if(santitizedString !== this._templateString) {
            this._templateString = santitizedString;
            this.updateLayout();
        }
    }
        */
}