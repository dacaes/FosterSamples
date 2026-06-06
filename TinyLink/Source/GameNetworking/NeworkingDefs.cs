using Foster.Framework;
using LiteNetLib.Utils;
using TinyLink;

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
    FlagsUpdate,
    ActorDied,
    ActorData
    // TODO, handle all actors snapshot from the room you join when client joins
}

[Flags]
public enum NetworkFlags : byte
{
    None = 0,
    Moving = 1 << 0,
    Facing = 1 << 1,
    FreeFlag2 = 1 << 2,
    FreeFlag3 = 1 << 3,
    FreeFlag4 = 1 << 4,
    FreeFlag5 = 1 << 5,
    FreeFlag6 = 1 << 6,
    FreeFlag7 = 1 << 7,
}

public interface ISerializable<T> where T : struct
{
    void Serialize(NetDataWriter writer);
    static abstract T Deserialize(NetDataReader reader);
}

public interface INetworkSerializable
{
    byte NetId {get; set;}
	bool IsNetworkGhost {get; set;}
    bool AutoNetworkSync {get; set;}

    virtual void NetworkSerialize(){}
    virtual void NetworkDeserialize(){}
}

public struct NetworkFlagsPayload : ISerializable<NetworkFlagsPayload>
{
    public NetworkFlags networkFlags;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put((byte)networkFlags);
    }

    public static NetworkFlagsPayload Deserialize(NetDataReader reader)
    {
        return new NetworkFlagsPayload
        {
            networkFlags = (NetworkFlags)reader.GetByte()
        };
    }

    /// <summary>
    /// Set a flag to true.
    /// </summary>
    public void SetFlag(NetworkFlags flag)
    {
        networkFlags |= flag;
    }

    /// <summary>
    /// Clear a flag (set to false).
    /// </summary>
    public void ClearFlag(NetworkFlags flag)
    {
        networkFlags &= ~flag;
    }

    /// <summary>
    /// Toggle a flag between true and false.
    /// </summary>
    public void ToggleFlag(NetworkFlags flag)
    {
        networkFlags ^= flag;
    }

    /// <summary>
    /// Check if a flag is set.
    /// </summary>
    public bool HasFlag(NetworkFlags flag)
    {
        return networkFlags.HasFlag(flag);
    }

    /// <summary>
    /// Set a flag to a specific boolean value.
    /// </summary>
    public void SetFlag(NetworkFlags flag, bool value)
    {
        if (value)
            SetFlag(flag);
        else
            ClearFlag(flag);
    }

    /// <summary>
    /// Get all flags as a NetworkFlags value.
    /// </summary>
    public NetworkFlags GetFlags()
    {
        return networkFlags;
    }

    /// <summary>
    /// Set all flags at once.
    /// </summary>
    public void SetFlags(NetworkFlags flags)
    {
        networkFlags = flags;
    }

    /// <summary>
    /// Clear all flags.
    /// </summary>
    public void Clear()
    {
        networkFlags = NetworkFlags.None;
    }
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

public struct RoomCell : ISerializable<RoomCell>
{
    public sbyte X;
    public sbyte Y;

    public RoomCell(Room room)
    {
        X = (sbyte)room.Cell.X;
        Y = (sbyte)room.Cell.Y;
    }

    public RoomCell(sbyte x, sbyte y)
    {
        X = x;
        Y = y;
    }

	public void Serialize(NetDataWriter writer)
	{
		writer.Put(X);
        writer.Put(Y);
	}
	
    public static RoomCell Deserialize(NetDataReader reader)
	{
        return new RoomCell
        {
		    X = reader.GetSByte(),
            Y = reader.GetSByte()
        };
	}
}

public struct PlayerData : ISerializable<PlayerData>
{
    public byte playerId;
    public Point2Payload positionPayload;
	public Player.States state;
    public NetworkFlagsPayload flagsPayload;

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

    public Signs Facing
    {
        get => flagsPayload.HasFlag(NetworkFlags.Facing) ? Signs.Positive : Signs.Negative;
        set => flagsPayload.SetFlag(NetworkFlags.Facing, value == Signs.Positive);
    }

    public bool Moving
    {
        get => flagsPayload.HasFlag(NetworkFlags.Moving);
        set => flagsPayload.SetFlag(NetworkFlags.Moving, value);
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(playerId);
        positionPayload.Serialize(writer);
        flagsPayload.Serialize(writer);
        writer.Put((byte)state);
    }

    public static PlayerData Deserialize(NetDataReader reader)
    {
        return new PlayerData
        {
            playerId = reader.GetByte(),
            positionPayload = Point2Payload.Deserialize(reader),
            flagsPayload = NetworkFlagsPayload.Deserialize(reader),
            state = (Player.States)reader.GetByte()
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
    public Player.States state;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(playerId);
        writer.Put((byte)state);
    }

    public static StateUpdateMessage Deserialize(NetDataReader reader)
    {
        return new StateUpdateMessage
        {
            playerId = reader.GetByte(),
            state = (Player.States)reader.GetByte()
        };
    }
}

public struct FlagsUpdateMessage : ISerializable<FlagsUpdateMessage>
{
    public byte playerId;
    public NetworkFlagsPayload flagsPayload;

    public Signs Facing
    {
        get => flagsPayload.HasFlag(NetworkFlags.Facing) ? Signs.Positive : Signs.Negative;
        set => flagsPayload.SetFlag(NetworkFlags.Facing, value == Signs.Positive);
    }

    public bool Moving
    {
        get => flagsPayload.HasFlag(NetworkFlags.Moving);
        set => flagsPayload.SetFlag(NetworkFlags.Moving, value);
    }


    public void Serialize(NetDataWriter writer)
    {
        writer.Put(playerId);
        flagsPayload.Serialize(writer);
    }

    public static FlagsUpdateMessage Deserialize(NetDataReader reader)
    {
        return new FlagsUpdateMessage
        {
            playerId = reader.GetByte(),
            flagsPayload = NetworkFlagsPayload.Deserialize(reader),
        };
    }
}

public struct ActorDied : ISerializable<ActorDied>
{
    public RoomCell roomCell;
    public byte netId;

	public void Serialize(NetDataWriter writer)
	{
        roomCell.Serialize(writer);
		writer.Put(netId);
	}

	public static ActorDied Deserialize(NetDataReader reader)
	{
		return new ActorDied
        {
            roomCell = RoomCell.Deserialize(reader),
            netId = reader.GetByte(),
        };
	}
}

public struct ActorData : ISerializable<ActorData>
{
    private const ushort IdMask = 0x00FF;
    private const ushort AliveMask = 0x0100;
    private ushort data;
    public RoomCell roomCell;

    public byte NetId
    {
        get => (byte)(data & IdMask);
        set
        {
            data = (ushort)((data & ~IdMask) | (value & IdMask));
        }
    }

    public bool Alive
    {
        get => (data & AliveMask) != 0;
        set
        {
            if (value)
                data |= AliveMask;
            else
                data = (ushort)(data & ~AliveMask);
        }
    }

    public void Serialize(NetDataWriter writer)
    {
        roomCell.Serialize(writer);
        writer.Put(data);
    }

    public static ActorData Deserialize(NetDataReader reader)
    {
        return new ActorData
        {
            roomCell = RoomCell.Deserialize(reader),
            data = reader.GetUShort(),
        };
    }
}