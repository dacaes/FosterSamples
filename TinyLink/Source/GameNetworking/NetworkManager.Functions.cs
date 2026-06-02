using Foster.Framework;
using LiteNetLib;
using TinyLink;

namespace GameNetworking;

public abstract partial class NetworkManager
{
        protected void UpdatePlayersFromPlayerData()
    {
        foreach (var playerData in _playersData.Values)
        {
            if (NetworkPlayers.TryGetValue(playerData.playerId, out var player))
            {
                player.Position = playerData.position;
                player.Facing = playerData.Facing;
                player.NetworkMoving = playerData.Moving;
                player.fsm.SetState((Player.States)playerData.state);
            }
            else if(LocalPlayerId != playerData.playerId) // we don't want to add ourselves
            {
                var newPlayer = new Player
                {
                    networkId = playerData.playerId,
                    IsNetworkGhost = true,
                    Position = playerData.position,
                    Facing = playerData.Facing,
                    NetworkMoving = playerData.Moving,
                    Game = Game
                };
                newPlayer.fsm.SetState((Player.States)playerData.state);
                NetworkPlayers.Add(playerData.playerId, newPlayer);
            }

        }
    }

    protected PlayerData HandlePlayerUpdate(NetPacketReader reader)
    {
        var playerData = PlayerData.Deserialize(reader);

        if (_playersData.TryGetValue(playerData.playerId, out var player))
        {
            player.position = playerData.position;
            player.flagsPayload = playerData.flagsPayload;
            player.state = playerData.state;
            _playersData[playerData.playerId] = player;
        }

        UpdatePlayersFromPlayerData();
        return playerData;
    }

    protected PositionUpdateMessage HandlePositionUpdate(NetPacketReader reader)
    {
        var positionUpdate = PositionUpdateMessage.Deserialize(reader);

        if (_playersData.TryGetValue(positionUpdate.playerId, out var player))
        {
            player.position = positionUpdate.position;
            _playersData[positionUpdate.playerId] = player;
        }

        UpdatePlayersFromPlayerData();
        return positionUpdate;
    }

    protected StateUpdateMessage HandleStateUpdate(NetPacketReader reader)
    {
        var stateUpdate = StateUpdateMessage.Deserialize(reader);

        if (_playersData.TryGetValue(stateUpdate.playerId, out var player))
        {
            player.state = stateUpdate.state;
            _playersData[stateUpdate.playerId] = player;
        }

        UpdatePlayersFromPlayerData();
        return stateUpdate;
    }

    protected FlagsUpdateMessage HandleFlagsUpdate(NetPacketReader reader)
    {
        var facingUpdate = FlagsUpdateMessage.Deserialize(reader);

        if (_playersData.TryGetValue(facingUpdate.playerId, out var player))
        {
            player.flagsPayload = facingUpdate.flagsPayload;
            _playersData[facingUpdate.playerId] = player;
        }

        UpdatePlayersFromPlayerData();
        return facingUpdate;
    }

    public void UpdateLocalPlayer(Point2 position, Player.States state, Signs facing, bool moving)
    {
        if (!_playersData.TryGetValue(LocalPlayerId, out var player)) return;

        player.position = position;
        player.Facing = facing;
        player.Moving = moving;
        player.state = (byte)state;
        _playersData[LocalPlayerId] = player;

        BroadcastUpdate(MessageType.PlayerUpdate, player, null);
    }

    public void UpdateLocalPlayerPosition(Point2 position)
    {
        if (!_playersData.TryGetValue(LocalPlayerId, out var player)) return;

        player.positionPayload = new Point2Payload(position.X, position.Y);
        _playersData[LocalPlayerId] = player;

        var positionUpdate = new PositionUpdateMessage
        {
            playerId = player.playerId,
            positionPayload = new Point2Payload(position.X, position.Y)
        };

         BroadcastUpdate(MessageType.PositionUpdate, positionUpdate, null);
    }

    public void UpdateLocalPlayerState(byte state)
    {
        if (!_playersData.TryGetValue(LocalPlayerId, out var player)) return;

        player.state = state;
        _playersData[LocalPlayerId] = player;

        var stateUpdate = new StateUpdateMessage
        {
            playerId = player.playerId,
            state = player.state
        };

        BroadcastUpdate(MessageType.StateUpdate, stateUpdate, null);
    }

    public void UpdateLocalPlayerFacing(Signs facing)
    {
        if (!_playersData.TryGetValue(LocalPlayerId, out var player)) return;

        player.Facing = facing;
        _playersData[LocalPlayerId] = player;

        var flagsUpdate = new FlagsUpdateMessage
        {
            playerId = player.playerId,
            Facing = player.Facing,
            Moving = player.Moving
        };

        BroadcastUpdate(MessageType.FlagsUpdate, flagsUpdate, null);
    }

    public void UpdateLocalPlayerMoving(bool moving)
    {
        if (!_playersData.TryGetValue(LocalPlayerId, out var player)) return;

        player.Moving = moving;
        _playersData[LocalPlayerId] = player;

        var flagsUpdate = new FlagsUpdateMessage
        {
            playerId = player.playerId,
            Facing = player.Facing,
            Moving = player.Moving
        };

        BroadcastUpdate(MessageType.FlagsUpdate, flagsUpdate, null);
    }
}
