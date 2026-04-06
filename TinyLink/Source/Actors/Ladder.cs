using Foster.Framework;

namespace TinyLink;

public class Ladder : Actor
{
	public Ladder()
	{
		Hitbox = new(new RectInt(0, 0, Game.TileSize, Game.TileSize));
		Mask = Actor.Masks.Ladder;
		Sprite = Assets.GetSprite("ladder");
		Depth = 5;
		Play("idle");
	}
}
