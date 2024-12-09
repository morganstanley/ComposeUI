import * as Adaptive from "adaptivecards";
import sanitizeHtml from 'sanitize-html';


export class customTemplate extends Adaptive.CardElement {
    private _id?: string;
    private _templateString!: string;
    private _templateElement?: HTMLElement;

    /*
    constructor(id:string,templateString?:string) {
        super();
        this._id = id;
        this._templateElement = document.createElement("div");
        if(templateString) {
            this._templateString = sanitizeHtml(templateString);
        } else {
            this._templateString = this._templateElement.outerHTML;
        }
        this._templateElement.id = this._id;
    }
        */


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

        Adaptive.setProperty(result, "id", this._id);
        return result;
    }

    parse(json: any, errors?: Array<Adaptive.IValidationError>) {
        super.parse(json, errors);

        this.id = Adaptive.getValueOrDefault(json["id"], "templateId");
    }

    updateLayout(processChildren: boolean = true) {
        super.updateLayout(processChildren);

        if (this.renderedElement) {
            //need to figure this
        }
    }

    renderSpeech(): string {
        return (Adaptive.isNullOrEmpty(this.id) ? "Progress" : "whoAmi");
    }


    get templateString(): string {
        return this._templateString;
    }

    set templateString(value: string) {
        let santitizedString = sanitizeHtml(value);

        if(santitizedString !== this._templateString) {
            this._templateString = santitizedString;
            this.updateLayout();
        }
    }
}