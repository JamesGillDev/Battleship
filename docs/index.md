# Battleship MAUI

Desktop Battleship game built with .NET MAUI.

## Current Release

- `v1.6.23`
- Public release build with the verified Windows startup fix for the published `.exe`
- Windows board rendering now combines fixed `10x10` input grids with deeper 3D ocean surfaces, macro wave drift, and lightweight overlay effects
- Hit explosions now use the real `explosion.png` art on both boards without reintroducing the WinUI layout cycle
- Enemy hover targeting now shows a pulsing circular target indicator before firing
- Sunk ships now keep smoking across every sunk grid block while the revealed ship layer remains visible
- Aircraft carrier sizing is trimmed slightly to fit the board composition better
- WinUI startup exceptions are still logged to `%LOCALAPPDATA%\BattleshipMaui\logs\crash.log` for local diagnostics
- Difficulty-based enemy targeting now applies smarter near-hit follow-up logic (especially on `Hard`)
- Shot audio playback timing was tightened so hit/miss effects trigger faster while preserving 4-way randomized miss rotation
- Sunk enemy ships now retain higher visibility with continuous smoke and cleared explosion overlays
- Enemy sunk ships now remain reliably visible on `Enemy Waters` during player turns
- Ship sprites and placement preview now use centered oversized bounds to eliminate all-side clipping while preserving overhang and larger carrier styling
- Board containers now allow edge overhang rendering beyond board bounds without changing overall board layout
- Ship overhang now renders on all four board sides via symmetric spill rails around the board grid
- Submarine sprite size was increased to better match the visual scale of other ships
- Theme command bar now shows `Theme` only once, and the theme dropdown height matches the `Theme Shift` button
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
