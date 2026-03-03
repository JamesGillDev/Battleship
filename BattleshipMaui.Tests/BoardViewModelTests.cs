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
    public void GameOver_RevealsEnemyFleetSprites()
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
        Assert.True(vm.ShowEnemyFleet);
        Assert.All(vm.EnemyShipSprites, ship => Assert.True(ship.IsRevealed));
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
        Assert.Equal(0.4, sprite.Opacity, 3);
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
        public void Play(GameFeedbackCue cue, bool soundEnabled, bool hapticsEnabled, bool reduceMotion)
        {
        }
    }
}
