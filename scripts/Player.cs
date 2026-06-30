using Godot;
using WixxTheBard.Controls;
using WixxTheBard.Movement;

namespace WixxTheBard;

/// <summary>
/// Wixx's <see cref="CharacterBody2D"/> — M2 core movement. It reads the
/// data-driven <see cref="GuitarInput"/> verbs (never raw indices — CLAUDE.md
/// rule 2), feeds them to the pure, authoritative <see cref="MovementCore"/>
/// (rule 7: gameplay state drives, visuals only read), and moves against the real
/// TileMap with <see cref="CharacterBody2D.MoveAndSlide"/>.
///
/// The Hold scheme, variable-height jump (with rule 4's forced-launch exemption
/// living in the core), and sprint all come from <see cref="MovementCore"/>; every
/// number comes from <see cref="Tunables"/> (rule 1). All of it runs in
/// <see cref="_PhysicsProcess"/> at the fixed tick (rule 3). The core's velocity is
/// in px/tick (the units the <c>/reference</c> constants assume); we scale it to
/// Godot's px/second using the engine's configured physics tick rate.
/// </summary>
public partial class Player : CharacterBody2D
{
    [Export] public Tunables Tunables { get; set; } = null!;

    private readonly MovementCore _core = new();
    private MovementTunables _moveTunables = null!;
    private float _ticksPerSecond;

    public override void _Ready()
    {
        // Fall back to the on-disk resource so the scene runs even if the
        // export slot wasn't wired in the editor.
        Tunables ??= GD.Load<Tunables>("res://config/Tunables.tres");
        _moveTunables = Tunables.BuildMovementTunables();
        _ticksPerSecond = (float)Engine.PhysicsTicksPerSecond;
    }

    public override void _PhysicsProcess(double delta)
    {
        var move = BuildInput(GuitarInput.Instance, IsOnFloor());
        _core.Tick(move, _moveTunables);

        // Core works in px/tick; MoveAndSlide integrates px/second * delta. With a
        // fixed 60 Hz step this restores the reference's per-tick displacement 1:1.
        Velocity = new Vector2(_core.VelocityX, _core.VelocityY) * _ticksPerSecond;
        MoveAndSlide();
    }

    private static MovementInput BuildInput(GuitarInput? input, bool onFloor)
    {
        if (input == null)
        {
            return new MovementInput(false, false, false, false, false, onFloor);
        }

        return new MovementInput(
            moveLeft: input.IsPressed(GuitarVerb.MoveLeft),
            moveRight: input.IsPressed(GuitarVerb.MoveRight),
            jumpHeld: input.IsPressed(GuitarVerb.Jump),
            jumpJustPressed: input.JustPressed(GuitarVerb.Jump),
            sprint: input.IsPressed(GuitarVerb.Sprint),
            onFloor: onFloor);
    }
}
