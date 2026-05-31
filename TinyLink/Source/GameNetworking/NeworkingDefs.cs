using Foster.Framework;
using LiteNetLib.Utils;

namespace GameNetworking;

public enum MessageType : byte
{
    PlayerJoined,
    PlayerLeft,
    AssignedPlayerId,
    AllPlayersSnapshot,
    PlayerUpdate,
    PositionUpdate,
    StateUpdate,
    FacingUpdate,
}

public interface ISerializable<T> where T : struct
{
    void Serialize(NetDataWriter writer);
    static abstract T Deserialize(NetDataReader reader);
}

public struct Point2Payload : ISerializable<Point2Payload>
{
    public int X;
    public int Y;

    public Point2Payload(int x, int y)
    {
        X = x;
        Y = y;
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(X);
        writer.Put(Y);
    }

    public static Point2Payload Deserialize(NetDataReader reader)
    {
        return new Point2Payload
        {
            X = reader.GetInt(),
            Y = reader.GetInt()
        };
    }
}

public struct PlayerData : ISerializable<PlayerData>
{
    public byte playerId;
    public Point2Payload positionPayload;
    public bool facing;
	public byte state;

    public Point2 position
    {
        get => new(positionPayload.X, positionPayload.Y);
        set { positionPayload.X = value.X;  positionPayload.Y = value.Y; }
    }

	public int X
    {
        get => positionPayload.X;
        set => positionPayload.X = value;
    }

    public int Y
    {
        get => positionPayload.Y;
        set => positionPayload.Y = value;
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(playerId);
        positionPayload.Serialize(writer);
        writer.Put(facing);
        writer.Put(state);
    }

    public static PlayerData Deserialize(NetDataReader reader)
    {
        return new PlayerData
        {
            playerId = reader.GetByte(),
            positionPayload = Point2Payload.Deserialize(reader),
            facing = reader.GetBool(),
            state = reader.GetByte()
        };
    }
}

public struct PositionUpdateMessage : ISerializable<PositionUpdateMessage>
{
    public byte playerId;
    public Point2Payload positionPayload;

    public Point2 position
    {
        get => new(positionPayload.X, positionPayload.Y);
        set { positionPayload.X = value.X;  positionPayload.Y = value.Y; }
    }

    public int X
    {
        get => positionPayload.X;
        set => positionPayload.X = value;
    }

    public int Y
    {
        get => positionPayload.Y;
        set => positionPayload.Y = value;
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(playerId);
        positionPayload.Serialize(writer);
    }

    public static PositionUpdateMessage Deserialize(NetDataReader reader)
    {
        return new PositionUpdateMessage
        {
            playerId = reader.GetByte(),
            positionPayload = Point2Payload.Deserialize(reader)
        };
    }
}

public struct StateUpdateMessage : ISerializable<StateUpdateMessage>
{
    public byte playerId;
    public byte state;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(playerId);
        writer.Put(state);
    }

    public static StateUpdateMessage Deserialize(NetDataReader reader)
    {
        return new StateUpdateMessage
        {
            playerId = reader.GetByte(),
            state = reader.GetByte()
        };
    }
}

public struct FacingUpdateMessage : ISerializable<FacingUpdateMessage>
{
    public byte playerId;
    public bool facing;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(playerId);
        writer.Put(facing);
    }

    public static FacingUpdateMessage Deserialize(NetDataReader reader)
    {
        return new FacingUpdateMessage
        {
            playerId = reader.GetByte(),
            facing = reader.GetBool()
        };
    }
}