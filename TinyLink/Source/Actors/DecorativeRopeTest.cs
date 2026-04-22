using System.Numerics;
using Foster.Framework;

namespace TinyLink;

public class DecorativeRopeTest : Actor
{
	private Point2 a,d;
	private Vector2 b,c;
	private int length;
	private float swing;
	private float period;

	public Color color = Color.Gray;

	public DecorativeRopeTest()
	{
		Mask = Masks.None;
		Depth = 5;
		a = new (132,55);
		d = new(150,55);
		length = 50;
		color = new Color(69,40,60,255);
		swing = 2;
		period = 4f;
	}

	public DecorativeRopeTest(Point2 start, Point2 end, int length, Color color, float swing, float period)
	{
		Mask = Masks.None;
		Depth = 5;
		a = start;
		d = end;
		this.length = length;
		this.color = color;
		this.swing = swing;
		this.period = period;
	}

	public override void Update()
	{
		Vector2 delta = d - a;
        float dist = delta.Length();
        float slack = length - dist;
		slack = MathF.Max(0, slack);
		float sag = slack * 0.75f;

		// Vector2 mid = (a + d) * 0.5f;
		Vector2 weightedMid = a + (d - a) * Calc.ClampedMap(-delta.Y,-length/2,length/2);

        // Contol points
		b = (a + weightedMid) * 0.5f;
		c = (d + weightedMid) * 0.5f;

        // Sag
		b.Y += sag;
		c.Y += sag;

        // Swing ("wind" sim)
        Vector2 ob = b;
        Vector2 oc = c;

        b.X = Time.SineWave(ob.X-swing, ob.X+swing,period);
        c.X = Time.SineWave(oc.X-swing, oc.X+swing,period * 1.05f); // Slight different time to desync b and c
	}

	public override void Render(Batcher batcher)
	{
		Point2 prev = a;
		int segments = 20;

		for (int i = 1; i <= segments; i++)
		{
			float t = i / (float)segments;
			Point2 p = (Point2)Calc.Bezier(a, b, c, d, t);

			batcher.Line(prev,p,2, color);
			prev = p;
		}
	}
}
