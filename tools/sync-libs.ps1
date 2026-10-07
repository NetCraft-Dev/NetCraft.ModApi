#./tools/sync-libs.ps1 -KernelOutput <NetCraft.ServerExe bin/Release/net10.0>
param(
    [Parameter(Mandatory = $true)]
    [string]$KernelOutput
)

$ErrorActionPreference = "Stop"

$libs = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot "..\libs"))
New-Item -ItemType Directory -Path $libs -Force | Out-Null


$sources = @()
$sources += Get-ChildItem (Join-Path $KernelOutput "kernel") -Filter *.dll -ErrorAction Stop
foreach ($name in @("NetCraft.dll", "NetCraft.ModLoader.dll")) {
    $path = Join-Path $KernelOutput $name
    if (-not (Test-Path $path)) {
        throw "missing $name under $KernelOutput"
    }
    $sources += Get-Item $path
}

foreach ($source in $sources) {
    Copy-Item $source.FullName $libs -Force
}

Write-Host "synced $($sources.Count) reference assemblies to $libs"
