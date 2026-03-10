[CmdletBinding()]
param(
    [string]$Project = "BattleshipMaui.csproj",
    [string]$Configuration = "Release",
    [string]$Framework = "net10.0-windows10.0.19041.0",
    [string]$RuntimeIdentifier = "win-x64",
    [ValidateSet("Solo", "Lan")]
    [string]$AppFlavor = "Solo"
)

$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$projectPath = (Resolve-Path (Join-Path $repoRoot $Project)).Path

$metadataJson = & dotnet msbuild $projectPath -nologo "-p:AppFlavor=$AppFlavor" -getProperty:PublicAppName -getProperty:Version -getProperty:ApplicationTitle
if ($LASTEXITCODE -ne 0) {
    throw "Could not evaluate public release metadata for AppFlavor '$AppFlavor'."
}

$metadata = $metadataJson | ConvertFrom-Json
$appName = $metadata.Properties.PublicAppName
$version = $metadata.Properties.Version
$applicationTitle = $metadata.Properties.ApplicationTitle

if ([string]::IsNullOrWhiteSpace($appName) -or [string]::IsNullOrWhiteSpace($version)) {
    throw "Could not determine the public release metadata for $Project."
}

$releaseName = "$appName-v$version-$RuntimeIdentifier"
$artifactsRoot = Join-Path $repoRoot "artifacts\release"
$publishDir = Join-Path $artifactsRoot $releaseName
$zipPath = Join-Path $artifactsRoot "$releaseName.zip"
$checksumPath = Join-Path $artifactsRoot "$releaseName.sha256"
$startHerePath = Join-Path $publishDir "START_HERE.txt"
$exeName = "$appName.exe"

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
    "-p:AppFlavor=$AppFlavor",
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

$startHereText = if ($AppFlavor -eq "Lan") {
@"
$applicationTitle

1. Extract the full zip to a normal folder on each PC.
2. Open $exeName on both PCs.
3. If Windows shows a SmartScreen prompt for this unsigned build, choose More info > Run anyway.
4. Press F11 any time you want to toggle true full-screen mode on or off.
5. On one PC, leave the default port or choose another unused port and click Host LAN.
6. On the other PC, enter the host PC's LAN IP and the same port, then click Join LAN.
7. Both players place fleets locally. The host fires first after both fleets are ready.

Target platform: Windows 10/11 x64
"@
}
else {
@"
$applicationTitle

1. Extract the full zip to a normal folder.
2. Open $exeName.
3. If Windows shows a SmartScreen prompt for this unsigned build, choose More info > Run anyway.
4. Press F11 any time you want to toggle true full-screen mode on or off.
5. Start a new mission, place your fleet, and battle the onboard CPU opponent.

Target platform: Windows 10/11 x64
"@
}

$startHereText | Set-Content -Path $startHerePath -Encoding ascii

Compress-Archive -Path $publishDir -DestinationPath $zipPath -CompressionLevel Optimal

$hash = (Get-FileHash -Path $zipPath -Algorithm SHA256).Hash.ToLowerInvariant()
"$hash  $(Split-Path -Leaf $zipPath)" | Set-Content -Path $checksumPath -Encoding ascii

Write-Host "Created:"
Write-Host "  $zipPath"
Write-Host "  $checksumPath"

[PSCustomObject]@{
    AppFlavor = $AppFlavor
    AppName = $appName
    Version = $version
    ReleaseName = $releaseName
    ZipPath = $zipPath
    ChecksumPath = $checksumPath
}
