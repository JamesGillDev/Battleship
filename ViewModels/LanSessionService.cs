using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Battleship.GameCore;

namespace BattleshipMaui.ViewModels;

public enum MatchMode
{
    SoloCpu = 0,
    Lan = 1
}

public enum LanRole
{
    None = 0,
    Host = 1,
    Guest = 2
}

public enum LanConnectionState
{
    Disconnected = 0,
    Hosting = 1,
    Connecting = 2,
    Connected = 3,
    Error = 4
}

public enum LanPayloadKind
{
    Fleet = 0,
    Shot = 1,
    ShotResult = 2,
    Reset = 3
}

public readonly record struct ShipPlacementPacket(string Name, int Row, int Col, ShipAxis Axis, int Size);

public readonly record struct LanShotResultPacket(
    int Row,
    int Col,
    AttackResult Result,
    bool IsHit,
    string? SunkShipName,
    string Message,
    bool GameOver);

public sealed class LanConnectionChangedEventArgs : EventArgs
{
    public LanConnectionState State { get; }
    public LanRole Role { get; }
    public string? RemoteEndpoint { get; }
    public string? ErrorMessage { get; }
    public IReadOnlyList<string> LocalAddresses { get; }

    public LanConnectionChangedEventArgs(
        LanConnectionState state,
        LanRole role,
        IReadOnlyList<string> localAddresses,
        string? remoteEndpoint = null,
        string? errorMessage = null)
    {
        State = state;
        Role = role;
        LocalAddresses = localAddresses;
        RemoteEndpoint = remoteEndpoint;
        ErrorMessage = errorMessage;
    }
}

public sealed class LanPayloadReceivedEventArgs : EventArgs
{
    public LanPayloadKind Kind { get; }
    public IReadOnlyList<ShipPlacementPacket> Fleet { get; }
    public BoardCoordinate Shot { get; }
    public LanShotResultPacket ShotResult { get; }

    private LanPayloadReceivedEventArgs(
        LanPayloadKind kind,
        IReadOnlyList<ShipPlacementPacket>? fleet = null,
        BoardCoordinate shot = default,
        LanShotResultPacket shotResult = default)
    {
        Kind = kind;
        Fleet = fleet ?? Array.Empty<ShipPlacementPacket>();
        Shot = shot;
        ShotResult = shotResult;
    }

    public static LanPayloadReceivedEventArgs ForFleet(IReadOnlyList<ShipPlacementPacket> fleet)
        => new(LanPayloadKind.Fleet, fleet: fleet);

    public static LanPayloadReceivedEventArgs ForShot(BoardCoordinate shot)
        => new(LanPayloadKind.Shot, shot: shot);

    public static LanPayloadReceivedEventArgs ForShotResult(LanShotResultPacket shotResult)
        => new(LanPayloadKind.ShotResult, shotResult: shotResult);

    public static LanPayloadReceivedEventArgs ForReset()
        => new(LanPayloadKind.Reset);
}

public interface ILanSessionService
{
    event EventHandler<LanConnectionChangedEventArgs>? ConnectionChanged;
    event EventHandler<LanPayloadReceivedEventArgs>? PayloadReceived;

    LanConnectionState State { get; }
    LanRole Role { get; }
    string? RemoteEndpoint { get; }

    IReadOnlyList<string> GetLocalAddresses();
    Task StartHostingAsync(int port, CancellationToken cancellationToken = default);
    Task JoinAsync(string host, int port, CancellationToken cancellationToken = default);
    Task SendFleetAsync(IReadOnlyList<ShipPlacementPacket> fleet, CancellationToken cancellationToken = default);
    Task SendShotAsync(BoardCoordinate shot, CancellationToken cancellationToken = default);
    Task SendShotResultAsync(LanShotResultPacket shotResult, CancellationToken cancellationToken = default);
    Task SendResetAsync(CancellationToken cancellationToken = default);
    Task DisconnectAsync(CancellationToken cancellationToken = default);
}

public sealed class TcpLanSessionService : ILanSessionService
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly SemaphoreSlim _sendLock = new(1, 1);
    private readonly Encoding _utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    private CancellationTokenSource? _lifecycleCts;
    private TcpListener? _listener;
    private TcpClient? _client;
    private StreamReader? _reader;
    private StreamWriter? _writer;

    public event EventHandler<LanConnectionChangedEventArgs>? ConnectionChanged;
    public event EventHandler<LanPayloadReceivedEventArgs>? PayloadReceived;

    public LanConnectionState State { get; private set; } = LanConnectionState.Disconnected;
    public LanRole Role { get; private set; } = LanRole.None;
    public string? RemoteEndpoint { get; private set; }

    public IReadOnlyList<string> GetLocalAddresses()
    {
        try
        {
            return Dns.GetHostEntry(Dns.GetHostName())
                .AddressList
                .Where(address => address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(address))
                .Select(address => address.ToString())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(address => address, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    public async Task StartHostingAsync(int port, CancellationToken cancellationToken = default)
    {
        if (port is < 1024 or > 65535)
            throw new ArgumentOutOfRangeException(nameof(port), "Port must be between 1024 and 65535.");

        await DisconnectAsync(cancellationToken);

        _lifecycleCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        Role = LanRole.Host;
        State = LanConnectionState.Hosting;
        RemoteEndpoint = null;
        RaiseConnectionChanged();

        try
        {
            _listener = new TcpListener(IPAddress.Any, port);
            _listener.Start();
        }
        catch
        {
            await CleanupTransportAsync();
            Role = LanRole.None;
            State = LanConnectionState.Error;
            RaiseConnectionChanged("Could not start LAN host on the selected port.");
            throw;
        }

        _ = AcceptClientAsync(_listener, _lifecycleCts.Token);
    }

    public async Task JoinAsync(string host, int port, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(host))
            throw new ArgumentException("Host address is required.", nameof(host));

        if (port is < 1024 or > 65535)
            throw new ArgumentOutOfRangeException(nameof(port), "Port must be between 1024 and 65535.");

        await DisconnectAsync(cancellationToken);

        _lifecycleCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        Role = LanRole.Guest;
        State = LanConnectionState.Connecting;
        RemoteEndpoint = null;
        RaiseConnectionChanged();

        var client = new TcpClient
        {
            NoDelay = true
        };

        try
        {
            await client.ConnectAsync(host.Trim(), port, cancellationToken);
        }
        catch
        {
            client.Dispose();
            await CleanupTransportAsync();
            Role = LanRole.None;
            State = LanConnectionState.Error;
            RaiseConnectionChanged("Could not connect to the host. Check the IP, port, and Windows Firewall.");
            throw;
        }

        AttachClient(client, _lifecycleCts.Token);
    }

    public Task SendFleetAsync(IReadOnlyList<ShipPlacementPacket> fleet, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(fleet);

        return SendEnvelopeAsync(new LanEnvelope
        {
            Type = "fleet",
            Fleet = fleet.ToArray()
        }, cancellationToken);
    }

    public Task SendShotAsync(BoardCoordinate shot, CancellationToken cancellationToken = default)
    {
        return SendEnvelopeAsync(new LanEnvelope
        {
            Type = "shot",
            Shot = new CoordinateEnvelope
            {
                Row = shot.Row,
                Col = shot.Col
            }
        }, cancellationToken);
    }

    public Task SendShotResultAsync(LanShotResultPacket shotResult, CancellationToken cancellationToken = default)
    {
        return SendEnvelopeAsync(new LanEnvelope
        {
            Type = "shotResult",
            ShotResult = new ShotResultEnvelope
            {
                Row = shotResult.Row,
                Col = shotResult.Col,
                Result = shotResult.Result,
                IsHit = shotResult.IsHit,
                SunkShipName = shotResult.SunkShipName,
                Message = shotResult.Message,
                GameOver = shotResult.GameOver
            }
        }, cancellationToken);
    }

    public Task SendResetAsync(CancellationToken cancellationToken = default)
    {
        return SendEnvelopeAsync(new LanEnvelope
        {
            Type = "reset"
        }, cancellationToken);
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await CleanupTransportAsync();
        Role = LanRole.None;
        State = LanConnectionState.Disconnected;
        RemoteEndpoint = null;
        RaiseConnectionChanged();
    }

    private async Task SendEnvelopeAsync(LanEnvelope envelope, CancellationToken cancellationToken)
    {
        if (State != LanConnectionState.Connected || _writer is null)
            throw new InvalidOperationException("No LAN opponent is connected.");

        string payload = JsonSerializer.Serialize(envelope, SerializerOptions);

        await _sendLock.WaitAsync(cancellationToken);
        try
        {
            await _writer.WriteLineAsync(payload).WaitAsync(cancellationToken);
            await _writer.FlushAsync().WaitAsync(cancellationToken);
        }
        finally
        {
            _sendLock.Release();
        }
    }

    private async Task AcceptClientAsync(TcpListener listener, CancellationToken cancellationToken)
    {
        try
        {
            TcpClient client = await listener.AcceptTcpClientAsync(cancellationToken);
            AttachClient(client, cancellationToken);
        }
        catch (OperationCanceledException)
        {
        }
        catch (ObjectDisposedException)
        {
        }
        catch
        {
            await HandleTransportFailureAsync("The LAN host stopped accepting connections.");
        }
    }

    private void AttachClient(TcpClient client, CancellationToken cancellationToken)
    {
        client.NoDelay = true;
        _client = client;

        NetworkStream networkStream = client.GetStream();
        _reader = new StreamReader(networkStream, _utf8NoBom, detectEncodingFromByteOrderMarks: false, bufferSize: 4096, leaveOpen: true);
        _writer = new StreamWriter(networkStream, _utf8NoBom, bufferSize: 4096, leaveOpen: true)
        {
            AutoFlush = true
        };

        State = LanConnectionState.Connected;
        RemoteEndpoint = client.Client.RemoteEndPoint?.ToString();
        RaiseConnectionChanged();

        _ = ReceiveLoopAsync(cancellationToken);
    }

    private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested && _reader is not null)
            {
                string? line = await _reader.ReadLineAsync().WaitAsync(cancellationToken);
                if (line is null)
                    break;

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                LanEnvelope? envelope = JsonSerializer.Deserialize<LanEnvelope>(line, SerializerOptions);
                if (envelope is null)
                    continue;

                RaisePayloadReceived(envelope);
            }

            if (!cancellationToken.IsCancellationRequested)
                await HandleTransportFailureAsync("The other player disconnected.");
        }
        catch (OperationCanceledException)
        {
        }
        catch (ObjectDisposedException)
        {
        }
        catch
        {
            if (!cancellationToken.IsCancellationRequested)
                await HandleTransportFailureAsync("The LAN connection was interrupted.");
        }
    }

    private async Task HandleTransportFailureAsync(string errorMessage)
    {
        await CleanupTransportAsync();
        Role = LanRole.None;
        State = LanConnectionState.Error;
        RemoteEndpoint = null;
        RaiseConnectionChanged(errorMessage);
    }

    private async Task CleanupTransportAsync()
    {
        try
        {
            _lifecycleCts?.Cancel();
        }
        catch
        {
        }

        try
        {
            _listener?.Stop();
        }
        catch
        {
        }

        try
        {
            _client?.Close();
        }
        catch
        {
        }

        try
        {
            _writer?.Dispose();
            _reader?.Dispose();
            _client?.Dispose();
            _listener?.Server?.Dispose();
        }
        catch
        {
        }

        _writer = null;
        _reader = null;
        _client = null;
        _listener = null;

        if (_lifecycleCts is not null)
        {
            _lifecycleCts.Dispose();
            _lifecycleCts = null;
        }

        await Task.CompletedTask;
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

    private void RaisePayloadReceived(LanEnvelope envelope)
    {
        switch (envelope.Type)
        {
            case "fleet" when envelope.Fleet is { Length: > 0 } fleet:
                PayloadReceived?.Invoke(this, LanPayloadReceivedEventArgs.ForFleet(fleet));
                break;

            case "shot" when envelope.Shot is not null:
                PayloadReceived?.Invoke(this, LanPayloadReceivedEventArgs.ForShot(new BoardCoordinate(envelope.Shot.Row, envelope.Shot.Col)));
                break;

            case "shotResult" when envelope.ShotResult is not null:
                PayloadReceived?.Invoke(this, LanPayloadReceivedEventArgs.ForShotResult(new LanShotResultPacket(
                    envelope.ShotResult.Row,
                    envelope.ShotResult.Col,
                    envelope.ShotResult.Result,
                    envelope.ShotResult.IsHit,
                    envelope.ShotResult.SunkShipName,
                    envelope.ShotResult.Message ?? string.Empty,
                    envelope.ShotResult.GameOver)));
                break;

            case "reset":
                PayloadReceived?.Invoke(this, LanPayloadReceivedEventArgs.ForReset());
                break;
        }
    }

    private sealed class LanEnvelope
    {
        public string Type { get; set; } = string.Empty;
        public ShipPlacementPacket[]? Fleet { get; set; }
        public CoordinateEnvelope? Shot { get; set; }
        public ShotResultEnvelope? ShotResult { get; set; }
    }

    private sealed class CoordinateEnvelope
    {
        public int Row { get; set; }
        public int Col { get; set; }
    }

    private sealed class ShotResultEnvelope
    {
        public int Row { get; set; }
        public int Col { get; set; }
        public AttackResult Result { get; set; }
        public bool IsHit { get; set; }
        public string? SunkShipName { get; set; }
        public string? Message { get; set; }
        public bool GameOver { get; set; }
    }
}
