. "$PSScriptRoot/dotnet-common.ps1"

$failedSolutions = @()

foreach ($sln in GetSolutions) {
    dotnet restore $sln
    
    if ($LASTEXITCODE -ne 0 ) { 
        $failedSolutions += $sln
    }
}

if ($failedSolutions.count -gt 0) {
    throw "Restore FAILED for solutions $failedSolutions"
}
