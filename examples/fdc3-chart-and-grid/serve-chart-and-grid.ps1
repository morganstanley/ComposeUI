Push-Location .
Set-Location $PSScriptRoot\..
npm i
npx nx run-many --target=build --projects=@morgan-stanley/composeui-example-chart,@morgan-stanley/composeui-example-grid --maxParallel=100
npx nx run-many --target=start --projects=@morgan-stanley/composeui-example-chart,@morgan-stanley/composeui-example-grid --maxParallel=100
Pop-Location
