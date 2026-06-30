using System;

namespace WixxTheBard.Performance;

/// <summary>
/// The rhythm-window math for one performance (SPEC §5.2/§5.4) — pure and Godot-free,
/// so it is unit-tested directly. It judges each strum against the chart and times out
/// unhit notes, classifying everything into <see cref="Judgment"/>s.
///
/// <b>Timing is calibration-relative (CLAUDE.md rule 6).</b> Every judgement is made
/// against the player's stored A/V latency offset, never raw clock time: a strum's
/// timestamp is shifted by <c>-offset</c> before it is compared to a note's target, and
/// the auto-miss deadline is shifted the same way. The sign matches
/// <see cref="WixxTheBard.Controls.LatencyCalibration"/> (positive offset = the player
/// tends to play late, so their input is pulled earlier to compensate). A player who is
/// consistently 50&#160;ms late with a +50&#160;ms offset judges dead-on.
///
/// One strike hits at most one note — the nearest un-judged note in the strummed lane
/// whose error is within the Good window. A strike that finds none is a
/// <see cref="Judgment.Stray"/>; the note it missed stays open for its own auto-miss.
/// </summary>
public sealed class PerformanceJudge
{
    private readonly NoteChart _chart;
    private readonly PerformanceTunables _tunables;
    private readonly double _offsetMs;
    private readonly Judgment?[] _judgments;

    public PerformanceJudge(NoteChart chart, PerformanceTunables tunables, double offsetMs)
    {
        _chart = chart;
        _tunables = tunables;
        _offsetMs = offsetMs;
        _judgments = new Judgment?[chart.NoteCount];
    }

    public int PerfectCount { get; private set; }

    public int GoodCount { get; private set; }

    public int MissCount { get; private set; }

    public int StrayCount { get; private set; }

    /// <summary>Notes that have a verdict (hit or auto-missed).</summary>
    public int JudgedCount => PerfectCount + GoodCount + MissCount;

    /// <summary>True once every note has been hit or auto-missed.</summary>
    public bool AllJudged => JudgedCount >= _chart.NoteCount;

    /// <summary>The verdict on note <paramref name="index"/>, or null if still open. For the HUD (rule 7).</summary>
    public Judgment? JudgmentOf(int index) =>
        index >= 0 && index < _judgments.Length ? _judgments[index] : null;

    /// <summary>
    /// Auto-miss any note whose calibration-relative window has fully elapsed by
    /// <paramref name="chartMs"/>. Idempotent; call once per tick after any strum.
    /// </summary>
    public void Advance(double chartMs)
    {
        double effective = chartMs - _offsetMs;
        var notes = _chart.Notes;
        for (int i = 0; i < notes.Count; i++)
        {
            if (_judgments[i].HasValue)
            {
                continue;
            }

            if (effective > notes[i].TargetMs + _tunables.GoodWindowMs)
            {
                _judgments[i] = Judgment.Miss;
                MissCount++;
            }
        }
    }

    /// <summary>
    /// Judge a strum at <paramref name="chartMs"/> with <paramref name="lane"/> fretted.
    /// Returns the verdict: <see cref="Judgment.Perfect"/> / <see cref="Judgment.Good"/>
    /// for the nearest open note in that lane within the Good window, else
    /// <see cref="Judgment.Stray"/>.
    /// </summary>
    public Judgment Strum(double chartMs, NoteLane lane)
    {
        double effective = chartMs - _offsetMs;
        var notes = _chart.Notes;

        int best = -1;
        double bestError = double.MaxValue;
        for (int i = 0; i < notes.Count; i++)
        {
            if (_judgments[i].HasValue || notes[i].Lane != lane)
            {
                continue;
            }

            double error = Math.Abs(effective - notes[i].TargetMs);
            if (error <= _tunables.GoodWindowMs && error < bestError)
            {
                bestError = error;
                best = i;
            }
        }

        if (best < 0)
        {
            StrayCount++;
            return Judgment.Stray;
        }

        Judgment verdict = bestError <= _tunables.PerfectWindowMs ? Judgment.Perfect : Judgment.Good;
        _judgments[best] = verdict;
        if (verdict == Judgment.Perfect)
        {
            PerfectCount++;
        }
        else
        {
            GoodCount++;
        }

        return verdict;
    }

    /// <summary>The final tally (SPEC §5.4). Call once the performance has ended.</summary>
    public PerformanceResult Result() => new(
        _chart.NoteCount,
        PerfectCount,
        GoodCount,
        MissCount,
        StrayCount,
        _tunables.SuccessThreshold);
}
