using Godot;

namespace WixxTheBard;

/// <summary>
/// M0 hello-world actor: a <see cref="CharacterBody2D"/> box driven by the
/// keyboard, proving real collision against the TileMap and fixed-step movement.
///
/// All physics runs in <see cref="_PhysicsProcess"/> at the fixed 60 Hz tick
/// (CLAUDE.md rule 3) and every number comes from <see cref="Tunables"/>
/// (rule 1). Input goes through named, data-driven actions — never raw keycodes
/// (rule 2 spirit); the real remappable guitar layer arrives in M1.
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

        var direction = Input.GetAxis("move_left", "move_right");
        if (direction != 0.0f)
        {
            velocity.X = Mathf.MoveToward(velocity.X, direction * Tunables.MoveSpeed, Tunables.Acceleration * dt);
        }
        else
        {
            velocity.X = Mathf.MoveToward(velocity.X, 0.0f, Tunables.Friction * dt);
        }

        if (Input.IsActionJustPressed("jump") && IsOnFloor())
        {
            velocity.Y = -Tunables.JumpVelocity;
        }

        Velocity = velocity;
        MoveAndSlide();
    }
}
