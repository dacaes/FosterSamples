using Foster.Framework;
using LiteNetLib;
using TinyLink;

namespace GameNetworking;

public abstract partial class NetworkManager
{
    protected NetManager _netManager;
    protected EventBasedNetListener _listener;
    public readonly Game Game;
    public Dictionary<int, PlayerData> PlayersData {get; set;}
    public Dictionary<int, Player> NetworkPlayers {get; protected set;} = new();
    public abstract byte LocalPlayerId {get; protected set;}

    private static NetworkManager _networkManager = null!;
    public static NetworkManager Instance => _networkManager;

    public NetworkManager(Game game)
    {
        PlayersData = new Dictionary<int, PlayerData>();
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
