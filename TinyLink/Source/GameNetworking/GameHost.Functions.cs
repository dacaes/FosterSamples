using LiteNetLib;
using LiteNetLib.Utils;
using Foster.Framework;
using TinyLink;

namespace GameNetworking;

public partial class GameHost
{
    private void SendAllPlayersSnapshot(NetPeer peer)
    {
        var writer = new NetDataWriter();
        writer.Put((byte)MessageType.AllPlayersSnapshot);
        writer.Put(_playersData.Count);

        foreach (var player in _playersData.Values)
        {
            player.Serialize(writer);
        }

        peer.Send(writer, DeliveryMethod.ReliableOrdered);
        Console.WriteLine($"[HOST] Sent snapshot with {_playersData.Count} players to peer");
    }

    private void BroadcastPlayerJoined(PlayerData player, NetPeer? excludePeer = null)
    {
        var writer = new NetDataWriter();
        writer.Put((byte)MessageType.PlayerJoined);
        player.Serialize(writer);

        _netManager.SendToAll(writer, DeliveryMethod.ReliableOrdered, excludePeer);
        Console.WriteLine($"[HOST] Broadcasted player {player.playerId} joined");
    }

    private void BroadcastPlayerLeft(int playerId)
    {
        var writer = new NetDataWriter();
        writer.Put((byte)MessageType.PlayerLeft);
        writer.Put(playerId);

        _netManager.SendToAll(writer, DeliveryMethod.ReliableOrdered);
        Console.WriteLine($"[HOST] Broadcasted player {playerId} left");
    }

    protected override void BroadcastUpdate<T>(MessageType messageType, T update, NetPeer? excludePeer = null, DeliveryMethod deliveryMethod = DeliveryMethod.Sequenced)
    {
        var writer = new NetDataWriter();
        writer.Put((byte)messageType);
        update.Serialize(writer);

        _netManager.SendToAll(writer, deliveryMethod, excludePeer);
    }

    // Keeping this as specific Broadcast example just in case templated Broadcast is slow and I want to go back
    // public void BroadcastPlayerUpdate(PlayerData playerData, NetPeer? excludePeer = null)
    // {
    //     var writer = new NetDataWriter();
    //     writer.Put((byte)MessageType.PlayerUpdate);
    //     playerData.Serialize(writer);

    //     _netManager.SendToAll(writer, DeliveryMethod.Sequenced, excludePeer);
    // }
}