# 从一次内核构建的输出重新捞引用程序集
# 内核接口变动之后跑一次 让 libs/ 跟上 NetCraft 仓库
# 用法: ./tools/sync-libs.ps1 -KernelOutput <NetCraft.ServerExe 的 bin/Release/net10.0>
param(
    [Parameter(Mandatory = $true)]
    [string]$KernelOutput
)

$ErrorActionPreference = "Stop"

$libs = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot "..\libs"))
New-Item -ItemType Directory -Path $libs -Force | Out-Null

# kernel/ 下是内核子库 主库与加载器在输出根目录
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
