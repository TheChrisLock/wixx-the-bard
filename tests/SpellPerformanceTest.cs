namespace WixxTheBard.Tests;

using GdUnit4;
using WixxTheBard.Performance;
using static GdUnit4.Assertions;

/// <summary>
/// Contract tests for <see cref="SpellPerformance"/> — the trigger→play→resolve state
/// machine (SPEC §5.2/§5.4). Drives it with a fixed ms-per-tick to verify the clock
/// advances, strums are judged, the performance completes a window after the last
/// note, the success/fail result is correct, and the stored latency offset threads
/// through (rule 6). Pure and Godot-free.
/// </summary>
[TestSuite]
public class SpellPerformanceTest
{
    private const double MsPerTick = 50.0; // arbitrary fixed step for deterministic tests

    private static PerformanceTunables Tuning() => new(
        perfectWindowMs: 45.0,
        goodWindowMs: 95.0,
        successThreshold: 0.6,
        timeSlowFactor: 0.15f,
        spellCooldownTicks: 150,
        resultBannerTicks: 120);

    private static NoteChart OneNote(double ms, NoteLane lane) =>
        new("test", new[] { new Note(ms, lane) });

    [TestCase]
    public void BeginActivatesAndStartsTheClockAtZero()
    {
        var perf = new SpellPerformance();
        perf.Begin(OneNote(100.0, NoteLane.Green), 0.0, Tuning());

        AssertThat(perf.IsActive).IsTrue();
        AssertThat(System.Math.Abs(perf.ChartMs - 0.0) < 1e-6).IsTrue();
    }

    [TestCase]
    public void TickAdvancesTheSongClock()
    {
        var perf = new SpellPerformance();
        perf.Begin(OneNote(100.0, NoteLane.Green), 0.0, Tuning());

        perf.Tick(MsPerTick, false, NoteLane.None);
        AssertThat(System.Math.Abs(perf.ChartMs - 50.0) < 1e-6).IsTrue();
    }

    [TestCase]
    public void StrumOnTheNoteTickJudgesIt()
    {
        var perf = new SpellPerformance();
        perf.Begin(OneNote(100.0, NoteLane.Green), 0.0, Tuning());

        perf.Tick(MsPerTick, false, NoteLane.None);          // 50ms
        PerformanceTick t = perf.Tick(MsPerTick, true, NoteLane.Green); // 100ms, strum

        AssertThat(t.StrumJudgment.HasValue).IsTrue();
        AssertThat(t.StrumJudgment!.Value == Judgment.Perfect).IsTrue();
    }

    [TestCase]
    public void CompletesAWindowAfterTheLastNoteWithAResult()
    {
        var perf = new SpellPerformance();
        perf.Begin(OneNote(100.0, NoteLane.Green), 0.0, Tuning());

        // Hit the note, then run until completion (endMs = 100 + 95 + 0 = 195).
        perf.Tick(MsPerTick, false, NoteLane.None); // 50
        perf.Tick(MsPerTick, true, NoteLane.Green); // 100 — hit

        PerformanceTick last = default;
        for (int i = 0; i < 10 && perf.IsActive; i++)
        {
            last = perf.Tick(MsPerTick, false, NoteLane.None);
        }

        AssertThat(last.Completed).IsTrue();
        AssertThat(perf.IsActive).IsFalse();
        AssertThat(last.Result.Success).IsTrue();
        AssertThat(last.Result.Hits).IsEqual(1);
    }

    [TestCase]
    public void NeverStrummingFailsWithAMiss()
    {
        var perf = new SpellPerformance();
        perf.Begin(OneNote(100.0, NoteLane.Green), 0.0, Tuning());

        PerformanceTick last = default;
        for (int i = 0; i < 20 && perf.IsActive; i++)
        {
            last = perf.Tick(MsPerTick, false, NoteLane.None);
        }

        AssertThat(last.Completed).IsTrue();
        AssertThat(last.Result.Success).IsFalse();
        AssertThat(last.Result.Miss).IsEqual(1);
    }

    [TestCase]
    public void StoredOffsetThreadsThroughToJudgement()
    {
        // +50 offset: a player strumming 50ms "late" (raw 150 for a 100ms note) is dead-on.
        var perf = new SpellPerformance();
        perf.Begin(OneNote(100.0, NoteLane.Green), offsetMs: 50.0, Tuning());

        perf.Tick(MsPerTick, false, NoteLane.None);          // 50
        perf.Tick(MsPerTick, false, NoteLane.None);          // 100
        PerformanceTick t = perf.Tick(MsPerTick, true, NoteLane.Green); // 150 raw → effective 100

        AssertThat(t.StrumJudgment!.Value == Judgment.Perfect).IsTrue();
    }

    [TestCase]
    public void TickAfterCompletionIsInert()
    {
        var perf = new SpellPerformance();
        perf.Begin(OneNote(100.0, NoteLane.Green), 0.0, Tuning());

        while (perf.IsActive)
        {
            perf.Tick(MsPerTick, false, NoteLane.None);
        }

        PerformanceTick after = perf.Tick(MsPerTick, true, NoteLane.Green);
        AssertThat(after.Completed).IsFalse();
        AssertThat(after.StrumJudgment.HasValue).IsFalse();
    }
}
