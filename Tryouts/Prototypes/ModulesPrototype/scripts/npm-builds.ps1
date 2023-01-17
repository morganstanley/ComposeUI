# Building the MessageRouter-web-client, because without it the chart couldn't be started.
cd $PSScriptRoot\..\..\..\Messaging-JS
yarn build

cd $PSScriptRoot\..\..\..\Plugins\ApplicationPlugins\chart\src
npm ci
npm run build