using LiteNetLib;
using LiteNetLib.Utils;
using Foster.Framework;
using TinyLink;

namespace GameNetworking;

public partial class GameClient
{
     private void HandleAllPlayersSnapshot(NetPacketReader reader)
    {
        int count = reader.GetInt();
        _players.Clear();

        for (int i = 0; i < count; i++)
        {
            var playerData = PlayerData.Deserialize(reader);
            _players[playerData.playerId] = playerData;
        }

        UpdatePlayersFromPlayerData();

        Console.WriteLine($"[CLIENT] Received snapshot with {count} players. Local Player ID: {_localPlayerId}");
    }

    private void HandleAssignedPlayerId(NetPacketReader reader)
    {
        int id = reader.GetInt();
        _localPlayerId = id;
        Console.WriteLine($"[CLIENT] Assigned local player ID: {_localPlayerId}");
    }

    private void HandlePlayerJoined(NetPacketReader reader)
    {
        var player = PlayerData.Deserialize(reader);
        _players[player.playerId] = player;
        
        Console.WriteLine($"[CLIENT] Player {player.playerId} joined");
    }

    private void HandlePlayerLeft(NetPacketReader reader)
    {
        int playerId = reader.GetInt();
        _players.Remove(playerId);
        NetworkPlayers.Remove(playerId);
        
        Console.WriteLine($"[CLIENT] Player {playerId} left");
    }

    public void SendPositionUpdate(int x, int y)
    {
        if (!IsConnected())
        {
            Console.WriteLine("[CLIENT] Not connected to server");
            return;
        }

        var posUpdate = new PositionUpdateMessage
        {
            playerId = _localPlayerId,
            positionPayload = new Point2Payload(x, y)
        };

        var writer = new NetDataWriter();
        writer.Put((byte)MessageType.PositionUpdate);
        posUpdate.Serialize(writer);

        _serverPeer.Send(writer, DeliveryMethod.Sequenced);
    }

    public void SendStateUpdate(int state)
    {
        if (!IsConnected())
        {
            Console.WriteLine("[CLIENT] Not connected to server");
            return;
        }

        var stateUpdate = new StateUpdateMessage
        {
            playerId = _localPlayerId,
            state = state
        };

        var writer = new NetDataWriter();
        writer.Put((byte)MessageType.StateUpdate);
        stateUpdate.Serialize(writer);

        _serverPeer.Send(writer, DeliveryMethod.Sequenced);
    }

    public void SendFacingUpdate(Signs facing)
    {
        if (!IsConnected())
        {
            Console.WriteLine("[CLIENT] Not connected to server");
            return;
        }

        var facingUpdate = new FacingUpdateMessage
        {
            playerId = _localPlayerId,
            facing = facing == Signs.Positive ? true : false
        };

        var writer = new NetDataWriter();
        writer.Put((byte)MessageType.FacingUpdate);
        facingUpdate.Serialize(writer);

        _serverPeer.Send(writer, DeliveryMethod.Sequenced);
    }

    public void SendPlayerUpdate(Point2 position, Signs facing, Player.States state)
    {
        if (!IsConnected())
        {
            Console.WriteLine("[CLIENT] Not connected to server");
            return;
        }

        var playerUpdate = new PlayerData
        {
            playerId = _localPlayerId,
            position = position,
            facing = facing == Signs.Positive ? true : false,
            state = (int)state
        };

        var writer = new NetDataWriter();
        writer.Put((byte)MessageType.PlayerUpdate);
        playerUpdate.Serialize(writer);

        _serverPeer.Send(writer, DeliveryMethod.Sequenced);
    }
}