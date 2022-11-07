# Building the MessageRouter-web-client, because without it the chart couldn't be started.
cd ..\..\Messaging\messaging-web-client\src
npm ci
npm run build

cd ..\..\..\Plugins\ApplicationPlugins\chart\src
npm ci
npm run build