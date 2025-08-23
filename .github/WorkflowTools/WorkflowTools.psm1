#Requires -Version 7
#Requires -Modules Az.CognitiveServices, Az.Accounts, Az.Sql, Az.Storage, Az.CosmosDB

# dotsource/import the functions in a cross-platform way
$publicPath = Join-Path -Path $PSScriptRoot -ChildPath 'Public'
$publicFunctionsList = Get-ChildItem -Path $publicPath -Filter '*.ps1' -File -ErrorAction SilentlyContinue
$publicFunctionsList | ForEach-Object {
    . $_.FullName
}
