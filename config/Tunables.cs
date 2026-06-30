using Godot;
using WixxTheBard.Controls;
using WixxTheBard.Movement;
using WixxTheBard.Verbs;

namespace WixxTheBard;

/// <summary>
/// The single source of truth for every gameplay number (CLAUDE.md rule 1).
/// No gameplay code may hardcode a movement/physics constant — it reads from here.
///
/// The movement/gravity values below are ported 1:1 from the validated
/// <c>/reference/WixxMovement.gd</c> grey-box (SPEC §14) and are expressed in
/// <b>per-60 Hz-tick units</b> — the units those constants assume (rule 3). They
/// are exposed for tuning rather than frozen; the Godot layer converts velocity to
/// px/second for <c>MoveAndSlide</c> using the engine's configured tick rate.
/// </summary>
[GlobalClass]
public partial class Tunables : Resource
{
    [ExportGroup("Movement — Hold scheme (per 60 Hz tick; ported from /reference)")]

    /// <summary>px/tick² added while a direction is held (reference MOVE_ACCEL).</summary>
    [Export] public float MoveAccel { get; set; } = 0.9f;

    /// <summary>Base horizontal speed cap in px/tick, before sprint (reference MAX_SPEED).</summary>
    [Export] public float MaxSpeed { get; set; } = 4.2f;

    /// <summary>Multiplicative horizontal decay per tick when no direction is held (reference FRICTION_HOLD).</summary>
    [Export] public float FrictionHold { get; set; } = 0.78f;

    /// <summary>Top sprint multiplier on <see cref="MaxSpeed"/> at full charge (reference SPRINT_MAX).</summary>
    [Export] public float SprintMaxMultiplier { get; set; } = 1.9f;

    /// <summary>Sprint charge gained per tick while Sprint is held.</summary>
    [Export] public float SprintChargeRate { get; set; } = 0.03f;

    /// <summary>Sprint charge bled per tick back toward 1.0 while Sprint is released.</summary>
    [Export] public float SprintDecayRate { get; set; } = 0.05f;

    [ExportGroup("Gravity & Jump (per 60 Hz tick; ported from /reference)")]

    /// <summary>px/tick² downward acceleration (reference GRAVITY).</summary>
    [Export] public float Gravity { get; set; } = 0.8f;

    /// <summary>Downward speed cap in px/tick — collision safety so a fast fall can't tunnel a tile.</summary>
    [Export] public float TerminalFallSpeed { get; set; } = 15.0f;

    /// <summary>Upward launch speed of a button jump in px/tick (reference JUMP_V).</summary>
    [Export] public float JumpVelocity { get; set; } = 13.0f;

    /// <summary>Upward-velocity multiplier when a button jump is released early — the variable-height cut.
    /// Forced launches are exempt (rule 4).</summary>
    [Export] public float JumpCutFactor { get; set; } = 0.85f;

    /// <summary>Gravity scale while rising with the jump sustained — the floaty rise (reference 0.6).</summary>
    [Export] public float RisingGravityFactor { get; set; } = 0.6f;

    [ExportGroup("Input (M1) — data-driven layer + remapper")]

    /// <summary>Deflection from an axis's learned rest at which it reads as engaged.
    /// Ported from <c>/reference</c> TILT_DEFLECT (0.35).</summary>
    [Export] public float AxisEngageThreshold { get; set; } = 0.35f;

    /// <summary>Larger deflection a deliberate push must clear to bind an axis (vs. noise).</summary>
    [Export] public float AxisCaptureThreshold { get; set; } = 0.5f;

    /// <summary>Deflection from rest treated as a fully-engaged analog value (crouch depth, M3).</summary>
    [Export] public float AxisFullDeflection { get; set; } = 1.0f;

    /// <summary>Fixed ticks spent learning each axis's rest value before a press-to-bind capture listens.</summary>
    [Export] public int BindingBaselineSamples { get; set; } = 12;

    /// <summary>Raw joypad buttons to scan (frets land on arbitrary indices — SPEC §14).</summary>
    [Export] public int ButtonScanCount { get; set; } = 32;

    /// <summary>Raw joypad axes to scan (whammy/tilt land on arbitrary axes).</summary>
    [Export] public int AxisScanCount { get; set; } = 10;

    [ExportGroup("Verbs — Lute swing & enemies (M3; per 60 Hz tick)")]

    /// <summary>Ticks the lute-swing hitbox stays live after a Yellow press (reference attack_timer = 12).</summary>
    [Export] public int SwingActiveTicks { get; set; } = 12;

    /// <summary>Horizontal speed (px/tick) an enemy contact pushes Wixx back — light feedback, not a damage system.</summary>
    [Export] public float EnemyKnockbackSpeed { get; set; } = 3.0f;

    /// <summary>Ticks of contact-immunity after a knockback so one touch can't repeat-stunlock.</summary>
    [Export] public int ContactInvulnTicks { get; set; } = 30;

    [ExportGroup("Verbs — Tilt super-jump (M3; per 60 Hz tick; ported from /reference)")]

    /// <summary>Upward launch speed of the super-jump in px/tick (reference SUPER_JUMP_V = 21, ~5× a normal jump).</summary>
    [Export] public float SuperJumpVelocity { get; set; } = 21.0f;

    /// <summary>Ticks the super-jump floats uncut so it reaches full height (reference SUPER_LAUNCH = 50; rule 4).</summary>
    [Export] public int SuperJumpLaunchTicks { get; set; } = 50;

    /// <summary>Cooldown ticks before the super-jump can fire again (reference SUPER_CD = 90, ~1.5 s @ 60 Hz).</summary>
    [Export] public int SuperJumpCooldownTicks { get; set; } = 90;

    [ExportGroup("Verbs — Whammy crouch/slide (M3; per 60 Hz tick)")]

    /// <summary>Horizontal speed (px/tick) a crouch must carry to trigger a slide (reference 1.5).</summary>
    [Export] public float SlideSpeedThreshold { get; set; } = 1.5f;

    /// <summary>Speed (px/tick) at or below which a committed slide ends and Wixx settles into a crouch.</summary>
    [Export] public float SlideStopSpeed { get; set; } = 0.5f;

    /// <summary>Per-tick horizontal decay during a committed slide — a slower bleed than <see cref="FrictionHold"/>
    /// so the slide glides a short distance before stopping, even with the strum held.</summary>
    [Export] public float SlideFriction { get; set; } = 0.92f;

    /// <summary>Top horizontal speed (px/tick) while ducked — a slow crouch-walk. Kept below
    /// <see cref="SlideSpeedThreshold"/> so shuffling never accidentally triggers a slide.</summary>
    [Export] public float CrouchWalkSpeed { get; set; } = 1.2f;

    /// <summary>Crouch collision height as a fraction of standing height — how low a slide ducks (reference ~0.5).</summary>
    [Export] public float CrouchHeightFactor { get; set; } = 0.5f;

    [ExportGroup("Tar / quicksand hazard (M3; per 60 Hz tick; ported from /reference)")]

    /// <summary>Constant downward pull each tick, in px (reference TAR_SINK = 0.55).</summary>
    [Export] public float TarSinkPerTick { get; set; } = 0.55f;

    /// <summary>Depth (px) a clean alternating strum climbs (reference TAR_KICK_RISE = 7).</summary>
    [Export] public float TarKickRise { get; set; } = 7.0f;

    /// <summary>Fraction of <see cref="TarKickRise"/> a same-direction strum earns — mashing one way barely helps (reference 0.15).</summary>
    [Export] public float TarWeakKickFactor { get; set; } = 0.15f;

    /// <summary>Forward nudge (px) per clean alternating strum (reference TAR_KICK_FWD = 8).</summary>
    [Export] public float TarKickForward { get; set; } = 8.0f;

    /// <summary>Depth (px) at which full submersion kills (reference TAR_DEATH = 58).</summary>
    [Export] public float TarDeathDepth { get; set; } = 58.0f;

    /// <summary>Depth (px) Wixx plunges to on contact — must exceed a couple of kicks (reference TAR_ENTRY_DEPTH = 26).</summary>
    [Export] public float TarEntryDepth { get; set; } = 26.0f;

    /// <summary>Upward launch speed (px/tick) of the surface-breach leap (reference TAR_EXIT_JUMP = 12).</summary>
    [Export] public float TarExitJumpVelocity { get; set; } = 12.0f;

    /// <summary>Forward launch speed (px/tick) carried out of the pit on breach (reference TAR_EXIT_FWD = 4).</summary>
    [Export] public float TarExitForwardVelocity { get; set; } = 4.0f;

    /// <summary>Ticks the breach leap floats uncut so it clears the pit (reference TAR_LAUNCH_FRAMES = 14; rule 4).</summary>
    [Export] public int TarExitLaunchTicks { get; set; } = 14;

    [ExportGroup("A/V Calibration (M1)")]

    /// <summary>Default latency offset (ms) until the player calibrates.</summary>
    [Export] public double DefaultLatencyOffsetMs { get; set; } = 0.0;

    /// <summary>Metronome period (ms) on the calibration screen.</summary>
    [Export] public double CalibrationBeatMs { get; set; } = 1000.0;

    /// <summary>Taps to gather before the calibration screen recommends an offset.</summary>
    [Export] public int CalibrationSampleTarget { get; set; } = 8;

    /// <summary>Build the Godot-free <see cref="InputTuning"/> the pure input layer reads.</summary>
    public InputTuning BuildInputTuning() => new(
        AxisEngageThreshold,
        AxisCaptureThreshold,
        AxisFullDeflection,
        BindingBaselineSamples,
        ButtonScanCount,
        AxisScanCount);

    /// <summary>Build the Godot-free <see cref="VerbTunables"/> the M3 verb logic reads.</summary>
    public VerbTunables BuildVerbTunables() => new(
        SwingActiveTicks,
        SuperJumpVelocity,
        SuperJumpLaunchTicks,
        SuperJumpCooldownTicks,
        SlideSpeedThreshold,
        SlideStopSpeed,
        CrouchHeightFactor,
        EnemyKnockbackSpeed,
        ContactInvulnTicks);

    /// <summary>Build the Godot-free <see cref="TarTunables"/> the pure tar state machine reads.</summary>
    public TarTunables BuildTarTunables() => new(
        TarSinkPerTick,
        TarKickRise,
        TarWeakKickFactor,
        TarKickForward,
        TarDeathDepth,
        TarEntryDepth,
        TarExitJumpVelocity,
        TarExitForwardVelocity,
        TarExitLaunchTicks);

    /// <summary>Build the Godot-free <see cref="MovementTunables"/> the pure movement core reads.</summary>
    public MovementTunables BuildMovementTunables() => new(
        MoveAccel,
        MaxSpeed,
        FrictionHold,
        SprintMaxMultiplier,
        SprintChargeRate,
        SprintDecayRate,
        Gravity,
        TerminalFallSpeed,
        JumpVelocity,
        JumpCutFactor,
        RisingGravityFactor,
        SlideFriction,
        CrouchWalkSpeed);
}
