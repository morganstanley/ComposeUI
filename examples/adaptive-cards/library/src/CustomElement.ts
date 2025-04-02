// Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License.
import * as AdaptiveCards from "adaptivecards";
import DOMPurify from "dompurify";

export class CustomTemplate extends AdaptiveCards.CardElement {
  static readonly JsonTypeName = "AdaptiveHTML";

  static readonly templateStringProperty = new AdaptiveCards.StringProperty(
    AdaptiveCards.Versions.v1_0,
    "templateString"
  );

  @AdaptiveCards.property(CustomTemplate.idProperty)
  get templateString(): string {
    return this.getValue(CustomTemplate.templateStringProperty);
  }

  set templateString(value: string) {
    const santitizedString = DOMPurify.sanitize(value);

    if (this.templateString !== santitizedString) {
      this.setValue(CustomTemplate.templateStringProperty, santitizedString);

      this.updateLayout();
    }
  }

  private templateElement?: HTMLElement;

  protected internalRender(): HTMLElement {
    const element = document.createElement("div");
    this.templateElement = document.createElement("div");
    this.templateElement.insertAdjacentHTML("afterbegin", this.templateString);
    element.append(this.templateElement);
    return element;
  }

  getJsonTypeName(): string {
    return CustomTemplate.JsonTypeName;
  }

  updateLayout(processChildren: boolean = true): void {
    super.updateLayout(processChildren);

    this.templateElement = document.createElement("div");
    this.templateElement.insertAdjacentHTML("afterbegin", this.templateString);
  }
}
