namespace WixxTheBard.Tests;

using GdUnit4;
using WixxTheBard.Verbs;
using static GdUnit4.Assertions;

/// <summary>
/// Contract tests for <see cref="TarState"/> — the tar / quicksand struggle (SPEC
/// §4.4). The validated feel rules encoded here: a constant sink, <b>clean
/// alternation</b> climbs (single-direction mashing barely helps), breaching the
/// surface leaves the tar, and full submersion drowns. Pure and Godot-free.
/// </summary>
[TestSuite]
public class TarStateTest
{
    // Ported 1:1 from /reference/WixxMovement.gd (TAR_* constants).
    private static TarTunables Tuning() => new(
        sinkPerTick: 0.55f,
        kickRise: 7.0f,
        weakKickFactor: 0.15f,
        kickForward: 8.0f,
        deathDepth: 58.0f,
        entryDepth: 26.0f,
        exitJumpVelocity: 12.0f,
        exitForwardVelocity: 4.0f,
        exitLaunchTicks: 14);

    [TestCase]
    public void EnterPlungesToEntryDepthAndSubmerges()
    {
        var t = Tuning();
        var tar = new TarState();

        tar.Enter(facing: 1, t);

        AssertThat(tar.Submerged).IsTrue();
        AssertThat(tar.Depth).IsEqualApprox(t.EntryDepth, 0.0001f);
        AssertThat(tar.Facing).IsEqual(1);
    }

    [TestCase]
    public void DoingNothingSinksDeeperEachTick()
    {
        var t = Tuning();
        var tar = new TarState();
        tar.Enter(1, t);

        var step = tar.Tick(kickUp: false, kickDown: false, t);

        AssertThat(step.Outcome).IsEqual(TarOutcome.Struggling);
        AssertThat(step.Depth).IsEqualApprox(t.EntryDepth + t.SinkPerTick, 0.0001f);
        AssertThat(step.ForwardStep).IsEqualApprox(0f, 0.0001f);
    }

    [TestCase]
    public void CleanAlternationClimbsAndNudgesForward()
    {
        var t = Tuning();
        var tar = new TarState();
        tar.Enter(facing: 1, t);

        // strum up, then strum down = clean alternation: each is a real climb + nudge.
        var up = tar.Tick(kickUp: true, kickDown: false, t);
        AssertThat(up.ForwardStep).IsEqualApprox(t.KickForward, 0.0001f); // facing +1 * 8
        // depth = 26 + 0.55 - 7
        AssertThat(up.Depth).IsEqualApprox(t.EntryDepth + t.SinkPerTick - t.KickRise, 0.0001f);

        var down = tar.Tick(kickUp: false, kickDown: true, t);
        AssertThat(down.ForwardStep).IsEqualApprox(t.KickForward, 0.0001f);
    }

    [TestCase]
    public void SameDirectionMashingBarelyHelps()
    {
        var t = Tuning();
        var clean = new TarState();
        var mash = new TarState();
        clean.Enter(1, t);
        mash.Enter(1, t);

        // First kick is "clean" for both (no prior strum), so prime them past it.
        clean.Tick(kickUp: true, kickDown: false, t);
        mash.Tick(kickUp: true, kickDown: false, t);

        // Now: clean alternates (down), mash repeats (up again).
        var cleanStep = clean.Tick(kickUp: false, kickDown: true, t);
        var mashStep = mash.Tick(kickUp: true, kickDown: false, t);

        // The alternating struggler is shallower (climbed more) than the masher.
        AssertThat(cleanStep.Depth).IsLess(mashStep.Depth);
        AssertThat(mashStep.ForwardStep).IsEqualApprox(0f, 0.0001f); // no forward nudge on a weak kick
    }

    [TestCase]
    public void BreachesTheSurfaceWithCleanStruggling()
    {
        var t = Tuning();
        var tar = new TarState();
        tar.Enter(1, t);

        TarOutcome outcome = TarOutcome.Struggling;
        bool up = true;
        for (int i = 0; i < 200 && outcome == TarOutcome.Struggling; i++)
        {
            var step = tar.Tick(kickUp: up, kickDown: !up, t);
            outcome = step.Outcome;
            up = !up; // alternate cleanly
        }

        AssertThat(outcome).IsEqual(TarOutcome.Breached);
        AssertThat(tar.Submerged).IsFalse();
    }

    [TestCase]
    public void DrownsWhenLeftToSink()
    {
        var t = Tuning();
        var tar = new TarState();
        tar.Enter(1, t);

        TarOutcome outcome = TarOutcome.Struggling;
        for (int i = 0; i < 500 && outcome == TarOutcome.Struggling; i++)
        {
            outcome = tar.Tick(false, false, t).Outcome;
        }

        AssertThat(outcome).IsEqual(TarOutcome.Drowned);
        AssertThat(tar.Submerged).IsFalse();
    }

    [TestCase]
    public void NegativeFacingNudgesBackward()
    {
        var t = Tuning();
        var tar = new TarState();
        tar.Enter(facing: -1, t);

        var step = tar.Tick(kickUp: true, kickDown: false, t);
        AssertThat(step.ForwardStep).IsEqualApprox(-t.KickForward, 0.0001f);
    }

    [TestCase]
    public void TickWhileNotSubmergedIsANoOp()
    {
        var t = Tuning();
        var tar = new TarState();

        var step = tar.Tick(true, false, t);

        AssertThat(step.Outcome).IsEqual(TarOutcome.Struggling);
        AssertThat(tar.Submerged).IsFalse();
    }
}
