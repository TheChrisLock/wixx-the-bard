using Godot;

namespace WixxTheBard;

/// <summary>
/// A basic dispatchable enemy (SPEC §3 — "Dispatch basic enemies with the Yellow
/// lute swing"). M3 keeps it deliberately minimal (rule 8 — scope discipline): it
/// is a target that the lute swing or a slide defeats, and that lightly knocks Wixx
/// back on a plain bump. No HP, score, or enemy-inflicted death — tar is M3's only
/// death state.
///
/// It is a <see cref="CharacterBody2D"/> so the player's swing/body
/// <see cref="Area2D"/>s detect it via <c>GetOverlappingBodies</c>, and it sits in
/// the <c>enemies</c> group. <see cref="Defeat"/> is idempotent so one swing tick
/// can't double-process it.
/// </summary>
public partial class Enemy : CharacterBody2D
{
    /// <summary>True once defeated — guards against a second hit in the same active window.</summary>
    public bool Defeated { get; private set; }

    public override void _Ready() => AddToGroup("enemies");

    /// <summary>Dispatch this enemy (swing hit or slide knockdown). Idempotent.</summary>
    public void Defeat()
    {
        if (Defeated)
        {
            return;
        }

        Defeated = true;
        // Grey-box feedback only; art is deferred to post-M6 (SPEC §8).
        var tween = CreateTween();
        tween.TweenProperty(this, "modulate:a", 0.0f, 0.12f);
        tween.TweenProperty(this, "scale", new Vector2(1.4f, 0.2f), 0.12f);
        tween.TweenCallback(Callable.From(QueueFree));
    }
}
