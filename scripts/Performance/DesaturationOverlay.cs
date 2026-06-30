using Godot;

namespace WixxTheBard.Performance;

/// <summary>
/// Drives the full-screen desaturation during a spell performance (SPEC §5.2/§8).
/// Pure presentation: it only reads the authoritative <see cref="Player"/> state
/// (CLAUDE.md rule 7) and fades the shader's <c>amount</c> up while performing and
/// back down after — it never drives a mechanic. Attach to the overlay
/// <see cref="ColorRect"/> that carries the Desaturate shader material.
///
/// The fade values below are visual feel (how grey, how fast), not gameplay tunables.
/// </summary>
public partial class DesaturationOverlay : ColorRect
{
    /// <summary>How grey the world goes at full performance (0 = none, 1 = monochrome).</summary>
    [Export] public float PerformingAmount { get; set; } = 0.85f;

    /// <summary>Desaturation units per second the fade moves toward its target.</summary>
    [Export] public float FadeSpeed { get; set; } = 4.0f;

    private Player? _player;
    private ShaderMaterial? _material;
    private float _amount;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        SetAnchorsPreset(LayoutPreset.FullRect);
        _material = Material as ShaderMaterial;
        Visible = false;
    }

    public override void _Process(double delta)
    {
        _player ??= GetTree().GetFirstNodeInGroup("player") as Player;

        float target = (_player?.IsPerforming ?? false) ? PerformingAmount : 0f;
        _amount = Mathf.MoveToward(_amount, target, FadeSpeed * (float)delta);
        _material?.SetShaderParameter("amount", _amount);
        Visible = _amount > 0.001f;
    }
}
