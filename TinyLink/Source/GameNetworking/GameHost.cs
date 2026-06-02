using LiteNetLib;
using LiteNetLib.Utils;
using TinyLink;

namespace GameNetworking;

public partial class GameHost : NetworkManager
{
    private byte _nextPlayerId = 1;
    private const byte MaxPlayers = 32; // never more than byte limit -1 (255 is considered an unset id)
    private const byte HostPlayerId = 0;
	public override byte LocalPlayerId {get => HostPlayerId; protected set {}}   // no set, it is a constant value for Host

    protected Dictionary<int, int> _peerIdToPlayerId = new Dictionary<int, int>(); // maps peer.Id to playerId


    public static GameHost RunHost(Game game, int port)
    {
        // Console.WriteLine("Enter port (default 9050): ");
        // string? portInput = Console.ReadLine();
        
        // port = int.TryParse(portInput, out var parsedPort) ? parsedPort : 9050;

        Console.WriteLine($"Host running on port {port}.");
        return new GameHost(game, port);
    }

    public GameHost(Game game, int port = 9050) : base(game)
    {
        _playersData[LocalPlayerId] = new PlayerData
        {
            playerId = LocalPlayerId,
            positionPayload = new Point2Payload(0, 0),
            state = (int) Player.States.Start
        };
        Console.WriteLine($"[HOST] Local player created with ID {LocalPlayerId}");

        if (_netManager.Start(port))
        {
            Console.WriteLine($"[HOST] Server started on port {port}");
        }
        else
        {
            Console.WriteLine("[HOST] Failed to start server");
        }
    }

    protected override void SetupListeners()
    {
        _listener.ConnectionRequestEvent += OnConnectionRequest;
        _listener.PeerConnectedEvent += OnPeerConnected;
        _listener.PeerDisconnectedEvent += OnPeerDisconnected;
        _listener.NetworkReceiveEvent += OnNetworkReceive;
    }

    private void OnConnectionRequest(ConnectionRequest request)
    {
        if (_playersData.Count < MaxPlayers)
        {
            request.AcceptIfKey("game");
            Console.WriteLine($"[HOST] Connection request accepted");
        }
        else
        {
            request.Reject();
            Console.WriteLine("[HOST] Connection rejected - server full");
        }
    }

    private void OnPeerConnected(NetPeer peer)
    {
        byte playerId = _nextPlayerId++;
        
        var newPlayer = new PlayerData
        {
            playerId = playerId,
            positionPayload = new Point2Payload(0, 0),
            state = (int) Player.States.Start
        };
        
        _playersData[playerId] = newPlayer;
        _peerIdToPlayerId[peer.Id] = playerId;  // Track the peer-to-player mapping
        Console.WriteLine($"[HOST] Player {playerId} connected (Total: {_playersData.Count})");
        // send assigned id explicitly, then send snapshot
        var idWriter = new NetDataWriter();
        idWriter.Put((byte)MessageType.AssignedPlayerId);
        idWriter.Put(playerId);
        peer.Send(idWriter, DeliveryMethod.ReliableOrdered);

        SendAllPlayersSnapshot(peer);
        BroadcastPlayerJoined(newPlayer, peer);
    }

    private void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        // Use the peer-to-player mapping to find the player
        if (_peerIdToPlayerId.TryGetValue(peer.Id, out int playerId))
        {
            _playersData.Remove(playerId);
            NetworkPlayers.Remove(playerId);
            _peerIdToPlayerId.Remove(peer.Id);
            Console.WriteLine($"[HOST] Player {playerId} disconnected (Total: {_playersData.Count})");
            BroadcastPlayerLeft(playerId);
        }
    }

    protected override void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliveryMethod)
    {
        byte messageType = reader.GetByte();

        switch ((MessageType)messageType)
        {
            case MessageType.PositionUpdate:
                var positionUpdate = HandlePositionUpdate(reader);
                BroadcastUpdate(MessageType.PositionUpdate, positionUpdate, peer);
                break;

            case MessageType.StateUpdate:
                var stateUpdate = HandleStateUpdate(reader);
                BroadcastUpdate(MessageType.StateUpdate, stateUpdate, peer);
                break;

            case MessageType.FlagsUpdate:
                var facingUpdate = HandleFlagsUpdate(reader);
                BroadcastUpdate(MessageType.FlagsUpdate, facingUpdate, peer);
                break;

            case MessageType.PlayerUpdate:
                var playerData = HandlePlayerUpdate(reader);
                BroadcastUpdate(MessageType.PlayerUpdate, playerData, peer);
                break;
        }

        reader.Recycle();
    }

    public override void Stop()
    {
        base.Stop();
        Console.WriteLine("[HOST] Server stopped");
    }
}
