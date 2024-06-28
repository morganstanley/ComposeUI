import "bootstrap/dist/css/bootstrap.css";

window.addEventListener('load', function () {
    const pricingForm = document.querySelector("#pricing");
    pricingForm.addEventListener('submit', submitPrice);
    
});

async function submitPrice(event) {
    event.preventDefault();

    const resolution = await window.fdc3.raiseIntent("StartChat");
    const result = await resolution.getResult();

    if (result && result.broadcast) {
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
            members:{
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

