using Godot;

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
}
