using System.Collections.Generic;
using WixxTheBard.Performance;

namespace WixxTheBard.Boss;

/// <summary>What stage of a Rock Off (SPEC §6) <see cref="BossFight"/> is in.</summary>
public enum BossStage
{
    /// <summary>No fight running.</summary>
    Idle,

    /// <summary>A Call & Response phase's boss-plays-first preview — the chart scrolls
    /// but nothing is judged (SPEC §6 "boss plays a phrase").</summary>
    Telegraph,

    /// <summary>The player is performing the current phase's chart — judged like a
    /// spell (rule 5: the strum's sole consumer right now).</summary>
    Performing,

    /// <summary>A short breather between two phases.</summary>
    PhaseGap,

    /// <summary>The fight is over — the boss's stolen song returns (SPEC §6/§7).</summary>
    Victory,

    /// <summary>The fight is over — Resolve ran out, or the final tally fell short.</summary>
    Defeat,
}

/// <summary>
/// The Rock Off boss duel (SPEC §6) as pure, Godot-free logic — the <c>Player</c>
/// drives it exactly the way it drives <see cref="SpellPerformance"/>, so the whole
/// multi-phase encounter is unit-tested. It is a sequence of <see cref="BossPhase"/>s,
/// each performed through its own <see cref="SpellPerformance"/> run (reusing the
/// rhythm-judging math rather than re-deriving it):
///
/// <list type="bullet">
/// <item><b>Call &amp; Response</b> — the boss telegraphs the phrase first (a
/// non-judged preview), then the player echoes the same chart. Misses cost
/// <see cref="Resolve"/>; the boss is never damaged here (SPEC §6).</item>
/// <item><b>Highway</b> — judged immediately, no telegraph. Landed notes accumulate
/// toward <see cref="BossAccuracy"/>; misses also cost Resolve (SPEC §6 — "missed
/// notes open you to boss attacks").</item>
/// </list>
///
/// <see cref="Resolve"/> hitting zero ends the fight in immediate <see cref="BossStage.Defeat"/>,
/// mid-phase-sequence — a Rock Off is real risk, not just a final tally (mirrors the
/// tar hazard's "unforgiving on purpose", SPEC §4.4). Reaching the end of the phase
/// list with Resolve still standing resolves to <see cref="BossStage.Victory"/> only if
/// the cumulative Highway accuracy clears <c>PerformanceTunables.SuccessThreshold</c> —
/// the same success bar a spell performance uses (rule 6: one consistent meaning of
/// "you played well enough" everywhere a chart is judged), so no new threshold number
/// is needed (rule 1).
/// </summary>
public sealed class BossFight
{
    private readonly SpellPerformance _performance = new();

    private IReadOnlyList<BossPhase> _phases = System.Array.Empty<BossPhase>();
    private BossTunables _bossTunables = null!;
    private PerformanceTunables _perfTunables = null!;
    private double _offsetMs;

    private int _phaseIndex;
    private double _telegraphMs;
    private double _telegraphEndMs;
    private int _gapTicksRemaining;

    private int _resolve;
    private int _highwayHits;
    private int _highwayNotesTotal;

    /// <summary>The current stage of the fight.</summary>
    public BossStage Stage { get; private set; } = BossStage.Idle;

    /// <summary>True while a fight is running and owns the strum (rule 5).</summary>
    public bool IsActive => Stage is BossStage.Telegraph or BossStage.Performing or BossStage.PhaseGap;

    /// <summary>The phase currently in play (or last played), for the HUD.</summary>
    public BossPhase? CurrentPhase => _phaseIndex < _phases.Count ? _phases[_phaseIndex] : null;

    /// <summary>1-based index of <see cref="CurrentPhase"/> among <see cref="PhaseCount"/>, for the HUD.</summary>
    public int PhaseNumber => _phaseIndex + 1;

    /// <summary>Total phases in this fight.</summary>
    public int PhaseCount => _phases.Count;

    /// <summary>Missed notes (any phase) Wixx can still take before defeat.</summary>
    public int Resolve => _resolve;

    /// <summary>The Resolve pool's starting size, for the HUD meter.</summary>
    public int ResolveMax => _bossTunables.ResolveMax;

    /// <summary>Cumulative Highway-note hit ratio so far, in [0,1] (1 with no Highway notes judged yet).</summary>
    public double BossAccuracy => _highwayNotesTotal <= 0 ? 1.0 : _highwayHits / (double)_highwayNotesTotal;

    /// <summary>The live judged performance while <see cref="Stage"/> is <see cref="BossStage.Performing"/>; the HUD reads it to draw the highway.</summary>
    public SpellPerformance? Performance => Stage == BossStage.Performing ? _performance : null;

    /// <summary>The chart being telegraphed while <see cref="Stage"/> is <see cref="BossStage.Telegraph"/>; the HUD reads it for the boss's-turn preview.</summary>
    public NoteChart? TelegraphChart => Stage == BossStage.Telegraph ? CurrentPhase?.Chart : null;

    /// <summary>Elapsed ms into the current telegraph; the HUD reads it to scroll the preview.</summary>
    public double TelegraphMs => _telegraphMs;

    /// <summary>The most recently completed phase's tally, for the HUD/banner.</summary>
    public PerformanceResult LastPhaseResult { get; private set; }

    /// <summary>
    /// Begin a Rock Off through <paramref name="phases"/> in order, judged against the
    /// player's stored A/V offset (rule 6 — same as a spell performance).
    /// </summary>
    public void Begin(IReadOnlyList<BossPhase> phases, double offsetMs, BossTunables bossTunables, PerformanceTunables perfTunables)
    {
        _phases = phases;
        _offsetMs = offsetMs;
        _bossTunables = bossTunables;
        _perfTunables = perfTunables;

        _phaseIndex = 0;
        _resolve = bossTunables.ResolveMax;
        _highwayHits = 0;
        _highwayNotesTotal = 0;
        LastPhaseResult = default;

        BeginCurrentPhase();
    }

    /// <summary>
    /// Advance one fixed tick. <paramref name="msThisTick"/> drives the telegraph and
    /// echo clocks at real time (rule 3); <paramref name="strumEdge"/>/
    /// <paramref name="heldLane"/> are only consumed while <see cref="Stage"/> is
    /// <see cref="BossStage.Performing"/> — a telegraph never judges input, mirroring
    /// strum-mode discipline (rule 5: exactly one consumer of "this input means a hit"
    /// at a time, even within the fight itself).
    /// </summary>
    public BossFightTick Tick(double msThisTick, bool strumEdge, NoteLane heldLane)
    {
        switch (Stage)
        {
            case BossStage.Telegraph:
                _telegraphMs += msThisTick;
                if (_telegraphMs >= _telegraphEndMs)
                {
                    Stage = BossStage.Performing;
                    _performance.Begin(CurrentPhase!.Chart, _offsetMs, _perfTunables);
                }

                return BossFightTick.None;

            case BossStage.Performing:
                PerformanceTick perfTick = _performance.Tick(msThisTick, strumEdge, heldLane);
                return perfTick.Completed ? ResolvePhase(perfTick.Result) : BossFightTick.None;

            case BossStage.PhaseGap:
                // One Tick() call == one fixed physics step (rule 3), so a plain tick
                // count is exact regardless of the ms argument.
                if (_gapTicksRemaining > 0)
                {
                    _gapTicksRemaining--;
                }

                if (_gapTicksRemaining <= 0)
                {
                    _phaseIndex++;
                    BeginCurrentPhase();
                }

                return BossFightTick.None;

            default:
                return BossFightTick.None;
        }
    }

    /// <summary>Abort/clear (respawn, scene reset).</summary>
    public void Reset()
    {
        Stage = BossStage.Idle;
        _phaseIndex = 0;
        _resolve = 0;
        _highwayHits = 0;
        _highwayNotesTotal = 0;
        _performance.Reset();
    }

    private void BeginCurrentPhase()
    {
        BossPhase phase = _phases[_phaseIndex];
        if (phase.Kind == BossPhaseKind.CallResponse)
        {
            Stage = BossStage.Telegraph;
            _telegraphMs = 0.0;
            _telegraphEndMs = phase.Chart.DurationMs + _perfTunables.GoodWindowMs + _bossTunables.TelegraphPauseMs;
        }
        else
        {
            Stage = BossStage.Performing;
            _performance.Begin(phase.Chart, _offsetMs, _perfTunables);
        }
    }

    /// <summary>
    /// Apply one completed phase's tally (SPEC §6): every miss costs Resolve
    /// regardless of phase kind, Highway hits accumulate toward
    /// <see cref="BossAccuracy"/>, then either Resolve has run out (immediate
    /// defeat), this was the last phase (final tally decides victory), or the fight
    /// moves on to a breather before the next phase.
    /// </summary>
    private BossFightTick ResolvePhase(PerformanceResult result)
    {
        LastPhaseResult = result;
        _resolve = System.Math.Max(0, _resolve - result.Miss);

        if (_phases[_phaseIndex].Kind == BossPhaseKind.Highway)
        {
            _highwayHits += result.Hits;
            _highwayNotesTotal += result.Total;
        }

        if (_resolve <= 0)
        {
            Stage = BossStage.Defeat;
            return new BossFightTick(phaseCompleted: true, fightCompleted: true, victory: false, result);
        }

        bool wasLastPhase = _phaseIndex + 1 >= _phases.Count;
        if (wasLastPhase)
        {
            bool victory = BossAccuracy >= _perfTunables.SuccessThreshold;
            Stage = victory ? BossStage.Victory : BossStage.Defeat;
            return new BossFightTick(phaseCompleted: true, fightCompleted: true, victory, result);
        }

        Stage = BossStage.PhaseGap;
        _gapTicksRemaining = _bossTunables.PhaseGapTicks;
        return new BossFightTick(phaseCompleted: true, fightCompleted: false, victory: false, result);
    }
}

/// <summary>Per-tick output of <see cref="BossFight.Tick"/>.</summary>
public readonly struct BossFightTick
{
    public static readonly BossFightTick None = default;

    public BossFightTick(bool phaseCompleted, bool fightCompleted, bool victory, PerformanceResult lastResult)
    {
        PhaseCompleted = phaseCompleted;
        FightCompleted = fightCompleted;
        Victory = victory;
        LastResult = lastResult;
    }

    /// <summary>True on the tick a phase (telegraph+echo, or a highway run) finishes.</summary>
    public bool PhaseCompleted { get; }

    /// <summary>True on the tick the whole fight ends; <see cref="Victory"/> is then valid.</summary>
    public bool FightCompleted { get; }

    /// <summary>Valid only when <see cref="FightCompleted"/> is true (SPEC §6).</summary>
    public bool Victory { get; }

    /// <summary>The phase tally that just resolved (valid when <see cref="PhaseCompleted"/>).</summary>
    public PerformanceResult LastResult { get; }
}
