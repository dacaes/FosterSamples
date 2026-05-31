using LiteNetLib;
using LiteNetLib.Utils;
using Foster.Framework;
using TinyLink;

namespace GameNetworking;

public partial class GameClient
{
    public Action<int> OnHandleAssignedPlayerId;

     private void HandleAllPlayersSnapshot(NetPacketReader reader)
    {
        int count = reader.GetInt();
        _playersData.Clear();

        for (int i = 0; i < count; i++)
        {
            var playerData = PlayerData.Deserialize(reader);
            _playersData[playerData.playerId] = playerData;
        }

        UpdatePlayersFromPlayerData();

        Console.WriteLine($"[CLIENT] Received snapshot with {count} players. Local Player ID: {LocalPlayerId}");
    }

    private void HandleAssignedPlayerId(NetPacketReader reader)
    {
        int id = reader.GetInt();
        LocalPlayerId = id;
        Console.WriteLine($"[CLIENT] Assigned local player ID: {LocalPlayerId}");
        OnHandleAssignedPlayerId?.Invoke(LocalPlayerId);
    }

    private void HandlePlayerJoined(NetPacketReader reader)
    {
        var player = PlayerData.Deserialize(reader);
        _playersData[player.playerId] = player;
        
        Console.WriteLine($"[CLIENT] Player {player.playerId} joined");
    }

    private void HandlePlayerLeft(NetPacketReader reader)
    {
        int playerId = reader.GetInt();
        _playersData.Remove(playerId);
        NetworkPlayers.Remove(playerId);
        
        Console.WriteLine($"[CLIENT] Player {playerId} left");
    }

    protected override void BroadcastUpdate<T>(MessageType messageType, T update,  NetPeer? excludePeer = null, DeliveryMethod deliveryMethod = DeliveryMethod.Sequenced)
    {
        if (!IsConnected())
        {
            Console.WriteLine("[CLIENT] Not connected to server");
            return;
        }

        var writer = new NetDataWriter();
        writer.Put((byte)messageType);
        update.Serialize(writer);

        // excludePeer is only used by the Host implementation
        _serverPeer.Send(writer, deliveryMethod);
    }
}