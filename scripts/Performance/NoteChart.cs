using System.Collections.Generic;

namespace WixxTheBard.Performance;

/// <summary>
/// A fixed note chart — the song for one ability (SPEC §5.2 "fixed charts: each
/// ability always uses the same song, so mastery = learning to play it"). This is
/// <b>content</b>, not a tunable: the note times are the melody, authored once. The
/// rhythm <i>windows</i>, success threshold, time-slow factor and cooldown that judge
/// a chart live in <c>Tunables</c> (CLAUDE.md rule 1); the notes themselves do not,
/// any more than level geometry does.
///
/// Notes are sorted by time on construction so the judge can rely on order.
/// </summary>
public sealed class NoteChart
{
    public NoteChart(string name, IReadOnlyList<Note> notes)
    {
        Name = name;
        var sorted = new List<Note>(notes);
        sorted.Sort(static (a, b) => a.TargetMs.CompareTo(b.TargetMs));
        Notes = sorted;
    }

    /// <summary>Display name of the ability/song (SPEC §7 — names may draw on the world).</summary>
    public string Name { get; }

    /// <summary>The notes, ascending by <see cref="Note.TargetMs"/>.</summary>
    public IReadOnlyList<Note> Notes { get; }

    /// <summary>Number of notes in the chart.</summary>
    public int NoteCount => Notes.Count;

    /// <summary>Time (ms) of the last note — the chart's playable length, 0 when empty.</summary>
    public double DurationMs => Notes.Count == 0 ? 0.0 : Notes[Notes.Count - 1].TargetMs;
}
