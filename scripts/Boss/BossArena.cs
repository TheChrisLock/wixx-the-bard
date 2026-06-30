using Godot;

namespace WixxTheBard.Boss;

/// <summary>
/// The Rock Off trigger zone (SPEC §6) — an <see cref="Area2D"/> that hands Wixx to
/// <see cref="Player.EnterBossFight"/> on body entry, the same handoff pattern as
/// <see cref="TarPit"/> (CLAUDE.md rule 7: this node only marks where the duel is;
/// the authoritative fight runs on the player). <see cref="Player.EnterBossFight"/>
/// itself ignores re-entry while a fight is already running or after the
/// Choirbreaker is defeated, so this node stays a dumb trigger.
/// </summary>
public partial class BossArena : Area2D
{
    private Vector2 _entryPoint;

    public override void _Ready()
    {
        _entryPoint = GlobalPosition;
        BodyEntered += OnBodyEntered;
    }

    private void OnBodyEntered(Node2D body)
    {
        if (body is Player player)
        {
            player.EnterBossFight(_entryPoint);
        }
    }
}
