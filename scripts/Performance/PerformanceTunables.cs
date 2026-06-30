namespace WixxTheBard.Performance;

/// <summary>
/// Plain-C# carrier for the M4 spell-performance feel constants — the rhythm windows,
/// success threshold, global time-slow factor, spell cooldown and result-banner hold
/// (SPEC §5.2/§5.4). Mirrors the other <c>*Tunables</c> carriers: the running game
/// builds it from the one <c>Tunables</c> resource at the Godot boundary (CLAUDE.md
/// rule 1 — numbers live in a single place); unit tests construct it directly so the
/// rhythm logic stays Godot-free and runs under plain <c>dotnet test</c>.
///
/// Window/cooldown/banner values are absolute (ms or fixed 60 Hz ticks) and so are
/// frame-rate independent (rule 3) — unlike the per-tick movement constants, a rhythm
/// window is naturally measured against the wall clock the note timing uses.
/// </summary>
public sealed class PerformanceTunables
{
    public PerformanceTunables(
        double perfectWindowMs,
        double goodWindowMs,
        double successThreshold,
        float timeSlowFactor,
        int spellCooldownTicks,
        int resultBannerTicks)
    {
        PerfectWindowMs = perfectWindowMs;
        GoodWindowMs = goodWindowMs;
        SuccessThreshold = successThreshold;
        TimeSlowFactor = timeSlowFactor;
        SpellCooldownTicks = spellCooldownTicks;
        ResultBannerTicks = resultBannerTicks;
    }

    /// <summary>±ms of a strike's calibration-relative error that scores a Perfect.</summary>
    public double PerfectWindowMs { get; }

    /// <summary>±ms that scores a Good; beyond this a strike is a stray and a note auto-misses.</summary>
    public double GoodWindowMs { get; }

    /// <summary>Fraction of notes (hits / total) that must land for the spell to fire (SPEC §5.4).</summary>
    public double SuccessThreshold { get; }

    /// <summary>World time scale while performing — enemies/hazards creep (SPEC §5.2 global slow). 1 = normal.</summary>
    public float TimeSlowFactor { get; }

    /// <summary>Cooldown ticks the spell takes to recharge after a performance — success or fail (SPEC §5.4).</summary>
    public int SpellCooldownTicks { get; }

    /// <summary>Ticks the success/fail result banner stays up after a performance (presentation hold).</summary>
    public int ResultBannerTicks { get; }
}
