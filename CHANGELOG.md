# Changelog

All notable changes to this project are documented in this file.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/)
and this project uses [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
