namespace WixxTheBard.Tests;

using GdUnit4;
using WixxTheBard.Boss;
using WixxTheBard.Performance;
using static GdUnit4.Assertions;

/// <summary>
/// Contract tests for <see cref="BossFight"/> — the Rock Off multi-phase duel (SPEC
/// §6). Drives it with a fixed ms-per-tick (matching <c>SpellPerformanceTest</c>) to
/// verify: a Call &amp; Response phase telegraphs before it judges, misses cost
/// Resolve in either phase kind, Highway hits accumulate toward the boss accuracy,
/// phases gap and advance in order, Resolve hitting zero ends the fight immediately,
/// and the final tally decides victory against the shared success threshold. Pure
/// and Godot-free.
/// </summary>
[TestSuite]
public class BossFightTest
{
    private const double MsPerTick = 50.0;

    private static PerformanceTunables PerfTuning() => new(
        perfectWindowMs: 45.0,
        goodWindowMs: 95.0,
        successThreshold: 0.6,
        timeSlowFactor: 0.15f,
        spellCooldownTicks: 150,
        resultBannerTicks: 120);

    private static BossTunables BossTuning(int resolveMax = 4, double telegraphPauseMs = 100.0, int phaseGapTicks = 3) =>
        new(resolveMax, telegraphPauseMs, phaseGapTicks);

    private static NoteChart OneNote(double ms, NoteLane lane) =>
        new("test", new[] { new Note(ms, lane) });

    private static BossPhase CallResponse(string name, double ms, NoteLane lane) =>
        new(name, BossPhaseKind.CallResponse, OneNote(ms, lane));

    private static BossPhase Highway(string name, double ms, NoteLane lane) =>
        new(name, BossPhaseKind.Highway, OneNote(ms, lane));

    /// <summary>Drives a Performing-stage phase to completion by hitting every note dead-on.</summary>
    private static BossFightTick PlayPerfectPhase(BossFight fight, NoteChart chart, NoteLane lane)
    {
        BossFightTick tick = default;
        double elapsed = 0.0;
        foreach (Note note in chart.Notes)
        {
            while (elapsed + MsPerTick < note.TargetMs)
            {
                tick = fight.Tick(MsPerTick, false, NoteLane.None);
                elapsed += MsPerTick;
            }

            tick = fight.Tick(MsPerTick, true, lane);
            elapsed += MsPerTick;
        }

        // Run out the auto-miss tail so the phase actually completes.
        for (int i = 0; i < 10 && fight.Stage == BossStage.Performing; i++)
        {
            tick = fight.Tick(MsPerTick, false, NoteLane.None);
        }

        return tick;
    }

    private static void AdvanceTicks(BossFight fight, int ticks)
    {
        for (int i = 0; i < ticks; i++)
        {
            fight.Tick(MsPerTick, false, NoteLane.None);
        }
    }

    [TestCase]
    public void BeginOnACallResponsePhaseStartsInTelegraph()
    {
        var fight = new BossFight();
        fight.Begin(new[] { CallResponse("intro", 100.0, NoteLane.Green) }, 0.0, BossTuning(), PerfTuning());

        AssertThat(fight.Stage == BossStage.Telegraph).IsTrue();
        AssertThat(fight.IsActive).IsTrue();
        AssertThat(fight.Resolve).IsEqual(4);
    }

    [TestCase]
    public void BeginOnAHighwayPhaseSkipsTheTelegraph()
    {
        var fight = new BossFight();
        fight.Begin(new[] { Highway("onslaught", 100.0, NoteLane.Green) }, 0.0, BossTuning(), PerfTuning());

        AssertThat(fight.Stage == BossStage.Performing).IsTrue();
    }

    [TestCase]
    public void TelegraphAdvancesIntoPerformingOnceItElapses()
    {
        var phase = CallResponse("intro", 100.0, NoteLane.Green);
        var fight = new BossFight();
        fight.Begin(new[] { phase }, 0.0, BossTuning(telegraphPauseMs: 50.0), PerfTuning());

        // Telegraph end = duration(100) + goodWindow(95) + pause(50) = 245ms.
        AdvanceTicks(fight, 4); // 200ms
        AssertThat(fight.Stage == BossStage.Telegraph).IsTrue();

        AdvanceTicks(fight, 1); // 250ms — past 245
        AssertThat(fight.Stage == BossStage.Performing).IsTrue();
    }

    [TestCase]
    public void TelegraphIgnoresStrumInput()
    {
        var phase = CallResponse("intro", 1000.0, NoteLane.Green);
        var fight = new BossFight();
        fight.Begin(new[] { phase }, 0.0, BossTuning(), PerfTuning());

        // A strum during the telegraph must not be judged — the boss owns this turn.
        fight.Tick(50.0, true, NoteLane.Green);
        AssertThat(fight.Resolve).IsEqual(4);
    }

    [TestCase]
    public void MissingAPhaseDuringEitherKindCostsResolve()
    {
        var fight = new BossFight();
        fight.Begin(new[] { Highway("onslaught", 100.0, NoteLane.Green) }, 0.0, BossTuning(resolveMax: 4), PerfTuning());

        // Never strum — the single note auto-misses.
        BossFightTick last = default;
        for (int i = 0; i < 20 && fight.IsActive; i++)
        {
            last = fight.Tick(MsPerTick, false, NoteLane.None);
        }

        AssertThat(last.FightCompleted).IsTrue();
        AssertThat(fight.Resolve).IsEqual(3); // one miss
    }

    [TestCase]
    public void HighwayHitsAccumulateTowardBossAccuracy()
    {
        var chart = Highway("onslaught", 100.0, NoteLane.Green).Chart;
        var fight = new BossFight();
        fight.Begin(new[] { Highway("onslaught", 100.0, NoteLane.Green) }, 0.0, BossTuning(), PerfTuning());

        PlayPerfectPhase(fight, chart, NoteLane.Green);

        AssertThat(fight.LastPhaseResult.Hits).IsEqual(1); // the hit actually landed...
        AssertThat(System.Math.Abs(fight.BossAccuracy - 1.0) < 1e-6).IsTrue(); // ...and counted toward accuracy
    }

    [TestCase]
    public void CallResponseHitsDoNotCountTowardBossAccuracy()
    {
        var chart = CallResponse("intro", 1000.0, NoteLane.Green).Chart;
        var fight = new BossFight();
        fight.Begin(new[] { CallResponse("intro", 1000.0, NoteLane.Green) }, 0.0, BossTuning(telegraphPauseMs: 0.0), PerfTuning());

        // Run past the telegraph (duration 1000 + good window 95 = 1095ms).
        AdvanceTicks(fight, 22); // 1100ms
        AssertThat(fight.Stage == BossStage.Performing).IsTrue();

        BossFightTick last = PlayPerfectPhase(fight, chart, NoteLane.Green);

        AssertThat(fight.LastPhaseResult.Hits).IsEqual(1); // the echo landed...
        AssertThat(last.Victory).IsTrue(); // ...so the (only) phase still resolves to victory...
        // ...but via the trivial "nothing judged yet" accuracy, not a counted call-response hit.
        AssertThat(System.Math.Abs(fight.BossAccuracy - 1.0) < 1e-6).IsTrue();
    }

    [TestCase]
    public void PhaseGapSeparatesTwoPhasesThenAdvances()
    {
        var phases = new[]
        {
            Highway("first", 100.0, NoteLane.Green),
            Highway("second", 100.0, NoteLane.Red),
        };
        var fight = new BossFight();
        fight.Begin(phases, 0.0, BossTuning(phaseGapTicks: 3), PerfTuning());

        PlayPerfectPhase(fight, phases[0].Chart, NoteLane.Green);
        AssertThat(fight.Stage == BossStage.PhaseGap).IsTrue();
        AssertThat(fight.PhaseNumber).IsEqual(1);

        AdvanceTicks(fight, 2);
        AssertThat(fight.Stage == BossStage.PhaseGap).IsTrue(); // not yet — 2 of 3 gap ticks

        AdvanceTicks(fight, 1);
        AssertThat(fight.Stage == BossStage.Performing).IsTrue();
        AssertThat(fight.PhaseNumber).IsEqual(2);
    }

    [TestCase]
    public void ResolveHittingZeroEndsTheFightImmediatelyInDefeat()
    {
        var phases = new[]
        {
            Highway("first", 100.0, NoteLane.Green),
            Highway("second", 100.0, NoteLane.Red),
            Highway("third", 100.0, NoteLane.Yellow),
        };
        var fight = new BossFight();
        fight.Begin(phases, 0.0, BossTuning(resolveMax: 1, phaseGapTicks: 1), PerfTuning());

        // Miss the very first phase — Resolve (1) hits zero before the sequence ends.
        BossFightTick last = default;
        for (int i = 0; i < 20 && fight.IsActive; i++)
        {
            last = fight.Tick(MsPerTick, false, NoteLane.None);
        }

        AssertThat(last.FightCompleted).IsTrue();
        AssertThat(last.Victory).IsFalse();
        AssertThat(fight.Stage == BossStage.Defeat).IsTrue();
        AssertThat(fight.PhaseNumber).IsEqual(1); // never reached the later phases
    }

    [TestCase]
    public void ClearingEveryHighwayNoteEndsInVictory()
    {
        var phases = new[]
        {
            CallResponse("intro", 200.0, NoteLane.Green),
            Highway("flurry", 200.0, NoteLane.Red),
        };
        var fight = new BossFight();
        fight.Begin(phases, 0.0, BossTuning(telegraphPauseMs: 0.0, phaseGapTicks: 1), PerfTuning());

        AdvanceTicks(fight, 6); // clear the telegraph (200 + 95 = 295ms)
        PlayPerfectPhase(fight, phases[0].Chart, NoteLane.Green); // perfect call-response echo

        AdvanceTicks(fight, 1); // phase gap
        BossFightTick last = PlayPerfectPhase(fight, phases[1].Chart, NoteLane.Red);

        AssertThat(last.FightCompleted).IsTrue();
        AssertThat(last.Victory).IsTrue();
        AssertThat(fight.Stage == BossStage.Victory).IsTrue();
        AssertThat(fight.Resolve > 0).IsTrue();
    }

    [TestCase]
    public void MissingAllHighwayNotesEndsInDefeatEvenWithResolveLeft()
    {
        // Resolve pool large enough to survive every miss, but the final accuracy
        // (0/1) falls short of the 0.6 success threshold.
        var phases = new[] { Highway("flurry", 100.0, NoteLane.Green) };
        var fight = new BossFight();
        fight.Begin(phases, 0.0, BossTuning(resolveMax: 10), PerfTuning());

        BossFightTick last = default;
        for (int i = 0; i < 20 && fight.IsActive; i++)
        {
            last = fight.Tick(MsPerTick, false, NoteLane.None);
        }

        AssertThat(last.FightCompleted).IsTrue();
        AssertThat(last.Victory).IsFalse();
        AssertThat(fight.Resolve > 0).IsTrue(); // didn't run out — failed on accuracy instead
    }

    [TestCase]
    public void ResetReturnsToIdleAndClearsState()
    {
        var fight = new BossFight();
        fight.Begin(new[] { Highway("onslaught", 100.0, NoteLane.Green) }, 0.0, BossTuning(), PerfTuning());

        fight.Reset();

        AssertThat(fight.Stage == BossStage.Idle).IsTrue();
        AssertThat(fight.IsActive).IsFalse();
        AssertThat(fight.Resolve).IsEqual(0);
    }
}
