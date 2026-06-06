using Foster.Framework;
using GameNetworking;

namespace TinyLink;

public class Bramble : Actor
{
	public Bramble()
	{
		Hitbox = new(new RectInt(-4, -8, 8, 8));
		Mask = Masks.Hazard;
		Sprite = Assets.GetSprite("bramble");
		Play("idle");
	}

	public override void OnPerformHit(Actor hitting) => Pop();
	public override void OnWasHit(Actor by) => Pop();

	public void Pop()
	{
		NetworkSerialize();
		Game.Destroy(this);
		Game.Create<Pop>(Position + new Point2(0, -4));
	}

	#region Networking
	public override void NetworkSerialize()
	{
		ActorData actorData = new ()
		{
			roomCell = Game.CurrentRoomCell,
			NetId = NetId,
			Alive = false,	//it is only serialized when destroyed
		};

		// Local update
		NetworkManager.Instance.ActorsData[(Game.CurrentRoomCell, NetId)] = actorData;

		ActorDied actorDied = new ()
		{
			roomCell = Game.CurrentRoomCell,
			netId = NetId
		};

        NetworkManager.Instance.BroadcastUpdate(MessageType.ActorDied, actorDied, null);
	}

	public override void NetworkDeserialize()
	{
		if(NetworkManager.Instance.ActorsData.TryGetValue((Game.CurrentRoomCell,NetId), out var actorData))
		{
			if(!actorData.Alive)
			{
				Pop();
			}
		}
		System.Console.WriteLine($"ERROR. Deserialize failed with actor netId: {NetId}");
	}
	#endregion
}