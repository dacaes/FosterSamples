using System.Numerics;
using Foster.Framework;
using static Teca.Audio;

namespace TinyLink;

public partial class Player : Actor
{
    private void GroundMovement(bool ducking)
    {
        if(IsNetworkGhost) return;

        // get input
		var inputX = Controls.Move.IntValue.X;
		var inputY = Controls.Move.IntValue.Y;
        
        // horizontal movement
		{
			// Acceleration
			Velocity.X += inputX * GroundAccel * Time.Delta;

			// Max Speed
			var maxspd = MaxGroundSpeed;
			maxspd = ducking ? maxspd * 0.3f : maxspd;
			if (MathF.Abs(Velocity.X) > maxspd)
				Velocity.X = Calc.Approach(Velocity.X, MathF.Sign(Velocity.X) * maxspd, 2000 * Time.Delta);

			// Friction
			if (inputX == 0)
				Velocity.X = Calc.Approach(Velocity.X, 0, Friction * Time.Delta);

			// Facing
			if (!IsNetworkGhost && inputX != 0)
				Facing = inputX;
		}

		// Start jumping
		if (Controls.Jump.ConsumePress())
		{
			// Step down jumpthru or a ladder
			if(inputY > 0 && OverlapsAny(Point2.Down, Masks.Jumpthru | Masks.Ladder))
			{
				Position += Point2.Down;
				fsm.ActivateTrigger("Airborne");
			}
			else
				StartJump();
		}
    }

    public void NormalState()
	{
		if (Moving)
			Play("run");
		else
			Play("idle");

		GroundMovement(false);
	}

	public void DuckingState()
	{
        if (Moving)
			Play("duck");
		else
			Play("duck");

		GroundMovement(true);
	}

	public void StartJump()
	{
		var input = Controls.Move.IntValue.X;
		Squish = new Vector2(0.65f, 1.4f);
		StopX();
		Velocity.X = input * MaxAirSpeed;
		jumpTimer = JumpTime;
		if(input != 0)
			Facing = input;
		fsm.ActivateTrigger("Airborne");
	}

	public void AirborneState()
	{
		if(IsNetworkGhost) return;

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

	public void ClimbingIdleState()
	{
        if(IsNetworkGhost) return;

		if (Controls.Jump.ConsumePress())
		{
			StartJump();
		}
	}

	public void ClimbingState()
	{
		if(IsNetworkGhost) return;
        
		// vertical movement
		{
			var input = Controls.Move.IntValue.Y;

			// if (MathF.Abs(input) > 0)
			// 	Play("climb");
			// else
			// 	Play("climb_idle");

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
			
			if(grounded && !attackImpulseOpportunityConsumed)
				Velocity.X = Facing * 60;
				
			attackImpulseOpportunityConsumed = true;
		}

		if (hitbox != null)
		{
# if DEBUG
			attackHitbox = hitbox;
#endif
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
}