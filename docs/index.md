# Battleship MAUI

Desktop Battleship game built with .NET MAUI.

## Current Public Releases

- `BattleshipMaui v1.9.11`
  - Dedicated single-player Windows build against the onboard CPU
- `LANBattleshipMAUI v2.2.11`
  - Dedicated same-network multiplayer Windows build for 2 PCs on the same LAN

Both releases are in **Public Release** status and ship as self-contained Windows `win-x64` zip downloads.

## Current Highlights

- `BattleshipMaui v1.9.11` now forces the native WinUI window to activate and show before and after the initial full-screen presenter switch so the published app no longer launches as a hidden background window.
- `LANBattleshipMAUI v2.2.11` now forces the native WinUI window to activate and show before and after the initial full-screen presenter switch so the published app no longer launches as a hidden background window.

## Quick Start

1. Clone the repo.
2. Install the .NET 10 SDK and MAUI workload.
3. Run `dotnet build BattleshipMaui.sln`.
4. Launch the flavor you want:
   - `dotnet run --project BattleshipMaui.csproj`
   - `dotnet run --project BattleshipMaui.csproj -p:AppFlavor=Lan`

## Publish Locally

Publish both public releases:

```powershell
.\scripts\Publish-PublicReleases.ps1
```

Publish a single flavor:

```powershell
.\scripts\Publish-WindowsZip.ps1 -AppFlavor Solo
.\scripts\Publish-WindowsZip.ps1 -AppFlavor Lan
```

Launch:

- `artifacts\release\BattleshipMaui-v1.9.11-win-x64\BattleshipMaui.exe`
- `artifacts\release\LANBattleshipMAUI-v2.2.11-win-x64\LANBattleshipMAUI.exe`

## GitHub Releases

- Push `v1.x.x` tags to publish the solo `BattleshipMaui` release.
- Push `v2.x.x` tags to publish the LAN `LANBattleshipMAUI` release.
- `.github/workflows/windows-release.yml` uploads the matching zip and `.sha256` checksum to the tagged release.

## Current CI Scope

GitHub Actions runs the `Category=Core9` test subset on each push and pull request.
