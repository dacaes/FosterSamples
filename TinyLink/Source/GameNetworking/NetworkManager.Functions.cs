using Foster.Framework;
using LiteNetLib;
using TinyLink;

namespace GameNetworking;

public abstract partial class NetworkManager
{
    protected void DeserializePlayer(PlayerData playerData)
    {
        if (NetworkPlayers.TryGetValue(playerData.playerId, out var player))
        {
            player.NetworkDeserialize();
        }
        else if(LocalPlayerId != playerData.playerId)
        {
            var newPlayer = new Player
            {
                NetworkId = playerData.playerId,
                IsNetworkGhost = true,
                Position = playerData.position,
                Facing = playerData.Facing,
                NetworkMoving = playerData.Moving,
                Game = Game
            };
            newPlayer.fsm.SetState(playerData.state);
            NetworkPlayers.Add(playerData.playerId, newPlayer);
        }
    }

    protected PlayerData HandlePlayerUpdate(NetPacketReader reader)
    {
        var playerData = PlayerData.Deserialize(reader);

        if (PlayersData.ContainsKey(playerData.playerId))
        {
            PlayersData[playerData.playerId] = playerData;
        }

        DeserializePlayer(playerData);

        return playerData;
    }

    protected PositionUpdateMessage HandlePositionUpdate(NetPacketReader reader)
    {
        var positionUpdate = PositionUpdateMessage.Deserialize(reader);

        if (PlayersData.TryGetValue(positionUpdate.playerId, out var playerData))
        {
            playerData.position = positionUpdate.position;
            PlayersData[positionUpdate.playerId] = playerData;
        }

        DeserializePlayer(playerData);

        return positionUpdate;
    }

    protected StateUpdateMessage HandleStateUpdate(NetPacketReader reader)
    {
        var stateUpdate = StateUpdateMessage.Deserialize(reader);

        if (PlayersData.TryGetValue(stateUpdate.playerId, out var playerData))
        {
            playerData.state = stateUpdate.state;
            PlayersData[stateUpdate.playerId] = playerData;
        }

        DeserializePlayer(playerData);

        return stateUpdate;
    }

    protected FlagsUpdateMessage HandleFlagsUpdate(NetPacketReader reader)
    {
        var facingUpdate = FlagsUpdateMessage.Deserialize(reader);

        if (PlayersData.TryGetValue(facingUpdate.playerId, out var playerData))
        {
            playerData.flagsPayload = facingUpdate.flagsPayload;
            PlayersData[facingUpdate.playerId] = playerData;
        }

        DeserializePlayer(playerData);

        return facingUpdate;
    }
}
