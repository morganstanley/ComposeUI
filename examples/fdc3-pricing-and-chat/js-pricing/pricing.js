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
    pricingForm.addEventListener('submit', submitPrice);
});

async function submitPrice(event) {
    event.preventDefault();

    const resolution = await window.fdc3.raiseIntent("StartChat", {
        type: 'fdc3.contact',
        name: 'Jane Doe',
        id: {
            email: 'jane@mail.com'
        }
    });
    const result = await resolution.getResult();

    if (result && result.broadcast) {
        const pricingForm = document.querySelector("#pricing");
        const formData = new FormData(pricingForm);

        const contact = {
            type: "fdc3.contact",
            name: formData.get("recipient")
        };

        const product = formData.get("product");
        const price = formData.get("price");
        result.broadcast({
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
}

