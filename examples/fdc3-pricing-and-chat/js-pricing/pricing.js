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
    const pricingForm = document.querySelector("#pricing");
    await this.window.fdc3.joinUserChannel("default");
    pricingForm.addEventListener('submit', app.submitPrice);
});

window.app = function () {
    let channel = null;
    let listener = null;

    let remoteChannelSpan = document.querySelector("#remote-channel-status");
    let remoteListenerSpan = document.querySelector("#remote-listener-status");
    let localChannelSpan = document.querySelector("#local-channel-status");
    let localListenerSpan = document.querySelector("#local-listener-status");


    async function submitPrice(event) {
        event.preventDefault();

        if (channel == null) {
            const resolution = await window.fdc3.raiseIntent("StartChat", {
                type: 'fdc3.contact',
                name: 'Jane Doe',
                id: {
                    email: 'jane@mail.com'
                }
            });
            channel = await resolution.getResult();

            if (!channel
                || !channel.broadcast
                || !channel.onDisconnect
                || !channel.onUnsubscribe
                || !channel.onAddContextListener) {
                channel = null;
                remoteChannelSpan.textContent = "Intent result did not return a private channel"
                return;
            }

            remoteChannelSpan.textContent = "Connected";

            channel.onDisconnect(() => {
                remoteChannelSpan.textContent = "Disconnected";
            });
            // Could be the same, but in separate callback for demonstration purposes
            channel.onDisconnect(() => {
                disconnect();
            });

            // We are assuming a single subscriber from the chat app
            channel.onUnsubscribe((ctx) => {
                remoteListenerSpan.textContent = "Unsubscribed";
            });

            channel.onAddContextListener((ctx) => {
                remoteListenerSpan.textContent = "Subscribed (Type: " + ctx + ")";
            });
        }

        localChannelSpan.textContent = "Connected. Id: " + channel.id;

        const pricingForm = document.querySelector("#pricing");
        const formData = new FormData(pricingForm);

        const contact = {
            type: "fdc3.contact",
            name: formData.get("recipient")
        };

        const product = formData.get("product");
        const price = formData.get("price");
        channel.broadcast({
            type: "fdc3.chat.initSettings",
            chatName: "Pricing for " + product,
            members: {
                type: "fdc3.contactList",
                contacts: [{
                    contact
                }]
            },
            message: {
                type: "fdc3.message",
                text: {
                    "text/plain": `Hi ${contact.name},\n Your price for ${product} is ${price}.`
                }
            }
        });
    }

    function addListener() {
        if (!channel || listener) {
            return;
        }
        listener = channel.addContextListener("test", () => alert("Context received"));
        localListenerSpan.textContent = "Added";
    }

    function removeListener() {
        if (!listener) {
            return;
        }
        listener.unsubscribe();
        listener = null;
    }

    function disconnect() {
        channel.disconnect();
        channel = null;
        remoteChannelSpan.textContent = "Disconnected";
        remoteListenerSpan.textContent = "Not subscribed";
        localChannelSpan.textContent = "Disconnected";
        localListenerSpan.textContent = "Not added";
    }

    return {
        addListener: addListener,
        removeListener: removeListener,
        disconnect: disconnect,
        submitPrice: submitPrice
    }
}();