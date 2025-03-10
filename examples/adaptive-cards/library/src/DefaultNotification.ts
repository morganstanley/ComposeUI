// Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License.
import { Types } from "./Shared";
import { Notification } from "./Notification";
import DefaultStyles from "./styles/defaultStyles.css";

export default class DefaultNotification extends Notification {
  adaptiveTemplate: object;

  constructor(type: Types, adaptiveTemplate: object) {
    super(type);
    this.adaptiveTemplate = adaptiveTemplate;
    this.card.parse(this.adaptiveTemplate);
  }

  public render(): void {
    let newTab: WindowProxy;
    let result: HTMLElement | undefined;

    if (!window.opener) {
      const otherTab = window.open(document.URL, "_blank", "popup");

      if (otherTab) {
        newTab = otherTab;
        otherTab.name = `${this.count}`;
      }
    }

    newTab!.addEventListener("load", () => {
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

    this.card.onExecuteAction = function () {
      newTab.close();
    };
  }

  public renderStyles(doc: Document): void {
    const style = document.createElement("style");
    style.innerText = DefaultStyles;
    doc.head.appendChild(style);
  }
}
