using Battleship.GameCore;
using BattleshipMaui.ViewModels;

namespace BattleshipMaui.Tests;

public class BoardViewModelTests
{
    [Fact]
    [Trait("Category", "Core9")]
    public void Constructor_StartsInPlacementPhase()
    {
        var vm = new BoardViewModel(
            new Random(7),
            new InMemoryGameStatsStore(),
            new InMemoryGameSettingsStore(GameSettingsSnapshot.Default with { HasSeenCommandBriefing = false }),
            new NoOpFeedbackService());

        Assert.Equal(100, vm.EnemyCells.Count);
        Assert.Equal(100, vm.PlayerCells.Count);
        Assert.Equal(5, vm.PlacementShips.Count);
        Assert.Empty(vm.PlayerShipSprites);
        Assert.True(vm.IsPlacementPhase);
        Assert.True(vm.CanPlaceShips);
        Assert.False(vm.CanFire);
        Assert.False(vm.ShowEnemyFleet);
        Assert.Equal(5, vm.EnemyShipSprites.Count);
        Assert.All(vm.EnemyShipSprites, ship => Assert.False(ship.IsRevealed));
        Assert.Equal(0, vm.CurrentGameTurns);
        Assert.Equal(0, vm.CurrentGameShots);
        Assert.Equal(0, vm.CurrentGameHits);
        Assert.Contains("No completed games yet.", vm.LastGameSummary);
        Assert.True(vm.IsOverlayVisible);
        Assert.Equal("Let's Fight!", vm.OverlayPrimaryActionText);
        Assert.Equal("Placement phase", vm.TurnMessage);
        Assert.Contains("Selected ship:", vm.PlacementSelectionMessage);
    }

    [Fact]
    public void Constructor_LoadsPersistedStats()
    {
        var store = new InMemoryGameStatsStore(new GameStatsSnapshot(
            Wins: 4,
            Losses: 2,
            Draws: 1,
            TotalTurns: 38,
            TotalShots: 54,
            TotalHits: 20));

        var vm = new BoardViewModel(new Random(9), store);

        Assert.Equal(4, vm.Wins);
        Assert.Equal(2, vm.Losses);
        Assert.Equal(1, vm.Draws);
        Assert.Equal(38, vm.TotalTurns);
        Assert.Equal(54, vm.TotalShots);
        Assert.Equal(20, vm.TotalHits);
        Assert.Equal(20d / 54d, vm.HitRate, 3);
        Assert.Contains("Record: 4-2", vm.StatsLine);
    }

    [Fact]
    public void Constructor_LoadsPersistedSettings()
    {
        var statsStore = new InMemoryGameStatsStore();
        var settingsStore = new InMemoryGameSettingsStore(new GameSettingsSnapshot(
            Difficulty: CpuDifficulty.Hard,
            AnimationSpeed: AnimationSpeed.Fast,
            SoundEnabled: false,
            HapticsEnabled: false,
            HighContrastMode: true,
            LargeTextMode: true,
            ReduceMotionMode: true,
            SettingsPanelOpen: false,
            HasSeenCommandBriefing: false));

        var vm = new BoardViewModel(new Random(10), statsStore, settingsStore, new NoOpFeedbackService());

        Assert.Equal(CpuDifficulty.Hard, vm.SelectedDifficulty);
        Assert.Equal(AnimationSpeed.Fast, vm.SelectedAnimationSpeed);
        Assert.False(vm.SoundEnabled);
        Assert.False(vm.HapticsEnabled);
        Assert.True(vm.HighContrastMode);
        Assert.True(vm.LargeTextMode);
        Assert.True(vm.ReduceMotionMode);
        Assert.False(vm.IsSettingsOpen);
    }

    [Fact]
    public void Constructor_DefaultsVolumesToTenPercent()
    {
        var vm = new BoardViewModel(
            new Random(1005),
            new InMemoryGameStatsStore(),
            new InMemoryGameSettingsStore(GameSettingsSnapshot.Default),
            new NoOpFeedbackService());

        Assert.Equal(0.10, vm.MusicVolume, 3);
        Assert.Equal(0.10, vm.SoundFxVolume, 3);
    }

    [Fact]
    public void Constructor_UpgradesLegacySettings_ToMusicEnabledByDefault()
    {
        var vm = new BoardViewModel(
            new Random(1001),
            new InMemoryGameStatsStore(),
            new InMemoryGameSettingsStore(GameSettingsSnapshot.Default with
            {
                MusicEnabled = false,
                HasConfiguredMusicPreference = false
            }),
            new NoOpFeedbackService());

        Assert.True(vm.MusicEnabled);
    }

    [Fact]
    public void Constructor_RespectsConfiguredMusicPreference()
    {
        var vm = new BoardViewModel(
            new Random(1003),
            new InMemoryGameStatsStore(),
            new InMemoryGameSettingsStore(GameSettingsSnapshot.Default with
            {
                MusicEnabled = false,
                HasConfiguredMusicPreference = true
            }),
            new NoOpFeedbackService());

        Assert.False(vm.MusicEnabled);
    }

    [Fact]
    public void EnemyCellTappedCommand_BeforePlacement_ShowsPlacementPrompt()
    {
        var vm = new BoardViewModel(new Random(11));
        var target = vm.EnemyCells[0];

        vm.EnemyCellTappedCommand.Execute(target);

        Assert.Equal("Place all ships on Your Fleet board before firing.", vm.StatusMessage);
        Assert.Equal(ShotMarkerState.None, target.MarkerState);
    }

    [Fact]
    public void DismissOverlayCommand_HidesOverlay()
    {
        var vm = new BoardViewModel(
            new Random(12),
            new InMemoryGameStatsStore(),
            new InMemoryGameSettingsStore(GameSettingsSnapshot.Default with { HasSeenCommandBriefing = false }),
            new NoOpFeedbackService());
        Assert.True(vm.IsOverlayVisible);

        vm.DismissOverlayCommand.Execute(null);

        Assert.False(vm.IsOverlayVisible);
    }

    [Fact]
    public void DismissOverlayCommand_FromIntroOverlay_StartsBackgroundMusic()
    {
        var musicService = new RecordingBackgroundMusicService();
        var vm = new BoardViewModel(
            new Random(120),
            new InMemoryGameStatsStore(),
            new InMemoryGameSettingsStore(GameSettingsSnapshot.Default with
            {
                HasSeenCommandBriefing = false,
                MusicEnabled = true,
                MusicVolume = 0.35,
                HasConfiguredMusicPreference = true
            }),
            new NoOpFeedbackService(),
            musicService);

        Assert.True(vm.IsOverlayVisible);
        Assert.Equal("Let's Fight!", vm.OverlayPrimaryActionText);
        Assert.False(musicService.LastEnabled);

        vm.DismissOverlayCommand.Execute(null);

        Assert.False(vm.IsOverlayVisible);
        Assert.True(musicService.LastEnabled);
        Assert.Equal(0.35, musicService.LastVolume, 3);
    }

    [Fact]
    public void PlayerCellTappedCommand_PlacesSelectedShip()
    {
        var vm = new BoardViewModel(new Random(13));
        var target = vm.PlayerCells[0];
        var selected = vm.PlacementShips.First(s => s.IsSelected);

        vm.PlayerCellTappedCommand.Execute(target);

        Assert.True(selected.IsPlaced);
        Assert.Single(vm.PlayerShipSprites);
        Assert.True(vm.IsPlacementPhase);
    }

    [Fact]
    public void PlayerCellTappedCommand_MarksPlacedCellsAsOccupied()
    {
        var vm = new BoardViewModel(new Random(15));
        var destroyer = vm.PlacementShips.Single(ship => ship.Name == "Destroyer");
        vm.SelectPlacementShipCommand.Execute(destroyer);

        vm.PlayerCellTappedCommand.Execute(vm.PlayerCells[0]);

        Assert.True(vm.PlayerCells[0].HasShip);
        Assert.True(vm.PlayerCells[1].HasShip);
        Assert.False(vm.PlayerCells[2].HasShip);
    }

    [Fact]
    public void RotatePlacementCommand_PlacesVerticalShip()
    {
        var vm = new BoardViewModel(new Random(17));
        vm.RotatePlacementCommand.Execute(null);
        vm.PlayerCellTappedCommand.Execute(vm.PlayerCells[0]);

        Assert.Single(vm.PlayerShipSprites);
        Assert.Equal(ShipAxis.Vertical, vm.PlayerShipSprites[0].Axis);
    }

    [Fact]
    [Trait("Category", "Core9")]
    public void CompletingPlacement_EnablesFiringAndBattleFlow()
    {
        var vm = new BoardViewModel(new Random(23));
        PlaceAllShips(vm);

        Assert.False(vm.IsPlacementPhase);
        Assert.True(vm.CanFire);
        Assert.Equal(5, vm.PlayerShipSprites.Count);

        var target = vm.EnemyCells[0];
        vm.EnemyCellTappedCommand.Execute(target);

        Assert.NotEqual(ShotMarkerState.None, target.MarkerState);
        Assert.StartsWith("Your last shot:", vm.PlayerLastShotMessage);
        Assert.StartsWith("Enemy last shot:", vm.EnemyLastShotMessage);
    }

    [Fact]
    public void UpdateEnemyHoverTarget_LocksAndClearsSingleEnemyCell()
    {
        var vm = new BoardViewModel(new Random(24));
        PlaceAllShips(vm);

        var target = vm.EnemyCells[12];
        var other = vm.EnemyCells[13];

        vm.UpdateEnemyHoverTarget(target);

        Assert.True(target.IsTargetLockVisible);
        Assert.False(other.IsTargetLockVisible);

        vm.ClearEnemyHoverTarget();

        Assert.False(target.IsTargetLockVisible);
        Assert.False(other.IsTargetLockVisible);
    }

    [Fact]
    public void EnemyHoverTarget_ClearsAfterCellResolvesToShotResult()
    {
        var vm = new BoardViewModel(new Random(25));
        PlaceAllShips(vm);

        var target = vm.EnemyCells[0];
        vm.UpdateEnemyHoverTarget(target);

        Assert.True(target.IsTargetLockVisible);

        vm.EnemyCellTappedCommand.Execute(target);

        Assert.NotEqual(ShotMarkerState.None, target.MarkerState);
        Assert.False(target.IsTargetLockVisible);
        Assert.DoesNotContain(vm.EnemyCells, cell => cell.IsTargetLockVisible);
    }

    [Fact]
    public void EnemyCellTappedCommand_SameCellTwiceShowsAlreadyFired()
    {
        var vm = new BoardViewModel(new Random(29));
        PlaceAllShips(vm);

        var target = vm.EnemyCells[5];
        vm.EnemyCellTappedCommand.Execute(target);
        int playerMarksAfterFirstTurn = vm.PlayerCells.Count(c => c.MarkerState != ShotMarkerState.None);

        vm.EnemyCellTappedCommand.Execute(target);

        Assert.Equal("You already fired at that cell.", vm.StatusMessage);
        Assert.Equal(playerMarksAfterFirstTurn, vm.PlayerCells.Count(c => c.MarkerState != ShotMarkerState.None));
    }

    [Fact]
    public void EnemyCellTappedCommand_ValidShotUpdatesAndSavesStats()
    {
        var store = new InMemoryGameStatsStore();
        var vm = new BoardViewModel(new Random(30), store);
        PlaceAllShips(vm);

        vm.EnemyCellTappedCommand.Execute(vm.EnemyCells[0]);

        Assert.Equal(1, vm.TotalTurns);
        Assert.Equal(1, vm.TotalShots);
        Assert.InRange(vm.TotalHits, 0, 1);
        Assert.True(store.SaveCount >= 1);
        Assert.Equal(vm.TotalTurns, store.Snapshot.TotalTurns);
        Assert.Equal(vm.TotalShots, store.Snapshot.TotalShots);
    }

    [Fact]
    public void ChangingDifficultyToEasy_MidGame_DoesNotForceImmediateDraw()
    {
        var vm = new BoardViewModel(new Random(301));
        PlaceAllShips(vm);

        vm.SelectedDifficulty = CpuDifficulty.Easy;
        vm.EnemyCellTappedCommand.Execute(vm.EnemyCells[0]);

        Assert.False(vm.IsGameOver);
        Assert.NotEqual("Draw", vm.TurnMessage);
        Assert.StartsWith("Enemy last shot:", vm.EnemyLastShotMessage);
        Assert.NotEqual("Enemy last shot: --", vm.EnemyLastShotMessage);
    }

    [Fact]
    public void HardMode_EnemyTurn_FiresAtMostOneShotPerTurn()
    {
        var vm = new BoardViewModel(
            new Random(302),
            new InMemoryGameStatsStore(),
            new InMemoryGameSettingsStore(GameSettingsSnapshot.Default with { Difficulty = CpuDifficulty.Hard }),
            new NoOpFeedbackService());

        PlaceShip(vm, "Aircraft Carrier", row: 4, col: 2, vertical: false);
        PlaceShip(vm, "Battleship", row: 5, col: 2, vertical: false);
        PlaceShip(vm, "Cruiser", row: 6, col: 2, vertical: false);
        PlaceShip(vm, "Submarine", row: 7, col: 2, vertical: false);
        PlaceShip(vm, "Destroyer", row: 8, col: 2, vertical: false);

        int attackedBeforeTurn = 0;
        bool sawEnemyHit = false;

        for (int turn = 0; turn < 16 && !vm.IsGameOver; turn++)
        {
            var target = vm.EnemyCells.First(cell => cell.MarkerState == ShotMarkerState.None);
            vm.EnemyCellTappedCommand.Execute(target);

            int attackedAfterTurn = vm.PlayerCells.Count(cell => cell.MarkerState != ShotMarkerState.None);
            Assert.InRange(attackedAfterTurn - attackedBeforeTurn, 0, 1);
            attackedBeforeTurn = attackedAfterTurn;

            sawEnemyHit |= vm.PlayerCells.Any(cell => cell.MarkerState is ShotMarkerState.Hit or ShotMarkerState.Sunk);
        }

        Assert.True(sawEnemyHit);
    }

    [Fact]
    [Trait("Category", "Core9")]
    public void NewGameCommand_ResetsToPlacementPhase()
    {
        var vm = new BoardViewModel(new Random(31));
        PlaceAllShips(vm);
        vm.EnemyCellTappedCommand.Execute(vm.EnemyCells[10]);

        vm.NewGameCommand.Execute(null);

        Assert.True(vm.IsPlacementPhase);
        Assert.True(vm.CanPlaceShips);
        Assert.False(vm.CanFire);
        Assert.Empty(vm.PlayerShipSprites);
        Assert.Equal(5, vm.EnemyShipSprites.Count);
        Assert.Equal(5, vm.PlacementShips.Count);
        Assert.False(vm.ShowEnemyFleet);
        Assert.All(vm.EnemyShipSprites, ship => Assert.False(ship.IsRevealed));
        Assert.All(vm.PlacementShips, s => Assert.False(s.IsPlaced));
        Assert.All(vm.EnemyCells, c => Assert.Equal(ShotMarkerState.None, c.MarkerState));
        Assert.All(vm.PlayerCells, c => Assert.Equal(ShotMarkerState.None, c.MarkerState));
        Assert.All(vm.EnemyCells, c => Assert.False(c.HasShip));
        Assert.All(vm.PlayerCells, c => Assert.False(c.HasShip));
        Assert.Equal("Your last shot: --", vm.PlayerLastShotMessage);
        Assert.Equal("Enemy last shot: --", vm.EnemyLastShotMessage);
    }

    [Fact]
    public void GameOver_OnlyRevealsDestroyedEnemyShips()
    {
        var vm = new BoardViewModel(new Random(37));
        PlaceAllShips(vm);

        foreach (var cell in vm.EnemyCells)
        {
            vm.EnemyCellTappedCommand.Execute(cell);
            if (vm.IsGameOver)
                break;
        }

        Assert.True(vm.IsGameOver);
        Assert.All(vm.EnemyShipSprites.Where(ship => ship.IsRevealed), ship => Assert.True(ship.IsSunk));
    }

    [Fact]
    public void GameOver_PersistsSingleOutcomeInRecord()
    {
        var store = new InMemoryGameStatsStore();
        var vm = new BoardViewModel(new Random(39), store);
        PlaceAllShips(vm);

        for (int turn = 0; turn < vm.EnemyCells.Count && !vm.IsGameOver; turn++)
        {
            var target = vm.EnemyCells.First(cell => cell.MarkerState == ShotMarkerState.None);
            vm.EnemyCellTappedCommand.Execute(target);
        }

        Assert.True(vm.IsGameOver);
        int totalOutcomes = vm.Wins + vm.Losses + vm.Draws;
        Assert.Equal(1, totalOutcomes);
        Assert.DoesNotContain("No completed games yet.", vm.LastGameSummary);
        Assert.Contains("Accuracy by phase:", vm.AnalyticsAccuracyByPhase);
        Assert.Contains("Streaks:", vm.AnalyticsStreaks);
        Assert.Equal(totalOutcomes, store.Snapshot.Wins + store.Snapshot.Losses + store.Snapshot.Draws);
    }

    [Fact]
    public void ResetStatsCommand_ClearsCumulativeAndCurrentStats()
    {
        var store = new InMemoryGameStatsStore(new GameStatsSnapshot(
            Wins: 3,
            Losses: 1,
            Draws: 1,
            TotalTurns: 45,
            TotalShots: 45,
            TotalHits: 18));

        var vm = new BoardViewModel(new Random(41), store);
        PlaceAllShips(vm);
        vm.EnemyCellTappedCommand.Execute(vm.EnemyCells[0]);
        Assert.True(vm.TotalShots > 0);

        vm.ResetStatsCommand.Execute(null);

        Assert.Equal(0, vm.Wins);
        Assert.Equal(0, vm.Losses);
        Assert.Equal(0, vm.Draws);
        Assert.Equal(0, vm.TotalTurns);
        Assert.Equal(0, vm.TotalShots);
        Assert.Equal(0, vm.TotalHits);
        Assert.Equal(0, vm.CurrentGameTurns);
        Assert.Equal(0, vm.CurrentGameShots);
        Assert.Equal(0, vm.CurrentGameHits);
        Assert.Equal("Stats reset.", vm.LastGameSummary);
        Assert.Equal(0, store.Snapshot.TotalShots);
    }

    [Fact]
    public void ShipSpriteVm_TracksRevealAndSunkState()
    {
        var sprite = new ShipSpriteVm(
            "Destroyer",
            "destroyer_2_pegs.png",
            startRow: 3,
            startCol: 4,
            length: 2,
            axis: ShipAxis.Horizontal,
            isEnemy: true,
            isRevealed: false);

        Assert.False(sprite.IsRevealed);
        Assert.False(sprite.IsSunk);
        Assert.Equal(0, sprite.Opacity, 3);

        sprite.MarkSunk();
        Assert.True(sprite.IsSunk);
        Assert.Equal(0, sprite.Opacity, 3);

        sprite.Reveal();
        Assert.True(sprite.IsRevealed);
        Assert.Equal(0.74, sprite.Opacity, 3);
    }

    [Fact]
    public void ShipSpriteVm_UsesPerShipImageScaleProfiles()
    {
        var destroyer = new ShipSpriteVm("Destroyer", "destroyer_2_pegs.png", 0, 0, 2, ShipAxis.Horizontal);
        var carrier = new ShipSpriteVm("Aircraft Carrier", "aircraft_carrier_5_pegs.png", 0, 0, 5, ShipAxis.Horizontal);
        var battleship = new ShipSpriteVm("Battleship", "battleship_4_pegs.png", 0, 0, 4, ShipAxis.Horizontal);
        var submarine = new ShipSpriteVm("Submarine", "submarine_3_pegs.png", 0, 0, 3, ShipAxis.Horizontal);

        Assert.True(destroyer.ImageScale > 1);
        Assert.True(carrier.ImageScale > 1);
        Assert.True(submarine.ImageScale > 1);
        Assert.NotEqual(destroyer.ImageScale, carrier.ImageScale);
        Assert.True(carrier.ImageScale > battleship.ImageScale);
    }

    [Fact]
    public void ShipSpriteVm_UsesOrientationSpecificScaleProfiles()
    {
        var battleshipHorizontal = new ShipSpriteVm("Battleship", "battleship_4_pegs.png", 0, 0, 4, ShipAxis.Horizontal);
        var battleshipVertical = new ShipSpriteVm("Battleship", "battleship_4_pegs.png", 0, 0, 4, ShipAxis.Vertical);
        var cruiserHorizontal = new ShipSpriteVm("Cruiser", "cruiser_3_pegs.png", 0, 0, 3, ShipAxis.Horizontal);
        var cruiserVertical = new ShipSpriteVm("Cruiser", "cruiser_3_pegs.png", 0, 0, 3, ShipAxis.Vertical);

        Assert.True(battleshipVertical.ImageScale > battleshipHorizontal.ImageScale);
        Assert.True(cruiserVertical.ImageScale > cruiserHorizontal.ImageScale);
    }

    [Fact]
    public void ShipSpriteVm_UsesImageAlignmentProfiles()
    {
        var battleshipHorizontal = new ShipSpriteVm("Battleship", "battleship_4_pegs.png", 0, 0, 4, ShipAxis.Horizontal);
        var battleshipVertical = new ShipSpriteVm("Battleship", "battleship_4_pegs.png", 0, 0, 4, ShipAxis.Vertical);

        Assert.NotEqual(0, Math.Round(battleshipHorizontal.ImageTranslationY, 2));
        Assert.NotEqual(0, Math.Round(battleshipVertical.ImageTranslationX, 2));
        Assert.InRange(Math.Abs(battleshipHorizontal.ImageTranslationY), 1.0, 6.0);
        Assert.InRange(Math.Abs(battleshipVertical.ImageTranslationX), 1.0, 6.0);
        Assert.NotEqual(
            Math.Round(battleshipHorizontal.ImageTranslationY, 2),
            Math.Round(battleshipVertical.ImageTranslationY, 2));
    }

    [Fact]
    public void ShipSpriteVm_AllShips_UseExtendedBoundsForGridOverlap()
    {
        var carrier = new ShipSpriteVm("Aircraft Carrier", "aircraft_carrier_5_pegs.png", 1, 1, 5, ShipAxis.Horizontal);
        var battleship = new ShipSpriteVm("Battleship", "battleship_4_pegs.png", 2, 2, 4, ShipAxis.Horizontal);
        var cruiser = new ShipSpriteVm("Cruiser", "cruiser_3_pegs.png", 2, 2, 3, ShipAxis.Horizontal);
        var submarine = new ShipSpriteVm("Submarine", "submarine_3_pegs.png", 3, 3, 3, ShipAxis.Horizontal);
        var destroyer = new ShipSpriteVm("Destroyer", "destroyer_2_pegs.png", 4, 4, 2, ShipAxis.Horizontal);

        double carrierBaseWidth = (5 * BoardViewModel.CellSize) - (2 * BoardViewModel.ShipVisualInset);
        double battleshipBaseWidth = (4 * BoardViewModel.CellSize) - (2 * BoardViewModel.ShipVisualInset);
        double cruiserBaseWidth = (3 * BoardViewModel.CellSize) - (2 * BoardViewModel.ShipVisualInset);
        double submarineBaseWidth = (3 * BoardViewModel.CellSize) - (2 * BoardViewModel.ShipVisualInset);
        double destroyerBaseWidth = (2 * BoardViewModel.CellSize) - (2 * BoardViewModel.ShipVisualInset);

        Assert.True(carrier.Bounds.Width > carrierBaseWidth);
        Assert.True(battleship.Bounds.Width > battleshipBaseWidth);
        Assert.True(cruiser.Bounds.Width > cruiserBaseWidth);
        Assert.True(submarine.Bounds.Width > submarineBaseWidth);
        Assert.True(destroyer.Bounds.Width > destroyerBaseWidth);
    }

    [Fact]
    public void LanMatch_HostStartsBattleAfterBothFleetsDeploy()
    {
        var lanService = new FakeLanSessionService();
        var vm = CreateLanViewModel(lanService, seed: 601);

        vm.SetMatchModeCommand.Execute("Lan");
        vm.HostLanCommand.Execute(null);
        lanService.SimulateConnected(LanRole.Host);

        PlaceAllShips(vm);

        Assert.False(vm.IsPlacementPhase);
        Assert.Single(lanService.SentFleets);
        Assert.False(vm.IsPlayerTurn);

        lanService.SimulateReceiveFleet(BuildRemoteFleetPackets());

        Assert.True(vm.IsLanMode);
        Assert.True(vm.IsLanConnected);
        Assert.True(vm.IsPlayerTurn);
        Assert.Equal("Your turn", vm.TurnMessage);
        Assert.Equal(5, vm.EnemyShipSprites.Count);
        Assert.All(vm.EnemyShipSprites, ship => Assert.False(ship.IsRevealed));
    }

    [Fact]
    public void LanMatch_RemoteShotResultUpdatesEnemyBoardAndStats()
    {
        var lanService = new FakeLanSessionService();
        var vm = CreateLanViewModel(lanService, seed: 602);

        vm.SetMatchModeCommand.Execute("Lan");
        vm.HostLanCommand.Execute(null);
        lanService.SimulateConnected(LanRole.Host);
        PlaceAllShips(vm);
        lanService.SimulateReceiveFleet(BuildRemoteFleetPackets());

        var target = vm.EnemyCells[0];
        vm.EnemyCellTappedCommand.Execute(target);

        Assert.Single(lanService.SentShots);
        Assert.Equal(new BoardCoordinate(0, 0), lanService.SentShots[0]);
        Assert.Equal("Shot in flight", vm.TurnMessage);

        lanService.SimulateReceiveShotResult(new LanShotResultPacket(
            Row: 0,
            Col: 0,
            Result: AttackResult.Miss,
            IsHit: false,
            SunkShipName: null,
            Message: "Miss!",
            GameOver: false));

        Assert.Equal(ShotMarkerState.Miss, target.MarkerState);
        Assert.Equal(1, vm.CurrentGameTurns);
        Assert.Equal(1, vm.CurrentGameShots);
        Assert.Equal(1, vm.TotalTurns);
        Assert.Equal("Opponent turn", vm.TurnMessage);
        Assert.StartsWith("Your last shot:", vm.PlayerLastShotMessage);
    }

    private static void PlaceAllShips(BoardViewModel vm)
    {
        PlaceShip(vm, "Aircraft Carrier", row: 0, col: 0, vertical: false);
        PlaceShip(vm, "Battleship", row: 1, col: 0, vertical: false);
        PlaceShip(vm, "Cruiser", row: 2, col: 0, vertical: false);
        PlaceShip(vm, "Submarine", row: 3, col: 0, vertical: false);
        PlaceShip(vm, "Destroyer", row: 4, col: 0, vertical: false);
    }

    private static void PlaceShip(BoardViewModel vm, string shipName, int row, int col, bool vertical)
    {
        var shipVm = vm.PlacementShips.Single(s => s.Name == shipName);
        vm.SelectPlacementShipCommand.Execute(shipVm);

        if (vm.IsVerticalPlacement != vertical)
            vm.RotatePlacementCommand.Execute(null);

        vm.PlayerCellTappedCommand.Execute(vm.PlayerCells[row * BoardViewModel.Size + col]);
        Assert.True(shipVm.IsPlaced);
    }

    private static BoardViewModel CreateLanViewModel(FakeLanSessionService lanService, int seed)
    {
        return new BoardViewModel(
            new Random(seed),
            new InMemoryGameStatsStore(),
            new InMemoryGameSettingsStore(GameSettingsSnapshot.Default with { HasSeenCommandBriefing = false }),
            new NoOpFeedbackService(),
            new RecordingBackgroundMusicService(),
            lanService);
    }

    private static IReadOnlyList<ShipPlacementPacket> BuildRemoteFleetPackets()
    {
        return
        [
            new ShipPlacementPacket("Aircraft Carrier", 0, 0, ShipAxis.Horizontal, 5),
            new ShipPlacementPacket("Battleship", 1, 0, ShipAxis.Horizontal, 4),
            new ShipPlacementPacket("Cruiser", 2, 0, ShipAxis.Horizontal, 3),
            new ShipPlacementPacket("Submarine", 3, 0, ShipAxis.Horizontal, 3),
            new ShipPlacementPacket("Destroyer", 4, 0, ShipAxis.Horizontal, 2)
        ];
    }

    private sealed class InMemoryGameStatsStore : IGameStatsStore
    {
        public GameStatsSnapshot Snapshot { get; private set; }
        public int SaveCount { get; private set; }

        public InMemoryGameStatsStore(GameStatsSnapshot snapshot = default)
        {
            Snapshot = snapshot;
        }

        public GameStatsSnapshot Load()
        {
            return Snapshot;
        }

        public void Save(GameStatsSnapshot snapshot)
        {
            Snapshot = snapshot;
            SaveCount++;
        }
    }

    private sealed class InMemoryGameSettingsStore : IGameSettingsStore
    {
        public GameSettingsSnapshot Snapshot { get; private set; }
        public int SaveCount { get; private set; }

        public InMemoryGameSettingsStore(GameSettingsSnapshot snapshot)
        {
            Snapshot = snapshot;
        }

        public GameSettingsSnapshot Load()
        {
            return Snapshot;
        }

        public void Save(GameSettingsSnapshot settings)
        {
            Snapshot = settings;
            SaveCount++;
        }
    }

    private sealed class NoOpFeedbackService : IGameFeedbackService
    {
        public void Play(GameFeedbackCue cue, bool soundEnabled, double soundFxVolume, bool hapticsEnabled, bool reduceMotion, string? shipName = null)
        {
        }
    }

    private sealed class RecordingBackgroundMusicService : IBackgroundMusicService
    {
        public bool LastEnabled { get; private set; }
        public double LastVolume { get; private set; }

        public void ApplySettings(bool enabled, double volume)
        {
            LastEnabled = enabled;
            LastVolume = volume;
        }
    }

    private sealed class FakeLanSessionService : ILanSessionService
    {
        public event EventHandler<LanConnectionChangedEventArgs>? ConnectionChanged;
        public event EventHandler<LanPayloadReceivedEventArgs>? PayloadReceived;

        public LanConnectionState State { get; private set; } = LanConnectionState.Disconnected;
        public LanRole Role { get; private set; } = LanRole.None;
        public string? RemoteEndpoint { get; private set; }

        public List<IReadOnlyList<ShipPlacementPacket>> SentFleets { get; } = new();
        public List<BoardCoordinate> SentShots { get; } = new();
        public List<LanShotResultPacket> SentShotResults { get; } = new();
        public int SentResetCount { get; private set; }

        public IReadOnlyList<string> GetLocalAddresses()
        {
            return ["192.168.1.55"];
        }

        public Task StartHostingAsync(int port, CancellationToken cancellationToken = default)
        {
            State = LanConnectionState.Hosting;
            Role = LanRole.Host;
            RemoteEndpoint = null;
            RaiseConnectionChanged();
            return Task.CompletedTask;
        }

        public Task JoinAsync(string host, int port, CancellationToken cancellationToken = default)
        {
            State = LanConnectionState.Connecting;
            Role = LanRole.Guest;
            RemoteEndpoint = $"{host}:{port}";
            RaiseConnectionChanged();
            return Task.CompletedTask;
        }

        public Task SendFleetAsync(IReadOnlyList<ShipPlacementPacket> fleet, CancellationToken cancellationToken = default)
        {
            SentFleets.Add(fleet.ToArray());
            return Task.CompletedTask;
        }

        public Task SendShotAsync(BoardCoordinate shot, CancellationToken cancellationToken = default)
        {
            SentShots.Add(shot);
            return Task.CompletedTask;
        }

        public Task SendShotResultAsync(LanShotResultPacket shotResult, CancellationToken cancellationToken = default)
        {
            SentShotResults.Add(shotResult);
            return Task.CompletedTask;
        }

        public Task SendResetAsync(CancellationToken cancellationToken = default)
        {
            SentResetCount++;
            return Task.CompletedTask;
        }

        public Task DisconnectAsync(CancellationToken cancellationToken = default)
        {
            State = LanConnectionState.Disconnected;
            Role = LanRole.None;
            RemoteEndpoint = null;
            RaiseConnectionChanged();
            return Task.CompletedTask;
        }

        public void SimulateConnected(LanRole role, string remoteEndpoint = "192.168.1.91:47652")
        {
            State = LanConnectionState.Connected;
            Role = role;
            RemoteEndpoint = remoteEndpoint;
            RaiseConnectionChanged();
        }

        public void SimulateReceiveFleet(IReadOnlyList<ShipPlacementPacket> fleet)
        {
            PayloadReceived?.Invoke(this, LanPayloadReceivedEventArgs.ForFleet(fleet));
        }

        public void SimulateReceiveShotResult(LanShotResultPacket shotResult)
        {
            PayloadReceived?.Invoke(this, LanPayloadReceivedEventArgs.ForShotResult(shotResult));
        }

        private void RaiseConnectionChanged(string? errorMessage = null)
        {
            ConnectionChanged?.Invoke(this, new LanConnectionChangedEventArgs(
                State,
                Role,
                GetLocalAddresses(),
                RemoteEndpoint,
                errorMessage));
        }
    }
}
