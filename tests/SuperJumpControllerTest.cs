namespace WixxTheBard.Tests;

using GdUnit4;
using WixxTheBard.Verbs;
using static GdUnit4.Assertions;

/// <summary>
/// Contract tests for <see cref="SuperJumpController"/> — the tilt super-jump gate
/// (SPEC §2.4/§13). The load-bearing behaviours: it fires on the tilt <b>level</b>
/// (not a clean edge), only from the floor, and is cooldown-gated so a sustained
/// tilt cannot re-launch. Pure and Godot-free.
/// </summary>
[TestSuite]
public class SuperJumpControllerTest
{
    private static VerbTunables Tuning() => new(
        swingActiveTicks: 12,
        superJumpVelocity: 21f,
        superJumpLaunchTicks: 50,
        superJumpCooldownTicks: 90,
        slideSpeedThreshold: 1.5f,
        slideStopSpeed: 0.5f,
        crouchHeightFactor: 0.5f,
        enemyKnockbackSpeed: 3f,
        contactInvulnTicks: 30);

    [TestCase]
    public void FiresOnTiltLevelFromTheFloorWhenReady()
    {
        var t = Tuning();
        var sj = new SuperJumpController();

        sj.Tick();
        bool fired = sj.TryFire(tiltEngaged: true, onFloor: true, t);

        AssertThat(fired).IsTrue();
        AssertThat(sj.IsReady).IsFalse(); // firing started the cooldown
    }

    [TestCase]
    public void DoesNotFireWhileAirborne()
    {
        var t = Tuning();
        var sj = new SuperJumpController();

        AssertThat(sj.TryFire(tiltEngaged: true, onFloor: false, t)).IsFalse();
        AssertThat(sj.IsReady).IsTrue(); // never consumed
    }

    [TestCase]
    public void SustainedTiltFiresOnlyOncePerCooldown()
    {
        var t = Tuning();
        var sj = new SuperJumpController();

        sj.Tick();
        AssertThat(sj.TryFire(true, true, t)).IsTrue(); // launches

        // Tilt stays engaged on the floor *through* the cooldown — it must not refire
        // while still recharging (level + cooldown, not edge — the SPEC §14 fix).
        int refires = 0;
        for (int i = 0; i < t.SuperJumpCooldownTicks - 1; i++)
        {
            sj.Tick();
            if (sj.TryFire(true, true, t))
            {
                refires++;
            }
        }

        AssertThat(refires).IsEqual(0);
        AssertThat(sj.IsReady).IsFalse(); // still one tick shy of recharged
    }

    [TestCase]
    public void FiresAgainAfterTheCooldownElapses()
    {
        var t = Tuning();
        var sj = new SuperJumpController();

        sj.Tick();
        sj.TryFire(true, true, t); // start cooldown

        for (int i = 0; i < t.SuperJumpCooldownTicks; i++)
        {
            sj.Tick();
        }

        AssertThat(sj.IsReady).IsTrue();
        AssertThat(sj.TryFire(true, true, t)).IsTrue();
    }

    [TestCase]
    public void ChargeFractionClimbsFromZeroToOne()
    {
        var t = Tuning();
        var sj = new SuperJumpController();

        sj.Tick();
        sj.TryFire(true, true, t);
        AssertThat(sj.ChargeFraction(t)).IsEqualApprox(0f, 0.01f);

        for (int i = 0; i < t.SuperJumpCooldownTicks; i++)
        {
            sj.Tick();
        }

        AssertThat(sj.ChargeFraction(t)).IsEqualApprox(1f, 0.0001f);
    }
}
