[CmdletBinding()]
param(
    [string]$Project = "BattleshipMaui.csproj",
    [string]$Configuration = "Release",
    [string]$Framework = "net10.0-windows10.0.19041.0",
    [string]$RuntimeIdentifier = "win-x64"
)

$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$projectPath = (Resolve-Path (Join-Path $repoRoot $Project)).Path

[xml]$projectXml = Get-Content -Path $projectPath
$version = @($projectXml.Project.PropertyGroup.Version | Where-Object { $_ })[0]

if ([string]::IsNullOrWhiteSpace($version)) {
    throw "Could not determine the project version from $Project."
}

$releaseName = "BattleshipMaui-v$version-$RuntimeIdentifier"
$artifactsRoot = Join-Path $repoRoot "artifacts\release"
$publishDir = Join-Path $artifactsRoot $releaseName
$zipPath = Join-Path $artifactsRoot "$releaseName.zip"
$checksumPath = Join-Path $artifactsRoot "$releaseName.sha256"
$startHerePath = Join-Path $publishDir "START_HERE.txt"

New-Item -ItemType Directory -Path $artifactsRoot -Force | Out-Null

if (Test-Path $publishDir) {
    Remove-Item -Path $publishDir -Recurse -Force
}

if (Test-Path $zipPath) {
    Remove-Item -Path $zipPath -Force
}

if (Test-Path $checksumPath) {
    Remove-Item -Path $checksumPath -Force
}

$publishArgs = @(
    "publish",
    $projectPath,
    "-c", $Configuration,
    "-f", $Framework,
    "-r", $RuntimeIdentifier,
    "--self-contained", "true",
    "-p:WindowsAppSDKSelfContained=true",
    "-p:PublishReadyToRun=false",
    "-p:PublishDir=$publishDir\"
)

Write-Host "Publishing $releaseName..."
& dotnet @publishArgs

if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE."
}

Copy-Item -Path (Join-Path $repoRoot "README.md") -Destination (Join-Path $publishDir "README.md") -Force
Copy-Item -Path (Join-Path $repoRoot "LICENSE.md") -Destination (Join-Path $publishDir "LICENSE.md") -Force

@"
Battleship MAUI

1. Extract the full zip to a normal folder.
2. Open BattleshipMaui.exe.
3. If Windows shows a SmartScreen prompt for this unsigned build, choose More info > Run anyway.
4. For LAN play, put this same build on both PCs, choose LAN Match in the header, host on one PC, and join from the other using the host PC's LAN IP and the same port.

Target platform: Windows 10/11 x64
"@ | Set-Content -Path $startHerePath -Encoding ascii

Compress-Archive -Path $publishDir -DestinationPath $zipPath -CompressionLevel Optimal

$hash = (Get-FileHash -Path $zipPath -Algorithm SHA256).Hash.ToLowerInvariant()
"$hash  $(Split-Path -Leaf $zipPath)" | Set-Content -Path $checksumPath -Encoding ascii

Write-Host "Created:"
Write-Host "  $zipPath"
Write-Host "  $checksumPath"
