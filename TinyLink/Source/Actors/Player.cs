using System.Numerics;
using Foster.Framework;

namespace TinyLink;

public class Player : Actor
{
	public enum States
	{
		Normal,
		LandOnRope,
		Rope,
		Airborne,
		Attack,
		Hurt,
		Start
	}

	public const int MaxHealth = 4;
	private const float MaxGroundSpeed = 60;
	private const float MaxAirSpeed = 70;
	private const float GroundAccel = 500;
	private const float AirAccel = 100;
	private const float Friction = 800;
	private const float AttackFriction = 150;
	private const float HurtFriction = 200;
	private const float Gravity = 450;
	private const float JumpForce = -105;
	private const float JumpTime = 0.18f;
	private const float HurtDuration = 0.5f;
	private const float DeathDuration = 1.5f;
	private const float InvincibleDuration = 1.5f;

	public int Health = MaxHealth;
	public States State;
	public Controls Controls => Game.Controls;

	private float stateDuration = 0;
	private float jumpTimer = 0;
	private bool grounded = false;
	private bool inRope = false;
	private bool ducking = false;

	private StateMachine<States> fsm = new();

	public Player()
	{
		State = States.Start;
		Sprite = Assets.GetSprite("player");
		Hitbox = new(new RectInt(-4, -12, 8, 12));
		Mask = Masks.Player;
		IFrameTime = InvincibleDuration;
		grounded = true;
		inRope = false;
		Play("sword");

		fsm.AddState(States.Normal, new State<States>(
			onEnter: () => { Play("idle"); },
			onUpdate: () => NormalState()
		));

		fsm.AddState(States.LandOnRope, new State<States>(
			onEnter: () =>
			{
				Squish = new Vector2(0.65f, 1.4f);
				fsm.ActivateTrigger("Rope");
			}
		));

		fsm.AddState(States.Rope, new State<States>(
			onEnter: () =>
			{
				inRope = true;

				var rope = OverlapsFirst(Masks.Rope);
				if (rope != null)
				{
					Position = rope.Position + (Facing == Signs.Positive ? new Point2(3, 8) : new Point2(5, 8));
				}
				Stop();
			},
			onUpdate: () => RopeState(),
			onExit: () => inRope = false
		));

		fsm.AddState(States.Airborne, new State<States>(
			onUpdate: () =>
			{
				AirborneState();
			}
		));

		fsm.AddState(States.Attack, new State<States>(
			onEnter: () =>
			{
				if (grounded)
					StopX();
			},
			onUpdate: () => { Console.WriteLine("attack"); AttackState(); }
		));

		fsm.AddState(States.Hurt, new State<States>()).OnUpdate = () => HurtState();
		fsm.AddState(States.Start, new State<States>()).OnUpdate = () => StartState();

		fsm.AddTransition(States.Normal, States.Attack, condition: () => Controls.Attack.ConsumePress());
		fsm.AddTransition(States.Normal, States.LandOnRope, condition: () => OverlapsAny(Masks.Rope) && !grounded); // was in the air and contacted rope
		fsm.AddTransition(States.Normal, States.Rope, condition: () => OverlapsAny(Masks.Rope) && grounded && Controls.Move.IntValue.Y < 0); // is in the ground and starts climbing rope
																																			 // fsm.AddTransition(States.Rope, States.Normal, condition: () => !OverlapsAny(Masks.Rope));
		fsm.AddTransition(States.Rope, States.Normal, condition: () => grounded && (MathF.Abs(Controls.Move.IntValue.X) > 0 || Controls.Move.IntValue.Y > 0));  // in the ground and pressing down or moving
		fsm.AddTransition(States.Airborne, States.Normal, condition: () => grounded);
		fsm.AddTransition(States.Airborne, States.Rope, condition: () => OverlapsAny(Masks.Rope) && !grounded && Controls.Move.IntValue.Y < 0);

		fsm.AddAnyTrigger("Normal", States.Normal);
		fsm.AddAnyTrigger("Rope", States.Rope);
		fsm.AddAnyTrigger("Airborne", States.Airborne);

		fsm.OnAnyEnter = () => stateDuration = 0f;
		fsm.SetState(States.Normal);
	}

	public override void Update()
	{
		base.Update();

		// update grounded state
		var nowGrounded = Velocity.Y >= 0 && Grounded();
		if (nowGrounded && !grounded)
			Squish = new Vector2(1.5f, 0.70f);
		grounded = nowGrounded;

		// increment state timer
		var wasState = State;

		fsm.Update();
		// state control
		// switch (State)
		// {
		// 	case States.Normal:
		// 		NormalState();
		// 		break;
		// 	case States.Rope:
		// 		RopeState();
		// 		break;
		// 	case States.Attack:
		// 		AttackState();
		// 		break;
		// 	case States.Hurt:
		// 		HurtState();
		// 		break;
		// 	case States.Start:
		// 		StartState();
		// 		break;
		// }

		// ducking collider(s)
		if (ducking && State != States.Normal)
			ducking = false;
		if (ducking)
			Hitbox = new(new RectInt(-4, -6, 8, 6));
		else
			Hitbox = new(new RectInt(-4, -12, 8, 12));

		// variable jumping
		if (jumpTimer > 0)
		{
			Velocity.Y = JumpForce;
			jumpTimer -= Time.Delta;
			if (!Controls.Jump.Down)
				jumpTimer = 0;
		}

		// gravity
		if (!grounded && !inRope)
		{
			float grav = Gravity;
			if (State == States.Normal && MathF.Abs(Velocity.Y) < 20 && Controls.Jump.Down)
				grav *= 0.40f;
			Velocity.Y += grav * Time.Delta;
		}

		// goto next room
		if (Health > 0)
		{
			if (Position.X > Game.Bounds.Right && !Game.Transition(Point2.Right))
			{
				Position.X = Game.Bounds.Right;
			}
			else if (Position.X < Game.Bounds.Left && !Game.Transition(Point2.Left))
			{
				Position.X = Game.Bounds.Left;
			}
			else if (Position.Y > Game.Bounds.Bottom + 12 && !Game.Transition(Point2.Down))
			{
				Health = 0;
				State = States.Hurt;
			}
			else if (Position.Y < Game.Bounds.Top)
			{
				if (Game.Transition(Point2.Up))
					Velocity.Y = -150;
				else
					Position.Y = Game.Bounds.Top;
			}
		}

		// detect getting hit
		if (OverlapsFirst(Masks.Enemy | Masks.Hazard) is Actor hit)
			hit.Hit(this);

		stateDuration += Time.Delta;
		// if (State != wasState)
		// 	stateDuration = 0.0f;
	}

	public void NormalState()
	{
		// update ducking state
		ducking = grounded && Controls.Move.IntValue.Y > 0;

		if (OverlapsAny(Masks.Rope) && (!grounded || MathF.Abs(Controls.Move.IntValue.Y) > 0))
		{
			State = States.Rope;
			return;
		}

		// get input
		var input = Controls.Move.IntValue.X;
		// if (ducking)
		// 	input = 0;

		// sprite
		if (grounded)
		{
			if (ducking)
				Play("duck");
			else if (input == 0)
				Play("idle");
			else
				Play("run");
		}
		else
		{
			Play("jump");
		}

		// horizontal movement
		{
			// Acceleration
			Velocity.X += input * (grounded ? GroundAccel : AirAccel) * Time.Delta;

			// Max Speed
			var maxspd = grounded ? MaxGroundSpeed : MaxAirSpeed;
			maxspd = ducking ? maxspd * 0.3f : maxspd;
			if (MathF.Abs(Velocity.X) > maxspd)
				Velocity.X = Calc.Approach(Velocity.X, MathF.Sign(Velocity.X) * maxspd, 2000 * Time.Delta);

			// Friction
			if (input == 0 && grounded)
				Velocity.X = Calc.Approach(Velocity.X, 0, Friction * Time.Delta);

			// Facing
			if (grounded && input != 0)
				Facing = input;
		}

		// Start jumping
		if (grounded && Controls.Jump.ConsumePress())
		{
			Squish = new Vector2(0.65f, 1.4f);
			StopX();
			Velocity.X = input * MaxAirSpeed;
			jumpTimer = JumpTime;
			fsm.ActivateTrigger("Airborne");
		}

		// Begin Attack
		if (Controls.Attack.ConsumePress())
		{
			State = States.Attack;
			if (grounded)
				StopX();
		}
	}

	public void AirborneState()
	{
		// after jumping
		// it allows to track the stateduration and if it is very short it doesn't transition to rope. basically to be able to jump from a rope

		Play("jump");

		var input = Controls.Move.IntValue.X;
		// horizontal movement
		{
			// Acceleration
			Velocity.X += input * AirAccel * Time.Delta;

			// Max Speed
			var maxspd = MaxAirSpeed;
			if (MathF.Abs(Velocity.X) > maxspd)
				Velocity.X = Calc.Approach(Velocity.X, MathF.Sign(Velocity.X) * maxspd, 2000 * Time.Delta);
		}
	}

	public void RopeState()
	{
		if (grounded)
			State = States.Normal;
		if (!OverlapsAny(Masks.Rope) || MathF.Abs(Controls.Move.IntValue.X) > 0)
			State = States.Normal;

		// StopX();

		// Console.WriteLine("rope");
		inRope = true;

		// vertical movement
		{
			var input = Controls.Move.IntValue.Y;

			if (MathF.Abs(input) > 0)
				Play("climb");
			else
				Play("climb_idle");

			// Rope acceleration
			Velocity.Y += input * 100 * Time.Delta;

			var maxspd = 30;
			if (MathF.Abs(Velocity.Y) > maxspd)
				Velocity.Y = Calc.Approach(Velocity.Y, MathF.Sign(Velocity.Y) * maxspd, 2000 * Time.Delta);

			// Friction
			if (input == 0)
				Velocity.Y = Calc.Approach(Velocity.Y, 0, Friction * Time.Delta);

			if (!OverlapsAny(new Point2(0, -14), Masks.Rope) && Velocity.Y < 0)
				StopY();

			// // Facing
			// if (input != 0)
			// 	Facing = input;
		}

		if (Controls.Jump.ConsumePress())
		{
			var input = Controls.Move.IntValue.X;
			Squish = new Vector2(0.65f, 1.4f);
			StopX();
			Velocity.X = input * MaxAirSpeed;
			jumpTimer = JumpTime;
			Facing = input;
			fsm.ActivateTrigger("Airborne");
		}
	}

	public void AttackState()
	{
		Play("attack", false);

		RectInt? hitbox = null;

		if (stateDuration < 0.2f)
		{
			hitbox = new RectInt(-16, -12, 17, 8);
		}
		else if (stateDuration < 0.50f)
		{
			hitbox = new RectInt(8, -8, 16, 8);
		}

		if (hitbox != null)
		{
			var it = hitbox.Value;
			if (Facing == Signs.Negative)
				it.X = -(it.X + it.Width);
			it += Position;

			if (Game.OverlapsFirst(it, Masks.Enemy | Masks.Hazard) is Actor hit)
				Hit(hit);
		}

		if (Grounded())
			Velocity.X = Calc.Approach(Velocity.X, 0, AttackFriction * Time.Delta);

		if (stateDuration >= Animation.Duration)
		{
			Play("idle");
			State = States.Normal;
			Console.WriteLine("end");
			fsm.ActivateTrigger("Normal");
		}
	}

	public void HurtState()
	{
		if (stateDuration <= 0 && Health <= 0)
		{
			foreach (var actor in Game.Actors)
				if (actor != this)
					Game.Destroy(actor);
			Game.Shake(0.1f);
		}

		Velocity.X = Calc.Approach(Velocity.X, 0, HurtFriction * Time.Delta);

		if (stateDuration >= HurtDuration && Health > 0)
			State = States.Normal;

		if (stateDuration >= DeathDuration && Health <= 0)
			Game.ReloadRoom();
	}

	public void StartState()
	{
		if (stateDuration >= 1.0f)
			State = States.Normal;
	}

	public override void OnWasHit(Actor by)
	{
		Game.Hitstun(0.1f);
		Game.Shake(0.1f);

		Play("hurt");

		Velocity = new Vector2(-Facing * 100, -80);
		State = States.Hurt;
		Health--;
	}
}
