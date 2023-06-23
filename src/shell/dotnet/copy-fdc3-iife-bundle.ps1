# Copying the IIFE bundle of FDC3 to the shell' Preload folder
# so that it can be preloaded for the Embedded Browser


$bundle = "./src/shell/js/composeui-fdc3/dist/fdc3-iife-bundle.js"
$destPath = Resolve-Path -Path "./src/shell/dotnet/Shell"

$preloadFolder = New-Item -Path $destPath -Name "Preload" -ItemType "directory" -Force

Copy-Item $bundle -Destination $preloadFolder
