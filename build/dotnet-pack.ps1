. "$PSScriptRoot/dotnet-common.ps1"

$failedSolutions = @()

foreach ($sln in GetSolutions) {
    dotnet pack $sln --configuration Release --no-build
    
    if ($LASTEXITCODE -ne 0 ) { 
        $failedSolutions += $sln
    }
}

if ($failedSolutions.count -gt 0) {
    throw "Build FAILED for solutions $failedSolutions"
}
