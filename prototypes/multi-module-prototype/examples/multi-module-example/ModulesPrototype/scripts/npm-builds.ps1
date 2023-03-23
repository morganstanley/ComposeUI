# Building the MessageRouter-web-client, because without it the chart couldn't be started.
cd $PSScriptRoot\..\..\..\..\..\..
npx lerna run build --scope '{@morgan-stanley/composeui-messaging-client,@morgan-stanley/composeui-example-chart}'
