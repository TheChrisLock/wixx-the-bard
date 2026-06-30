using Godot;
using WixxTheBard.Controls;

namespace WixxTheBard;

/// <summary>
/// The single source of truth for every gameplay number (CLAUDE.md rule 1).
/// No gameplay code may hardcode a movement/physics constant — it reads from here.
///
/// M0 holds only the handful of numbers the hello-world box needs, in
/// pixels/second (CharacterBody2D space). The validated feel constants from
/// <c>/reference/WixxMovement.gd</c> (per-frame @ 60 Hz) are ported in M2, where
/// the real movement contract lives. These M0 values are placeholders, exposed
/// for tuning rather than copied from the prototype.
/// </summary>
[GlobalClass]
public partial class Tunables : Resource
{
    [ExportGroup("Movement (placeholder — ported properly in M2)")]
    [Export] public float MoveSpeed { get; set; } = 160.0f;
    [Export] public float Acceleration { get; set; } = 1200.0f;
    [Export] public float Friction { get; set; } = 1400.0f;

    [ExportGroup("Gravity & Jump (placeholder — ported properly in M2)")]
    [Export] public float Gravity { get; set; } = 980.0f;
    [Export] public float MaxFallSpeed { get; set; } = 600.0f;
    [Export] public float JumpVelocity { get; set; } = 360.0f;

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
}
