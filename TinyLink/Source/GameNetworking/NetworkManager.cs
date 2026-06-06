using Foster.Framework;
using LiteNetLib;
using TinyLink;

namespace GameNetworking;

public abstract partial class NetworkManager
{
    protected const byte MaxPlayers = 3; // never more than byte limit -1 (255 is considered an unset id)
    protected const byte HostPlayerId = 0;
    protected NetManager _netManager;
    protected EventBasedNetListener _listener;
    public bool IsHost {get; protected set;}
    public readonly Game Game;
    public Dictionary<int, PlayerData> PlayersData {get; set;} = new();
    public Dictionary<int, Player> NetworkPlayers {get; protected set;} = new();
    public abstract byte LocalPlayerId {get; protected set;}

    public Dictionary<(RoomCell,byte), ActorData> ActorsData {get; set;} = new();
    public Dictionary<int, Actor> NetworkActors {get; protected set;} = new();



    private static NetworkManager _networkManager = null!;
    public static NetworkManager Instance => _networkManager;

    public NetworkManager(Game game)
    {
        _listener = new EventBasedNetListener();
        _netManager = new NetManager(_listener);
        Game = game;
        _networkManager = this;

        SetupListeners();
    }

    protected abstract void SetupListeners();

    protected abstract void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliveryMethod);
    public abstract void BroadcastUpdate<T>(MessageType messageType, T update,  NetPeer? excludePeer = null, DeliveryMethod deliveryMethod = DeliveryMethod.Sequenced) 
        where T : struct, ISerializable<T>;

    public virtual void Poll()
    {
        _netManager.PollEvents();
    }

    public virtual void Stop()
    {
        _netManager.Stop();
    }

    public bool IsRunning => _netManager.IsRunning;
}
