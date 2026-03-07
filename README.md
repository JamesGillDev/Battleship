# Battleship MAUI

[![.NET](https://img.shields.io/badge/.NET-10.0-blueviolet)](https://dotnet.microsoft.com/)
[![MAUI](https://img.shields.io/badge/Framework-.NET%20MAUI-0f6cbd)](https://learn.microsoft.com/dotnet/maui/)
[![Solo Release](https://img.shields.io/badge/BattleshipMaui-v1.7.1-2ea44f)](#versioning--releases)
[![LAN Release](https://img.shields.io/badge/LANBattleshipMAUI-v2.0.0-2ea44f)](#versioning--releases)
[![License](https://img.shields.io/badge/License-BSL%201.1-blue.svg)](./LICENSE.md)

A polished Battleship game built with .NET MAUI and a shared C# game core. This repository now publishes 2 separate Windows apps from the same codebase:

- `BattleshipMaui`: the original single-player release against the onboard CPU.
- `LANBattleshipMAUI`: a dedicated same-network multiplayer release for 2 Windows PCs on the same LAN.

## Versioning & Releases
- Current solo public app release: `BattleshipMaui v1.7.1`
- Current LAN public app release: `LANBattleshipMAUI v2.0.0`
- Release history and public-release notes: [CHANGELOG.md](./CHANGELOG.md)
- Tag mapping:
  - `v1.x.x` publishes the solo `BattleshipMaui` line.
  - `v2.x.x` publishes the LAN `LANBattleshipMAUI` line.
- Public distribution format: self-contained Windows `win-x64` zip

## Release Readiness
- `BattleshipMaui v1.7.1` is in **Public Release** status.
- `LANBattleshipMAUI v2.0.0` is in **Public Release** status.

## Product Lines

### BattleshipMaui
- Original single-player Battleship against the computer enemy
- Manual fleet placement with hover preview and right-click rotation
- Adjustable CPU difficulty from Settings
- Persistent stats, post-game recap, music, FX, and visual themes

### LANBattleshipMAUI
- Dedicated LAN release for 2 players on the same local network
- Host/join flow directly in the app header
- Private local fleet placement on both PCs
- Synced alternating turns, synced rematch via `New Mission`, and session disconnect support

## Tech Stack
- .NET 10
- .NET MAUI (Windows target currently configured)
- C# game engine in `Battleship.GameCore`
- xUnit test project in `BattleshipMaui.Tests`

## Project Structure
- `Battleship.GameCore/`: game-domain logic (`GameBoard`, ships, attacks, results)
- `ViewModels/`: gameplay state, turn loop, placement, AI, LAN session flow, stats persistence
- `Behaviors/`: UI animation behavior for reveal and board effects
- `MainPage.xaml`: full game UI
- `BattleshipMaui.Tests/`: unit tests for board rules, AI targeting, and LAN view-model flow

## Run Locally
1. Clone the repository.
2. Open in Visual Studio 2022+ with the MAUI workload installed.
3. Build and run the flavor you want.

CLI alternatives:

```powershell
dotnet build BattleshipMaui.sln
dotnet run --project BattleshipMaui.csproj
dotnet run --project BattleshipMaui.csproj -p:AppFlavor=Lan
```

- `dotnet run --project BattleshipMaui.csproj` launches the solo `BattleshipMaui` build.
- `dotnet run --project BattleshipMaui.csproj -p:AppFlavor=Lan` launches the dedicated `LANBattleshipMAUI` build.

## Publish Public Windows Zips
Publish one flavor:

```powershell
.\scripts\Publish-WindowsZip.ps1 -AppFlavor Solo
.\scripts\Publish-WindowsZip.ps1 -AppFlavor Lan
```

Publish both public releases together:

```powershell
.\scripts\Publish-PublicReleases.ps1
```

This creates:
- `artifacts\release\BattleshipMaui-v1.7.1-win-x64.zip`
- `artifacts\release\BattleshipMaui-v1.7.1-win-x64.sha256`
- `artifacts\release\LANBattleshipMAUI-v2.0.0-win-x64.zip`
- `artifacts\release\LANBattleshipMAUI-v2.0.0-win-x64.sha256`

After extracting the zips, launch:
- `BattleshipMaui-v1.7.1-win-x64\BattleshipMaui.exe`
- `LANBattleshipMAUI-v2.0.0-win-x64\LANBattleshipMAUI.exe`

The public zips are built as:
- `.NET self-contained`
- `WindowsAppSDKSelfContained=true`
- unpackaged Windows desktop output

## LAN Session Setup
Use the dedicated LAN build: `LANBattleshipMAUI v2.0.0`.

1. Put the same published LAN zip on both PCs.
2. Extract the zip on both PCs and launch `LANBattleshipMAUI.exe`.
3. Make sure both machines are on the same router or switch.
4. On the host PC:
   - Leave the default port `47652` or choose another unused port.
   - Note one of the LAN IPs shown in the header.
   - Click `Host LAN`.
5. On the joining PC:
   - Enter the host PC's LAN IP in `Host IP`.
   - Enter the same port number.
   - Click `Join LAN`.
6. Both players place fleets locally on their own PC.
7. The host fires the first shot after both fleets are ready.
8. Use `New Mission` for a synced rematch and `Disconnect` to close the LAN session.

If the connection fails:
- Make sure the host IP is the host PC's local network address, not a public internet IP.
- Allow `LANBattleshipMAUI.exe` through Windows Firewall on private networks if Windows prompts.
- Confirm both PCs are running the same published LAN build.

## Gameplay

### BattleshipMaui
1. Press `New Mission`.
2. Place all ships on `Your Fleet`.
3. Fire on `Enemy Waters`.
4. Alternate turns against the CPU until one fleet is sunk.
5. Adjust CPU difficulty in Settings when needed.

### LANBattleshipMAUI
1. Complete the steps in [LAN Session Setup](#lan-session-setup).
2. Place all ships on both PCs.
3. The host fires first.
4. Alternate turns until one fleet is sunk.
5. Use `New Mission` to start a synced rematch.

## Controls
- `New Mission`: resets boards and starts a new mission
- `Reset Stats`: clears saved cumulative stats
- `Host LAN`: starts listening on the selected port in `LANBattleshipMAUI`
- `Join LAN`: connects to the host PC by LAN IP and matching port in `LANBattleshipMAUI`
- `Disconnect`: closes the current LAN session in `LANBattleshipMAUI`
- `Theme Shift` and the theme picker: switch among 10 visual themes
- `Rotate` or right-click: rotates the selected ship during placement

## Testing
Run the default solution test pass:

```powershell
dotnet test BattleshipMaui.sln
```

Run the LAN-flavor test pass:

```powershell
dotnet test BattleshipMaui.Tests\BattleshipMaui.Tests.csproj -p:AppFlavor=Lan
```

Run the CI core subset:

```powershell
dotnet test BattleshipMaui.Tests\BattleshipMaui.Tests.csproj --filter "Category=Core9"
```

## CI
- Core tests workflow: `.github/workflows/core-tests.yml`
- Public Windows release workflow: `.github/workflows/windows-release.yml`
- Push `v1.x.x` tags for the solo public release.
- Push `v2.x.x` tags for the LAN public release.

## License
This project is licensed under the **Business Source License 1.1 (BSL)**.
See [LICENSE.md](./LICENSE.md) for full terms.

- Additional Use Grant: None
- Change Date: 2029-01-01
- Change License: Apache License 2.0

---
Battleship MAUI | Developed by JamesGillDev and contributors
