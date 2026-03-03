using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Battleship.GameCore;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Graphics;

namespace BattleshipMaui.ViewModels;

public class BoardViewModel : ObservableObject
{
    private readonly Random _random;
    private readonly IGameStatsStore _statsStore;
    private readonly IGameSettingsStore _settingsStore;
    private readonly IGameFeedbackService _feedbackService;
    private readonly Dictionary<string, Ship> _playerShipsByName = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, ShipSpriteVm> _playerSpritesByName = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, ShipSpriteVm> _enemySpritesByName = new(StringComparer.OrdinalIgnoreCase);
    private readonly Queue<BoardCoordinate> _easyEnemyShotQueue = new();
    private readonly List<PlayerShotRecord> _currentGameShotHistory = new();

    private GameBoard? _playerBoard;
    private GameBoard? _enemyBoard;
    private EnemyTargetingStrategy? _enemyTargetingStrategy;
    private PlacementShipVm? _selectedPlacementShip;

    private bool _isPlayerTurn;
    private bool _isGameOver;
    private bool _isPlacementPhase;
    private bool _isVerticalPlacement;
    private string _turnMessage = string.Empty;
    private string _statusMessage = string.Empty;
    private string _playerLastShotMessage = string.Empty;
    private string _enemyLastShotMessage = string.Empty;
    private bool _showEnemyFleet;
    private int _wins;
    private int _losses;
    private int _draws;
    private int _totalTurns;
    private int _totalShots;
    private int _totalHits;
    private int _currentGameTurns;
    private int _currentGameShots;
    private int _currentGameHits;
    private string _lastGameSummary = "No completed games yet.";
    private string _analyticsAccuracyByPhase = "Accuracy by phase: --";
    private string _analyticsStreaks = "Streaks: --";
    private string _analyticsBestSequence = "Best turn sequence: --";

    private bool _isSettingsOpen;
    private bool _isOverlayVisible;
    private bool _showOverlayRecap;
    private bool _showOverlayAnalytics;
    private string _overlayTitle = "Operation Start";
    private string _overlaySubtitle = "Place your fleet to begin the battle.";
    private string _overlayPrimaryActionText = "Deploy Fleet";

    private CpuDifficulty _selectedDifficulty = CpuDifficulty.Standard;
    private AnimationSpeed _selectedAnimationSpeed = AnimationSpeed.Normal;
    private bool _soundEnabled = true;
    private bool _hapticsEnabled = true;
    private bool _highContrastMode;
    private bool _largeTextMode;
    private bool _reduceMotionMode;
    private bool _hasSeenCommandBriefing;
    private BoardViewMode _boardViewMode = BoardViewMode.Enemy;
    private bool _isTurnTransitionActive;
    private string _turnTransitionMessage = string.Empty;
    private bool _isResolvingEnemyTurn;
    private bool _isResolvingPlayerShot;
    private int _gameSessionId;

    public const int Size = 10;
    public const double CellSize = 36;
    public const double BoardAxisRailSize = 20;
    public const double BoardRailSpacing = 4;
    public const double MissPegSize = 16;

    private static readonly ShipTemplate[] FleetTemplates =
    {
        new("Aircraft Carrier", 5, "aircraft_carrier_5_pegs.png"),
        new("Battleship", 4, "battleship_4_pegs.png"),
        new("Cruiser", 3, "cruiser_3_pegs.png"),
        new("Submarine", 3, "submarine_3_pegs.png"),
        new("Destroyer", 2, "destroyer_2_pegs.png")
    };

    public double BoardPixelSize => Size * CellSize;
    public double BoardFramePixelSize => BoardPixelSize + BoardAxisRailSize + BoardRailSpacing;
    public double CellPixelSize => CellSize;
    public double AxisRailPixelSize => BoardAxisRailSize;
    public double MissPegPixelSize => MissPegSize;
    public IReadOnlyList<string> RowLabels { get; } = Enumerable.Range(0, Size)
        .Select(i => ((char)('A' + i)).ToString())
        .ToArray();
    public IReadOnlyList<string> ColumnLabels { get; } = Enumerable.Range(1, Size)
        .Select(i => i.ToString())
        .ToArray();

    public ObservableCollection<BoardCellVm> EnemyCells { get; } = new();
    public ObservableCollection<BoardCellVm> PlayerCells { get; } = new();
    public ObservableCollection<ShipSpriteVm> PlayerShipSprites { get; } = new();
    public ObservableCollection<ShipSpriteVm> EnemyShipSprites { get; } = new();
    public ObservableCollection<PlacementShipVm> PlacementShips { get; } = new();
    public ObservableCollection<FleetRecapItemVm> FleetRecapItems { get; } = new();

    public IReadOnlyList<CpuDifficulty> DifficultyOptions { get; } = Enum.GetValues<CpuDifficulty>();
    public IReadOnlyList<AnimationSpeed> AnimationSpeedOptions { get; } = Enum.GetValues<AnimationSpeed>();

    public ICommand EnemyCellTappedCommand { get; }
    public ICommand PlayerCellTappedCommand { get; }
    public ICommand SelectPlacementShipCommand { get; }
    public ICommand RotatePlacementCommand { get; }
    public ICommand NewGameCommand { get; }
    public ICommand ResetStatsCommand { get; }
    public ICommand ToggleSettingsPanelCommand { get; }
    public ICommand DismissOverlayCommand { get; }
    public ICommand SetBoardViewModeCommand { get; }

    public bool IsPlayerTurn
    {
        get => _isPlayerTurn;
        private set
        {
            if (_isPlayerTurn == value) return;
            _isPlayerTurn = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanFire));
        }
    }

    public bool IsGameOver
    {
        get => _isGameOver;
        private set
        {
            if (_isGameOver == value) return;
            _isGameOver = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanFire));
            OnPropertyChanged(nameof(CanPlaceShips));
            OnPropertyChanged(nameof(CanRotatePlacement));
        }
    }

    public bool IsPlacementPhase
    {
        get => _isPlacementPhase;
        private set
        {
            if (_isPlacementPhase == value) return;
            _isPlacementPhase = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanFire));
            OnPropertyChanged(nameof(CanPlaceShips));
            OnPropertyChanged(nameof(CanRotatePlacement));
            OnPropertyChanged(nameof(PlacementSelectionMessage));
        }
    }

    public bool IsVerticalPlacement
    {
        get => _isVerticalPlacement;
        private set
        {
            if (_isVerticalPlacement == value) return;
            _isVerticalPlacement = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(PlacementOrientationText));
        }
    }

    public bool CanFire => !IsGameOver && !IsPlacementPhase && IsPlayerTurn && !_isResolvingEnemyTurn && !_isResolvingPlayerShot;
    public bool CanPlaceShips => !IsGameOver && IsPlacementPhase;
    public bool CanRotatePlacement => CanPlaceShips && _selectedPlacementShip is not null;

    public string TurnMessage
    {
        get => _turnMessage;
        private set
        {
            if (_turnMessage == value) return;
            _turnMessage = value;
            OnPropertyChanged();
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set
        {
            if (_statusMessage == value) return;
            _statusMessage = value;
            OnPropertyChanged();
        }
    }

    public string PlayerLastShotMessage
    {
        get => _playerLastShotMessage;
        private set
        {
            if (_playerLastShotMessage == value) return;
            _playerLastShotMessage = value;
            OnPropertyChanged();
        }
    }

    public string EnemyLastShotMessage
    {
        get => _enemyLastShotMessage;
        private set
        {
            if (_enemyLastShotMessage == value) return;
            _enemyLastShotMessage = value;
            OnPropertyChanged();
        }
    }

    public bool ShowEnemyFleet
    {
        get => _showEnemyFleet;
        private set
        {
            if (_showEnemyFleet == value) return;
            _showEnemyFleet = value;
            OnPropertyChanged();
        }
    }

    public int Wins
    {
        get => _wins;
        private set
        {
            if (_wins == value) return;
            _wins = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(StatsLine));
        }
    }

    public int Losses
    {
        get => _losses;
        private set
        {
            if (_losses == value) return;
            _losses = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(StatsLine));
        }
    }

    public int Draws
    {
        get => _draws;
        private set
        {
            if (_draws == value) return;
            _draws = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(StatsLine));
        }
    }

    public int TotalTurns
    {
        get => _totalTurns;
        private set
        {
            if (_totalTurns == value) return;
            _totalTurns = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(StatsLine));
        }
    }

    public int TotalShots
    {
        get => _totalShots;
        private set
        {
            if (_totalShots == value) return;
            _totalShots = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HitRate));
            OnPropertyChanged(nameof(StatsLine));
        }
    }

    public int TotalHits
    {
        get => _totalHits;
        private set
        {
            if (_totalHits == value) return;
            _totalHits = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HitRate));
            OnPropertyChanged(nameof(StatsLine));
        }
    }

    public double HitRate => TotalShots == 0 ? 0 : (double)TotalHits / TotalShots;

    public string StatsLine =>
        $"Record: {Wins}-{Losses}" +
        (Draws > 0 ? $" ({Draws} draws)" : string.Empty) +
        $"   Lifetime turns: {TotalTurns}   Lifetime hit rate: {HitRate:P0}";

    public int CurrentGameTurns
    {
        get => _currentGameTurns;
        private set
        {
            if (_currentGameTurns == value) return;
            _currentGameTurns = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CurrentGameHitRate));
            OnPropertyChanged(nameof(CurrentGameStatsLine));
        }
    }

    public int CurrentGameShots
    {
        get => _currentGameShots;
        private set
        {
            if (_currentGameShots == value) return;
            _currentGameShots = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CurrentGameHitRate));
            OnPropertyChanged(nameof(CurrentGameStatsLine));
        }
    }

    public int CurrentGameHits
    {
        get => _currentGameHits;
        private set
        {
            if (_currentGameHits == value) return;
            _currentGameHits = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CurrentGameHitRate));
            OnPropertyChanged(nameof(CurrentGameStatsLine));
        }
    }

    public double CurrentGameHitRate => CurrentGameShots == 0 ? 0 : (double)CurrentGameHits / CurrentGameShots;

    public string CurrentGameStatsLine =>
        $"Current mission: turns {CurrentGameTurns}, shots {CurrentGameShots}, hit rate {CurrentGameHitRate:P0}";

    public string LastGameSummary
    {
        get => _lastGameSummary;
        private set
        {
            if (_lastGameSummary == value) return;
            _lastGameSummary = value;
            OnPropertyChanged();
        }
    }

    public string AnalyticsAccuracyByPhase
    {
        get => _analyticsAccuracyByPhase;
        private set
        {
            if (_analyticsAccuracyByPhase == value) return;
            _analyticsAccuracyByPhase = value;
            OnPropertyChanged();
        }
    }

    public string AnalyticsStreaks
    {
        get => _analyticsStreaks;
        private set
        {
            if (_analyticsStreaks == value) return;
            _analyticsStreaks = value;
            OnPropertyChanged();
        }
    }

    public string AnalyticsBestSequence
    {
        get => _analyticsBestSequence;
        private set
        {
            if (_analyticsBestSequence == value) return;
            _analyticsBestSequence = value;
            OnPropertyChanged();
        }
    }

    public bool IsSettingsOpen
    {
        get => _isSettingsOpen;
        set
        {
            if (_isSettingsOpen == value) return;
            _isSettingsOpen = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SettingsToggleText));
            SaveSettings();
        }
    }

    public string SettingsToggleText => IsSettingsOpen ? "Close Settings" : "Show Settings";

    public CpuDifficulty SelectedDifficulty
    {
        get => _selectedDifficulty;
        set
        {
            if (_selectedDifficulty == value) return;
            _selectedDifficulty = value;
            OnPropertyChanged();
            SaveSettings();
        }
    }

    public AnimationSpeed SelectedAnimationSpeed
    {
        get => _selectedAnimationSpeed;
        set
        {
            if (_selectedAnimationSpeed == value) return;
            _selectedAnimationSpeed = value;
            OnPropertyChanged();
            ApplyAnimationSettings();
            SaveSettings();
        }
    }

    public bool SoundEnabled
    {
        get => _soundEnabled;
        set
        {
            if (_soundEnabled == value) return;
            _soundEnabled = value;
            OnPropertyChanged();
            SaveSettings();
        }
    }

    public bool HapticsEnabled
    {
        get => _hapticsEnabled;
        set
        {
            if (_hapticsEnabled == value) return;
            _hapticsEnabled = value;
            OnPropertyChanged();
            SaveSettings();
        }
    }

    public bool HighContrastMode
    {
        get => _highContrastMode;
        set
        {
            if (_highContrastMode == value) return;
            _highContrastMode = value;
            OnPropertyChanged();
            ApplyVisualSettings();
            SaveSettings();
        }
    }

    public bool LargeTextMode
    {
        get => _largeTextMode;
        set
        {
            if (_largeTextMode == value) return;
            _largeTextMode = value;
            OnPropertyChanged();
            ApplyVisualSettings();
            SaveSettings();
        }
    }

    public bool ReduceMotionMode
    {
        get => _reduceMotionMode;
        set
        {
            if (_reduceMotionMode == value) return;
            _reduceMotionMode = value;
            OnPropertyChanged();
            ApplyAnimationSettings();
            SaveSettings();
        }
    }

    public BoardViewMode BoardViewMode
    {
        get => _boardViewMode;
        private set
        {
            if (_boardViewMode == value) return;
            _boardViewMode = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(EnemyBoardTabBackground));
            OnPropertyChanged(nameof(PlayerBoardTabBackground));
            OnPropertyChanged(nameof(BoardFocusSummary));
        }
    }

    public Color EnemyBoardTabBackground => BoardViewMode == BoardViewMode.Enemy
        ? Color.FromArgb("#3f8ecd")
        : Color.FromArgb("#1d3146");

    public Color PlayerBoardTabBackground => BoardViewMode == BoardViewMode.Player
        ? Color.FromArgb("#3f8ecd")
        : Color.FromArgb("#1d3146");

    public string BoardFocusSummary => BoardViewMode switch
    {
        BoardViewMode.Player => "Command Center: Fleet Ops",
        _ => "Command Center: Fire Control"
    };

    public bool IsTurnTransitionActive
    {
        get => _isTurnTransitionActive;
        private set
        {
            if (_isTurnTransitionActive == value) return;
            _isTurnTransitionActive = value;
            OnPropertyChanged();
        }
    }

    public string TurnTransitionMessage
    {
        get => _turnTransitionMessage;
        private set
        {
            if (_turnTransitionMessage == value) return;
            _turnTransitionMessage = value;
            OnPropertyChanged();
        }
    }

    public bool IsOverlayVisible
    {
        get => _isOverlayVisible;
        private set
        {
            if (_isOverlayVisible == value) return;
            _isOverlayVisible = value;
            OnPropertyChanged();
        }
    }

    public bool ShowOverlayRecap
    {
        get => _showOverlayRecap;
        private set
        {
            if (_showOverlayRecap == value) return;
            _showOverlayRecap = value;
            OnPropertyChanged();
        }
    }

    public bool ShowOverlayAnalytics
    {
        get => _showOverlayAnalytics;
        private set
        {
            if (_showOverlayAnalytics == value) return;
            _showOverlayAnalytics = value;
            OnPropertyChanged();
        }
    }

    public string OverlayTitle
    {
        get => _overlayTitle;
        private set
        {
            if (_overlayTitle == value) return;
            _overlayTitle = value;
            OnPropertyChanged();
        }
    }

    public string OverlaySubtitle
    {
        get => _overlaySubtitle;
        private set
        {
            if (_overlaySubtitle == value) return;
            _overlaySubtitle = value;
            OnPropertyChanged();
        }
    }

    public string OverlayPrimaryActionText
    {
        get => _overlayPrimaryActionText;
        private set
        {
            if (_overlayPrimaryActionText == value) return;
            _overlayPrimaryActionText = value;
            OnPropertyChanged();
        }
    }

    public string PlacementOrientationText =>
        IsVerticalPlacement ? "Orientation: Vertical" : "Orientation: Horizontal";

    public string PlacementSelectionMessage
    {
        get
        {
            if (!IsPlacementPhase)
                return "Battle in progress.";

            if (_selectedPlacementShip is null)
                return "All ships are placed.";

            return $"Selected ship: {_selectedPlacementShip.Name} ({_selectedPlacementShip.Size})";
        }
    }

    public string ScoreLine
    {
        get
        {
            int enemySunk = _enemyBoard?.ShipsSunk ?? 0;
            int enemyTotal = _enemyBoard?.TotalShips ?? 0;
            int playerSunk = _playerBoard?.ShipsSunk ?? 0;
            int playerTotal = _playerBoard?.TotalShips ?? 0;
            return $"Enemy sunk: {enemySunk}/{enemyTotal}   Your ships sunk: {playerSunk}/{playerTotal}";
        }
    }

    public BoardViewModel()
        : this(
            new Random(),
            new JsonFileGameStatsStore(),
            new JsonFileGameSettingsStore(),
            new DefaultGameFeedbackService())
    {
    }

    public BoardViewModel(Random random)
        : this(
            random,
            new JsonFileGameStatsStore(),
            new JsonFileGameSettingsStore(),
            new DefaultGameFeedbackService())
    {
    }

    public BoardViewModel(Random random, IGameStatsStore statsStore)
        : this(
            random,
            statsStore,
            new JsonFileGameSettingsStore(),
            new DefaultGameFeedbackService())
    {
    }

    public BoardViewModel(
        Random random,
        IGameStatsStore statsStore,
        IGameSettingsStore settingsStore,
        IGameFeedbackService feedbackService)
    {
        _random = random ?? throw new ArgumentNullException(nameof(random));
        _statsStore = statsStore ?? throw new ArgumentNullException(nameof(statsStore));
        _settingsStore = settingsStore ?? throw new ArgumentNullException(nameof(settingsStore));
        _feedbackService = feedbackService ?? throw new ArgumentNullException(nameof(feedbackService));

        EnemyCellTappedCommand = new Command<BoardCellVm>(OnEnemyCellTapped);
        PlayerCellTappedCommand = new Command<BoardCellVm>(OnPlayerCellTapped);
        SelectPlacementShipCommand = new Command<PlacementShipVm>(OnSelectPlacementShip);
        RotatePlacementCommand = new Command(TogglePlacementOrientation);
        NewGameCommand = new Command(StartNewGame);
        ResetStatsCommand = new Command(ResetStats);
        ToggleSettingsPanelCommand = new Command(() => IsSettingsOpen = !IsSettingsOpen);
        DismissOverlayCommand = new Command(DismissOverlay);
        SetBoardViewModeCommand = new Command<string?>(SetBoardViewModeFromToken);

        LoadStats();
        LoadSettings();
        ApplyVisualSettings();
        ApplyAnimationSettings();
        InitializeCells(EnemyCells, isPlayerBoard: false);
        InitializeCells(PlayerCells, isPlayerBoard: true);
        StartNewGame();
    }

    private void LoadStats()
    {
        var snapshot = _statsStore.Load();
        Wins = snapshot.Wins;
        Losses = snapshot.Losses;
        Draws = snapshot.Draws;
        TotalTurns = snapshot.TotalTurns;
        TotalShots = snapshot.TotalShots;
        TotalHits = snapshot.TotalHits;
    }

    private void LoadSettings()
    {
        var settings = _settingsStore.Load();
        _selectedDifficulty = settings.Difficulty;
        _selectedAnimationSpeed = settings.AnimationSpeed;
        _soundEnabled = settings.SoundEnabled;
        _hapticsEnabled = settings.HapticsEnabled;
        _highContrastMode = settings.HighContrastMode;
        _largeTextMode = settings.LargeTextMode;
        _reduceMotionMode = settings.ReduceMotionMode;
        _isSettingsOpen = false;
        _hasSeenCommandBriefing = settings.HasSeenCommandBriefing;
    }

    private void ResetCurrentGameStats()
    {
        CurrentGameTurns = 0;
        CurrentGameShots = 0;
        CurrentGameHits = 0;
    }

    private void ResetStats()
    {
        Wins = 0;
        Losses = 0;
        Draws = 0;
        TotalTurns = 0;
        TotalShots = 0;
        TotalHits = 0;
        ResetCurrentGameStats();
        _currentGameShotHistory.Clear();
        AnalyticsAccuracyByPhase = "Accuracy by phase: --";
        AnalyticsStreaks = "Streaks: --";
        AnalyticsBestSequence = "Best turn sequence: --";
        LastGameSummary = "Stats reset.";
        SaveStats();
        StatusMessage = "Saved stats reset.";
    }

    private void SaveSettings()
    {
        _settingsStore.Save(new GameSettingsSnapshot(
            SelectedDifficulty,
            SelectedAnimationSpeed,
            SoundEnabled,
            HapticsEnabled,
            HighContrastMode,
            LargeTextMode,
            ReduceMotionMode,
            IsSettingsOpen,
            _hasSeenCommandBriefing));
    }

    private static double GetAnimationSpeedMultiplier(AnimationSpeed speed)
    {
        return speed switch
        {
            AnimationSpeed.Slow => 1.35,
            AnimationSpeed.Fast => 0.75,
            _ => 1.0
        };
    }

    private void ApplyVisualSettings()
    {
        ThemeTokenService.Apply(highContrast: HighContrastMode, largeText: LargeTextMode);
    }

    private void ApplyAnimationSettings()
    {
        AnimationRuntimeSettings.SpeedMultiplier = GetAnimationSpeedMultiplier(SelectedAnimationSpeed);
        AnimationRuntimeSettings.ReduceMotion = ReduceMotionMode;
    }

    private static int ScalePause(int milliseconds)
    {
        double scaled = milliseconds * AnimationRuntimeSettings.SpeedMultiplier;
        return (int)Math.Clamp(scaled, 40, 1600);
    }

    private static bool CanUseMainThreadPacing()
    {
        try
        {
            return MainThread.IsMainThread;
        }
        catch
        {
            return false;
        }
    }

    private bool ShouldUseCinematicTurnPacing => !ReduceMotionMode && CanUseMainThreadPacing();

    private void SetEnemyTurnResolutionState(bool isActive)
    {
        if (_isResolvingEnemyTurn == isActive)
            return;

        _isResolvingEnemyTurn = isActive;
        OnPropertyChanged(nameof(CanFire));
    }

    private void SetPlayerShotResolutionState(bool isActive)
    {
        if (_isResolvingPlayerShot == isActive)
            return;

        _isResolvingPlayerShot = isActive;
        OnPropertyChanged(nameof(CanFire));
    }

    private async Task PauseForDramaAsync(int milliseconds, string transitionMessage)
    {
        TurnTransitionMessage = transitionMessage;
        IsTurnTransitionActive = true;

        if (!ShouldUseCinematicTurnPacing)
            return;

        await Task.Delay(ScalePause(milliseconds));
    }

    private void ClearTurnTransition()
    {
        IsTurnTransitionActive = false;
        TurnTransitionMessage = string.Empty;
    }

    private void SetBoardViewMode(BoardViewMode mode)
    {
        BoardViewMode = mode;
    }

    private void SetBoardViewModeFromToken(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return;

        if (!Enum.TryParse(token, ignoreCase: true, out BoardViewMode mode))
            return;

        SetBoardViewMode(mode);
    }

    private void ApplyAutoBoardFocus()
    {
        if (IsGameOver)
        {
            SetBoardViewMode(BoardViewMode.Enemy);
            return;
        }

        if (IsPlacementPhase)
        {
            SetBoardViewMode(BoardViewMode.Player);
            return;
        }

        SetBoardViewMode(IsPlayerTurn ? BoardViewMode.Enemy : BoardViewMode.Player);
    }

    private void EmitFeedback(GameFeedbackCue cue)
    {
        _feedbackService.Play(cue, SoundEnabled, HapticsEnabled, ReduceMotionMode);
    }

    private void DismissOverlay()
    {
        IsOverlayVisible = false;
    }

    private void SaveStats()
    {
        _statsStore.Save(new GameStatsSnapshot(
            Wins,
            Losses,
            Draws,
            TotalTurns,
            TotalShots,
            TotalHits));
    }

    private void RecordPlayerShot(ShotInfo shot)
    {
        _currentGameShotHistory.Add(new PlayerShotRecord(
            CurrentGameTurns + 1,
            shot.Row,
            shot.Col,
            shot.IsHit));

        TotalTurns++;
        TotalShots++;
        CurrentGameTurns++;
        CurrentGameShots++;

        if (shot.IsHit)
        {
            TotalHits++;
            CurrentGameHits++;
        }

        SaveStats();
    }

    private void RecordGameOutcome(GameOutcome outcome)
    {
        switch (outcome)
        {
            case GameOutcome.Win:
                Wins++;
                break;
            case GameOutcome.Loss:
                Losses++;
                break;
            case GameOutcome.Draw:
                Draws++;
                break;
        }

        ComputePostGameAnalytics();
        LastGameSummary =
            $"{outcome} - turns {CurrentGameTurns}, shots {CurrentGameShots}, hits {CurrentGameHits}, hit rate {CurrentGameHitRate:P0}";

        SaveStats();
    }

    private void ComputePostGameAnalytics()
    {
        if (_currentGameShotHistory.Count == 0)
        {
            AnalyticsAccuracyByPhase = "Accuracy by phase: --";
            AnalyticsStreaks = "Streaks: --";
            AnalyticsBestSequence = "Best turn sequence: --";
            return;
        }

        int totalShots = _currentGameShotHistory.Count;
        int segmentSize = Math.Max(1, (int)Math.Ceiling(totalShots / 3d));

        var opening = _currentGameShotHistory.Take(segmentSize).ToList();
        var mid = _currentGameShotHistory.Skip(segmentSize).Take(segmentSize).ToList();
        var end = _currentGameShotHistory.Skip(segmentSize * 2).ToList();

        AnalyticsAccuracyByPhase =
            $"Accuracy by phase: Opening {FormatAccuracy(opening)}, Midgame {FormatAccuracy(mid)}, Endgame {FormatAccuracy(end)}";

        int longestHitStreak = 0;
        int longestMissStreak = 0;
        int currentHitStreak = 0;
        int currentMissStreak = 0;

        int bestRunStart = -1;
        int bestRunLength = 0;
        int currentRunStart = -1;
        int currentRunLength = 0;

        for (int index = 0; index < _currentGameShotHistory.Count; index++)
        {
            bool isHit = _currentGameShotHistory[index].IsHit;

            if (isHit)
            {
                currentHitStreak++;
                currentMissStreak = 0;
                longestHitStreak = Math.Max(longestHitStreak, currentHitStreak);

                if (currentRunLength == 0)
                    currentRunStart = index;

                currentRunLength++;
                if (currentRunLength > bestRunLength)
                {
                    bestRunLength = currentRunLength;
                    bestRunStart = currentRunStart;
                }
            }
            else
            {
                currentMissStreak++;
                currentHitStreak = 0;
                longestMissStreak = Math.Max(longestMissStreak, currentMissStreak);
                currentRunLength = 0;
                currentRunStart = -1;
            }
        }

        AnalyticsStreaks = $"Streaks: best hits {longestHitStreak}, best misses {longestMissStreak}";

        if (bestRunLength <= 1 || bestRunStart < 0)
        {
            AnalyticsBestSequence = "Best turn sequence: No multi-hit streak recorded.";
            return;
        }

        var bestRunCoordinates = _currentGameShotHistory
            .Skip(bestRunStart)
            .Take(bestRunLength)
            .Select(shot => ToBoardCoordinate(shot.Row, shot.Col));

        AnalyticsBestSequence =
            $"Best turn sequence: {string.Join(" -> ", bestRunCoordinates)} ({bestRunLength} hits)";
    }

    private static string FormatAccuracy(IReadOnlyCollection<PlayerShotRecord> shots)
    {
        if (shots.Count == 0)
            return "--";

        int hits = shots.Count(shot => shot.IsHit);
        return $"{(double)hits / shots.Count:P0}";
    }

    private void BuildFleetRecap()
    {
        FleetRecapItems.Clear();
        if (_enemyBoard is null)
            return;

        foreach (var template in FleetTemplates)
        {
            var ship = _enemyBoard.Fleet.FirstOrDefault(s => string.Equals(s.Name, template.Name, StringComparison.OrdinalIgnoreCase));
            if (ship is null)
                continue;

            FleetRecapItems.Add(new FleetRecapItemVm(
                ship.Name,
                template.ImageSource,
                ship.IsSunk,
                ship.IsSunk ? "Destroyed" : "Survived"));
        }
    }

    private void ShowGameStartOverlay()
    {
        FleetRecapItems.Clear();
        OverlayTitle = "Command Briefing";
        OverlaySubtitle = "1) Choose ships from the tray, right-click on your fleet grid to rotate.\n2) Place all ships, then switch to Fire Control and fire at enemy coordinates.\n3) Hits trigger follow-up intel. Sink all enemy ships to win.";
        OverlayPrimaryActionText = "Begin Mission";
        ShowOverlayRecap = false;
        ShowOverlayAnalytics = false;
        IsOverlayVisible = true;
    }

    private void ShowGameOverOverlay(GameOutcome outcome)
    {
        BuildFleetRecap();

        OverlayTitle = outcome switch
        {
            GameOutcome.Win => "Victory Debrief",
            GameOutcome.Loss => "Mission Lost",
            _ => "Draw Debrief"
        };

        OverlaySubtitle = outcome switch
        {
            GameOutcome.Win => "Enemy fleet neutralized. Review your battle analytics below.",
            GameOutcome.Loss => "Your fleet was destroyed. Review the battle and prepare a better opening.",
            _ => "All shots exhausted. Review the battle recap and analytics."
        };

        OverlayPrimaryActionText = "Close Debrief";
        ShowOverlayRecap = true;
        ShowOverlayAnalytics = true;
        IsOverlayVisible = true;
    }

    private void StartNewGame()
    {
        ResetCurrentGameStats();
        _currentGameShotHistory.Clear();
        AnalyticsAccuracyByPhase = "Accuracy by phase: --";
        AnalyticsStreaks = "Streaks: --";
        AnalyticsBestSequence = "Best turn sequence: --";

        _playerBoard = new GameBoard(Size);
        _enemyBoard = new GameBoard(Size);

        var playerFleet = CreateFleet();
        var enemyFleet = CreateFleet();

        PlaceFleetRandomly(_enemyBoard, enemyFleet, allowVertical: true);

        _playerBoard.SetFleet(playerFleet);
        _enemyBoard.SetFleet(enemyFleet);

        ResetCells(EnemyCells, clearShips: true);
        ResetCells(PlayerCells, clearShips: true);
        PlayerShipSprites.Clear();
        EnemyShipSprites.Clear();
        _playerSpritesByName.Clear();
        _enemySpritesByName.Clear();

        InitializePlacementShips(playerFleet);
        BuildEnemyShipSprites(enemyFleet);
        InitializeEnemyTargeting();

        IsGameOver = false;
        IsPlacementPhase = true;
        IsVerticalPlacement = false;
        IsPlayerTurn = false;
        ShowEnemyFleet = false;
        SetEnemyTurnResolutionState(false);
        SetPlayerShotResolutionState(false);
        _gameSessionId++;
        ClearTurnTransition();
        SetBoardViewMode(BoardViewMode.Player);

        TurnMessage = "Placement phase";
        StatusMessage = "Select a ship and tap Your Fleet board to place it.";
        PlayerLastShotMessage = "Your last shot: --";
        EnemyLastShotMessage = "Enemy last shot: --";
        OverlayPrimaryActionText = "Begin Mission";

        if (!_hasSeenCommandBriefing)
        {
            ShowGameStartOverlay();
            _hasSeenCommandBriefing = true;
            SaveSettings();
        }
        else
        {
            IsOverlayVisible = false;
        }

        EmitFeedback(GameFeedbackCue.NewGame);

        OnPropertyChanged(nameof(PlacementOrientationText));
        OnPropertyChanged(nameof(PlacementSelectionMessage));
        OnPropertyChanged(nameof(ScoreLine));
    }

    private static void InitializeCells(ObservableCollection<BoardCellVm> cells, bool isPlayerBoard)
    {
        cells.Clear();
        for (int row = 0; row < Size; row++)
        {
            for (int col = 0; col < Size; col++)
            {
                cells.Add(new BoardCellVm(row, col, isPlayerBoard));
            }
        }
    }

    private static void ResetCells(ObservableCollection<BoardCellVm> cells, bool clearShips)
    {
        foreach (var cell in cells)
            cell.Reset(clearShips);
    }

    private static List<Ship> CreateFleet()
    {
        var fleet = new List<Ship>(FleetTemplates.Length);
        foreach (var template in FleetTemplates)
            fleet.Add(new Ship(template.Name, template.Size));

        return fleet;
    }

    private void InitializePlacementShips(IEnumerable<Ship> playerFleet)
    {
        PlacementShips.Clear();
        _playerShipsByName.Clear();

        foreach (var template in FleetTemplates)
        {
            var ship = playerFleet.First(s => string.Equals(s.Name, template.Name, StringComparison.OrdinalIgnoreCase));
            _playerShipsByName[ship.Name] = ship;
            PlacementShips.Add(new PlacementShipVm(ship.Name, ship.Size, template.ImageSource));
        }

        SetSelectedPlacementShip(PlacementShips.FirstOrDefault());
    }

    private void SetSelectedPlacementShip(PlacementShipVm? ship)
    {
        foreach (var placementShip in PlacementShips)
            placementShip.IsSelected = false;

        _selectedPlacementShip = ship is not null && !ship.IsPlaced ? ship : null;
        if (_selectedPlacementShip is not null)
            _selectedPlacementShip.IsSelected = true;

        OnPropertyChanged(nameof(CanRotatePlacement));
        OnPropertyChanged(nameof(PlacementSelectionMessage));
    }

    private void OnSelectPlacementShip(PlacementShipVm? ship)
    {
        if (!CanPlaceShips || ship is null)
            return;

        if (ship.IsPlaced)
        {
            StatusMessage = $"{ship.Name} is already placed.";
            return;
        }

        SetSelectedPlacementShip(ship);
        StatusMessage = $"Placing {ship.Name} ({ship.Size}). Tap a starting cell on Your Fleet board.";
    }

    private void TogglePlacementOrientation()
    {
        if (!CanPlaceShips)
            return;

        IsVerticalPlacement = !IsVerticalPlacement;
        StatusMessage = $"{PlacementOrientationText}.";
    }

    private void OnPlayerCellTapped(BoardCellVm? targetCell)
    {
        if (!CanPlaceShips || targetCell is null || _playerBoard is null)
            return;

        if (_selectedPlacementShip is null)
        {
            StatusMessage = "Select a ship to place.";
            return;
        }

        if (!_playerShipsByName.TryGetValue(_selectedPlacementShip.Name, out var ship))
        {
            StatusMessage = $"Could not find ship data for {_selectedPlacementShip.Name}.";
            return;
        }

        ShipOrientation orientation = IsVerticalPlacement ? ShipOrientation.Vertical : ShipOrientation.Horizontal;
        bool placed = _playerBoard.TryPlaceShip(ship, targetCell.Row, targetCell.Col, orientation);

        if (!placed)
        {
            StatusMessage = $"Cannot place {_selectedPlacementShip.Name} at {ToBoardCoordinate(targetCell.Row, targetCell.Col)}. Try another cell or rotate.";
            return;
        }

        _selectedPlacementShip.IsPlaced = true;
        AddPlayerShipSprite(ship, _selectedPlacementShip.ImageSource);
        ApplyPlayerShipPresence(ship);
        EmitFeedback(GameFeedbackCue.PlaceShip);

        var coordinate = ToBoardCoordinate(targetCell.Row, targetCell.Col);
        StatusMessage = $"Placed {_selectedPlacementShip.Name} at {coordinate}.";

        var next = PlacementShips.FirstOrDefault(s => !s.IsPlaced);
        if (next is null)
        {
            CompletePlacementPhase();
            return;
        }

        SetSelectedPlacementShip(next);
    }

    private void CompletePlacementPhase()
    {
        SetSelectedPlacementShip(null);
        IsPlacementPhase = false;
        IsPlayerTurn = true;
        TurnMessage = "Your turn";
        StatusMessage = "All ships placed. Tap a cell on Enemy Waters to fire.";
        ApplyAutoBoardFocus();
        EmitFeedback(GameFeedbackCue.PlacementComplete);
    }

    private void AddPlayerShipSprite(Ship ship, string imageSource)
    {
        if (ship.Positions.Count == 0)
            return;

        int row = ship.Positions.Min(p => p.Row);
        int col = ship.Positions.Min(p => p.Col);
        bool isVertical = ship.Positions.Select(p => p.Col).Distinct().Count() == 1;

        var sprite = new ShipSpriteVm(
            ship.Name,
            imageSource,
            row,
            col,
            ship.Size,
            isVertical ? ShipAxis.Vertical : ShipAxis.Horizontal,
            isEnemy: false,
            isRevealed: true);

        PlayerShipSprites.Add(sprite);
        _playerSpritesByName[ship.Name] = sprite;
    }

    private void ApplyPlayerShipPresence(Ship ship)
    {
        foreach (var position in ship.Positions)
        {
            int index = position.Row * Size + position.Col;
            if (index >= 0 && index < PlayerCells.Count)
                PlayerCells[index].SetShipPresence(true);
        }
    }

    private void BuildEnemyShipSprites(IEnumerable<Ship> enemyFleet)
    {
        EnemyShipSprites.Clear();
        _enemySpritesByName.Clear();

        foreach (var ship in enemyFleet)
        {
            if (ship.Positions.Count == 0)
                continue;

            int row = ship.Positions.Min(p => p.Row);
            int col = ship.Positions.Min(p => p.Col);
            bool isVertical = ship.Positions.Select(p => p.Col).Distinct().Count() == 1;

            string imageSource = FleetTemplates
                .First(t => string.Equals(t.Name, ship.Name, StringComparison.OrdinalIgnoreCase))
                .ImageSource;

            var sprite = new ShipSpriteVm(
                ship.Name,
                imageSource,
                row,
                col,
                ship.Size,
                isVertical ? ShipAxis.Vertical : ShipAxis.Horizontal,
                isEnemy: true,
                isRevealed: false);

            EnemyShipSprites.Add(sprite);
            _enemySpritesByName[ship.Name] = sprite;
        }
    }

    private void PlaceFleetRandomly(GameBoard board, IEnumerable<Ship> fleet, bool allowVertical)
    {
        foreach (var ship in fleet)
        {
            bool placed = false;

            for (int attempts = 0; attempts < 500 && !placed; attempts++)
            {
                int row = _random.Next(Size);
                int col = _random.Next(Size);
                ShipOrientation orientation = allowVertical && _random.Next(2) == 0
                    ? ShipOrientation.Vertical
                    : ShipOrientation.Horizontal;

                placed = board.TryPlaceShip(ship, row, col, orientation);
            }

            if (!placed)
                throw new InvalidOperationException($"Could not place ship: {ship.Name}");
        }
    }

    private void InitializeEnemyTargeting()
    {
        _enemyTargetingStrategy = null;
        _easyEnemyShotQueue.Clear();

        if (SelectedDifficulty == CpuDifficulty.Easy)
        {
            var coordinates = new List<BoardCoordinate>(Size * Size);
            for (int row = 0; row < Size; row++)
            {
                for (int col = 0; col < Size; col++)
                    coordinates.Add(new BoardCoordinate(row, col));
            }

            for (int i = coordinates.Count - 1; i > 0; i--)
            {
                int j = _random.Next(i + 1);
                (coordinates[i], coordinates[j]) = (coordinates[j], coordinates[i]);
            }

            foreach (var coordinate in coordinates)
                _easyEnemyShotQueue.Enqueue(coordinate);

            return;
        }

        _enemyTargetingStrategy = new EnemyTargetingStrategy(Size, _random);
    }

    private bool TryGetNextEnemyTarget(out BoardCoordinate target)
    {
        if (SelectedDifficulty == CpuDifficulty.Easy)
        {
            if (_easyEnemyShotQueue.Count > 0)
            {
                target = _easyEnemyShotQueue.Dequeue();
                return true;
            }

            target = default;
            return false;
        }

        if (_enemyTargetingStrategy is null)
        {
            target = default;
            return false;
        }

        try
        {
            target = _enemyTargetingStrategy.GetNextShot();
            return true;
        }
        catch (InvalidOperationException)
        {
            target = default;
            return false;
        }
    }

    private void RevealEnemyFleet()
    {
        ShowEnemyFleet = true;
        foreach (var sprite in EnemyShipSprites)
            sprite.Reveal();
    }

    private void OnEnemyCellTapped(BoardCellVm? targetCell)
    {
        if (targetCell is null || _enemyBoard is null || _playerBoard is null)
            return;

        if (IsGameOver)
        {
            StatusMessage = "Game over. Press New Game to play again.";
            return;
        }

        if (IsPlacementPhase)
        {
            StatusMessage = "Place all ships on Your Fleet board before firing.";
            return;
        }

        if (!IsPlayerTurn || _isResolvingEnemyTurn || _isResolvingPlayerShot)
        {
            StatusMessage = "Turn sequence in progress.";
            return;
        }

        if (targetCell.MarkerState != ShotMarkerState.None)
        {
            StatusMessage = "You already fired at that cell.";
            return;
        }

        if (ShouldUseCinematicTurnPacing)
        {
            _ = ResolvePlayerShotWithPacingAsync(targetCell, _gameSessionId);
            return;
        }

        var playerShot = _enemyBoard.Attack(targetCell.Row, targetCell.Col);

        if (playerShot.Result == AttackResult.AlreadyTried)
        {
            StatusMessage = "You already fired at that cell.";
            return;
        }

        RecordPlayerShot(playerShot);
        ApplyShotResult(EnemyCells, playerShot);
        EmitFeedback(playerShot.Result switch
        {
            AttackResult.Sunk => GameFeedbackCue.Sunk,
            AttackResult.Hit => GameFeedbackCue.Hit,
            _ => GameFeedbackCue.Miss
        });

        PlayerLastShotMessage = $"Your last shot: {ToBoardCoordinate(playerShot.Row, playerShot.Col)} - {playerShot.Message}";
        StatusMessage = "Enemy is firing...";
        OnPropertyChanged(nameof(ScoreLine));

        if (playerShot.Result == AttackResult.Sunk &&
            playerShot.SunkShipName is not null &&
            _enemySpritesByName.TryGetValue(playerShot.SunkShipName, out var enemySprite))
        {
            enemySprite.MarkSunk();
        }

        if (_enemyBoard.AllShipsSunk)
        {
            IsGameOver = true;
            IsPlayerTurn = false;
            TurnMessage = "Victory";
            StatusMessage = "All enemy ships sunk. You win.";
            EnemyLastShotMessage = "Enemy last shot: --";
            RecordGameOutcome(GameOutcome.Win);
            EmitFeedback(GameFeedbackCue.Win);
            RevealEnemyFleet();
            ClearTurnTransition();
            ApplyAutoBoardFocus();
            ShowGameOverOverlay(GameOutcome.Win);
            return;
        }

        IsPlayerTurn = false;
        TurnMessage = "Enemy turn";
        ApplyAutoBoardFocus();

        if (ShouldUseCinematicTurnPacing)
        {
            _ = ResolveEnemyTurnWithPacingAsync(_gameSessionId);
            return;
        }

        EnemyTakeTurn();
    }

    private async Task ResolvePlayerShotWithPacingAsync(BoardCellVm targetCell, int sessionId)
    {
        if (_enemyBoard is null || _playerBoard is null)
            return;

        SetPlayerShotResolutionState(true);
        try
        {
            string targetCoordinate = ToBoardCoordinate(targetCell.Row, targetCell.Col);
            targetCell.SetTargetLocked(true);

            await PauseForDramaAsync(320, $"Target lock {targetCoordinate}. Charging weapons...");
            if (sessionId != _gameSessionId || IsGameOver || IsPlacementPhase || !IsPlayerTurn)
                return;

            var playerShot = _enemyBoard.Attack(targetCell.Row, targetCell.Col);
            if (playerShot.Result == AttackResult.AlreadyTried)
            {
                StatusMessage = "You already fired at that cell.";
                return;
            }

            RecordPlayerShot(playerShot);
            ApplyShotResult(EnemyCells, playerShot);
            EmitFeedback(playerShot.Result switch
            {
                AttackResult.Sunk => GameFeedbackCue.Sunk,
                AttackResult.Hit => GameFeedbackCue.Hit,
                _ => GameFeedbackCue.Miss
            });

            PlayerLastShotMessage = $"Your last shot: {targetCoordinate} - {playerShot.Message}";
            OnPropertyChanged(nameof(ScoreLine));

            if (playerShot.Result == AttackResult.Sunk &&
                playerShot.SunkShipName is not null &&
                _enemySpritesByName.TryGetValue(playerShot.SunkShipName, out var enemySprite))
            {
                enemySprite.MarkSunk();
            }

            await PauseForDramaAsync(playerShot.IsHit ? 560 : 420, BuildPlayerShotCallout(playerShot));
            if (sessionId != _gameSessionId)
                return;

            if (_enemyBoard.AllShipsSunk)
            {
                IsGameOver = true;
                IsPlayerTurn = false;
                TurnMessage = "Victory";
                StatusMessage = "All enemy ships sunk. You win.";
                EnemyLastShotMessage = "Enemy last shot: --";
                RecordGameOutcome(GameOutcome.Win);
                EmitFeedback(GameFeedbackCue.Win);
                RevealEnemyFleet();
                ClearTurnTransition();
                ApplyAutoBoardFocus();
                ShowGameOverOverlay(GameOutcome.Win);
                return;
            }

            IsPlayerTurn = false;
            TurnMessage = "Enemy turn";
            StatusMessage = "Enemy command is preparing a counterstrike.";
            ApplyAutoBoardFocus();

            await PauseForDramaAsync(460, "Enemy command plotting return fire...");
            if (sessionId != _gameSessionId || IsGameOver)
                return;

            await ResolveEnemyTurnWithPacingAsync(sessionId);
        }
        finally
        {
            targetCell.SetTargetLocked(false);
            SetPlayerShotResolutionState(false);
        }
    }

    private async Task ResolveEnemyTurnWithPacingAsync(int sessionId)
    {
        if (_isResolvingEnemyTurn || _playerBoard is null)
            return;

        SetEnemyTurnResolutionState(true);
        try
        {
            await PauseForDramaAsync(520, "Enemy plotting trajectory...");

            if (sessionId != _gameSessionId || IsGameOver)
                return;

            await EnemyTakeTurnCinematicAsync(sessionId);

            if (sessionId == _gameSessionId && !IsGameOver)
                await PauseForDramaAsync(260, "Telemetry relay complete. Your command window is open.");
        }
        finally
        {
            SetEnemyTurnResolutionState(false);
            if (sessionId == _gameSessionId)
                ClearTurnTransition();
        }
    }

    private async Task EnemyTakeTurnCinematicAsync(int sessionId)
    {
        if (_playerBoard is null)
            return;

        int maxShotsThisTurn = SelectedDifficulty == CpuDifficulty.Hard ? 2 : 1;
        ShotInfo? lastShot = null;

        for (int shotNumber = 0; shotNumber < maxShotsThisTurn; shotNumber++)
        {
            if (!TryGetNextEnemyTarget(out var target))
            {
                IsGameOver = true;
                TurnMessage = "Draw";
                StatusMessage = "No remaining shots.";
                EnemyLastShotMessage = "Enemy last shot: --";
                RecordGameOutcome(GameOutcome.Draw);
                EmitFeedback(GameFeedbackCue.Draw);
                RevealEnemyFleet();
                ApplyAutoBoardFocus();
                ShowGameOverOverlay(GameOutcome.Draw);
                return;
            }

            int targetIndex = target.Row * Size + target.Col;
            BoardCellVm? targetCell = targetIndex >= 0 && targetIndex < PlayerCells.Count
                ? PlayerCells[targetIndex]
                : null;
            string targetCoordinate = ToBoardCoordinate(target.Row, target.Col);

            targetCell?.SetTargetLocked(true);
            try
            {
                await PauseForDramaAsync(360, $"Enemy lock acquired at {targetCoordinate}...");
                if (sessionId != _gameSessionId || IsGameOver)
                    return;

                var enemyShot = _playerBoard.Attack(target.Row, target.Col);
                lastShot = enemyShot;

                if (_enemyTargetingStrategy is not null)
                    _enemyTargetingStrategy.RegisterShotOutcome(target, enemyShot.Result);

                ApplyShotResult(PlayerCells, enemyShot);

                if (enemyShot.Result == AttackResult.Sunk &&
                    enemyShot.SunkShipName is not null &&
                    _playerSpritesByName.TryGetValue(enemyShot.SunkShipName, out var sprite))
                {
                    sprite.MarkSunk();
                }

                OnPropertyChanged(nameof(ScoreLine));
                EmitFeedback(enemyShot.Result switch
                {
                    AttackResult.Sunk => GameFeedbackCue.Sunk,
                    AttackResult.Hit => GameFeedbackCue.Hit,
                    _ => GameFeedbackCue.Miss
                });

                EnemyLastShotMessage = $"Enemy last shot: {targetCoordinate} - {enemyShot.Message}";
                await PauseForDramaAsync(enemyShot.IsHit ? 560 : 420, BuildEnemyShotCallout(targetCoordinate, enemyShot));
                if (sessionId != _gameSessionId || IsGameOver)
                    return;

                if (_playerBoard.AllShipsSunk)
                {
                    IsGameOver = true;
                    TurnMessage = "Defeat";
                    StatusMessage = "All your ships have been sunk. You lose.";
                    RecordGameOutcome(GameOutcome.Loss);
                    EmitFeedback(GameFeedbackCue.Loss);
                    RevealEnemyFleet();
                    ApplyAutoBoardFocus();
                    ShowGameOverOverlay(GameOutcome.Loss);
                    return;
                }

                bool grantBonusShot = SelectedDifficulty == CpuDifficulty.Hard && enemyShot.IsHit;
                if (!grantBonusShot)
                    break;

                StatusMessage = "Enemy scored a hit and takes an aggressive follow-up shot.";
                await PauseForDramaAsync(340, "Enemy loading aggressive follow-up salvo...");
                if (sessionId != _gameSessionId || IsGameOver)
                    return;
            }
            finally
            {
                targetCell?.SetTargetLocked(false);
            }
        }

        if (lastShot is null)
            return;

        IsPlayerTurn = true;
        TurnMessage = "Your turn";
        StatusMessage = "Target window open. Tap a cell on Enemy Waters to fire.";
        ApplyAutoBoardFocus();
    }

    private void EnemyTakeTurn()
    {
        if (_playerBoard is null)
            return;

        int maxShotsThisTurn = SelectedDifficulty == CpuDifficulty.Hard ? 2 : 1;
        ShotInfo? lastShot = null;

        for (int shotNumber = 0; shotNumber < maxShotsThisTurn; shotNumber++)
        {
            if (!TryGetNextEnemyTarget(out var target))
            {
                IsGameOver = true;
                TurnMessage = "Draw";
                StatusMessage = "No remaining shots.";
                EnemyLastShotMessage = "Enemy last shot: --";
                RecordGameOutcome(GameOutcome.Draw);
                EmitFeedback(GameFeedbackCue.Draw);
                RevealEnemyFleet();
                ApplyAutoBoardFocus();
                ShowGameOverOverlay(GameOutcome.Draw);
                return;
            }

            var enemyShot = _playerBoard.Attack(target.Row, target.Col);
            lastShot = enemyShot;

            if (_enemyTargetingStrategy is not null)
                _enemyTargetingStrategy.RegisterShotOutcome(target, enemyShot.Result);

            ApplyShotResult(PlayerCells, enemyShot);

            if (enemyShot.Result == AttackResult.Sunk &&
                enemyShot.SunkShipName is not null &&
                _playerSpritesByName.TryGetValue(enemyShot.SunkShipName, out var sprite))
            {
                sprite.MarkSunk();
            }

            OnPropertyChanged(nameof(ScoreLine));

            EmitFeedback(enemyShot.Result switch
            {
                AttackResult.Sunk => GameFeedbackCue.Sunk,
                AttackResult.Hit => GameFeedbackCue.Hit,
                _ => GameFeedbackCue.Miss
            });

            if (_playerBoard.AllShipsSunk)
            {
                IsGameOver = true;
                TurnMessage = "Defeat";
                EnemyLastShotMessage = $"Enemy last shot: {ToBoardCoordinate(enemyShot.Row, enemyShot.Col)} - {enemyShot.Message}";
                StatusMessage = "All your ships have been sunk. You lose.";
                RecordGameOutcome(GameOutcome.Loss);
                EmitFeedback(GameFeedbackCue.Loss);
                RevealEnemyFleet();
                ApplyAutoBoardFocus();
                ShowGameOverOverlay(GameOutcome.Loss);
                return;
            }

            bool grantBonusShot = SelectedDifficulty == CpuDifficulty.Hard && enemyShot.IsHit;
            if (!grantBonusShot)
                break;

            StatusMessage = "Enemy scored a hit and takes an aggressive follow-up shot.";
        }

        if (lastShot is null)
            return;

        IsPlayerTurn = true;
        TurnMessage = "Your turn";
        EnemyLastShotMessage = $"Enemy last shot: {ToBoardCoordinate(lastShot.Row, lastShot.Col)} - {lastShot.Message}";
        StatusMessage = "Tap a cell on Enemy Waters to fire.";
        ApplyAutoBoardFocus();
    }

    private static string BuildPlayerShotCallout(ShotInfo shot)
    {
        return shot.Result switch
        {
            AttackResult.Sunk when !string.IsNullOrWhiteSpace(shot.SunkShipName) =>
                $"Direct hit. Enemy {shot.SunkShipName} sunk.",
            AttackResult.Hit => "Direct hit on enemy hull.",
            _ => "Splashdown. Shot missed target."
        };
    }

    private static string BuildEnemyShotCallout(string coordinate, ShotInfo shot)
    {
        return shot.Result switch
        {
            AttackResult.Sunk when !string.IsNullOrWhiteSpace(shot.SunkShipName) =>
                $"Enemy strike {coordinate}: {shot.SunkShipName} sunk.",
            AttackResult.Hit => $"Enemy strike {coordinate}: direct hit.",
            _ => $"Enemy strike {coordinate}: missed."
        };
    }

    private static void ApplyShotResult(ObservableCollection<BoardCellVm> cells, ShotInfo shot)
    {
        int index = shot.Row * Size + shot.Col;
        if (index < 0 || index >= cells.Count)
            return;

        cells[index].ApplyShot(shot);
    }

    private static string ToBoardCoordinate(int row, int col)
    {
        char letter = (char)('A' + row);
        return $"{letter}{col + 1}";
    }
}

public enum CpuDifficulty
{
    Standard = 0,
    Easy = 1,
    Hard = 2
}

public enum AnimationSpeed
{
    Normal = 0,
    Slow = 1,
    Fast = 2
}

public enum BoardViewMode
{
    Enemy = 0,
    Player = 1
}

public enum GameFeedbackCue
{
    NewGame = 0,
    PlaceShip = 1,
    PlacementComplete = 2,
    Miss = 3,
    Hit = 4,
    Sunk = 5,
    Win = 6,
    Loss = 7,
    Draw = 8
}

public readonly record struct PlayerShotRecord(int TurnNumber, int Row, int Col, bool IsHit);

public sealed class FleetRecapItemVm
{
    public string Name { get; }
    public string ImageSource { get; }
    public bool IsSunk { get; }
    public string StatusText { get; }
    public Color StatusColor => IsSunk ? Color.FromArgb("#ff8a6b") : Color.FromArgb("#8bd0ff");

    public FleetRecapItemVm(string name, string imageSource, bool isSunk, string statusText)
    {
        Name = name;
        ImageSource = imageSource;
        IsSunk = isSunk;
        StatusText = statusText;
    }
}

public enum GameOutcome
{
    Win = 0,
    Loss = 1,
    Draw = 2
}

public enum ShotMarkerState
{
    None = 0,
    Miss = 1,
    Hit = 2
}

public class BoardCellVm : ObservableObject
{
    private ShotMarkerState _markerState;
    private bool _hasShip;
    private bool _isTargetLocked;

    public int Row { get; }
    public int Col { get; }
    public bool IsPlayerBoard { get; }

    public ShotMarkerState MarkerState
    {
        get => _markerState;
        private set
        {
            if (_markerState == value) return;
            _markerState = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(MarkerText));
            OnPropertyChanged(nameof(MarkerColor));
            OnPropertyChanged(nameof(MarkerImage));
            OnPropertyChanged(nameof(IsHitMarkerVisible));
            OnPropertyChanged(nameof(IsMissMarkerVisible));
            OnPropertyChanged(nameof(MarkerStateText));
            OnPropertyChanged(nameof(IsTargetLockVisible));
            OnPropertyChanged(nameof(CellFillColor));
            OnPropertyChanged(nameof(CellStrokeColor));
            OnPropertyChanged(nameof(AccessibilityText));
        }
    }

    public bool HasShip
    {
        get => _hasShip;
        private set
        {
            if (_hasShip == value) return;
            _hasShip = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CellFillColor));
            OnPropertyChanged(nameof(CellStrokeColor));
            OnPropertyChanged(nameof(AccessibilityText));
        }
    }

    public bool IsTargetLocked
    {
        get => _isTargetLocked;
        private set
        {
            if (_isTargetLocked == value) return;
            _isTargetLocked = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsTargetLockVisible));
            OnPropertyChanged(nameof(CellFillColor));
            OnPropertyChanged(nameof(CellStrokeColor));
            OnPropertyChanged(nameof(AccessibilityText));
        }
    }

    public string CoordinateText => $"{(char)('A' + Row)}{Col + 1}";

    public string MarkerText => MarkerState switch
    {
        _ => string.Empty
    };

    public Color MarkerColor => Colors.Transparent;

    public bool IsHitMarkerVisible => MarkerState == ShotMarkerState.Hit;
    public bool IsMissMarkerVisible => MarkerState == ShotMarkerState.Miss;
    public bool IsTargetLockVisible => IsTargetLocked && MarkerState == ShotMarkerState.None;
    public double MissPegSize => BoardViewModel.MissPegSize;
    public Color MissPegFillColor => IsPlayerBoard ? Color.FromArgb("#f2f8ff") : Color.FromArgb("#e6f3ff");
    public Color MissPegStrokeColor => IsPlayerBoard ? Color.FromArgb("#7ea5c8") : Color.FromArgb("#6f9bc2");
    public Color MissPegCapColor => IsPlayerBoard ? Color.FromArgb("#c7e1f4") : Color.FromArgb("#bdd9ef");

    public Color CellFillColor => MarkerState switch
    {
        ShotMarkerState.Hit => Color.FromArgb("#3d2619"),
        ShotMarkerState.Miss => IsPlayerBoard ? Color.FromArgb("#214a6f") : Color.FromArgb("#1f4f79"),
        _ when IsTargetLocked => IsPlayerBoard ? Color.FromArgb("#275f8a") : Color.FromArgb("#2970a1"),
        _ when IsPlayerBoard && HasShip => Color.FromArgb("#2e648c"),
        _ => IsPlayerBoard ? Color.FromArgb("#173b5e") : Color.FromArgb("#1a4369")
    };

    public Color CellStrokeColor => MarkerState switch
    {
        ShotMarkerState.Hit => Color.FromArgb("#ff9366"),
        ShotMarkerState.Miss => Color.FromArgb("#9ac3e5"),
        _ when IsTargetLocked => Color.FromArgb("#8fd9ff"),
        _ when IsPlayerBoard && HasShip => Color.FromArgb("#b8dcf8"),
        _ => Color.FromArgb("#3d658b")
    };

    public string? MarkerImage => MarkerState == ShotMarkerState.Hit ? "explosion.png" : null;

    public string MarkerStateText => MarkerState switch
    {
        ShotMarkerState.Hit => "hit",
        ShotMarkerState.Miss => "miss",
        _ => "untargeted"
    };

    public string AccessibilityText => IsPlayerBoard
        ? $"{CoordinateText}, {(HasShip ? "occupied" : "clear")}, {(IsTargetLockVisible ? "targeted, " : string.Empty)}{MarkerStateText}"
        : $"{CoordinateText}, {(IsTargetLockVisible ? "targeted, " : string.Empty)}{MarkerStateText}";

    public BoardCellVm(int row, int col, bool isPlayerBoard)
    {
        Row = row;
        Col = col;
        IsPlayerBoard = isPlayerBoard;
    }

    public void ApplyShot(ShotInfo shot)
    {
        IsTargetLocked = false;
        MarkerState = shot.IsHit ? ShotMarkerState.Hit : ShotMarkerState.Miss;
    }

    public void SetTargetLocked(bool isLocked)
    {
        if (MarkerState != ShotMarkerState.None && isLocked)
            return;

        IsTargetLocked = isLocked;
    }

    public void SetShipPresence(bool hasShip)
    {
        HasShip = hasShip;
    }

    public void Reset(bool clearShips)
    {
        if (clearShips)
            HasShip = false;

        IsTargetLocked = false;
        MarkerState = ShotMarkerState.None;
    }
}

public class ShipSpriteVm : ObservableObject
{
    private bool _isSunk;
    private bool _isRevealed;

    public string Name { get; }
    public string ImageSource { get; }
    public int StartRow { get; }
    public int StartCol { get; }
    public int Length { get; }
    public ShipAxis Axis { get; }
    public bool IsEnemy { get; }

    public Rect Bounds
    {
        get
        {
            double cell = BoardViewModel.CellSize;
            double lengthPixels = Length * cell;
            if (Axis == ShipAxis.Horizontal)
                return new Rect(StartCol * cell, StartRow * cell, lengthPixels, cell);

            double offset = (lengthPixels - cell) / 2d;
            return new Rect(
                (StartCol * cell) - offset,
                (StartRow * cell) + offset,
                lengthPixels,
                cell);
        }
    }

    public double Rotation => Axis == ShipAxis.Vertical ? 90 : 0;

    public bool IsSunk
    {
        get => _isSunk;
        private set
        {
            if (_isSunk == value) return;
            _isSunk = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(Opacity));
            OnPropertyChanged(nameof(StrokeColor));
            OnPropertyChanged(nameof(BackgroundColor));
        }
    }

    public bool IsRevealed
    {
        get => _isRevealed;
        private set
        {
            if (_isRevealed == value) return;
            _isRevealed = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(Opacity));
        }
    }

    public double Opacity
    {
        get
        {
            if (!IsRevealed)
                return 0;

            return IsSunk ? 0.4 : 0.95;
        }
    }

    public Color StrokeColor => IsSunk
        ? Color.FromArgb("#ff8a6b")
        : IsEnemy
            ? Color.FromArgb("#7c8ea6")
            : Color.FromArgb("#70839a");

    public Color BackgroundColor => IsSunk
        ? Color.FromArgb("#442018")
        : IsEnemy
            ? Color.FromArgb("#1a2836")
            : Color.FromArgb("#1d2734");

    public ShipSpriteVm(
        string name,
        string imageSource,
        int startRow,
        int startCol,
        int length,
        ShipAxis axis,
        bool isEnemy = false,
        bool isRevealed = true)
    {
        Name = name;
        ImageSource = imageSource;
        StartRow = startRow;
        StartCol = startCol;
        Length = length;
        Axis = axis;
        IsEnemy = isEnemy;
        _isRevealed = isRevealed;
    }

    public void MarkSunk()
    {
        IsSunk = true;
    }

    public void Reveal()
    {
        IsRevealed = true;
    }
}

public class PlacementShipVm : ObservableObject
{
    private bool _isPlaced;
    private bool _isSelected;

    public string Name { get; }
    public int Size { get; }
    public string ImageSource { get; }

    public bool IsPlaced
    {
        get => _isPlaced;
        set
        {
            if (_isPlaced == value) return;
            _isPlaced = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(DisplayName));
            OnPropertyChanged(nameof(CardBackground));
            OnPropertyChanged(nameof(CardStroke));
        }
    }

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value) return;
            _isSelected = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CardBackground));
            OnPropertyChanged(nameof(CardStroke));
        }
    }

    public string DisplayName => IsPlaced ? $"{Name} ({Size}) - Placed" : $"{Name} ({Size})";

    public Color CardBackground => IsPlaced
        ? Color.FromArgb("#23553e")
        : IsSelected
            ? Color.FromArgb("#2a4f87")
            : Color.FromArgb("#1c2735");

    public Color CardStroke => IsPlaced
        ? Color.FromArgb("#7fe3ab")
        : IsSelected
            ? Color.FromArgb("#9fc3ff")
            : Color.FromArgb("#4f6178");

    public PlacementShipVm(string name, int size, string imageSource)
    {
        Name = name;
        Size = size;
        ImageSource = imageSource;
    }
}

public readonly record struct BoardCoordinate(int Row, int Col);

public sealed record ShipTemplate(string Name, int Size, string ImageSource);

public enum ShipAxis
{
    Horizontal = 0,
    Vertical = 1
}

public abstract class ObservableObject : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        if (propertyName is null)
            return;

        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
