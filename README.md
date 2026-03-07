# Battleship MAUI

[![.NET](https://img.shields.io/badge/.NET-10.0-blueviolet)](https://dotnet.microsoft.com/)
[![MAUI](https://img.shields.io/badge/Framework-.NET%20MAUI-0f6cbd)](https://learn.microsoft.com/dotnet/maui/)
[![Release](https://img.shields.io/badge/Release-v1.7.0-2ea44f)](#versioning--releases)
[![License](https://img.shields.io/badge/License-BSL%201.1-blue.svg)](./LICENSE.md)

A polished, fully playable Battleship game built with .NET MAUI and a C# game core.

## Versioning & Releases
- Current public app release version: `v1.7.0`
- Release history and iteration details: [CHANGELOG.md](./CHANGELOG.md)
- Recommended GitHub release tag format: `vMAJOR.MINOR.PATCH` (example: `v1.7.0`)
- Public release distribution format: self-contained Windows `win-x64` zip

## Release Readiness
- `v1.7.0` is in **Public Release** status.
- Version tags now produce a self-contained Windows `win-x64` zip so players can extract it and launch `BattleshipMaui.exe` immediately.

## Highlights
- `LAN Match` now lets two players run the published Windows `.exe` on separate PCs on the same local network, host/join a session, place fleets privately, and take alternating turns against each other.
- Windows published app startup is fixed with a stable fixed-grid board rendering path, so the local `.exe` no longer closes on launch.
- WinUI crash logging remains enabled at `%LOCALAPPDATA%\BattleshipMaui\logs\crash.log` for real unhandled startup/runtime failures.
- Board surfaces now use deeper animated ocean rendering with subsurface contours, crest-shadow layering, specular sweeps, and beveled 3D cell shading while keeping the stable startup fix.
- Enemy AI now uses stronger hunt scoring and better hit-cluster resolution on both `Easy` and `Hard`, while `Hard` no longer gets an illegal bonus shot after a hit.
- Hit explosions now use the real `Resources/Images/explosion.png` art in the board overlay pipeline, and miss splash markers remain renderer-driven instead of returning to the old per-cell visual tree.
- Enemy hover targeting now shows a `3x3` pulsing acquisition circle that converges onto the target, and the same pre-impact lock indicator is used during both player cinematic shots and enemy targeting beats.
- Sunk ships now emit denser, brighter smoke on every sunk grid block, and the ship-level smoke haze is more visible on revealed sunk sprites.
- Aircraft carrier sprite sizing was trimmed slightly so the carrier no longer overwhelms the board compared with the rest of the fleet.
- Ship image rendering now uses a restrained visual-fit alignment profile so player ships, enemy reveals, and placement preview stay closer to the reference board compositions instead of overcorrecting to raw asset bounds.
- Side-by-side `Enemy Waters` and `Your Fleet` boards keep `A-J` / `1-10` rails aligned with ship overlays.
- Manual fleet placement supports left-click deploy, right-click rotation, and live hover preview.
- Ten visual themes are available through both the `Theme Shift` button and the theme picker.
- Turn-based player-vs-CPU combat keeps the smarter near-hit follow-up targeting logic, especially on `Hard`.
- Enemy sunk-ship reveals persist cleanly on `Enemy Waters`.
- Ship sprites and placement preview keep their overhang/clipping fixes for full-length rendering at board edges.
- Background music and sound-FX controls remain available from the settings popup.
- Welcome/summary overlays, persistent stats, unit tests, and GitHub Actions CI remain part of the public release.

## Tech Stack
- .NET 10
- .NET MAUI (Windows target currently configured)
- C# game engine in `Battleship.GameCore`
- xUnit test project in `BattleshipMaui.Tests`

## Project Structure
- `Battleship.GameCore/`: game-domain logic (`GameBoard`, ships, attacks, results).
- `ViewModels/`: gameplay state, turn loop, placement, AI, stats persistence.
- `Behaviors/`: UI animation behavior for ship reveal/sunk effects.
- `MainPage.xaml`: full game UI (boards, overlays, controls, stats panel).
- `BattleshipMaui.Tests/`: unit tests for game board, AI targeting, and view model flow.

## Run Locally
1. Clone the repository.
2. Open in Visual Studio 2022+ with MAUI workload installed.
3. Build and run:
   - Startup project: `BattleshipMaui`
   - Target: Windows

CLI alternative:
```powershell
dotnet build BattleshipMaui.sln
dotnet run --project BattleshipMaui.csproj
```

Release publish for a public zip download:
```powershell
.\scripts\Publish-WindowsZip.ps1
```

This creates:
- `artifacts\release\BattleshipMaui-v1.7.0-win-x64.zip`
- `artifacts\release\BattleshipMaui-v1.7.0-win-x64.sha256`

The zip is built as:
- `.NET self-contained`
- `WindowsAppSDKSelfContained=true`
- unpackaged Windows desktop output

After extracting the zip, launch:
`BattleshipMaui-v1.7.0-win-x64\BattleshipMaui.exe`

Raw CLI equivalent:
```powershell
dotnet publish BattleshipMaui.csproj -c Release -f net10.0-windows10.0.19041.0 -r win-x64 --self-contained true -p:WindowsAppSDKSelfContained=true -p:PublishReadyToRun=false -p:PublishDir=artifacts\release\BattleshipMaui-v1.7.0-win-x64\
```

GitHub public release flow:
- Push a version tag such as `v1.7.0`.
- GitHub Actions runs `.github/workflows/windows-release.yml`.
- The workflow uploads the zip and SHA-256 file to the GitHub Release for that tag.

If startup fails on Windows, review:
`%LOCALAPPDATA%\BattleshipMaui\logs\crash.log`

## LAN Match Setup
1. Put the same published Windows build on both PCs. Both machines must be on the same local network.
2. Launch the app on both PCs and switch the header from `Solo vs CPU` to `LAN Match`.
3. On the host PC:
   - Leave the default port `47652` or choose another unused port.
   - Note one of the LAN IPs shown in the header.
   - Click `Host LAN`.
4. On the joining PC:
   - Enter the host PC's LAN IP in `Host IP`.
   - Enter the same port number.
   - Click `Join LAN`.
5. Place fleets on both PCs. The host takes the first shot once both fleets are deployed.
6. Use `New Mission` for a synced rematch and `Disconnect` to close the LAN session.

If the connection fails:
- Make sure both PCs are on the same router or switch.
- Make sure the host IP is the host PC's local address, not a public internet IP.
- Allow `BattleshipMaui.exe` through Windows Firewall on private networks if Windows prompts.

## Gameplay
1. Choose `Solo vs CPU` or `LAN Match`.
2. If using LAN, complete the steps in [LAN Match Setup](#lan-match-setup).
3. Press `New Mission`.
4. Place all ships on `Your Fleet`:
   - Select ship card.
   - Right-click on your fleet board (or use `Orientation`) to rotate.
   - Left-click board cells to place.
5. Fire on `Enemy Waters` by tapping a cell.
6. Alternate turns with the CPU or the connected LAN player until one fleet is sunk.
7. At game-over:
   - Result is shown.
   - Enemy fleet is revealed.
   - Stats are updated and saved.

## Controls
- `New Mission`: reset boards and begin placement. In LAN mode this also signals a synced rematch to the other PC.
- `Reset Stats`: clear saved cumulative stats.
- `Solo vs CPU` / `LAN Match`: switch between local AI play and same-network multiplayer.
- `Host LAN`: start listening on the selected port and share your local IP with the other player.
- `Join LAN`: connect to the host PC by LAN IP and matching port.
- `Disconnect`: close the current LAN session.
- `Theme` picker: switch among 10 visual themes from the command header.
- `Rotate` / right-click: switch ship placement orientation.
- `Fire Control` / `Fleet Ops`: set board focus while both boards remain visible.

## Testing
Run full local suite:
```powershell
dotnet test BattleshipMaui.sln
```

Run the CI core subset (exactly 9 tests):
```powershell
dotnet test BattleshipMaui.Tests/BattleshipMaui.Tests.csproj --filter "Category=Core9"
```

## CI
GitHub Actions workflow: `.github/workflows/core-tests.yml`
- Triggers on every push and PR.
- Runs the 9 tagged core tests (`Category=Core9`) on Windows.

## License
This project is licensed under the **Business Source License 1.1 (BSL)**.
See [LICENSE.md](./LICENSE.md) for full terms.

- Additional Use Grant: None
- Change Date: 2029-01-01
- Change License: Apache License 2.0

---
Battleship MAUI | Developed by JamesGillDev and contributors
