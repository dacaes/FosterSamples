using System.Numerics;
using Foster.Framework;

namespace TinyLink;

public class Player : Actor
{
	public enum States
	{
		Normal,
		LandOnClimbable,
		Climbing,
		Airborne,
		Attack,
		Hurt,
		Start
	}

	public const int MaxHealth = 4;
	private const float MaxGroundSpeed = 60;
	private const float MaxAirSpeed = 70;
	private const float MaxClimbingSpeed = 30;
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
	public Controls Controls => Game.Controls;

	private float stateDuration = 0;
	private float jumpTimer = 0;
	private bool grounded = false;
	private bool ducking = false;

	private StateMachine<States> fsm = new();

	public Player()
	{
		Sprite = Assets.GetSprite("player");
		Hitbox = new(new RectInt(-4, -12, 8, 12));
		Mask = Masks.Player;
		IFrameTime = InvincibleDuration;
		grounded = true;
		Play("sword");

		// Normal: in the ground, where you can walk, duck, jump...
		fsm.AddState(States.Normal, new State<States>(
			onEnter: () => { Play("idle"); },
			onUpdate: () => NormalState(),
			onExit: () => ducking = false
		));

		// Land On Climbable: Just to do a cool effect when you transition from Airborne to climcable (e.g. Rope, Ladder)
		fsm.AddState(States.LandOnClimbable, new State<States>(
			onEnter: () =>
			{
				Squish = new Vector2(0.65f, 1.4f);
				fsm.ActivateTrigger("Climbing");
			}
		));

		// Climbing
		fsm.AddState(States.Climbing, new State<States>(
			onEnter: () =>
			{
				// climb overlapping rope or ladder
				var rope = OverlapsFirst(Masks.Rope | Masks.Ladder);
				if (rope != null)
				{
					// Position = rope.Position + (Facing == Signs.Positive ? new Point2(3, 16) : new Point2(4, 16));
					Position = rope.Position + new Point2(4, 16);
				}
				else
				{
					// climb down ladder
					var ladder = OverlapsFirst(Point2.Down, Masks.Ladder);
					if (ladder != null)
					{
						Position = ladder.Position + new Point2(4, 6);
					}
				}

				Stop();
			},
			onUpdate: () => ClimbingState()
		));

		// Airborne: In the air
		fsm.AddState(States.Airborne, new State<States>(
			onUpdate: () =>
			{
				AirborneState();
			}
		));

		// Attack
		fsm.AddState(States.Attack, new State<States>(
			onEnter: () =>
			{
				if (grounded)
					StopX();
			},
			onUpdate: () => { AttackState(); }
		));

		//Hurt
		fsm.AddState(States.Hurt, new State<States>(
			onEnter: () =>
			{
				if (Health <= 0)
				{
					foreach (var actor in Game.Actors)
						if (actor != this)
							Game.Destroy(actor);
					Game.Shake(0.1f);
				}
			},
			onUpdate: () => HurtState()
		));

		// Start
		fsm.AddState(States.Start, new State<States>()).OnUpdate = () => StartState();

		// Transitions
		fsm.AddTransition(States.Normal, States.Airborne, condition: () => !grounded);
		fsm.AddTransition(States.Normal, States.Attack, condition: () => Controls.Attack.ConsumePress());
		fsm.AddTransition(States.Normal, States.LandOnClimbable, condition: () => OverlapsAny(Masks.Rope | Masks.Ladder) && !grounded); // was in the air and contacted climbable
		fsm.AddTransition(States.Normal, States.Climbing, condition: () => OverlapsAny(Masks.Rope | Masks.Ladder) && Controls.Move.IntValue.Y < 0); // is in the ground and starts climbing
		fsm.AddTransition(States.Normal, States.Climbing, condition: () => OverlapsAny(Point2.Down, Masks.Ladder) && Controls.Move.IntValue.Y > 0); // if on top of a ladder and going down

		fsm.AddTransition(States.Attack, States.Climbing, condition: () => OverlapsAny(Masks.Rope | Masks.Ladder) && Controls.Move.IntValue.Y < 0); // can cancel attack and grab climbable

		fsm.AddTransition(States.Climbing, States.Normal, condition: () => grounded && (MathF.Abs(Controls.Move.IntValue.X) > 0 || Controls.Move.IntValue.Y > 0));  // in the ground and pressing down or moving

		fsm.AddTransition(States.Airborne, States.Normal, condition: () => grounded);
		fsm.AddTransition(States.Airborne, States.Attack, condition: () => Controls.Attack.ConsumePress());
		fsm.AddTransition(States.Airborne, States.Climbing, condition: () => OverlapsAny(Masks.Rope | Masks.Ladder) && Controls.Move.IntValue.Y < 0);

		// Add triggers
		fsm.AddAnyTrigger("Normal", States.Normal);
		fsm.AddAnyTrigger("Climbing>", States.Climbing);
		fsm.AddAnyTrigger("Airborne", States.Airborne);
		fsm.AddAnyTrigger("Hurt", States.Hurt);

		// Reset the state duration when entering any state
		fsm.OnAnyEnter = () => stateDuration = 0f;

		// Initial state
		fsm.SetState(States.Start);
	}

	public override void Update()
	{
		base.Update();

		// update grounded state
		var nowGrounded = Velocity.Y >= 0 && Grounded();
		if (nowGrounded && !grounded)
			Squish = new Vector2(1.5f, 0.70f);
		grounded = nowGrounded;

		fsm.Update();

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
		if (!grounded && fsm.CurrentState != States.Climbing && fsm.CurrentState != States.LandOnClimbable)
		{
			float grav = Gravity;
			if (fsm.CurrentState == States.Airborne && MathF.Abs(Velocity.Y) < 20 && Controls.Jump.Down)
				grav *= 0.40f;	// air momentum at the peak of the jump
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
				fsm.ActivateTrigger("Hurt");
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
	}

	public void NormalState()
	{
		// update ducking state
		ducking = grounded && Controls.Move.IntValue.Y > 0;

		// get input
		var input = Controls.Move.IntValue.X;
		// if (ducking)
		// 	input = 0;

		// sprite
		if (ducking)
			Play("duck");
		else if (input == 0)
			Play("idle");
		else
			Play("run");

		// horizontal movement
		{
			// Acceleration
			Velocity.X += input * GroundAccel * Time.Delta;

			// Max Speed
			var maxspd = MaxGroundSpeed;
			maxspd = ducking ? maxspd * 0.3f : maxspd;
			if (MathF.Abs(Velocity.X) > maxspd)
				Velocity.X = Calc.Approach(Velocity.X, MathF.Sign(Velocity.X) * maxspd, 2000 * Time.Delta);

			// Friction
			if (input == 0)
				Velocity.X = Calc.Approach(Velocity.X, 0, Friction * Time.Delta);

			// Facing
			if (input != 0)
				Facing = input;
		}

		// Start jumping
		if (Controls.Jump.ConsumePress())
		{
			StartJump();
		}
	}

	public void StartJump()
	{
		var input = Controls.Move.IntValue.X;
		Squish = new Vector2(0.65f, 1.4f);
		StopX();
		Velocity.X = input * MaxAirSpeed;
		jumpTimer = JumpTime;
		Facing = input;
		fsm.ActivateTrigger("Airborne");
	}

	public void AirborneState()
	{
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

	public void ClimbingState()
	{
		// vertical movement
		{
			var input = Controls.Move.IntValue.Y;

			if (MathF.Abs(input) > 0)
				Play("climb");
			else
				Play("climb_idle");

			// Climbing acceleration
			Velocity.Y += input * 100 * Time.Delta;

			var maxspd = MaxClimbingSpeed;
			if (MathF.Abs(Velocity.Y) > maxspd)
				Velocity.Y = Calc.Approach(Velocity.Y, MathF.Sign(Velocity.Y) * maxspd, 2000 * Time.Delta);

			// Friction
			if (input == 0)
				Velocity.Y = Calc.Approach(Velocity.Y, 0, Friction * Time.Delta);

			if(OverlapsAny(Masks.Rope))
			{
				if (!OverlapsAny(new Point2(0, -14), Masks.Rope) && Velocity.Y < 0)
					StopY();
			}
			else if(!OverlapsAny(Masks.Ladder))
			{
				fsm.ActivateTrigger("Normal");
				StopY();
			}

			// // Facing
			// if (input != 0)
			// 	Facing = input;
		}

		if (Controls.Jump.ConsumePress())
		{
			StartJump();
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
			if(grounded)
				fsm.ActivateTrigger("Normal");
			else
				fsm.ActivateTrigger("Airborne");
		}
	}

	public void HurtState()
	{
		Velocity.X = Calc.Approach(Velocity.X, 0, HurtFriction * Time.Delta);

		if (stateDuration >= HurtDuration && Health > 0)
		{
			if(grounded)
				fsm.ActivateTrigger("Normal");
			else
				fsm.ActivateTrigger("Airborne");
		}

		if (stateDuration >= DeathDuration && Health <= 0)
			Game.ReloadRoom();
	}

	public void StartState()
	{
		if (stateDuration >= 1.0f)
			fsm.ActivateTrigger("Normal");
	}

	public override void OnWasHit(Actor by)
	{
		Game.Hitstun(0.1f);
		Game.Shake(0.1f);

		Play("hurt");

		Velocity = new Vector2(-Facing * 100, -80);
		fsm.ActivateTrigger("Hurt");
		Health--;
	}
}
