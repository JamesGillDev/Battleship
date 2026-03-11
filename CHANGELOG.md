# Changelog

All notable changes to this project are documented in this file.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/)
and this project uses [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [2.2.6] - 2026-03-11

### Changed
- Corrected the LAN startup presentation so the intro cards now render as a true full-screen layer with no visible dashboard or welcome overlay behind them.
- Replaced the broken LAN VS Code intro artwork with the packaged official VS Code logo so the second startup card renders cleanly.

### Release
- `v2.2.6` is in **Public Release** status.
- Public release distribution is a self-contained Windows `win-x64` zip named `LANBattleshipMAUI-v2.2.6-win-x64.zip`.

## [2.2.5] - 2026-03-11

### Changed
- Randomized the LAN defeat outcome callout so `Lost.wav` and `Enemy_Won.wav` are now selected at random instead of playing in a fixed order.
- Randomized the LAN win and draw outcome callouts across the supplied `War_Over.wav`, `Victory!!.wav`, and `Victory.wav` pool instead of replaying a fixed clip sequence.

### Release
- `v2.2.5` is in **Public Release** status.
- Public release distribution is a self-contained Windows `win-x64` zip named `LANBattleshipMAUI-v2.2.5-win-x64.zip`.

## [1.9.6] - 2026-03-11

### Changed
- Corrected the solo startup presentation so the intro cards now render as a true full-screen layer with no visible dashboard or welcome overlay behind them.
- Replaced the broken solo VS Code intro artwork with the packaged official VS Code logo so the second startup card renders cleanly.

### Release
- `v1.9.6` is in **Public Release** status.
- Public release distribution is a self-contained Windows `win-x64` zip named `BattleshipMaui-v1.9.6-win-x64.zip`.

## [1.9.5] - 2026-03-11

### Changed
- Randomized the solo defeat outcome callout so `Lost.wav` and `Enemy_Won.wav` are now selected at random instead of playing in a fixed order.
- Randomized the solo win and draw outcome callouts across the supplied `War_Over.wav`, `Victory!!.wav`, and `Victory.wav` pool instead of replaying a fixed clip sequence.

### Release
- `v1.9.5` is in **Public Release** status.
- Public release distribution is a self-contained Windows `win-x64` zip named `BattleshipMaui-v1.9.5-win-x64.zip`.

## [2.2.4] - 2026-03-11

### Changed
- Corrected the LAN startup/title presentation so the intro now covers the full window with a black background and shows the packaged `vs_code.png` logo card.
- Expanded LAN commander audio routing to use the supplied `User-Player_Vessel_Destroyed.wav`, `Victory!!.wav`, `Victory.wav`, `Lost.wav`, `Enemy_Won.wav`, and `War_Over.wav` clips while ducking music to `2%` during gameplay audio playback.

### Release
- `v2.2.4` is in **Public Release** status.
- Public release distribution is a self-contained Windows `win-x64` zip named `LANBattleshipMAUI-v2.2.4-win-x64.zip`.

## [1.9.4] - 2026-03-11

### Changed
- Shortened solo CPU strike analysis to `2-4` seconds and replaced the giant center reticle with a floating thinking bubble while a roaming board target lock sweeps the player fleet before committing with the supplied `Target_Locked.wav` cue.
- Corrected the solo startup/title presentation so the intro now covers the full window with a black background and shows the packaged `vs_code.png` logo card.
- Expanded solo commander audio routing to use the supplied `User-Player_Vessel_Destroyed.wav`, `Victory!!.wav`, `Victory.wav`, `Lost.wav`, `Enemy_Won.wav`, and `War_Over.wav` clips while ducking music to `2%` during gameplay audio playback.

### Release
- `v1.9.4` is in **Public Release** status.
- Public release distribution is a self-contained Windows `win-x64` zip named `BattleshipMaui-v1.9.4-win-x64.zip`.

## [2.2.3] - 2026-03-10

### Changed
- Background music now ducks to `5%` while the LAN commander hit, miss, and destroyed voice lines play, then restores the selected music volume after playback.

### Release
- `v2.2.3` is in **Public Release** status.
- Public release distribution is a self-contained Windows `win-x64` zip named `LANBattleshipMAUI-v2.2.3-win-x64.zip`.

## [1.9.3] - 2026-03-10

### Changed
- Background music now ducks to `5%` while the solo commander hit, miss, and destroyed voice lines play, then restores the selected music volume after playback.

### Release
- `v1.9.3` is in **Public Release** status.
- Public release distribution is a self-contained Windows `win-x64` zip named `BattleshipMaui-v1.9.3-win-x64.zip`.

## [2.2.2] - 2026-03-10

### Changed
- Added a `Quit` action to the `Esc` command menu so the LAN build can close directly from the in-game command console.

### Release
- `v2.2.2` is in **Public Release** status.
- Public release distribution is a self-contained Windows `win-x64` zip named `LANBattleshipMAUI-v2.2.2-win-x64.zip`.

## [1.9.2] - 2026-03-10

### Changed
- Added a `Quit` action to the `Esc` command menu so the solo build can close directly from the in-game command console.

### Release
- `v1.9.2` is in **Public Release** status.
- Public release distribution is a self-contained Windows `win-x64` zip named `BattleshipMaui-v1.9.2-win-x64.zip`.

## [2.2.1] - 2026-03-10

### Changed
- Replaced the LAN startup narration with the exact supplied `Echo_startup.wav`, `VS_Code_startup.wav`, and `Title.wav` files.
- Replaced the LAN commander callouts with the exact supplied `Direct_Hit.wav`, `Target_Missed.wav`, and `Enemy_Vessel_Destroyed.wav` files.
- Swapped the second startup card artwork to the official VS Code logo asset.

### Release
- `v2.2.1` is in **Public Release** status.
- Public release distribution is a self-contained Windows `win-x64` zip named `LANBattleshipMAUI-v2.2.1-win-x64.zip`.

## [1.9.1] - 2026-03-10

### Changed
- Replaced the solo startup narration with the exact supplied `Echo_startup.wav`, `VS_Code_startup.wav`, and `Title.wav` files.
- Replaced the solo commander callouts with the exact supplied `Direct_Hit.wav`, `Target_Missed.wav`, and `Enemy_Vessel_Destroyed.wav` files.
- Swapped the second startup card artwork to the official VS Code logo asset.

### Release
- `v1.9.1` is in **Public Release** status.
- Public release distribution is a self-contained Windows `win-x64` zip named `BattleshipMaui-v1.9.1-win-x64.zip`.

## [2.2.0] - 2026-03-10

### Added
- Added a full cinematic startup sequence for `LANBattleshipMAUI` with voiced title cards, `Esc` skip support, and a final fade into gameplay.
- Added an `Esc` command menu that now hosts the former settings controls plus mission actions during play.
- Added packaged WAV voice clips for startup narration and commander hit/miss callouts.

### Changed
- The LAN release now starts in borderless full-screen mode by default.
- Replaced the WinUI `KeyboardAccelerator` path for `F11` with raw key handling so the stray `F11` bubble no longer appears during gameplay.
- Removed the settings button from the main dashboard and moved those options into the `Esc` command menu.

### Release
- `v2.2.0` is in **Public Release** status.
- Public release distribution is a self-contained Windows `win-x64` zip named `LANBattleshipMAUI-v2.2.0-win-x64.zip`.

## [1.9.0] - 2026-03-10

### Added
- Added a full cinematic startup sequence for `BattleshipMaui` with voiced title cards, `Esc` skip support, and a final fade into gameplay.
- Added an `Esc` command menu that now hosts the former settings controls plus mission actions during play.
- Added packaged WAV voice clips for startup narration and commander hit/miss callouts.

### Changed
- The solo release now starts in borderless full-screen mode by default.
- Replaced the WinUI `KeyboardAccelerator` path for `F11` with raw key handling so the stray `F11` bubble no longer appears during gameplay.
- Removed the settings button from the main dashboard and moved those options into the `Esc` command menu.

### Release
- `v1.9.0` is in **Public Release** status.
- Public release distribution is a self-contained Windows `win-x64` zip named `BattleshipMaui-v1.9.0-win-x64.zip`.

## [2.1.1] - 2026-03-10

### Added
- Added an `F11` keyboard shortcut that toggles `LANBattleshipMAUI` into and out of true Windows full-screen mode.

### Release
- `v2.1.1` is in **Public Release** status.
- Public release distribution is a self-contained Windows `win-x64` zip named `LANBattleshipMAUI-v2.1.1-win-x64.zip`.

## [1.8.1] - 2026-03-10

### Added
- Added an `F11` keyboard shortcut that toggles `BattleshipMaui` into and out of true Windows full-screen mode.

### Release
- `v1.8.1` is in **Public Release** status.
- Public release distribution is a self-contained Windows `win-x64` zip named `BattleshipMaui-v1.8.1-win-x64.zip`.

## [2.1.0] - 2026-03-10

### Added
- Added an optional LAN turn-cinematic strike overlay that shows incoming and outgoing target coordinates before the shot resolves.
- Added a LAN hit intel bubble that appears after animated hit confirmations without interrupting miss flow.
- Added commander-style spoken combat callouts for hit, miss, and ship-destroyed outcomes in the LAN release.

### Changed
- Increased board presentation size during combat so LAN matches devote most of the window to the 2 gameboards.
- Collapsed the distracting top command sections during live gameplay into a compact combat header while keeping the full command deck available before combat.
- Reduced LAN placement-board sizing and status-card pressure so the placement phase no longer hides rows `I` and `J`.

### Release
- `v2.1.0` is in **Public Release** status.
- Public release distribution is a self-contained Windows `win-x64` zip named `LANBattleshipMAUI-v2.1.0-win-x64.zip`.

## [1.8.0] - 2026-03-10

### Added
- Added a default single-player turn-cinematic strike overlay to give CPU exchanges more suspense and readability.
- Added commander-style spoken combat callouts for hit, miss, and ship-destroyed outcomes in the solo release.

### Changed
- Increased board presentation size during combat so the solo app uses the window primarily for the enemy and player boards.
- Collapsed the top command sections during live gameplay into a compact combat header to reduce visual clutter.

### Release
- `v1.8.0` is in **Public Release** status.
- Public release distribution is a self-contained Windows `win-x64` zip named `BattleshipMaui-v1.8.0-win-x64.zip`.

## [2.0.0] - 2026-03-07

### Added
- Added a dedicated `LANBattleshipMAUI` public release line so LAN multiplayer now ships as its own Windows app instead of sharing the solo public build.
- Added flavor-aware publish and release packaging so the repository can produce separate `v1.x.x` solo and `v2.x.x` LAN public zips from the same codebase.

### Changed
- Updated the app shell, command header, and startup briefing so the LAN build launches directly into host/join LAN flow with no mixed-mode switch in the published UI.
- Kept the existing LAN synchronization, fleet exchange, turn flow, and rematch behavior inside the dedicated LAN release line.

### Release
- `v2.0.0` is in **Public Release** status.
- Public release distribution is a self-contained Windows `win-x64` zip named `LANBattleshipMAUI-v2.0.0-win-x64.zip`.

## [1.7.1] - 2026-03-07

### Changed
- Restored `BattleshipMaui` as the dedicated original single-player public release line for player-vs-CPU gameplay.
- Updated the shared app shell, README, release workflow, and publish scripts so `v1.x.x` tags now map cleanly to the solo build while `v2.x.x` tags map to the LAN build.

### Release
- `v1.7.1` is in **Public Release** status.
- Public release distribution is a self-contained Windows `win-x64` zip named `BattleshipMaui-v1.7.1-win-x64.zip`.

## [1.7.0] - 2026-03-07

### Added
- Added LAN multiplayer for two Windows builds on the same local network, including host/join commands, fleet exchange, synchronized turns, and rematch reset propagation.
- Added explicit in-app and README setup instructions so players can reliably start a LAN session from the published `.exe`.

### Changed
- Updated the command-deck header to expose `Solo vs CPU` and `LAN Match` modes with live connection status, host IP entry, port entry, and connect/disconnect controls.
- Extended automated coverage with LAN view-model tests for host startup, fleet synchronization, and remote shot resolution.

### Release
- `v1.7.0` is in **Public Release** status.
- Public release distribution remains a self-contained Windows `win-x64` zip, now including LAN host/join support for same-network play.

## [1.6.27] - 2026-03-06

### Changed
- Rebuilt enemy targeting to use probability-weighted hunt scoring, stronger hit-cluster resolution, and better remaining-ship awareness instead of relying on mostly random easy-mode queues.
- Improved both `Easy` and `Hard` AI accuracy while preserving difficulty separation, with hard mode now winning on smarter shot selection rather than extra salvos.
- Added a self-contained Windows `win-x64` release packaging flow that produces a downloadable zip with the unpackaged desktop executable and its runtime dependencies.

### Fixed
- Removed the hard-mode bonus-shot path so the enemy now fires exactly once per turn on every difficulty.
- Preserved enemy targeting knowledge when difficulty is recalibrated midgame by priming the strategy from the current player board state.

### Release
- `v1.6.27` is in **Public Release** status.
- Public release distribution now uses a self-contained Windows zip so players can extract the download and launch `BattleshipMaui.exe` immediately.
- Tagged GitHub releases now upload the versioned `.zip` and matching `.sha256` checksum automatically.

## [1.6.26] - 2026-03-05

### Changed
- Retuned sunk-smoke rendering to use denser board plumes, larger haze layers, and stronger ember glow so destroyed ships read more clearly during play.
- Increased ship-level sunk smoke opacity and drift so revealed sunk sprites keep a more visible smoke column without altering the stable board layout.

### Release
- `v1.6.26` is marked ready for **public release**.

## [1.6.25] - 2026-03-05

### Changed
- Retuned ship image alignment to use softer visual-fit offsets derived from the art assets, keeping overlays closer to the reference fleet screenshots.

### Fixed
- Reduced overcorrection in ship-art centering so player ships, enemy ship reveals, and placement preview render with more natural positioning on the board.

### Release
- `v1.6.25` is marked ready for **public release**.

## [1.6.24] - 2026-03-05

### Changed
- Expanded the pre-impact targeting animation into a `3x3` acquisition pulse that contracts toward the selected strike cell.
- Corrected ship-art alignment using per-image content offsets so enemy ship reveals and player overlays use the same centered composition.

### Fixed
- Player cinematic shots now keep the target-acquisition indicator visible briefly before resolving to hit or miss, matching the enemy pre-fire lock behavior.
- Enemy ship image overlays now match the player fleet's fit more closely instead of reading slightly offset during reveal.

### Release
- `v1.6.24` is marked ready for **public release**.

## [1.6.23] - 2026-03-05

### Changed
- Added an enemy-board hover target state so desktop pointer movement can preview the next shot location before the player clicks.
- Tuned the aircraft carrier sprite profile down slightly so its board footprint and placement preview stay large without feeling oversized.

### Fixed
- Added a pulsing circular pre-impact lock indicator that appears before a shot resolves to hit or miss, including both player hover targeting and enemy lock-on beats.
- Preserved the stable board renderer path while extending the targeting overlay behavior.

### Release
- `v1.6.23` is marked ready for **public release**.

## [1.6.22] - 2026-03-05

### Changed
- Deepened the board ocean renderer with stronger subsurface contouring, crest-shadow passes, specular light sweeps, and heavier bevel shading so each grid reads with more 3D depth.
- Added continuous drifting smoke to every sunk board cell so destroyed ships keep smoking across all occupied blocks instead of only at the sprite layer.

### Fixed
- Switched hit blasts to the real `Resources/Images/explosion.png` artwork through the board overlay renderer.
- Kept the richer board VFX path on top of the stable WinUI startup-safe rendering pipeline.

### Release
- `v1.6.22` is marked ready for **public release**.

## [1.6.21] - 2026-03-05

### Changed
- Reintroduced advanced board visuals through board-level renderers that animate layered ocean motion, caustics, and beveled cell shading without returning to the old per-cell layout tree.
- Restored macro board-frame wave drift on top of the new board surface renderer for a stronger 3D water effect.

### Fixed
- Restored visible hit explosions and miss splash markers on both boards using lightweight overlay rendering that remains stable on WinUI startup.
- Preserved the Windows startup fix while bringing the richer board visuals back.

### Release
- `v1.6.21` is marked ready for **public release**.

## [1.6.20] - 2026-03-05

### Changed
- Replaced the Windows board rendering path with fixed `10x10` grid hosts for enemy cells, player cells, and impact overlays so board layout is deterministic at startup.
- Simplified per-cell board visuals on Windows to stable fill/stroke state cues instead of large per-cell animated overlay trees.

### Fixed
- Resolved the real WinUI `LayoutCycleException` behind the black-window startup failure, so the published Windows `.exe` now launches without swallowing the exception.
- Removed the temporary WinUI `LayoutCycleException` handler from the Windows app startup path after verifying the real fix.

### Release
- `v1.6.20` is marked ready for **public release**.

## [1.6.19] - 2026-03-05

### Added
- Added local crash logging for startup failures under `%LOCALAPPDATA%\BattleshipMaui\logs\crash.log`.

### Fixed
- Prevented the published Windows app from exiting immediately on launch by handling the WinUI `LayoutCycleException` that was terminating startup.

### Release
- `v1.6.19` is marked ready for **public release**.

## [1.6.18] - 2026-03-05

### Changed
- Board presentation was upgraded to an ocean-water look with animated wave shimmer layers inside each board frame.
- Added a dedicated `BoardWaterFlowAnimationBehavior` so wave motion stays visual-only and respects existing animation speed/reduce-motion settings.
- Enhanced board-cell surface lighting for a richer watery depth while preserving ship/marker overlay ordering and gameplay layout geometry.

### Release
- `v1.6.18` is marked ready for **public release**.

## [1.6.17] - 2026-03-05

### Fixed
- Removed duplicate `Theme` wording in the top command bar so the control label appears only once.
- Updated theme dropdown sizing/alignment so its visible control height matches the adjacent `Theme Shift` button.

### Release
- `v1.6.17` is marked ready for **public release**.

## [1.6.16] - 2026-03-05

### Fixed
- Updated both board card grids to include symmetric spill rails so ship overhang can render on all 4 sides (left, top, right, bottom) without distorting board/cell layout.
- Kept ship overlays and placement preview unclipped at board layer boundaries to preserve edge-overhang rendering consistency.

### Changed
- Increased submarine sprite scale in both orientations so submarine visual size better matches the other ship classes.

### Release
- `v1.6.16` is marked ready for **public release**.

## [1.6.15] - 2026-03-05

### Fixed
- Removed board-edge clipping of ship overhang by allowing board host containers to render outside bounds while preserving board dimensions.
- Ship bow/stern/side overlap now renders cleanly when placed on outer rows/columns (both enemy and player boards), without shifting layout.

### Release
- `v1.6.15` is marked ready for **public release**.

## [1.6.14] - 2026-03-05

### Fixed
- Removed remaining ship sprite clipping on both boards by rendering ship imagery outside the bordered hull container, eliminating bow/stern and side cut-off.
- Applied the same no-clip rendering path to placement preview so previewed ships match final placed visuals.
- Replaced transform-based ship image scaling with centered oversized image bounds, fixing persistent all-side clipping on WinUI while preserving ship overhang.

### Changed
- Retained enlarged `Aircraft Carrier` scale and full-ship overhang behavior after clipping fix.
- Retained submarine-hit underwater explosion playback at fixed `20%` volume.

### Release
- `v1.6.14` is marked ready for **public release**.

## [1.6.13] - 2026-03-05

### Changed
- Ship visual overhang tuning now applies to all ship classes (enemy and player), removing edge cut-off at bow/stern while preserving intentional overlap style.
- `Aircraft Carrier` was increased in visual scale for stronger board presence after overlap corrections.
- Submarine hit playback volume is now fixed at `20%` for `daviddumaisaudio-large-underwater-explosion-190270.mp3`.

### Fixed
- Enemy sunk ships now remain visible during player turns while keeping sunk reveal behavior intact.

### Release
- `v1.6.13` is marked ready for **public release**.

## [1.6.12] - 2026-03-05

### Changed
- Enemy targeting follow-up behavior was refined again so post-hit shots consistently bias nearby cells, with smarter directional prioritization preserved for `Hard`.
- Cruiser and Destroyer sprite bounds/placement preview bounds now include intentional end-bleed so ships can visually hang over neighboring grid squares instead of clipping at the bow/stern.

### Fixed
- Enemy sunk ship visibility now persists reliably during player turns by ensuring the revealed enemy ship layer remains above the enemy board cell layer.
- Confirmed submarine-hit audio routing now consistently uses `daviddumaisaudio-large-underwater-explosion-190270.mp3` whenever either side's submarine is struck.

### Release
- `v1.6.12` is marked ready for **public release**.

## [1.6.11] - 2026-03-05

### Changed
- Enemy targeting now scales more clearly by difficulty:
  - `Hard` now ranks adjacent follow-up targets by directional reach after successful hits for stronger ship-finishing behavior.
  - `Easy` still remains less aggressive, but now takes an immediate nearby follow-up shot after landing a hit.

### Fixed
- Reduced perceived shot-audio latency by moving effects to preloaded per-track playback on Windows and applying faster-start miss-clip timing while keeping randomized 4-track miss rotation with no immediate repeats.
- Enemy sunk ships are now easier to read with higher sunk opacity.
- Enemy ship explosions are now removed after sink resolution and replaced by continuous smoke animation over the sunk ship sprite.

### Release
- `v1.6.11` is marked ready for **public release**.

## [1.6.10] - 2026-03-05

### Fixed
- Restored enemy-hit explosion visibility on the `Your Fleet` board by rendering hit impact effects above player ship sprites.

### Release
- `v1.6.10` is marked ready for **public release**.

## [1.6.9] - 2026-03-05

### Added
- New animated miss marker treatment that renders each miss as a layered water-splash effect (foam core, ripple rings, and droplet spray) on both boards.

### Changed
- Increased hit-marker visual intensity with brighter blast glow and stronger impact timing so strike feedback is easier to read.
- Added a fixed `3` second player-shot reveal delay before enemy response during cinematic turn flow.

### Fixed
- Improved hit/miss readability across themes by increasing per-cell marker contrast during resolved shot states.

### Release
- `v1.6.9` is marked ready for **public release**.

## [1.6.8] - 2026-03-04

### Changed
- Increased `Aircraft Carrier` sprite scale significantly for both horizontal and vertical orientations so it renders larger on the fleet grid.

### Fixed
- Carrier placement preview and placed-board sprite now overlap grid lanes with the same stronger visual style used by `Battleship`.

### Release
- `v1.6.8` is marked ready for **public release**.

## [1.6.7] - 2026-03-04

### Changed
- Increased ship visual scale again for the top three ships: `Aircraft Carrier`, `Battleship`, and `Cruiser` (both orientations) for stronger in-grid visibility, allowing intentional visual overlap where needed.

### Fixed
- Placement preview and placed-board ship sizing now better match requested readability for the three largest ship classes.

### Release
- `v1.6.7` is marked ready for **public release**.

## [1.6.6] - 2026-03-04

### Changed
- Increased ship visual scale again for `Aircraft Carrier` and `Battleship` (both orientations) so placement better fills intended grid footprint.
- Reduced settings-panel background dimming so board state remains more visible while the command panel is open.

### Fixed
- Flame hit effect now runs as a continuous flicker loop while the cell remains in hit state (instead of a one-time pulse).
- Flame animation now reliably starts for hit cells even when board cells are rebound/recycled by the grid template.
- Flame glow intensity and footprint were tuned so the fire layer is clearly visible below the explosion overlay without breaking grid lock.

### Release
- `v1.6.6` is marked ready for **public release**.

## [1.6.5] - 2026-03-04

### Added
- Added `Sound FX Volume` slider in settings with persisted value support.
- Added animated flame layer for hit cells on both boards, rendered beneath the explosion marker and locked to the impacted grid cell.
- Added quarter-turn randomized explosion orientation (`0`, `90`, `180`, `270`) for hit markers.

### Changed
- Background music default volume now starts at `10%`.
- Sound FX default volume now starts at `10%`.
- Miss-splash audio randomization now guarantees no immediate repeat of the previously played miss clip.
- Ship scale tuning now supports orientation-specific profiles per ship class.

### Fixed
- Enemy ship sprites now reveal only after that ship is sunk (never before), and remain layered below the explosion overlay.
- Increased vertical ship sizing for `Aircraft Carrier`, `Battleship`, `Cruiser`, and `Submarine` while keeping `Destroyer` unchanged.
- Increased horizontal sizing for `Aircraft Carrier` and slightly for `Battleship`, while keeping `Cruiser`, `Submarine`, and `Destroyer` horizontal sizing aligned to requested targets.

### Release
- `v1.6.5` is marked ready for **public release**.

## [1.6.4] - 2026-03-04

### Changed
- Music now defaults to enabled for legacy settings that never explicitly set a music preference.
- Added a persisted `HasConfiguredMusicPreference` flag so explicit user music choices remain respected after the first migration.
- Retuned ship sprite rendering with per-ship scale profiles to better match each ship class silhouette on the board.

### Fixed
- Corrected undersized ship visuals on the board and placement preview while preserving existing overlay/grid alignment behavior.
- Ensured preview ship sizing matches placed ship sizing for `Aircraft Carrier`, `Battleship`, `Cruiser`, `Submarine`, and `Destroyer`.

### Release
- `v1.6.4` is marked ready for **public release**.

## [1.6.3] - 2026-03-04

### Added
- Integrated real ship-impact and miss-impact audio playback using packaged MP3 assets.
- Added ship-specific hit audio mapping:
  - `Destroyer`, `Aircraft Carrier`, `Cruiser`, `Battleship` -> `soundreality-explosion-fx-343683.mp3`
  - `Submarine` -> `daviddumaisaudio-large-underwater-explosion-190270.mp3`
- Added randomized miss splash/explosion audio rotation across:
  - `Waterside_Explosion_Water_Sound_Effects1.mp3`
  - `Waterside_Explosion_Water_Sound_Effects2.mp3`
  - `Waterside_Explosion_Water_Sound_Effects3.mp3`
  - `Waterside_Explosion_Water_Sound_Effects4.mp3`

### Changed
- Background music start behavior now gates playback until the player confirms the intro briefing with `Let's Fight!`.
- Expanded audio content packaging so all `Resources/Audio/*.mp3` files are copied to output/publish.
- Updated release metadata and version markers to `v1.6.3`.

### Fixed
- Corrected ship sprite warping by preserving image aspect ratio and rotating vertical ships instead of stretching.
- Corrected placement preview ship distortion using matching rotation/aspect behavior.
- Resolved board bottom cut-off pressure by allowing vertical page scrolling for gameplay content while keeping overlays fixed.
- Retuned board geometry by reducing cell size from `46` to `44` for better default fit.

### Release
- `v1.6.3` is marked ready for **public release**.

## [1.6.2] - 2026-03-03

### Changed
- Increased board cell sizing for larger, easier-to-read gameboards.
- Reduced enemy "Thinking" sequence duration to a single 2-7 second anticipation cycle.
- Music playback now re-triggers when the player dismisses the welcome overlay (`Let's Fight!`).

### Fixed
- Corrected vertical ship rendering so placed ships no longer appear partially clipped.
- Improved ship placement centering by tightening sprite inset and image fill behavior.
- Ensured enemy shot results remain visible on the player board by reducing ship overlay opacity.

### Release
- `v1.6.2` is marked ready for public release.

## [1.6.1] - 2026-03-03

### Changed
- Stabilized `Theme Shift` behavior so theme changes no longer alter board/cell geometry.
- Tuned enemy cinematic pacing to a single "Thinking" cycle lasting 5-10 seconds total.
- Removed player-shot cinematic command prompt delays for immediate fire resolution.

### Fixed
- Realigned both boards so ships, grid cells, and `A-J` / `1-10` markers stay synchronized.
- Corrected ship overlay sizing/centering so sprites match locked grid squares.
- Improved Windows board container normalization to prevent spacing drift in cell rendering.
- Hardened background music source resolution so autoplay reliably starts on app open.

### Release
- `v1.6.1` is marked ready for public release.

## [1.6.0] - 2026-03-03

### Added
- Live fleet placement preview on hover so ship position/orientation is visible before deployment.
- `Theme Shift` command button to cycle through all 10 themes from the main command header.
- Theme shape profiles (cards, cells, board frame, pegs, ship plates) for stronger per-theme visual identity.
- Welcome mission popup at app open with updated instructions and `Let's Fight!` action.

### Changed
- Reworked ship sprite layout math for strict cell alignment (horizontal and vertical).
- Updated player ship deployment animation to slide/fade in from outside board bounds on placement.
- Improved board rendering with layered frame depth and stronger 3D-style surface treatment.
- Music playback now fades in automatically on start and retains low default baseline (25%).
- Updated release metadata and README references for `v1.6.0`.

### Fixed
- Corrected off-center ship rendering during placement/gameplay board states.
- Reduced visual drift between ship overlay geometry and board grid coordinates.

### Release
- `v1.6.0` is marked ready for public release.

## [1.5.0] - 2026-03-03

### Added
- Ten-theme visual system with `RetroWave 80s` as the default preset.
- Main-page theme picker for rapid command-center skin switching.
- Cinematic "Thinking" transition stage with animated trailing dots and color-cycling command spinner.
- Looping in-game background music playback (`War_Music_Background_25_Volume.mp3`) with persisted settings.
- Music controls in Settings (on/off toggle + live volume slider).

### Changed
- Rethemed command header, transition card, and settings card to use dynamic palette tokens.
- Expanded settings layout to keep all toggle labels visible and aligned at all supported window sizes.
- Persisted game settings schema now includes selected theme, music enabled state, and music volume.
- Updated release metadata and README references for `v1.5.0`.

### Fixed
- Ensured published output includes the background music asset for local release builds.

## [1.4.1] - 2026-03-03

### Changed
- Updated project version metadata to `v1.4.1`.
- Updated README release badge/version references and public release readiness statement.

### Fixed
- Locked Command Center board row layout so the game boards stay in a fixed vertical position.
- Embedded the naval ship icon directly into the Windows EXE for consistent published app icon behavior.

## [1.4.0] - 2026-03-03

### Added
- New naval-ship app icon artwork and ocean-themed visual identity.
- Medium-large peg markers for miss states on both boards.
- Explicit release-readiness status for public release (`v1.4.0`).

### Changed
- Overhauled Command Center UI to a game-focused deck layout.
- Converted gameplay view to always-visible side-by-side boards with even two-column split.
- Retuned board sizing and typography for full board visibility without scrolling.
- Refreshed palette, card styling, and control bar presentation for a modern arcade feel.

### Fixed
- Removed single-board view behavior that hid one board during focus changes.
- Eliminated tiny miss-dot rendering in favor of readable peg markers.
- Resolved board clipping pressure by reducing cell size and tightening top-area spacing.

## [1.3.0] - 2026-02-27

### Added
- First-launch "Command Briefing" overlay with updated gameplay instructions.
- Right-click rotation support on fleet cells and ship tray cards.
- Richer sound cue sequences for miss/new game/placement events.

### Changed
- Reworked main screen into a board-focused command-center layout with a fixed top control bar.
- Increased board cell sizing to improve ship-grid alignment and readability.
- Clarified stat wording to distinguish lifetime turns from current mission stats.
- Added cinematic turn transition messaging to keep turn pacing readable.

### Fixed
- Removed scroll-container behavior that caused board clipping and awkward ship displacement.
- Updated settings persistence schema and tests for command-briefing visibility state.

## [1.2.0] - 2026-02-25

### Added
- New ship and combat image assets for gameplay polish.

### Changed
- Standardized image file naming in app resources.
- Updated release metadata to use a consistent semantic version (`1.2.0`).

### Fixed
- CI workflow reliability for MAUI workload setup and test execution.

## [1.1.0] - 2026-02-25

### Added
- Persistent stats panel with win/loss/draw and shot metrics.
- Expanded ship UI polish and feedback mechanics during play.

### Changed
- Improved turn flow, game settings behavior, and post-game UX.

## [1.0.0] - 2026-02-25

### Added
- First public playable release of Battleship MAUI.
- Manual fleet placement with rotation.
- Player-vs-CPU turn loop with hunt/target attack logic.
- End-game reveal behavior and core gameplay tests.
