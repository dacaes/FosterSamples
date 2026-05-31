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

    public void BroadcastUpdate<T>(MessageType messageType, T update, NetPeer? excludePeer = null) 
        where T : struct, ISerializable<T>
    {
        var writer = new NetDataWriter();
        writer.Put((byte)messageType);
        update.Serialize(writer);

        _netManager.SendToAll(writer, DeliveryMethod.Sequenced, excludePeer);
    }

    // Keeping this as specific Broadcast example just in case templated Broadcast is slow and I want to go back
    // public void BroadcastPlayerUpdate(PlayerData playerData, NetPeer? excludePeer = null)
    // {
    //     var writer = new NetDataWriter();
    //     writer.Put((byte)MessageType.PlayerUpdate);
    //     playerData.Serialize(writer);

    //     _netManager.SendToAll(writer, DeliveryMethod.Sequenced, excludePeer);
    // }

    public void UpdateLocalPlayerPosition(Point2 position)
    {
        if (!_playersData.TryGetValue(HostPlayerId, out var player)) return;

        player.positionPayload = new Point2Payload(position.X, position.Y);
        _playersData[HostPlayerId] = player;

        var positionUpdate = new PositionUpdateMessage
        {
            playerId = player.playerId,
            positionPayload = new Point2Payload(position.X, position.Y)
        };

         BroadcastUpdate(MessageType.PositionUpdate, positionUpdate, null);
    }

    public void UpdateLocalPlayerState(int state)
    {
        if (!_playersData.TryGetValue(HostPlayerId, out var player)) return;

        player.state = state;
        _playersData[HostPlayerId] = player;

        var stateUpdate = new StateUpdateMessage
        {
            playerId = player.playerId,
            state = player.state
        };

        BroadcastUpdate(MessageType.StateUpdate, stateUpdate, null);
    }

    public void UpdateLocalPlayerFacing(Signs facing)
    {
        if (!_playersData.TryGetValue(HostPlayerId, out var player)) return;

        player.facing = facing == Signs.Positive ? true : false;
        _playersData[HostPlayerId] = player;

        var facingUpdate = new FacingUpdateMessage
        {
            playerId = player.playerId,
            facing = player.facing
        };

        BroadcastUpdate(MessageType.FacingUpdate, facingUpdate, null);
    }

    public void UpdateLocalPlayer(Point2 position, Signs facing, Player.States state)
    {
        if (!_playersData.TryGetValue(HostPlayerId, out var player)) return;

        player.position = position;
        player.facing = facing == Signs.Positive;
        player.state = (int)state;
        _playersData[HostPlayerId] = player;

        BroadcastUpdate(MessageType.PlayerUpdate, player, null);
    }
}