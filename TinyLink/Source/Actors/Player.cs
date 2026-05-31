using System.Numerics;
using Foster.Framework;
using static Teca.Audio;

namespace TinyLink;

public partial class Player : Actor
{
	public enum States : byte
	{
		Normal,
		Ducking,
		EnterClimbable,
		LandOnClimbable,
		Climbing,
		ClimbingIdle,
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
	private bool attackImpulseOpportunityConsumed = false;

	public StateMachine<States> fsm = new();
	public bool IsClimbing => fsm.CurrentState is States.Climbing or States.ClimbingIdle or States.LandOnClimbable or States.EnterClimbable;
	public bool IsDucking => fsm.CurrentState is States.Ducking;

	public Player()
	{
		Sprite = Assets.GetSprite("player");
		Hitbox = new(new RectInt(-4, -12, 8, 12));
		Mask = Masks.Player;
		IFrameTime = InvincibleDuration;
		grounded = true;

		// Normal
		fsm.AddState(States.Normal, new State<States>(
			onEnter: () => { Play("idle"); },
			onUpdate: () => NormalState()
		));

		//Ducking
		fsm.AddState(States.Ducking, new State<States>(
			onEnter: () => { Play("duck"); },
			onUpdate: () => DuckingState()
		));

		// Land On Climbable: Just to do a cool effect when you transition from Airborne to climcable (e.g. Rope, Ladder)
		fsm.AddState(States.LandOnClimbable, new State<States>(
			onEnter: () =>
			{
				Squish = new Vector2(0.65f, 1.4f);
				fsm.ActivateTrigger("EnterClimbable");
			}
		));

		//EnterClimbable
		fsm.AddState(States.EnterClimbable, new State<States>(
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
				Play("climb_idle");
				Stop();

				if(MathF.Abs(Controls.Move.IntValue.Y) > 0)
					fsm.ActivateTrigger("Climbing");
				else
					fsm.ActivateTrigger("ClimbingIdle");
			}
		));

		//ClimbingIdle
		fsm.AddState(States.ClimbingIdle, new State<States>(
			onEnter: () =>
			{
				Play("climb_idle");
				Stop();
			},
			onUpdate: () => ClimbingIdleState()
		));
		

		// Climbing
		fsm.AddState(States.Climbing, new State<States>(
			onEnter: () =>
			{
				Play("climb");
			},
			onUpdate: () => ClimbingState()
		));

		// Airborne: In the air
		fsm.AddState(States.Airborne, new State<States>(
			onEnter: () =>
			{
				Play("jump");
			},
			onUpdate: () =>
			{
				AirborneState();
			}
		));

		// Attack
		fsm.AddState(States.Attack, new State<States>(
			onEnter: () =>
			{
				// if (grounded)
					// StopX();
				attackImpulseOpportunityConsumed = false;
				PlaySound("slash");
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
		fsm.AddState(States.Start, new State<States>(
			onEnter: () =>
			{
				Play("sword");
			},
			onUpdate: () =>
			{
				StartState();
			}
		));

		// Reset the state duration when entering any state
		fsm.OnAnyEnter = () => stateDuration = 0f;

		// Initial state
		fsm.SetState(States.Start);

		// Add condition based transitions
		fsm.AddTransition(States.Normal, States.Ducking, condition: () => grounded && Controls.Move.IntValue.Y > 0);
		fsm.AddTransition(States.Ducking, States.Normal, condition: () => !(grounded && Controls.Move.IntValue.Y > 0));

		fsm.AddTransition([States.Normal, States.Ducking], States.Airborne, condition: () => !grounded);
		fsm.AddTransition([States.Normal, States.Ducking], States.Attack, condition: () => Controls.Attack.ConsumePress());
		fsm.AddTransition(States.Normal, States.EnterClimbable, condition: () => OverlapsAny(Masks.Rope | Masks.Ladder) && Controls.Move.IntValue.Y < 0); // is in the ground and starts climbing
		fsm.AddTransition(States.Normal, States.EnterClimbable, condition: () => OverlapsAny(Point2.Down * 12, Masks.Ladder) && Controls.Move.IntValue.Y > 0); // if on top of a ladder and going down

		fsm.AddTransition(States.Attack, States.EnterClimbable, condition: () => OverlapsAny(Masks.Rope | Masks.Ladder) && Controls.Move.IntValue.Y < 0); // can cancel attack and grab climbable

		fsm.AddTransition(States.ClimbingIdle, States.Climbing, condition: () => !grounded && MathF.Abs(Controls.Move.IntValue.Y) > 0);
		fsm.AddTransition(States.ClimbingIdle, States.Airborne, condition: () => !grounded && Controls.Jump.Down && Controls.Move.IntValue.Y > 0);  // Step down

		fsm.AddTransition(States.Climbing, States.ClimbingIdle, condition: () => !grounded && Velocity.Y == 0 && Controls.Move.IntValue.Y == 0);
		fsm.AddTransition(States.Climbing, States.Airborne, condition: () => !grounded && Controls.Jump.Down && Controls.Move.IntValue.Y > 0);  // Step down
		fsm.AddTransition(States.Climbing, States.Normal, condition: () => grounded && Controls.Move.IntValue.Y >= 0 && (MathF.Abs(Controls.Move.IntValue.X) > 0 || Controls.Move.IntValue.Y > 0));  // in the ground and pressing down or moving, but not moving up

		fsm.AddTransition(States.Airborne, States.Normal, condition: () => grounded);
		fsm.AddTransition(States.Airborne, States.Attack, condition: () => Controls.Attack.ConsumePress());
		fsm.AddTransition(States.Airborne, States.LandOnClimbable, condition: () => OverlapsAny(Masks.Rope | Masks.Ladder) && Controls.Move.IntValue.Y < 0);

		// Add triggers based global transitions
		fsm.AddGlobalTransition(States.Normal, trigger: "Normal");
		fsm.AddGlobalTransition(States.Climbing, trigger: "Climbing");
		fsm.AddGlobalTransition(States.ClimbingIdle, trigger: "ClimbingIdle");
		fsm.AddGlobalTransition(States.EnterClimbable, trigger: "EnterClimbable");
		fsm.AddGlobalTransition(States.Airborne, trigger: "Airborne");
		fsm.AddGlobalTransition(States.Hurt, trigger: "Hurt");
	}

	public override void Update()
	{
		if(IsNetworkGhost)
		{
			Velocity = Vector2.Zero;
		}

		base.Update();

		// update grounded state
		var nowGrounded = Velocity.Y >= 0 && Grounded();
		if (nowGrounded && !grounded)
			Squish = new Vector2(1.5f, 0.70f);
		grounded = nowGrounded;

		if(!IsNetworkGhost)
			fsm.Update();
		else
			fsm.UpdateCurrentState();

		// if(IsNetworkGhost) System.Console.WriteLine(fsm.CurrentState);

		if (IsDucking)
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

		// detect getting hit
		if (OverlapsFirst(Masks.Enemy | Masks.Hazard) is Actor hit)
			hit.Hit(this);

		stateDuration += Time.Delta;

		if(IsNetworkGhost) return;

		// gravity
		if (!grounded && !IsClimbing)
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
				Position = new (Game.Bounds.Right, Position.Y);
			}
			else if (Position.X < Game.Bounds.Left && !Game.Transition(Point2.Left))
			{
				Position = new (Game.Bounds.Left, Position.Y);
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
					Position = new (Position.X, Game.Bounds.Top);
			}
		}
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

#if DEBUG
	RectInt? attackHitbox = null;

	public void RenderAttackDebug(Batcher batcher, Point2 offset, Color color)
	{
		batcher.PushMatrix(offset);

		if (attackHitbox.HasValue)
		{
			var rect = attackHitbox.Value;
			if (Facing == Signs.Negative)
				rect.X = -(rect.X + rect.Width);
			batcher.RectLine(rect + offset, 1, color);
		}

		batcher.PopMatrix();

		attackHitbox = null;
	}
#endif
}
