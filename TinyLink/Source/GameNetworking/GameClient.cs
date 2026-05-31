using LiteNetLib;
using LiteNetLib.Utils;
using TinyLink;

namespace GameNetworking;

public partial class GameClient : NetworkManager
{
    private NetPeer _serverPeer = null!;
    protected int _localPlayerId = -1;
    public override int LocalPlayerId {get => _localPlayerId; protected set => _localPlayerId = value;}

    public static GameClient RunClient(Game game)
    {
        // Console.WriteLine("Enter server IP (default 127.0.0.1): ");
        // string? ip = Console.ReadLine();
        // if (string.IsNullOrWhiteSpace(ip))
        // {
        //     ip = "127.0.0.1";
        // }

        // Console.WriteLine("Enter server port (default 9050): ");
        // string? portInput = Console.ReadLine();
        // var port = int.TryParse(portInput, out var parsedPort) ? parsedPort : 9050;

        var ip = "127.0.0.1";
        var port = 9050; 

        Console.WriteLine($"Connecting to {ip}:{port}...\n");
        return new GameClient(game, ip, port);
    }

    public GameClient(Game game, string serverIp, int serverPort) : base(game)
    {
        // Start the NetManager before attempting to connect
        if (_netManager.Start())
        {
            var writer = new NetDataWriter();
            writer.Put("game"); // send connection key expected by host
            _serverPeer = _netManager.Connect(serverIp, serverPort, writer);
            Console.WriteLine($"[CLIENT] Attempting to connect to {serverIp}:{serverPort}");
        }
        else
        {
            Console.WriteLine("[CLIENT] Failed to start network manager");
        }
    }

    protected override void SetupListeners()
    {
        _listener.PeerConnectedEvent += OnPeerConnected;
        _listener.PeerDisconnectedEvent += OnPeerDisconnected;
        _listener.NetworkReceiveEvent += OnNetworkReceive;
    }

    private void OnPeerConnected(NetPeer peer)
    {
        Console.WriteLine("[CLIENT] Connected to server");
    }

    private void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        Console.WriteLine("[CLIENT] Disconnected from server");
    }

    protected override void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliveryMethod)
    {
        byte messageType = reader.GetByte();

        switch ((MessageType)messageType)
        {
            case MessageType.AllPlayersSnapshot:
                HandleAllPlayersSnapshot(reader);
                break;

            case MessageType.AssignedPlayerId:
                HandleAssignedPlayerId(reader);
                break;

            case MessageType.PlayerJoined:
                HandlePlayerJoined(reader);
                break;

            case MessageType.PlayerLeft:
                HandlePlayerLeft(reader);
                break;

            case MessageType.PlayerUpdate:
                HandlePlayerUpdate(reader);
                break;

            case MessageType.PositionUpdate:
                HandlePositionUpdate(reader);
                break;

            case MessageType.StateUpdate:
                HandleStateUpdate(reader);
                break;
            
            case MessageType.FacingUpdate:
                HandleStateUpdate(reader);
                break;
        }

        reader.Recycle();
    }

    public override void Stop()
    {
        base.Stop();
        Console.WriteLine("[CLIENT] Disconnected");
    }

    public bool IsConnected() => _serverPeer != null && _serverPeer.ConnectionState == ConnectionState.Connected;
}
