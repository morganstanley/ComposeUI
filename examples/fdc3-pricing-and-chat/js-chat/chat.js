import "bootstrap/dist/css/bootstrap.css";

window.addEventListener('load', async function () {
    intentListener = await window.fdc3.addIntentListener("startChat", handleChatIntent);
});

function handleChatIntent(context, contextMetada) {
    console.log("Chat intent received. Received context:", context, ", metadata:", contextMetada);
};
