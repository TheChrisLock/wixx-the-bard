namespace WixxTheBard.Verbs;

/// <summary>
/// The lute-swing (Yellow) verb as pure, Godot-free logic (SPEC §2.3 — "snappy,
/// low-commitment basic attack"). A Yellow press opens a short active window during
/// which the swing hitbox is live; the window is re-armed on each fresh press so the
/// attack stays spammable and snappy (mirrors the reference attack_timer). It is
/// authoritative gameplay state — visuals read <see cref="IsActive"/> and
/// <see cref="Facing"/>, never the other way round (CLAUDE.md rule 7).
/// </summary>
public sealed class SwingController
{
    private int _activeTicks;

    /// <summary>True while the swing hitbox should deal damage this tick.</summary>
    public bool IsActive => _activeTicks > 0;

    /// <summary>Direction the active swing was launched toward (+1 right, −1 left); visuals read it.</summary>
    public int Facing { get; private set; } = 1;

    /// <summary>
    /// Advance one fixed tick. A fresh Yellow press (<paramref name="swingJustPressed"/>)
    /// re-opens the active window for <c>SwingActiveTicks</c>, facing
    /// <paramref name="facing"/>. The swing never fires while submerged in tar
    /// (<paramref name="suppressed"/>) — strum/verb mode discipline (rule 5).
    /// </summary>
    public void Tick(bool swingJustPressed, int facing, bool suppressed, VerbTunables t)
    {
        if (_activeTicks > 0)
        {
            _activeTicks--;
        }

        if (swingJustPressed && !suppressed)
        {
            _activeTicks = t.SwingActiveTicks;
            if (facing != 0)
            {
                Facing = facing;
            }
        }
    }

    /// <summary>Clear the active window (respawn / scene reset).</summary>
    public void Reset() => _activeTicks = 0;
}
