namespace WixxTheBard.Movement;

/// <summary>
/// Plain-C# carrier for the movement-feel constants, mirroring
/// <c>WixxTheBard.Controls.InputTuning</c>. The running game builds this from the
/// <c>Tunables</c> resource at the Godot boundary (CLAUDE.md rule 1 — the values
/// live in one place); unit tests construct it directly so <see cref="MovementCore"/>
/// stays Godot-free and runnable under <c>dotnet test</c>.
///
/// All values are in <b>per-60 Hz-tick units</b>, ported 1:1 from the validated
/// <c>/reference/WixxMovement.gd</c> grey-box (SPEC §14). They assume the fixed
/// 60 Hz physics step (rule 3); the Godot layer scales velocity to px/second for
/// <c>MoveAndSlide</c> using the engine's configured tick rate.
/// </summary>
public sealed class MovementTunables
{
    public MovementTunables(
        float moveAccel,
        float maxSpeed,
        float frictionHold,
        float sprintMaxMultiplier,
        float sprintChargeRate,
        float sprintDecayRate,
        float gravity,
        float terminalFallSpeed,
        float jumpVelocity,
        float jumpCutFactor,
        float risingGravityFactor,
        float slideFriction,
        float crouchWalkSpeed)
    {
        MoveAccel = moveAccel;
        MaxSpeed = maxSpeed;
        FrictionHold = frictionHold;
        SprintMaxMultiplier = sprintMaxMultiplier;
        SprintChargeRate = sprintChargeRate;
        SprintDecayRate = sprintDecayRate;
        Gravity = gravity;
        TerminalFallSpeed = terminalFallSpeed;
        JumpVelocity = jumpVelocity;
        JumpCutFactor = jumpCutFactor;
        RisingGravityFactor = risingGravityFactor;
        SlideFriction = slideFriction;
        CrouchWalkSpeed = crouchWalkSpeed;
    }

    /// <summary>px/tick² added to horizontal velocity while a direction is held (reference MOVE_ACCEL).</summary>
    public float MoveAccel { get; }

    /// <summary>Base horizontal speed cap in px/tick, before the sprint multiplier (reference MAX_SPEED).</summary>
    public float MaxSpeed { get; }

    /// <summary>Multiplicative horizontal decay per tick when no direction is held (reference FRICTION_HOLD).</summary>
    public float FrictionHold { get; }

    /// <summary>Top sprint multiplier on <see cref="MaxSpeed"/> at full charge (reference SPRINT_MAX).</summary>
    public float SprintMaxMultiplier { get; }

    /// <summary>Sprint charge gained per tick while Sprint is held.</summary>
    public float SprintChargeRate { get; }

    /// <summary>Sprint charge bled per tick back toward 1.0 while Sprint is released.</summary>
    public float SprintDecayRate { get; }

    /// <summary>px/tick² downward acceleration (reference GRAVITY).</summary>
    public float Gravity { get; }

    /// <summary>Downward speed cap in px/tick — collision safety so a fast fall can't tunnel a tile (not in the proto).</summary>
    public float TerminalFallSpeed { get; }

    /// <summary>Upward launch speed of a button jump in px/tick (reference JUMP_V).</summary>
    public float JumpVelocity { get; }

    /// <summary>Upward-velocity multiplier applied when a button jump is released early — the variable-height cut.
    /// Forced launches are exempt (CLAUDE.md rule 4).</summary>
    public float JumpCutFactor { get; }

    /// <summary>Gravity scale while rising with the jump sustained (held button or forced launch) — the floaty rise.</summary>
    public float RisingGravityFactor { get; }

    /// <summary>Multiplicative horizontal decay per tick during a committed slide — a slower bleed than
    /// <see cref="FrictionHold"/> so the slide glides a short distance before stopping.</summary>
    public float SlideFriction { get; }

    /// <summary>Top horizontal speed (px/tick) while ducked — a slow crouch-walk. Kept below the slide
    /// trigger speed so shuffling never accidentally becomes a slide.</summary>
    public float CrouchWalkSpeed { get; }
}
