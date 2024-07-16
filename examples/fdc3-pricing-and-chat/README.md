# FDC3 Private Channels example
## Pricing and Chat

This example serves as a demonstration for using the FDC3 Private Channel feature.
The base scenario to test is to open the "Pricing Example" app, fill out the form and click "Send". This will raise a startChat intent that can be handled by the JS Chat Example app - a fake chat app - by returning a private channel that listens to the fdc3.chat.initSettings context. The Pricing app will use this channel to send a context via the chat app, that will be displayed.

IMPORTANT: This example only serves as a demonstration on the usage of Private Channels, and it is by no means a correct example for a full-fledged chat scenario.

Both apps feature buttons at the button to control additional features of Private Channels. The Pricing app displays the channel and listener status at the bottom, while the Chat app displays informational messages based on events.