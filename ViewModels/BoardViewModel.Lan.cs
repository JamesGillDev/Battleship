using Battleship.GameCore;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Graphics;

namespace BattleshipMaui.ViewModels;

public partial class BoardViewModel
{
    private const int DefaultLanPort = 47652;

    private readonly ILanSessionService _lanSessionService;
    private MatchMode _selectedMatchMode = MatchMode.SoloCpu;
    private LanConnectionState _lanConnectionState = LanConnectionState.Disconnected;
    private LanRole _lanRole = LanRole.None;
    private string _lanHostAddress = string.Empty;
    private string _lanPortText = DefaultLanPort.ToString();
    private string _lanRemoteEndpoint = "Not connected";
    private string _lanLastError = string.Empty;
    private string _lanLocalAddressSummary = string.Empty;
    private bool _isLocalFleetReady;
    private bool _isRemoteFleetReady;
    private bool _hasSharedLocalFleetWithPeer;
    private BoardCellVm? _pendingLanShotCell;

    public MatchMode SelectedMatchMode
    {
        get => _selectedMatchMode;
        private set
        {
            if (_selectedMatchMode == value)
                return;

            _selectedMatchMode = value;
            OnPropertyChanged();
            RaiseLanUiStateChanged();
        }
    }

    public bool IsLanMode => SelectedMatchMode == MatchMode.Lan;
    public bool IsCpuMode => !IsLanMode;

    public LanConnectionState LanConnectionState
    {
        get => _lanConnectionState;
        private set
        {
            if (_lanConnectionState == value)
                return;

            _lanConnectionState = value;
            OnPropertyChanged();
            RaiseLanUiStateChanged();
        }
    }

    public LanRole LanRole
    {
        get => _lanRole;
        private set
        {
            if (_lanRole == value)
                return;

            _lanRole = value;
            OnPropertyChanged();
            RaiseLanUiStateChanged();
        }
    }

    public string LanHostAddress
    {
        get => _lanHostAddress;
        set
        {
            string normalized = value?.Trim() ?? string.Empty;
            if (_lanHostAddress == normalized)
                return;

            _lanHostAddress = normalized;
            OnPropertyChanged();
            RaiseLanUiStateChanged();
        }
    }

    public string LanPortText
    {
        get => _lanPortText;
        set
        {
            string normalized = string.IsNullOrWhiteSpace(value)
                ? DefaultLanPort.ToString()
                : value.Trim();

            if (_lanPortText == normalized)
                return;

            _lanPortText = normalized;
            OnPropertyChanged();
            RaiseLanUiStateChanged();
        }
    }

    public string LanRemoteEndpoint
    {
        get => _lanRemoteEndpoint;
        private set
        {
            string normalized = string.IsNullOrWhiteSpace(value) ? "Not connected" : value;
            if (_lanRemoteEndpoint == normalized)
                return;

            _lanRemoteEndpoint = normalized;
            OnPropertyChanged();
            RaiseLanUiStateChanged();
        }
    }

    public bool IsLanConnected => LanConnectionState == LanConnectionState.Connected;

    public bool CanHostLan => IsLanMode && (LanConnectionState is LanConnectionState.Disconnected or LanConnectionState.Error);
    public bool CanJoinLan =>
        IsLanMode &&
        (LanConnectionState is LanConnectionState.Disconnected or LanConnectionState.Error) &&
        !string.IsNullOrWhiteSpace(LanHostAddress);
    public bool CanDisconnectLan => IsLanMode && (LanConnectionState is LanConnectionState.Hosting or LanConnectionState.Connecting or LanConnectionState.Connected);

    public Color SoloMatchTabBackground => IsCpuMode
        ? ResolveThemeColor("GameColorAccentSoft", "#3f8ecd")
        : ResolveThemeColor("GameColorSurfaceAlt", "#1d3146");

    public Color LanMatchTabBackground => IsLanMode
        ? ResolveThemeColor("GameColorAccentSoft", "#3f8ecd")
        : ResolveThemeColor("GameColorSurfaceAlt", "#1d3146");

    public string LanStatusLine => IsCpuMode
        ? "Solo vs CPU is active."
        : LanConnectionState switch
        {
            LanConnectionState.Hosting => $"Hosting on port {ResolveLanPortOrDefault()}. Waiting for the other commander.",
            LanConnectionState.Connecting => $"Connecting to {LanHostAddress}:{ResolveLanPortOrDefault()}...",
            LanConnectionState.Connected => LanRole switch
            {
                LanRole.Host => $"LAN match connected. You are the host. Remote endpoint: {LanRemoteEndpoint}.",
                LanRole.Guest => $"LAN match connected. You joined the host at {LanRemoteEndpoint}.",
                _ => $"LAN match connected to {LanRemoteEndpoint}."
            },
            LanConnectionState.Error when !string.IsNullOrWhiteSpace(_lanLastError) => _lanLastError,
            _ => "LAN idle. Host on one PC, join from the other using the host IP and port."
        };

    public string LanConnectionHelpText => IsCpuMode
        ? "Switch to LAN Match to play another person on your local network."
        : string.IsNullOrWhiteSpace(_lanLocalAddressSummary)
            ? $"Share this PC's LAN IP and port {ResolveLanPortOrDefault()} with the other player."
            : $"This PC LAN IPs: {_lanLocalAddressSummary}. The joining player should enter one of these IPs and port {ResolveLanPortOrDefault()}.";

    private void InitializeLanSession()
    {
        _lanSessionService.ConnectionChanged += OnLanConnectionChanged;
        _lanSessionService.PayloadReceived += OnLanPayloadReceived;
        _lanLocalAddressSummary = string.Join(", ", _lanSessionService.GetLocalAddresses());
        RaiseLanUiStateChanged();
    }

    private void RaiseLanUiStateChanged()
    {
        OnPropertyChanged(nameof(IsLanMode));
        OnPropertyChanged(nameof(IsCpuMode));
        OnPropertyChanged(nameof(IsLanConnected));
        OnPropertyChanged(nameof(CanHostLan));
        OnPropertyChanged(nameof(CanJoinLan));
        OnPropertyChanged(nameof(CanDisconnectLan));
        OnPropertyChanged(nameof(SoloMatchTabBackground));
        OnPropertyChanged(nameof(LanMatchTabBackground));
        OnPropertyChanged(nameof(LanStatusLine));
        OnPropertyChanged(nameof(LanConnectionHelpText));
    }

    private string BuildGameStartOverlaySubtitle()
    {
        return IsLanMode
            ? "1) Select LAN Match. On one PC choose Host LAN; on the other enter the host IP and choose Join LAN.\n2) Place your fleet locally on both PCs.\n3) After both fleets are deployed, the host takes the first shot.\n4) If Windows asks about firewall access, allow the app on your private network."
            : "1) Pick a ship, then hover over Your Fleet to preview live placement.\n2) Right-click to rotate. Left-click to deploy.\n3) Fire on Enemy Waters and sink the full fleet before they sink yours.\n4) Use Theme Shift for dramatic style changes and Settings for music/FX.";
    }

    private string BuildStartNewGameStatusMessage()
    {
        if (IsCpuMode)
            return "Select a ship and tap Your Fleet board to place it.";

        return IsLanConnected
            ? "Select a ship and deploy your fleet. The host opens fire once both fleets are ready."
            : "Select a ship and deploy your fleet. Then host a match or join the host PC by IP.";
    }

    private void ResetLanMissionState()
    {
        _isLocalFleetReady = false;
        _isRemoteFleetReady = false;
        _hasSharedLocalFleetWithPeer = false;
        ClearPendingLanShotLock();
    }

    private bool TryCompleteLanPlacementPhase()
    {
        if (IsCpuMode)
            return false;

        SetSelectedPlacementShip(null);
        ClearPlacementPreview();
        IsPlacementPhase = false;
        IsPlayerTurn = false;
        _isLocalFleetReady = true;
        _hasSharedLocalFleetWithPeer = false;
        TurnMessage = "Awaiting opponent";
        StatusMessage = IsLanConnected
            ? "Fleet deployed. Transmitting layout to the other player."
            : "Fleet deployed. Connect to the other PC to transmit your fleet.";
        ApplyAutoBoardFocus();
        EmitFeedback(GameFeedbackCue.PlacementComplete);
        _ = ShareLocalFleetIfReadyAsync();
        TryStartLanBattle();
        return true;
    }

    private bool TryHandleLanEnemyCellTapped(BoardCellVm targetCell)
    {
        if (IsCpuMode)
            return false;

        if (!IsLanConnected)
        {
            StatusMessage = "Connect to the other player before firing.";
            return true;
        }

        if (!_isRemoteFleetReady)
        {
            StatusMessage = "Waiting for the other player to finish deploying their fleet.";
            return true;
        }

        _ = FireLanShotAsync(targetCell);
        return true;
    }

    private async Task SetMatchModeAsync(string? token)
    {
        if (string.IsNullOrWhiteSpace(token) || !Enum.TryParse(token, ignoreCase: true, out MatchMode mode) || mode == SelectedMatchMode)
            return;

        if (mode == MatchMode.SoloCpu)
            await _lanSessionService.DisconnectAsync();

        SelectedMatchMode = mode;
        ResetLanMissionState();

        if (IsCpuMode)
        {
            StatusMessage = "Solo vs CPU selected.";
            StartNewGame(broadcastLanReset: false);
            return;
        }

        await _lanSessionService.DisconnectAsync();
        StatusMessage = "LAN Match selected. Host on one PC and join from the other using the host IP and port.";
        StartNewGame(broadcastLanReset: false);
    }

    private async Task HostLanAsync()
    {
        if (!TryParseLanPort(out int port, out string errorMessage))
        {
            StatusMessage = errorMessage;
            return;
        }

        try
        {
            await _lanSessionService.StartHostingAsync(port);
            StatusMessage = string.IsNullOrWhiteSpace(_lanLocalAddressSummary)
                ? $"Hosting on port {port}. Share this PC's LAN IP with the other player."
                : $"Hosting on {_lanLocalAddressSummary}:{port}. Share one of those IPs with the other player.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Could not start LAN host: {ex.Message}";
        }
    }

    private async Task JoinLanAsync()
    {
        if (string.IsNullOrWhiteSpace(LanHostAddress))
        {
            StatusMessage = "Enter the host computer's LAN IP address first.";
            return;
        }

        if (!TryParseLanPort(out int port, out string errorMessage))
        {
            StatusMessage = errorMessage;
            return;
        }

        try
        {
            await _lanSessionService.JoinAsync(LanHostAddress, port);
            StatusMessage = $"Connected to {LanHostAddress}:{port}. Deploy your fleet when ready.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Could not join LAN host: {ex.Message}";
        }
    }

    private async Task DisconnectLanAsync()
    {
        await _lanSessionService.DisconnectAsync();
        StatusMessage = "LAN session closed.";
    }

    private void OnLanConnectionChanged(object? sender, LanConnectionChangedEventArgs e)
    {
        RunOnMainThread(() =>
        {
            LanConnectionState = e.State;
            LanRole = e.Role;
            LanRemoteEndpoint = e.RemoteEndpoint ?? "Not connected";
            _lanLastError = e.ErrorMessage ?? string.Empty;
            _lanLocalAddressSummary = string.Join(", ", e.LocalAddresses);
            RaiseLanUiStateChanged();

            if (!IsLanMode)
                return;

            switch (e.State)
            {
                case LanConnectionState.Hosting:
                    TurnMessage = "Awaiting connection";
                    break;

                case LanConnectionState.Connecting:
                    TurnMessage = "Connecting";
                    break;

                case LanConnectionState.Connected:
                    if (IsPlacementPhase)
                        StatusMessage = "Connection established. Both players can place ships now. The host fires first.";

                    _ = ShareLocalFleetIfReadyAsync();
                    TryStartLanBattle();
                    break;

                case LanConnectionState.Error:
                case LanConnectionState.Disconnected:
                    _isRemoteFleetReady = false;
                    _hasSharedLocalFleetWithPeer = false;
                    ClearPendingLanShotLock();
                    SetEnemyTurnResolutionState(false);
                    SetPlayerShotResolutionState(false);
                    IsPlayerTurn = false;

                    if (!IsPlacementPhase && !IsGameOver)
                        TurnMessage = "Connection lost";

                    if (!string.IsNullOrWhiteSpace(_lanLastError))
                        StatusMessage = _lanLastError;
                    break;
            }
        });
    }

    private void OnLanPayloadReceived(object? sender, LanPayloadReceivedEventArgs e)
    {
        RunOnMainThread(() =>
        {
            if (IsCpuMode)
                return;

            switch (e.Kind)
            {
                case LanPayloadKind.Fleet:
                    HandleRemoteFleetReceived(e.Fleet);
                    break;

                case LanPayloadKind.Shot:
                    HandleRemoteShotReceived(e.Shot);
                    break;

                case LanPayloadKind.ShotResult:
                    HandleRemoteShotResultReceived(e.ShotResult);
                    break;

                case LanPayloadKind.Reset:
                    StartNewGame(broadcastLanReset: false);
                    StatusMessage = "The other player started a new mission. Re-deploy your fleet.";
                    break;
            }
        });
    }

    private void HandleRemoteFleetReceived(IReadOnlyList<ShipPlacementPacket> placements)
    {
        _enemyBoard = new GameBoard(Size);
        var enemyFleet = CreateFleet();
        _enemyBoard.SetFleet(enemyFleet);

        ResetCells(EnemyCells, clearShips: true);
        EnemyShipSprites.Clear();
        _enemySpritesByName.Clear();
        ShowEnemyFleet = false;

        if (!TryApplyFleetPlacements(_enemyBoard, placements))
        {
            StatusMessage = "The remote fleet layout could not be loaded.";
            return;
        }

        BuildEnemyShipSprites(enemyFleet);
        _isRemoteFleetReady = true;
        StatusMessage = _isLocalFleetReady
            ? "Opponent fleet received. Battle may begin."
            : "Opponent fleet received. Finish placing your ships.";
        TryStartLanBattle();
    }

    private void TryStartLanBattle()
    {
        if (!IsLanMode || !_isLocalFleetReady || !_isRemoteFleetReady || !IsLanConnected)
            return;

        bool hostStarts = LanRole == LanRole.Host;
        IsPlayerTurn = hostStarts;
        TurnMessage = hostStarts ? "Your turn" : "Opponent turn";
        StatusMessage = hostStarts
            ? "Both fleets are ready. Open fire on Enemy Waters."
            : "Both fleets are ready. Waiting for the host to fire first.";
        ApplyAutoBoardFocus();
    }

    private async Task ShareLocalFleetIfReadyAsync()
    {
        if (!IsLanMode || !_isLocalFleetReady || _hasSharedLocalFleetWithPeer || !IsLanConnected || _playerBoard is null)
            return;

        try
        {
            await _lanSessionService.SendFleetAsync(BuildFleetPlacementPackets(_playerBoard.Fleet));
            _hasSharedLocalFleetWithPeer = true;

            if (!_isRemoteFleetReady)
                StatusMessage = "Fleet transmitted. Waiting for the other player to deploy.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Could not transmit your fleet: {ex.Message}";
        }
    }

    private async Task FireLanShotAsync(BoardCellVm targetCell)
    {
        SetPlayerShotResolutionState(true);
        _pendingLanShotCell = targetCell;
        targetCell.SetTargetLocked(true);

        try
        {
            string targetCoordinate = ToBoardCoordinate(targetCell.Row, targetCell.Col);
            await _lanSessionService.SendShotAsync(new BoardCoordinate(targetCell.Row, targetCell.Col));
            TurnMessage = "Shot in flight";
            StatusMessage = $"Shot transmitted to {targetCoordinate}. Awaiting battle report.";
        }
        catch (Exception ex)
        {
            ClearPendingLanShotLock();
            SetPlayerShotResolutionState(false);
            StatusMessage = $"Could not send shot: {ex.Message}";
        }
    }

    private void HandleRemoteShotReceived(BoardCoordinate target)
    {
        if (_playerBoard is null)
            return;

        ShotInfo shot = _playerBoard.Attack(target.Row, target.Col);
        ApplyShotResult(PlayerCells, shot);

        if (shot.Result == AttackResult.Sunk &&
            shot.SunkShipName is not null &&
            _playerSpritesByName.TryGetValue(shot.SunkShipName, out var sprite))
        {
            sprite.MarkSunk();
        }

        OnPropertyChanged(nameof(ScoreLine));
        EmitShotFeedback(shot);

        string coordinate = ToBoardCoordinate(target.Row, target.Col);
        EnemyLastShotMessage = $"Enemy last shot: {coordinate} - {shot.Message}";

        bool defeated = _playerBoard.AllShipsSunk;
        _ = SendLanShotResultSafeAsync(new LanShotResultPacket(
            target.Row,
            target.Col,
            shot.Result,
            shot.IsHit,
            shot.SunkShipName,
            shot.Message,
            defeated));

        if (defeated)
        {
            IsGameOver = true;
            IsPlayerTurn = false;
            TurnMessage = "Defeat";
            StatusMessage = "All your ships have been sunk. You lose.";
            RecordGameOutcome(GameOutcome.Loss);
            EmitFeedback(GameFeedbackCue.Loss);
            RevealEnemyFleet();
            ApplyAutoBoardFocus();
            ShowGameOverOverlay(GameOutcome.Loss);
            return;
        }

        IsPlayerTurn = true;
        TurnMessage = "Your turn";
        StatusMessage = $"{BuildEnemyShotCallout(coordinate, shot)} Your turn.";
        ApplyAutoBoardFocus();
    }

    private void HandleRemoteShotResultReceived(LanShotResultPacket packet)
    {
        ClearPendingLanShotLock();
        SetPlayerShotResolutionState(false);

        if (packet.Result is AttackResult.AlreadyTried or AttackResult.Invalid)
        {
            TurnMessage = "Your turn";
            StatusMessage = string.IsNullOrWhiteSpace(packet.Message)
                ? "The remote board rejected that shot. Try another target."
                : packet.Message;
            IsPlayerTurn = true;
            ApplyAutoBoardFocus();
            return;
        }

        int targetIndex = packet.Row * Size + packet.Col;
        if (targetIndex < 0 || targetIndex >= EnemyCells.Count || EnemyCells[targetIndex].MarkerState != ShotMarkerState.None)
            return;

        var shot = new ShotInfo(packet.Row, packet.Col, packet.Result, packet.IsHit, packet.SunkShipName, packet.Message);
        SyncEnemyBoardWithShotResult(shot);
        RecordPlayerShot(shot);
        ApplyShotResult(EnemyCells, shot);
        EmitShotFeedback(shot);

        PlayerLastShotMessage = $"Your last shot: {ToBoardCoordinate(shot.Row, shot.Col)} - {shot.Message}";
        StatusMessage = BuildPlayerShotCallout(shot);
        OnPropertyChanged(nameof(ScoreLine));

        if (shot.Result == AttackResult.Sunk)
            RevealEnemyShipOnSunk(shot.SunkShipName);

        if (packet.GameOver)
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
        TurnMessage = "Opponent turn";
        StatusMessage = $"{BuildPlayerShotCallout(shot)} Awaiting incoming fire.";
        ApplyAutoBoardFocus();
    }

    private void SyncEnemyBoardWithShotResult(ShotInfo shot)
    {
        if (_enemyBoard is null || !_enemyBoard.InBounds(shot.Row, shot.Col) || _enemyBoard.IsAlreadyAttacked(shot.Row, shot.Col))
            return;

        _enemyBoard.Attack(shot.Row, shot.Col);
    }

    private void ClearPendingLanShotLock()
    {
        if (_pendingLanShotCell is not null)
            _pendingLanShotCell.SetTargetLocked(false);

        _pendingLanShotCell = null;
    }

    private async Task SendLanShotResultSafeAsync(LanShotResultPacket packet)
    {
        try
        {
            await _lanSessionService.SendShotResultAsync(packet);
        }
        catch
        {
        }
    }

    private async Task BroadcastLanResetIfNeededAsync()
    {
        if (!IsLanMode || !IsLanConnected)
            return;

        try
        {
            await _lanSessionService.SendResetAsync();
        }
        catch
        {
        }
    }

    private static IReadOnlyList<ShipPlacementPacket> BuildFleetPlacementPackets(IEnumerable<Ship> fleet)
    {
        return fleet
            .Where(ship => ship.Positions.Count > 0)
            .Select(ship =>
            {
                int row = ship.Positions.Min(position => position.Row);
                int col = ship.Positions.Min(position => position.Col);
                bool isVertical = ship.Positions.Select(position => position.Col).Distinct().Count() == 1;
                return new ShipPlacementPacket(
                    ship.Name,
                    row,
                    col,
                    isVertical ? ShipAxis.Vertical : ShipAxis.Horizontal,
                    ship.Size);
            })
            .ToArray();
    }

    private static bool TryApplyFleetPlacements(GameBoard board, IEnumerable<ShipPlacementPacket> placements)
    {
        var shipsByName = board.Fleet.ToDictionary(ship => ship.Name, StringComparer.OrdinalIgnoreCase);

        foreach (var placement in placements)
        {
            if (!shipsByName.TryGetValue(placement.Name, out var ship))
                return false;

            ShipOrientation orientation = placement.Axis == ShipAxis.Vertical
                ? ShipOrientation.Vertical
                : ShipOrientation.Horizontal;

            if (!board.TryPlaceShip(ship, placement.Row, placement.Col, orientation))
                return false;
        }

        return board.Fleet.All(ship => ship.IsPlaced);
    }

    private bool TryParseLanPort(out int port, out string errorMessage)
    {
        if (!int.TryParse(LanPortText, out port) || port is < 1024 or > 65535)
        {
            errorMessage = "Enter a LAN port between 1024 and 65535.";
            port = DefaultLanPort;
            return false;
        }

        errorMessage = string.Empty;
        return true;
    }

    private int ResolveLanPortOrDefault()
    {
        return int.TryParse(LanPortText, out int port) && port is >= 1024 and <= 65535
            ? port
            : DefaultLanPort;
    }

    private static void RunOnMainThread(Action action)
    {
        try
        {
            if (MainThread.IsMainThread)
            {
                action();
                return;
            }

            MainThread.BeginInvokeOnMainThread(action);
        }
        catch
        {
            action();
        }
    }
}
