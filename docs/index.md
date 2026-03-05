# Battleship MAUI

Desktop Battleship game built with .NET MAUI.

## Current Release

- `v1.6.13`
- Public release build with command-center board-first UI and top-bar controls
- Difficulty-based enemy targeting now applies smarter near-hit follow-up logic (especially on `Hard`)
- Shot audio playback timing was tightened so hit/miss effects trigger faster while preserving 4-way randomized miss rotation
- Sunk enemy ships now retain higher visibility with continuous smoke and cleared explosion overlays
- Enemy sunk ships now remain reliably visible on `Enemy Waters` during player turns
- All ship visuals now overhang neighboring cells without bow/stern clipping, and carrier scale was increased
- Submarine-hit underwater audio now plays at a fixed 20% volume
- Manual fleet placement with right-click rotation + live placement preview
- Carrier sprite remains at larger overlap styling for improved fleet readability

## Quick Start

1. Clone the repo.
2. Install .NET 10 SDK and MAUI workload.
3. Run `dotnet build BattleshipMaui.sln`.
4. Run the app from Visual Studio or `dotnet run`.

## Publish Locally

```powershell
dotnet publish BattleshipMaui.csproj -c Release -f net10.0-windows10.0.19041.0 -r win-x64 --self-contained false
```

Launch:
`bin\Release\net10.0-windows10.0.19041.0\win-x64\publish\BattleshipMaui.exe`

## Current CI Scope

GitHub Actions runs the `Category=Core9` test subset (9 tests) on each push and pull request.
