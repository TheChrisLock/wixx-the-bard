namespace WixxTheBard.Tests;

using System;
using GdUnit4;
using WixxTheBard.Performance;
using static GdUnit4.Assertions;

/// <summary>
/// Tests for <see cref="NoteChart"/> and the M4 <see cref="SpellCharts.Kindle"/>
/// content: notes sort by time, duration tracks the last note, and the shipped chart
/// is a well-formed tier-1 single-note song (SPEC §5.2/§5.3). Pure and Godot-free.
/// </summary>
[TestSuite]
public class NoteChartTest
{
    [TestCase]
    public void NotesAreSortedByTime()
    {
        var chart = new NoteChart("c", new[]
        {
            new Note(300.0, NoteLane.Green),
            new Note(100.0, NoteLane.Red),
            new Note(200.0, NoteLane.Yellow),
        });

        AssertThat(System.Math.Abs(chart.Notes[0].TargetMs - 100.0) < 1e-6).IsTrue();
        AssertThat(System.Math.Abs(chart.Notes[1].TargetMs - 200.0) < 1e-6).IsTrue();
        AssertThat(System.Math.Abs(chart.Notes[2].TargetMs - 300.0) < 1e-6).IsTrue();
    }

    [TestCase]
    public void DurationIsTheLastNoteTime()
    {
        var chart = new NoteChart("c", new[]
        {
            new Note(100.0, NoteLane.Green),
            new Note(450.0, NoteLane.Blue),
        });

        AssertThat(System.Math.Abs(chart.DurationMs - 450.0) < 1e-6).IsTrue();
    }

    [TestCase]
    public void EmptyChartHasZeroDurationAndCount()
    {
        var chart = new NoteChart("empty", Array.Empty<Note>());

        AssertThat(chart.NoteCount).IsEqual(0);
        AssertThat(System.Math.Abs(chart.DurationMs - 0.0) < 1e-6).IsTrue();
    }

    [TestCase]
    public void KindleIsAWellFormedTierOneChart()
    {
        NoteChart kindle = SpellCharts.Kindle();

        AssertThat(kindle.Name).IsEqual("Kindle");
        AssertThat(kindle.NoteCount > 0).IsTrue();

        // Sorted, every note on a real lane, ~3s long (SPEC §5.2/§5.3).
        double prev = -1.0;
        foreach (Note n in kindle.Notes)
        {
            AssertThat(n.TargetMs >= prev).IsTrue();
            AssertThat(n.Lane != NoteLane.None).IsTrue();
            prev = n.TargetMs;
        }

        AssertThat(kindle.DurationMs > 0.0 && kindle.DurationMs <= 4000.0).IsTrue();
    }
}
