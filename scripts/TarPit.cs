using Godot;

namespace WixxTheBard;

/// <summary>
/// A tar / quicksand region (SPEC §4.4). It is an <see cref="Area2D"/> with a
/// rectangular shape; when Wixx's body enters, it hands him to
/// <see cref="Player.EnterTar"/> with the surface line and the pit's horizontal
/// bounds. The struggle itself is run by the authoritative pure
/// <see cref="WixxTheBard.Verbs.TarState"/> on the player (CLAUDE.md rule 7) — this
/// node only marks where the hazard is. Its geometry (position/size) is level
/// layout, not gameplay-feel tuning, so it is allowed to live in the scene rather
/// than <c>Tunables</c>.
/// </summary>
public partial class TarPit : Area2D
{
    private float _surfaceY;
    private float _leftX;
    private float _rightX;

    public override void _Ready()
    {
        var shape = GetNode<CollisionShape2D>("CollisionShape2D");
        if (shape.Shape is RectangleShape2D rect)
        {
            Vector2 center = shape.GlobalPosition;
            Vector2 half = rect.Size * 0.5f;
            _surfaceY = center.Y - half.Y; // top edge = the tar surface
            _leftX = center.X - half.X;
            _rightX = center.X + half.X;
        }

        BodyEntered += OnBodyEntered;
    }

    private void OnBodyEntered(Node2D body)
    {
        if (body is Player player)
        {
            player.EnterTar(_surfaceY, _leftX, _rightX);
        }
    }
}
