using System;

namespace WixxTheBard.Movement;

/// <summary>
/// The pure, Godot-free movement physics for Wixx — the authoritative gameplay
/// state that visuals read from (CLAUDE.md rule 7). It ports the validated feel of
/// <c>/reference/WixxMovement.gd</c> (Hold scheme, variable-height jump, sprint
/// charge) onto a model that the real <see cref="Godot.CharacterBody2D"/> drives
/// against real collision — the prototype's hand-rolled floor integration is *not*
/// copied; only its numbers and behaviour are.
///
/// Velocity is carried in <b>px/tick</b> (the 60 Hz units the reference constants
/// assume — rule 3); the Godot layer converts to px/second for <c>MoveAndSlide</c>.
/// One <see cref="Tick"/> = one fixed physics step. The struct holds no Godot types
/// and allocates nothing on the hot path (rule 9), so it is unit-tested directly.
/// </summary>
public sealed class MovementCore
{
    /// <summary>Horizontal velocity, px/tick. Positive = right.</summary>
    public float VelocityX { get; private set; }

    /// <summary>Vertical velocity, px/tick. Positive = down (Godot screen space).</summary>
    public float VelocityY { get; private set; }

    /// <summary>Last horizontal facing (+1 right, -1 left); visuals read this, never set it.</summary>
    public int Facing { get; private set; } = 1;

    /// <summary>Current sprint multiplier, in [1, <c>SprintMaxMultiplier</c>].</summary>
    public float SprintCharge { get; private set; } = 1f;

    /// <summary>Remaining ticks a forced launch is floating; while &gt; 0 the jump cut is suppressed (rule 4).</summary>
    public int LaunchFrames { get; private set; }

    /// <summary>True while a forced (non-button) launch is in progress.</summary>
    public bool InLaunch => LaunchFrames > 0;

    /// <summary>
    /// Begin a forced launch — a super-jump or tar-exit leap (SPEC §2.4 / §4.4).
    /// These are <b>not</b> triggered by the jump button, so the variable-height
    /// cut must never touch them; for <paramref name="frames"/> ticks the rise is
    /// kept whole and gravity stays soft. This is CLAUDE.md rule 4, baked into the
    /// physics contract so M3's super-jump and tar leap inherit the exemption and
    /// can never silently collapse to a fraction of their height.
    /// </summary>
    /// <param name="upwardVelocity">Launch speed in px/tick (positive value).</param>
    /// <param name="frames">Ticks the launch floats uncut; should cover the whole rise.</param>
    public void ForcedLaunch(float upwardVelocity, int frames)
    {
        VelocityY = -upwardVelocity;
        LaunchFrames = frames;
    }

    /// <summary>
    /// Zero linear velocity without disturbing facing/sprint/launch — used when Wixx
    /// is handed to an external driver for a tick (e.g. the tar struggle suspends
    /// <c>MoveAndSlide</c> and positions him from the authoritative tar depth).
    /// </summary>
    public void ZeroVelocity()
    {
        VelocityX = 0f;
        VelocityY = 0f;
        LaunchFrames = 0;
    }

    /// <summary>
    /// Set horizontal velocity directly — the forward carry of the tar-exit leap
    /// (SPEC §4.4) or an enemy-contact knockback. The vertical launch of the breach
    /// leap goes through <see cref="ForcedLaunch"/> so it stays exempt from the cut
    /// (rule 4); this only supplies the sideways component.
    /// </summary>
    public void ApplyHorizontalImpulse(float velocityX, int facing)
    {
        VelocityX = velocityX;
        if (facing != 0)
        {
            Facing = facing;
        }
    }

    /// <summary>Reset all motion state (respawn / scene reset).</summary>
    public void Reset()
    {
        VelocityX = 0f;
        VelocityY = 0f;
        Facing = 1;
        SprintCharge = 1f;
        LaunchFrames = 0;
    }

    /// <summary>Advance one fixed physics tick.</summary>
    public void Tick(in MovementInput input, MovementTunables t)
    {
        // --- Sprint charge: Red held builds it, released bleeds it back to 1.0. ---
        SprintCharge = input.Sprint
            ? Math.Min(t.SprintMaxMultiplier, SprintCharge + t.SprintChargeRate)
            : Math.Max(1f, SprintCharge - t.SprintDecayRate);
        float maxSpeed = t.MaxSpeed * SprintCharge;

        // --- Horizontal (Hold / Scheme B): held direction accelerates, none decays.
        //     Crouch/slide override the strum: a committed slide glides to a stop on
        //     SlideFriction (a short distance, ignoring held strum), and a settled
        //     crouch ducks in place (no running while ducked). ---
        int dir = (input.MoveRight ? 1 : 0) - (input.MoveLeft ? 1 : 0);
        if (input.Sliding)
        {
            VelocityX *= t.SlideFriction;
        }
        else if (input.Crouching)
        {
            VelocityX *= t.FrictionHold;
        }
        else if (dir != 0)
        {
            VelocityX += dir * t.MoveAccel;
            Facing = dir;
        }
        else
        {
            VelocityX *= t.FrictionHold;
        }

        VelocityX = Math.Clamp(VelocityX, -maxSpeed, maxSpeed);

        // --- Forced-launch timer (rule 4): count down before reading it below. ---
        if (LaunchFrames > 0)
        {
            LaunchFrames--;
        }

        // --- Button jump: only the jump button leaves the floor here; forced
        //     launches enter via ForcedLaunch and are exempt from the cut. ---
        if (input.JumpJustPressed && input.OnFloor)
        {
            VelocityY = -t.JumpVelocity;
        }

        // --- Vertical: gravity + the variable-height cut. ---
        if (input.OnFloor && VelocityY > 0f)
        {
            // Resting on the floor; floor-snap keeps us grounded with zero fall speed.
            VelocityY = 0f;
        }
        else if (!input.OnFloor)
        {
            // A HELD jump button OR an active forced launch sustains the arc.
            // rule 4: a forced launch (LaunchFrames > 0) is never cut.
            bool sustaining = input.JumpHeld || LaunchFrames > 0;
            if (!sustaining && VelocityY < 0f)
            {
                VelocityY *= t.JumpCutFactor;
            }

            float gravity = (sustaining && VelocityY < 0f)
                ? t.Gravity * t.RisingGravityFactor
                : t.Gravity;
            VelocityY = Math.Min(VelocityY + gravity, t.TerminalFallSpeed);
        }
    }
}
