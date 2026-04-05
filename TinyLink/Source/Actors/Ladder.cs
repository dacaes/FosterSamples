using Foster.Framework;

namespace TinyLink;

public class Ladder : Actor
{
	public Ladder()
	{
		Hitbox = new(new RectInt(0, 0, Game.TileSize, Game.TileSize / 4));
		Mask = Actor.Masks.Rope;
		Sprite = Assets.GetSprite("ladder");
		Depth = 5;
		Play("idle");
	}
}
