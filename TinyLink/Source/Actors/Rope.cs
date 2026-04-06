using Foster.Framework;

namespace TinyLink;

public class Rope : Actor
{
	public Rope()
	{
		Hitbox = new(new RectInt(Game.TileSize / 2 -1, 0, 2, Game.TileSize));
		Mask = Actor.Masks.Rope;
		Sprite = Assets.GetSprite("rope");
		Depth = 5;
		Play("idle");
	}
}
