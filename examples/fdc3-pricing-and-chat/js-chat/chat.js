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