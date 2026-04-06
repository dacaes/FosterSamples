using Foster.Framework;

namespace TinyLink;

public class Ivy : Actor
{
	public Ivy()
	{
		Hitbox = new(new RectInt(Game.TileSize / 2 -1, 0, 1, Game.TileSize));
		Mask = Actor.Masks.Rope;
		Sprite = Assets.GetSprite("ivy");
		Depth = 5;
		Play("idle");
	}
}
