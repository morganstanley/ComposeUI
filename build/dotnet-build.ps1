. "$PSScriptRoot/dotnet-common.ps1"

$failedSolutions = @()

foreach ($sln in GetSolutions) {
    dotnet build $sln --configuration Release --no-restore
    
    if ($LASTEXITCODE -ne 0 ) { 
        $failedSolutions += $sln
    }
}

if ($failedSolutions.count -gt 0) {
    throw "Build FAILED for solutions $failedSolutions"
}
