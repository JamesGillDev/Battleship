# Battleship MAUI

[![.NET](https://img.shields.io/badge/.NET-10.0-blueviolet)](https://dotnet.microsoft.com/)
[![MAUI](https://img.shields.io/badge/Framework-.NET%20MAUI-0f6cbd)](https://learn.microsoft.com/dotnet/maui/)
[![Release](https://img.shields.io/badge/Release-v1.6.1-2ea44f)](#versioning--releases)
[![License](https://img.shields.io/badge/License-BSL%201.1-blue.svg)](./LICENSE.md)

A polished, fully playable Battleship game built with .NET MAUI and a C# game core.

## Versioning & Releases
- Current public app release version: `v1.6.1`
- Release history and iteration details: [CHANGELOG.md](./CHANGELOG.md)
- Recommended GitHub release tag format: `vMAJOR.MINOR.PATCH` (example: `v1.6.1`)

## Release Readiness
- `v1.6.1` is **ready for public release**.

## Highlights
- Full command-center visual overhaul with a game-first style.
- New default `RetroWave 80s` theme plus 9 additional high-contrast visual presets.
- Side-by-side board layout with even two-column split and no board scrolling.
- Main-page theme picker for instant style switching across 10 distinct themes.
- `Theme Shift` button for rapid cycling through all 10 presets with consistent board geometry.
- Focus cues for `Fire Control` and `Fleet Ops` while both boards stay visible.
- Enemy-only "Thinking" prompt sequence with a single 5-10 second anticipation cycle.
- Manual fleet placement (left-click place + right-click rotate).
- Live hover-based fleet placement preview before click-to-place.
- Board markers (`A-J`, `1-10`) now stay locked to grid cells with centered ship overlays.
- Player ship deployment animation now slides/fades in from outside board bounds.
- Turn-based player vs CPU combat.
- Smart CPU hunt/target strategy after hits.
- Ship image overlays with sunk/reveal animations.
- Medium-large peg markers for misses and animated impact markers for hits.
- Updated naval ship app icon and refreshed ocean-themed palette.
- Looping background battle music with settings for on/off and live volume adjustment.
- Background music now auto-fades in on start and defaults to 25% volume.
- Welcome mission popup now appears at app open with instructions and `Let's Fight!` action.
- First-launch "Command Briefing" overlay with gameplay instructions.
- End-game enemy fleet reveal.
- Coordinate labels (`A-J` and `1-10`) and accessibility hints.
- Persistent stats:
  - Wins, losses, draws
  - Lifetime turns, shots, hit rate
  - Current-game summary + last-game summary
- Unit tests and GitHub Actions CI.

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

Release publish + run `.exe` directly:
```powershell
dotnet publish BattleshipMaui.csproj -c Release -f net10.0-windows10.0.19041.0 -r win-x64 --self-contained false
```
Then launch:
`bin\Release\net10.0-windows10.0.19041.0\win-x64\publish\BattleshipMaui.exe`

## Gameplay
1. Press `New Game`.
2. Place all ships on `Your Fleet`:
   - Select ship card.
   - Right-click on your fleet board (or use `Orientation`) to rotate.
   - Left-click board cells to place.
3. Fire on `Enemy Waters` by tapping a cell.
4. Alternate turns with CPU until one fleet is sunk.
5. At game-over:
   - Result is shown.
   - Enemy fleet is revealed.
   - Stats are updated and saved.

## Controls
- `New Game`: reset boards and begin placement.
- `Reset Stats`: clear saved cumulative stats.
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
