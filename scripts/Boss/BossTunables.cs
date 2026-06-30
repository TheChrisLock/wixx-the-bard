namespace WixxTheBard.Boss;

/// <summary>
/// Plain-C# carrier for the M5 Rock Off pacing/risk constants (SPEC §6), mirroring
/// the other <c>*Tunables</c> carriers: the running game builds it from the one
/// <c>Tunables</c> resource at the Godot boundary (CLAUDE.md rule 1); unit tests
/// construct it directly so <see cref="BossFight"/> stays Godot-free.
///
/// The rhythm windows, calibration offset and success threshold a Rock Off judges
/// against are <b>not</b> duplicated here — every phase is performed through the
/// same <c>WixxTheBard.Performance.PerformanceTunables</c> a spell uses, so a player's
/// calibration and the meaning of "Perfect"/"Good"/success are consistent everywhere
/// the game judges a strum (rule 6).
/// </summary>
public sealed class BossTunables
{
    public BossTunables(int resolveMax, double telegraphPauseMs, int phaseGapTicks)
    {
        ResolveMax = resolveMax;
        TelegraphPauseMs = telegraphPauseMs;
        PhaseGapTicks = phaseGapTicks;
    }

    /// <summary>Missed notes (any phase) Wixx can take before a Rock Off ends in defeat.</summary>
    public int ResolveMax { get; }

    /// <summary>Silent pause (ms) after a Call & Response telegraph finishes scrolling,
    /// before the player's echo of the same phrase begins.</summary>
    public double TelegraphPauseMs { get; }

    /// <summary>Fixed ticks of breathing room between phases (CLAUDE.md rule 3).</summary>
    public int PhaseGapTicks { get; }
}
