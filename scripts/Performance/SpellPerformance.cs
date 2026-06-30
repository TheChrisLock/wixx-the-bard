using System;

namespace WixxTheBard.Performance;

/// <summary>
/// The spell-performance state machine (SPEC §5.2) as pure, Godot-free logic the
/// <c>Player</c> drives — so the whole trigger→play→resolve loop is unit-tested.
///
/// The note clock advances in <b>real wall time</b> (the caller feeds it true ms per
/// fixed tick), so the song plays at tempo and input stays sampled at the fixed 60 Hz
/// step (CLAUDE.md rule 3). The global time-slow (SPEC §5.2) is applied to the
/// <i>world</i> by the caller via <see cref="PerformanceTunables.TimeSlowFactor"/> —
/// deliberately <b>not</b> by scaling engine time, which would starve the rhythm input.
///
/// Timing judgement is calibration-relative (rule 6): the player's A/V offset is taken
/// in <see cref="Begin"/> and threaded into the <see cref="PerformanceJudge"/>.
/// </summary>
public sealed class SpellPerformance
{
    private PerformanceTunables _tunables = null!;
    private PerformanceJudge _judge = null!;
    private double _endMs;
    private double _perfectWindowMs;
    private double _goodWindowMs;

    /// <summary>True while a performance is running (the strum is in "play" mode — rule 5).</summary>
    public bool IsActive { get; private set; }

    /// <summary>Elapsed song time in ms (drives the right→left note scroll — rule 7).</summary>
    public double ChartMs { get; private set; }

    /// <summary>The chart being performed, or null when idle. For the HUD (rule 7).</summary>
    public NoteChart? Chart { get; private set; }

    /// <summary>The live judge, or null when idle. For the HUD to read per-note verdicts (rule 7).</summary>
    public PerformanceJudge? Judge => IsActive || ChartMs > 0 ? _judge : null;

    /// <summary>Verdict of the most recent strum (for a HUD flash), or null.</summary>
    public Judgment? LastStrum { get; private set; }

    /// <summary>±ms Perfect window — exposed so the HUD can size the strike zone.</summary>
    public double PerfectWindowMs => _perfectWindowMs;

    /// <summary>±ms Good window — exposed so the HUD can size the strike zone.</summary>
    public double GoodWindowMs => _goodWindowMs;

    /// <summary>
    /// Begin a performance of <paramref name="chart"/>, judging against the player's
    /// stored latency <paramref name="offsetMs"/> (rule 6). The performance ends a Good
    /// window after the last note — plus the offset's magnitude, so even a heavily
    /// shifted calibration still lets every note reach its auto-miss deadline.
    /// </summary>
    public void Begin(NoteChart chart, double offsetMs, PerformanceTunables tunables)
    {
        _tunables = tunables;
        _judge = new PerformanceJudge(chart, tunables, offsetMs);
        Chart = chart;
        ChartMs = 0.0;
        LastStrum = null;
        _perfectWindowMs = tunables.PerfectWindowMs;
        _goodWindowMs = tunables.GoodWindowMs;
        _endMs = chart.DurationMs + tunables.GoodWindowMs + Math.Abs(offsetMs);
        IsActive = true;
    }

    /// <summary>
    /// Advance one fixed tick by <paramref name="msThisTick"/> of song time. If
    /// <paramref name="strumEdge"/> fired this tick, the strum is judged against the
    /// currently-fretted <paramref name="heldLane"/>. Auto-misses elapsed notes, then
    /// reports whether the performance completed and, if so, its result (SPEC §5.4).
    /// </summary>
    public PerformanceTick Tick(double msThisTick, bool strumEdge, NoteLane heldLane)
    {
        if (!IsActive)
        {
            return new PerformanceTick(null, false, default);
        }

        ChartMs += msThisTick;

        Judgment? strumVerdict = null;
        if (strumEdge)
        {
            strumVerdict = _judge.Strum(ChartMs, heldLane);
            LastStrum = strumVerdict;
        }

        _judge.Advance(ChartMs);

        bool completed = ChartMs >= _endMs;
        if (!completed)
        {
            return new PerformanceTick(strumVerdict, false, default);
        }

        // Defensive: anything still open at the deadline is a miss (keeps the tally whole).
        _judge.Advance(_endMs + _tunables.GoodWindowMs + 1.0);
        IsActive = false;
        return new PerformanceTick(strumVerdict, true, _judge.Result());
    }

    /// <summary>Abort/clear (respawn, scene reset).</summary>
    public void Reset()
    {
        IsActive = false;
        ChartMs = 0.0;
        Chart = null;
        LastStrum = null;
    }
}

/// <summary>Per-tick output of <see cref="SpellPerformance.Tick"/>.</summary>
public readonly struct PerformanceTick
{
    public PerformanceTick(Judgment? strumJudgment, bool completed, PerformanceResult result)
    {
        StrumJudgment = strumJudgment;
        Completed = completed;
        Result = result;
    }

    /// <summary>The verdict of this tick's strum, or null if there was no strum.</summary>
    public Judgment? StrumJudgment { get; }

    /// <summary>True on the tick the performance ends; <see cref="Result"/> is then valid.</summary>
    public bool Completed { get; }

    /// <summary>The final tally — valid only when <see cref="Completed"/> is true (SPEC §5.4).</summary>
    public PerformanceResult Result { get; }
}
