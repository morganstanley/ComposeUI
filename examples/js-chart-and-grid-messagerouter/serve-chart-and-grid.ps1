Push-Location .
Set-Location $PSScriptRoot\..
npm i
npx nx run-many --target=build --projects=@morgan-stanley/composeui-example-chart-messagerouter,@morgan-stanley/composeui-example-grid-messagerouter --maxParallel=100
npx nx run-many --target=start --projects=@morgan-stanley/composeui-example-chart-messagerouter,@morgan-stanley/composeui-example-grid-messagerouter --maxParallel=100
Pop-Location
