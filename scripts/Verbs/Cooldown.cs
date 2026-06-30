namespace WixxTheBard.Verbs;

/// <summary>
/// A pure, Godot-free fixed-tick cooldown gate. Used by the super-jump (SPEC §2.4
/// — "fire on tilt level + cooldown") and reusable for any other gated verb. It
/// counts in fixed 60 Hz ticks (CLAUDE.md rule 3) and holds no Godot types, so it
/// is unit-tested directly. The duration lives in <c>Tunables</c> (rule 1); this
/// type never names a number.
/// </summary>
public sealed class Cooldown
{
    /// <summary>Ticks remaining before the gate is ready again.</summary>
    public int Remaining { get; private set; }

    /// <summary>True when the gate has fully recharged and may fire.</summary>
    public bool IsReady => Remaining <= 0;

    /// <summary>Normalised recharge progress in [0,1] for the lute-glow visual (1 = ready).</summary>
    public float Fraction(int totalTicks)
    {
        if (totalTicks <= 0)
        {
            return 1f;
        }

        float ready = (totalTicks - Remaining) / (float)totalTicks;
        return ready < 0f ? 0f : ready > 1f ? 1f : ready;
    }

    /// <summary>Begin the cooldown for <paramref name="totalTicks"/> fixed ticks.</summary>
    public void Start(int totalTicks)
    {
        Remaining = totalTicks < 0 ? 0 : totalTicks;
    }

    /// <summary>Advance one fixed tick, decaying the remaining time toward ready.</summary>
    public void Tick()
    {
        if (Remaining > 0)
        {
            Remaining--;
        }
    }

    /// <summary>Force the gate ready (respawn / scene reset).</summary>
    public void Reset() => Remaining = 0;
}
