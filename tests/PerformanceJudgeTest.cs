namespace WixxTheBard.Tests;

using System.Collections.Generic;
using GdUnit4;
using WixxTheBard.Performance;
using static GdUnit4.Assertions;

/// <summary>
/// Contract tests for <see cref="PerformanceJudge"/> — the calibration-relative
/// rhythm-window math (SPEC §5.2/§5.4, CLAUDE.md rule 6). The load-bearing
/// behaviours: Perfect/Good/Miss/Stray classification, nearest-note selection, the
/// auto-miss deadline, and — critically — that timing is judged against the stored
/// A/V offset, never raw clock time. Pure and Godot-free.
/// </summary>
[TestSuite]
public class PerformanceJudgeTest
{
    private static PerformanceTunables Tuning() => new(
        perfectWindowMs: 45.0,
        goodWindowMs: 95.0,
        successThreshold: 0.6,
        timeSlowFactor: 0.15f,
        spellCooldownTicks: 150,
        resultBannerTicks: 120);

    private static NoteChart OneNote(double ms, NoteLane lane) =>
        new("test", new[] { new Note(ms, lane) });

    private static NoteChart Chart(params Note[] notes) => new("test", new List<Note>(notes));

    [TestCase]
    public void StrumOnTargetIsPerfect()
    {
        var judge = new PerformanceJudge(OneNote(1000.0, NoteLane.Green), Tuning(), offsetMs: 0.0);
        bool perfect = judge.Strum(1000.0, NoteLane.Green) == Judgment.Perfect;

        AssertThat(perfect).IsTrue();
        AssertThat(judge.PerfectCount).IsEqual(1);
        AssertThat(judge.AllJudged).IsTrue();
    }

    [TestCase]
    public void StrumInsideGoodButOutsidePerfectIsGood()
    {
        var judge = new PerformanceJudge(OneNote(1000.0, NoteLane.Green), Tuning(), 0.0);
        bool good = judge.Strum(1070.0, NoteLane.Green) == Judgment.Good; // 70ms: > 45, <= 95

        AssertThat(good).IsTrue();
        AssertThat(judge.GoodCount).IsEqual(1);
    }

    [TestCase]
    public void StrumOutsideGoodWindowIsStrayAndLeavesNoteOpen()
    {
        var judge = new PerformanceJudge(OneNote(1000.0, NoteLane.Green), Tuning(), 0.0);
        bool stray = judge.Strum(1120.0, NoteLane.Green) == Judgment.Stray; // 120ms > 95

        AssertThat(stray).IsTrue();
        AssertThat(judge.StrayCount).IsEqual(1);
        AssertThat(judge.JudgedCount).IsEqual(0); // the note is still hittable
    }

    [TestCase]
    public void StrumWrongLaneIsStray()
    {
        var judge = new PerformanceJudge(OneNote(1000.0, NoteLane.Green), Tuning(), 0.0);
        bool stray = judge.Strum(1000.0, NoteLane.Red) == Judgment.Stray;

        AssertThat(stray).IsTrue();
        AssertThat(judge.JudgedCount).IsEqual(0);
    }

    [TestCase]
    public void LatePlayerCompensatedByPositiveOffset()
    {
        // Player consistently 60ms late; a +60 offset pulls their input back to dead-on (rule 6).
        var judge = new PerformanceJudge(OneNote(1000.0, NoteLane.Green), Tuning(), offsetMs: 60.0);
        bool perfect = judge.Strum(1060.0, NoteLane.Green) == Judgment.Perfect;

        AssertThat(perfect).IsTrue();
    }

    [TestCase]
    public void EarlyPlayerCompensatedByNegativeOffset()
    {
        var judge = new PerformanceJudge(OneNote(1000.0, NoteLane.Green), Tuning(), offsetMs: -60.0);
        bool perfect = judge.Strum(940.0, NoteLane.Green) == Judgment.Perfect;

        AssertThat(perfect).IsTrue();
    }

    [TestCase]
    public void NearestOpenNoteInLaneIsChosen()
    {
        // Two Green notes both within the Good window of a strum at 1050: pick the nearer.
        var judge = new PerformanceJudge(
            Chart(new Note(1000.0, NoteLane.Green), new Note(1080.0, NoteLane.Green)),
            Tuning(),
            0.0);

        bool perfect = judge.Strum(1050.0, NoteLane.Green) == Judgment.Perfect; // nearer note 1080, err 30

        AssertThat(perfect).IsTrue();
        AssertThat(judge.JudgmentOf(1).HasValue).IsTrue(); // the 1080 note was consumed
        AssertThat(judge.JudgmentOf(0).HasValue).IsFalse(); // the 1000 note left open
    }

    [TestCase]
    public void NoteAutoMissesOnceItsWindowFullyElapses()
    {
        var judge = new PerformanceJudge(OneNote(1000.0, NoteLane.Green), Tuning(), 0.0);

        judge.Advance(1095.0); // exactly at the edge — not yet missed
        AssertThat(judge.MissCount).IsEqual(0);

        judge.Advance(1096.0); // just past — auto-missed
        AssertThat(judge.MissCount).IsEqual(1);
        AssertThat(judge.AllJudged).IsTrue();
    }

    [TestCase]
    public void AutoMissDeadlineShiftsWithOffset()
    {
        // +100 offset shifts the player's window 100ms later in raw time, so the note
        // should not auto-miss until raw 1195 (effective 1095).
        var judge = new PerformanceJudge(OneNote(1000.0, NoteLane.Green), Tuning(), offsetMs: 100.0);

        judge.Advance(1190.0);
        AssertThat(judge.MissCount).IsEqual(0);

        judge.Advance(1196.0);
        AssertThat(judge.MissCount).IsEqual(1);
    }

    [TestCase]
    public void ResultSucceedsAtOrAboveThreshold()
    {
        var judge = new PerformanceJudge(
            Chart(
                new Note(100.0, NoteLane.Green),
                new Note(200.0, NoteLane.Red),
                new Note(300.0, NoteLane.Yellow),
                new Note(400.0, NoteLane.Blue),
                new Note(500.0, NoteLane.Orange)),
            Tuning(),
            0.0);

        judge.Strum(100.0, NoteLane.Green);
        judge.Strum(200.0, NoteLane.Red);
        judge.Strum(300.0, NoteLane.Yellow);
        judge.Advance(10_000.0); // time out the rest

        PerformanceResult r = judge.Result();
        AssertThat(r.Hits).IsEqual(3);
        AssertThat(r.Miss).IsEqual(2);
        AssertThat(r.Success).IsTrue(); // 3/5 = 0.6 >= threshold
    }

    [TestCase]
    public void ResultFailsBelowThreshold()
    {
        var judge = new PerformanceJudge(
            Chart(
                new Note(100.0, NoteLane.Green),
                new Note(200.0, NoteLane.Red),
                new Note(300.0, NoteLane.Yellow),
                new Note(400.0, NoteLane.Blue),
                new Note(500.0, NoteLane.Orange)),
            Tuning(),
            0.0);

        judge.Strum(100.0, NoteLane.Green);
        judge.Strum(200.0, NoteLane.Red);
        judge.Advance(10_000.0);

        PerformanceResult r = judge.Result();
        AssertThat(r.Success).IsFalse(); // 2/5 = 0.4
    }

    [TestCase]
    public void PerfectRunNeedsAllPerfectAndNoStray()
    {
        var chart = Chart(new Note(100.0, NoteLane.Green), new Note(200.0, NoteLane.Red));

        var clean = new PerformanceJudge(chart, Tuning(), 0.0);
        clean.Strum(100.0, NoteLane.Green);
        clean.Strum(200.0, NoteLane.Red);
        AssertThat(clean.Result().IsPerfect).IsTrue();

        var withStray = new PerformanceJudge(chart, Tuning(), 0.0);
        withStray.Strum(100.0, NoteLane.Green);
        withStray.Strum(150.0, NoteLane.Blue); // stray
        withStray.Strum(200.0, NoteLane.Red);
        AssertThat(withStray.Result().IsPerfect).IsFalse();
    }
}
