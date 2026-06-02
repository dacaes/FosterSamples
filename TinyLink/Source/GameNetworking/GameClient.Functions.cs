using LiteNetLib;
using LiteNetLib.Utils;

namespace GameNetworking;

public partial class GameClient
{
    public Action<byte>? OnHandleAssignedPlayerId;

     private void HandleAllPlayersSnapshot(NetPacketReader reader)
    {
        int count = reader.GetInt();
        PlayersData.Clear();

        for (int i = 0; i < count; i++)
        {
            var playerData = PlayerData.Deserialize(reader);
            PlayersData.Add(playerData.playerId,playerData);
            DeserializePlayer(playerData);
        }

        Console.WriteLine($"[CLIENT] Received snapshot with {count} players. Local Player ID: {LocalPlayerId}");
    }

    private void HandleAssignedPlayerId(NetPacketReader reader)
    {
        byte id = reader.GetByte();
        LocalPlayerId = id;
        Console.WriteLine($"[CLIENT] Assigned local player ID: {LocalPlayerId}");
        OnHandleAssignedPlayerId?.Invoke(LocalPlayerId);
    }

    private void HandlePlayerJoined(NetPacketReader reader)
    {
        var playerData = PlayerData.Deserialize(reader);
        if(!PlayersData.ContainsKey(playerData.playerId))
        {
            // System.Console.WriteLine($"add {playerData.playerId}");
            PlayersData[playerData.playerId] = playerData;
        }
        
        Console.WriteLine($"[CLIENT] Player {playerData.playerId} joined");
    }

    private void HandlePlayerLeft(NetPacketReader reader)
    {
        int playerId = reader.GetInt();
        PlayersData.Remove(playerId);
        NetworkPlayers.Remove(playerId);
        
        Console.WriteLine($"[CLIENT] Player {playerId} left");
    }

    public override void BroadcastUpdate<T>(MessageType messageType, T update,  NetPeer? excludePeer = null, DeliveryMethod deliveryMethod = DeliveryMethod.Sequenced)
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