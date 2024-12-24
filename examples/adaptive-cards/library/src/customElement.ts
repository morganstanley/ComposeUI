import * as AdaptiveCards from "adaptivecards";
import sanitizeHtml from "sanitize-html";

export class CustomTemplate extends AdaptiveCards.CardElement {
  private parser = new DOMParser();
  static readonly JsonTypeName = "AdaptiveHTML";

  //#region Schema
  static readonly templateStringProperty = new AdaptiveCards.StringProperty(
    AdaptiveCards.Versions.v1_0,
    "templateString"
  );

  @AdaptiveCards.property(CustomTemplate.idProperty)
  get templateString(): string {
    return this.getValue(CustomTemplate.templateStringProperty);
  }

  set templateString(value: string) {
    let santitizedString = sanitizeHtml(value);

    if (this.templateString !== santitizedString) {
      this.setValue(CustomTemplate.templateStringProperty, santitizedString);

      this.updateLayout();
    }
  }

  //#endregion

  private _templateElement?: HTMLElement;

  protected internalRender(): HTMLElement {
    let element = document.createElement("div");

    this._templateElement = document.createElement("div");

    this._templateElement.insertAdjacentHTML("afterbegin", this.templateString);

    element.append(this._templateElement);

    return element;
  }

  getJsonTypeName(): string {
    return CustomTemplate.JsonTypeName;
  }

  updateLayout(processChildren: boolean = true) {
    super.updateLayout(processChildren);

    this._templateElement = document.createElement("div");

    this._templateElement.insertAdjacentHTML("afterbegin", this.templateString);
  }
}
