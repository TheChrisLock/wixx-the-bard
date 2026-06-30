using System.Collections.Generic;
using WixxTheBard.Performance;

namespace WixxTheBard.Boss;

/// <summary>
/// The vertical slice's one Rock Off (SPEC §12 — "one Rock Off boss"): <see cref="Choirbreaker"/>,
/// a Silent Empire enforcer (SPEC §7) who silenced this region's song. Five phases,
/// escalating exactly as SPEC §6 describes: "learn a phrase in call-and-response,
/// then survive a highway onslaught, then a harder phrase, then a faster highway,
/// into a final flurry." Each phase's notes are fixed content — the boss's musical
/// identity (SPEC §6) — not feel-tunables (CLAUDE.md rule 1); only the chart
/// <i>density/tempo</i> carries the escalation, so every phase still judges through
/// the same windows and threshold as everywhere else in the game (rule 6).
/// </summary>
public static class BossCharts
{
    /// <summary>Display name of the vertical slice's boss, for the HUD/banner.</summary>
    public const string ChoirbreakerName = "The Choirbreaker";

    /// <summary>The five phases of the Choirbreaker Rock Off, in performance order.</summary>
    public static IReadOnlyList<BossPhase> Choirbreaker() => new[]
    {
        new BossPhase("The Challenge", BossPhaseKind.CallResponse, new NoteChart("The Challenge", new[]
        {
            new Note(600.0, NoteLane.Green),
            new Note(1200.0, NoteLane.Red),
            new Note(1800.0, NoteLane.Yellow),
            new Note(2400.0, NoteLane.Green),
        })),

        new BossPhase("Forced March", BossPhaseKind.Highway, new NoteChart("Forced March", new[]
        {
            new Note(400.0, NoteLane.Green),
            new Note(900.0, NoteLane.Yellow),
            new Note(1400.0, NoteLane.Red),
            new Note(1900.0, NoteLane.Yellow),
            new Note(2400.0, NoteLane.Blue),
            new Note(2900.0, NoteLane.Green),
            new Note(3400.0, NoteLane.Red),
            new Note(3900.0, NoteLane.Yellow),
        })),

        new BossPhase("The Reprise", BossPhaseKind.CallResponse, new NoteChart("The Reprise", new[]
        {
            new Note(350.0, NoteLane.Green),
            new Note(700.0, NoteLane.Yellow),
            new Note(1050.0, NoteLane.Blue),
            new Note(1400.0, NoteLane.Red),
            new Note(1750.0, NoteLane.Orange),
            new Note(2100.0, NoteLane.Yellow),
        })),

        new BossPhase("Drumhead", BossPhaseKind.Highway, new NoteChart("Drumhead", new[]
        {
            new Note(280.0, NoteLane.Green),
            new Note(560.0, NoteLane.Red),
            new Note(840.0, NoteLane.Yellow),
            new Note(1120.0, NoteLane.Blue),
            new Note(1400.0, NoteLane.Orange),
            new Note(1680.0, NoteLane.Green),
            new Note(1960.0, NoteLane.Red),
            new Note(2240.0, NoteLane.Yellow),
            new Note(2520.0, NoteLane.Blue),
            new Note(2800.0, NoteLane.Orange),
            new Note(3080.0, NoteLane.Green),
            new Note(3360.0, NoteLane.Red),
        })),

        new BossPhase("Last Stand", BossPhaseKind.Highway, new NoteChart("Last Stand", new[]
        {
            new Note(220.0, NoteLane.Green),
            new Note(440.0, NoteLane.Yellow),
            new Note(660.0, NoteLane.Red),
            new Note(880.0, NoteLane.Blue),
            new Note(1100.0, NoteLane.Orange),
            new Note(1320.0, NoteLane.Green),
            new Note(1540.0, NoteLane.Yellow),
            new Note(1760.0, NoteLane.Red),
            new Note(1980.0, NoteLane.Blue),
            new Note(2200.0, NoteLane.Orange),
            new Note(2420.0, NoteLane.Green),
            new Note(2640.0, NoteLane.Yellow),
            new Note(2860.0, NoteLane.Red),
            new Note(3080.0, NoteLane.Blue),
            new Note(3300.0, NoteLane.Orange),
            new Note(3520.0, NoteLane.Green),
        })),
    };
}
