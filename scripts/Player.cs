using Godot;
using WixxTheBard.Controls;

namespace WixxTheBard;

/// <summary>
/// Hello-world actor: a <see cref="CharacterBody2D"/> box on the real TileMap,
/// proving collision and fixed-step movement. As of M1 it reads the data-driven
/// <see cref="GuitarInput"/> layer (verbs, never raw indices — CLAUDE.md rule 2),
/// so the same box responds to a bound guitar or the keyboard fallback. This is
/// the surface for feel-verifying the input pipeline on real hardware; the full
/// Hold-scheme movement contract (sprint, variable jump) lands in M2.
///
/// All physics runs in <see cref="_PhysicsProcess"/> at the fixed 60 Hz tick
/// (rule 3) and every number comes from <see cref="Tunables"/> (rule 1).
/// </summary>
public partial class Player : CharacterBody2D
{
    [Export] public Tunables Tunables { get; set; } = null!;

    public override void _Ready()
    {
        // Fall back to the on-disk resource so the scene runs even if the
        // export slot wasn't wired in the editor.
        Tunables ??= GD.Load<Tunables>("res://config/Tunables.tres");
    }

    public override void _PhysicsProcess(double delta)
    {
        var dt = (float)delta;
        var velocity = Velocity;

        if (!IsOnFloor())
        {
            velocity.Y = Mathf.Min(velocity.Y + Tunables.Gravity * dt, Tunables.MaxFallSpeed);
        }

        var input = GuitarInput.Instance;
        var direction = 0.0f;
        if (input != null)
        {
            if (input.IsPressed(GuitarVerb.MoveRight))
            {
                direction += 1.0f;
            }

            if (input.IsPressed(GuitarVerb.MoveLeft))
            {
                direction -= 1.0f;
            }
        }

        if (direction != 0.0f)
        {
            velocity.X = Mathf.MoveToward(velocity.X, direction * Tunables.MoveSpeed, Tunables.Acceleration * dt);
        }
        else
        {
            velocity.X = Mathf.MoveToward(velocity.X, 0.0f, Tunables.Friction * dt);
        }

        if (input != null && input.JustPressed(GuitarVerb.Jump) && IsOnFloor())
        {
            velocity.Y = -Tunables.JumpVelocity;
        }

        Velocity = velocity;
        MoveAndSlide();
    }
}
