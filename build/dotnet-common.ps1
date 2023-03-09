function GetSolutions {
    return Get-ChildItem -Recurse -Include *.sln |
    # TODO: Prevent dotnet solutions leaking into node_modules (eg. from process explorer backend)
    Where-Object { $_.FullName -notlike "*\node_modules\*" } |
    ForEach-Object { $_.FullName }
}
