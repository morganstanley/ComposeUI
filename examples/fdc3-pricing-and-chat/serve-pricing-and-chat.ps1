Push-Location .
Set-Location $PSScriptRoot\..
npm i
npx lerna run build --stream --scope "{@morgan-stanley/composeui-example-pricing,@morgan-stanley/composeui-example-chat}"
npx lerna run start --stream --scope "{@morgan-stanley/composeui-example-pricing,@morgan-stanley/composeui-example-chat}"
Pop-Location
