[CmdletBinding()]
param(
    [string]$Project = "BattleshipMaui.csproj",
    [string]$Configuration = "Release",
    [string]$Framework = "net10.0-windows10.0.19041.0",
    [string]$RuntimeIdentifier = "win-x64"
)

$ErrorActionPreference = "Stop"

$publishScript = Join-Path $PSScriptRoot "Publish-WindowsZip.ps1"

$soloRelease = & $publishScript -Project $Project -Configuration $Configuration -Framework $Framework -RuntimeIdentifier $RuntimeIdentifier -AppFlavor Solo
$lanRelease = & $publishScript -Project $Project -Configuration $Configuration -Framework $Framework -RuntimeIdentifier $RuntimeIdentifier -AppFlavor Lan

Write-Host ""
Write-Host "Published public releases:"
Write-Host "  $($soloRelease.ReleaseName)"
Write-Host "  $($soloRelease.ZipPath)"
Write-Host "  $($lanRelease.ReleaseName)"
Write-Host "  $($lanRelease.ZipPath)"
