Push-Location .
Set-Location $PSScriptRoot\..\..
npm i
npx lerna run build --stream --scope "{@morgan-stanley/composeui-example-chart-messagerouter,@morgan-stanley/composeui-example-grid-messagerouter}"
npx lerna run start --stream --scope "{@morgan-stanley/composeui-example-chart-messagerouter,@morgan-stanley/composeui-example-grid-messagerouter}"
Pop-Location
