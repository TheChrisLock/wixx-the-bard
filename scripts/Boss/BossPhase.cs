using WixxTheBard.Performance;

namespace WixxTheBard.Boss;

/// <summary>
/// Which of the Rock Off's two duel formats a <see cref="BossPhase"/> is (SPEC §6).
/// </summary>
public enum BossPhaseKind
{
    /// <summary>The boss telegraphs a phrase, then the player must echo it on the
    /// highway. Teaches the boss's tells; a miss costs <see cref="BossFight"/> Resolve,
    /// it never damages the boss (SPEC §6 — "miss lets the boss land a hit").</summary>
    CallResponse,

    /// <summary>A continuous note stream judged in real time. Landed notes damage the
    /// boss; missed notes cost Resolve (SPEC §6 — "landing notes damages the boss,
    /// missed notes open you to boss attacks").</summary>
    Highway,
}

/// <summary>
/// One stage of a Rock Off (SPEC §6) — its musical format and the chart that plays
/// it. Like <see cref="NoteChart"/>, this is fixed <b>content</b> (the boss's
/// authored phrases), not a feel-tunable (CLAUDE.md rule 1); the pacing/Resolve
/// numbers that judge a fight live in <see cref="BossTunables"/>.
/// </summary>
public sealed class BossPhase
{
    public BossPhase(string name, BossPhaseKind kind, NoteChart chart)
    {
        Name = name;
        Kind = kind;
        Chart = chart;
    }

    /// <summary>Display name of the phase, for the HUD (SPEC §7 — names draw on the world).</summary>
    public string Name { get; }

    /// <summary>Call & Response or Highway (SPEC §6).</summary>
    public BossPhaseKind Kind { get; }

    /// <summary>The phrase/stream this phase performs.</summary>
    public NoteChart Chart { get; }
}
