// Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License.
import * as ToastNotification from "@morgan-stanley/adaptive-card-notification";

const cardTemplate =
  await ToastNotification.ToastNotification.createNotification({
    type: "info",
    templateID: "custom",
    htmlTemplate:
      "<p>This is a template that can have any html in it.</p> <ul><li>Like a list like this:</li> </ul>",
  });

const cardNoTemplate =
  await ToastNotification.ToastNotification.createNotification({
    type: "error",
    templateID: "default",
    data: { $root: { message: "I am a message sent by user." } },
  });

document.getElementById("custom").onclick = (event) => {
  cardTemplate.render();
};

document.getElementById("default").onclick = (event) => {
  cardNoTemplate.render();
};
