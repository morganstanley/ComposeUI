. "$PSScriptRoot/dotnet-common.ps1"

$failedSolutions = @()

foreach ($sln in GetSolutions) {
    dotnet test $sln --no-build --configuration Release --verbosity normal --collect:"XPlat Code Coverage"
    
    if ($LASTEXITCODE -ne 0 ) { 
        $failedSolutions += $sln
    }
}

if ($failedSolutions.count -gt 0) {
    throw "Build FAILED for solutions $failedSolutions"
}
