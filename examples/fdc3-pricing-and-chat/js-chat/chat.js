/*
 * Morgan Stanley makes this available to you under the Apache License,
 * Version 2.0 (the "License"). You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0.
 *
 * See the NOTICE file distributed with this work for additional information
 * regarding copyright ownership. Unless required by applicable law or agreed
 * to in writing, software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
 * or implied. See the License for the specific language governing permissions
 * and limitations under the License.
 */

import "bootstrap/dist/css/bootstrap.css";

window.addEventListener('load', async function () {
    intentListener = await window.fdc3.addIntentListener("StartChat", handleChatIntent);

    await window.fdc3.joinUserChannel("default");
});

async function handleChatIntent(context, contextMetada) {
    const channel = await window.fdc3.createPrivateChannel();

    contextListener = await channel.addContextListener("fdc3.chat.initSettings",
        function (context, _) {
            addChatMessage(context.message.text["text/plain"]);
        }
    );

    return channel;
};

function addChatMessage(text) {
    var chatDiv = document.querySelector("#chat");
    var p = document.createElement("span");
    p.classList.add("text-bg-primary");
    p.classList.add("badge");
    p.classList.add("p3");
    p.textContent = text;

    chatDiv.appendChild(p);
}