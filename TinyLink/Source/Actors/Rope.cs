using Foster.Framework;

namespace TinyLink;

public class Rope : Actor
{
	public Rope()
	{
		Hitbox = new(new RectInt(0, 0, Game.TileSize, Game.TileSize / 4));
		Mask = Actor.Masks.Rope;
		Sprite = Assets.GetSprite("rope");
		Depth = 5;
		Play("idle");
	}
}
