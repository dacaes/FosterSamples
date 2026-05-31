using Foster.Framework;
using LiteNetLib;
using TinyLink;

namespace GameNetworking;

public abstract partial class NetworkManager
{
    protected NetManager _netManager;
    protected EventBasedNetListener _listener;
    protected Dictionary<int, PlayerData> _playersData;
    public IReadOnlyDictionary<int, PlayerData> PlayersData => _playersData;
    public Dictionary<int, Player> NetworkPlayers {get; protected set;} = new();
    private Game game;
    public abstract int LocalPlayerId {get; protected set;}



    public NetworkManager(Game game)
    {
        _playersData = new Dictionary<int, PlayerData>();
        _listener = new EventBasedNetListener();
        _netManager = new NetManager(_listener);
        this.game = game;

        SetupListeners();
    }

    protected abstract void SetupListeners();

    protected abstract void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliveryMethod);
    protected abstract void BroadcastUpdate<T>(MessageType messageType, T update,  NetPeer? excludePeer = null, DeliveryMethod deliveryMethod = DeliveryMethod.Sequenced) 
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
