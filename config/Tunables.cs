using Godot;
using WixxTheBard.Controls;
using WixxTheBard.Movement;

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
        RisingGravityFactor);
}
