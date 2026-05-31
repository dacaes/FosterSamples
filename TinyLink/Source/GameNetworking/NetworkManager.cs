using Foster.Framework;
using LiteNetLib;
using TinyLink;

namespace GameNetworking;

public abstract class NetworkManager
{
    protected NetManager _netManager;
    protected EventBasedNetListener _listener;
    protected Dictionary<int, PlayerData> _players;
    public Dictionary<int, Player> NetworkPlayers {get; protected set;} = new();

    private Game game;

    public IReadOnlyDictionary<int, PlayerData> Players => _players;

    public NetworkManager(Game game)
    {
        _players = new Dictionary<int, PlayerData>();
        _listener = new EventBasedNetListener();
        _netManager = new NetManager(_listener);
        this.game = game;

        SetupListeners();
    }

    protected abstract void SetupListeners();

    protected abstract void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliveryMethod);

    public virtual void Poll()
    {
        _netManager.PollEvents();
    }

    public virtual void Stop()
    {
        _netManager.Stop();
    }

    public bool IsRunning => _netManager.IsRunning;

    public abstract int GetLocalPlayerId();

    protected void UpdatePlayersFromPlayerData()
    {
        foreach (var playerData in _players.Values)
        {
            if (NetworkPlayers.TryGetValue(playerData.playerId, out var player))
            {
                player.Position = playerData.position;
                player.Facing = playerData.facing ? Signs.Positive : Signs.Negative;
                player.fsm.SetState((Player.States)playerData.state);
            }
                
            else if(GetLocalPlayerId() != playerData.playerId) // we don't want to add ourselves
            {
                var newPlayer = new Player
                {
                    id = playerData.playerId,
                    IsNetworkGhost = true,
                    Position = playerData.position,
                    Facing = playerData.facing ? Signs.Positive : Signs.Negative,
                    Game = game
                };
                newPlayer.fsm.SetState((Player.States)playerData.state);
                NetworkPlayers.Add(playerData.playerId, newPlayer);
            }

        }
    }

    protected PlayerData HandlePlayerUpdate(NetPacketReader reader)
    {
        var playerData = PlayerData.Deserialize(reader);

        if (_players.TryGetValue(playerData.playerId, out var player))
        {
            player.position = playerData.position;
            player.facing = playerData.facing;
            player.state = playerData.state;
            _players[playerData.playerId] = player;
        }

        UpdatePlayersFromPlayerData();
        return playerData;
    }

    protected PositionUpdateMessage HandlePositionUpdate(NetPacketReader reader)
    {
        var positionUpdate = PositionUpdateMessage.Deserialize(reader);

        if (_players.TryGetValue(positionUpdate.playerId, out var player))
        {
            player.position = positionUpdate.position;
            _players[positionUpdate.playerId] = player;
        }

        UpdatePlayersFromPlayerData();
        return positionUpdate;
    }

    protected StateUpdateMessage HandleStateUpdate(NetPacketReader reader)
    {
        var stateUpdate = StateUpdateMessage.Deserialize(reader);

        if (_players.TryGetValue(stateUpdate.playerId, out var player))
        {
            player.state = stateUpdate.state;
            _players[stateUpdate.playerId] = player;
        }

        UpdatePlayersFromPlayerData();
        return stateUpdate;
    }

    protected FacingUpdateMessage HandleFacingUpdate(NetPacketReader reader)
    {
        var facingUpdate = FacingUpdateMessage.Deserialize(reader);

        if (_players.TryGetValue(facingUpdate.playerId, out var player))
        {
            player.facing = facingUpdate.facing;
            _players[facingUpdate.playerId] = player;
        }

        UpdatePlayersFromPlayerData();
        return facingUpdate;
    }
}
