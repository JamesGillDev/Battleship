# Battleship MAUI

Desktop Battleship game built with .NET MAUI.

## Current Release

- `v1.6.8`
- Public release build with command-center board-first UI and top-bar controls
- Animated board-view transitions (`Fire Control` / `Fleet Ops`)
- Manual fleet placement with right-click rotation + live placement preview
- Carrier sprite now renders substantially larger with intentional overlap styling

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
