[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string] $Configuration = 'Release',
    [switch] $SkipTests
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$solution = Join-Path $repositoryRoot 'IisSqlConnectionInventory.slnx'
$offlineConfig = Join-Path $repositoryRoot 'NuGet.Offline.Config'
$toolManifest = Join-Path $repositoryRoot 'dotnet-tools.json'

Push-Location $repositoryRoot
try {
    dotnet restore $solution --configfile $offlineConfig
    if ($LASTEXITCODE -ne 0) { throw 'Solution offline restore failed.' }

    dotnet restore .\src\Inventory.IisCollector\Inventory.IisCollector.csproj -r win-x64 --configfile $offlineConfig
    if ($LASTEXITCODE -ne 0) { throw 'IIS Collector win-x64 offline restore failed.' }

    dotnet restore .\src\Inventory.SqlCollector\Inventory.SqlCollector.csproj -r win-x64 --configfile $offlineConfig
    if ($LASTEXITCODE -ne 0) { throw 'Database Collector win-x64 offline restore failed.' }

    dotnet tool restore --configfile $offlineConfig --tool-manifest $toolManifest
    if ($LASTEXITCODE -ne 0) { throw 'Local dotnet tools offline restore failed.' }

    dotnet build $solution -c $Configuration --no-restore
    if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }

    if (-not $SkipTests) {
        dotnet test $solution -c $Configuration --no-build --no-restore
        if ($LASTEXITCODE -ne 0) { throw 'Tests failed.' }
    }
}
finally {
    Pop-Location
}
