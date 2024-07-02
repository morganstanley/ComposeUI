import "bootstrap/dist/css/bootstrap.css";

window.addEventListener('load', async function () {
    intentListener = await window.fdc3.addIntentListener("StartChat", handleChatIntent);
    console.log("added intent listener");

    await window.fdc3.joinUserChannel("default");
    console.log("joined user channel");

});

async function handleChatIntent(context, contextMetada) {
    const channel = await window.fdc3.createPrivateChannel();
    return channel;
};
