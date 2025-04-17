# Adds the Shell project's Debug output directory to the PATH environment variable 
# so that it can be invoked from command line and the node launcher

$path = [System.Environment]::GetEnvironmentVariable("PATH", [EnvironmentVariableTarget]::User)
$paths = $path -split ";"
$shellPath = Resolve-Path -Path "./src/Shell/bin/Debug/net8.0-windows"

if ($paths.indexof($shellPath.Path) -ne -1) { exit }

$paths += $shellPath.Path
$path = $paths -join ";"
[System.Environment]::SetEnvironmentVariable("PATH", $path, [EnvironmentVariableTarget]::User)
Write-Host "Appended '$($shellPath.Path)' to PATH"
