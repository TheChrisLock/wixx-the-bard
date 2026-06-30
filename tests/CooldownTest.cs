namespace WixxTheBard.Tests;

using GdUnit4;
using WixxTheBard.Verbs;
using static GdUnit4.Assertions;

/// <summary>
/// Contract tests for <see cref="Cooldown"/> — the pure fixed-tick gate behind the
/// super-jump (SPEC §2.4 cooldown lock). Godot-free, so it runs under plain
/// <c>dotnet test</c>.
/// </summary>
[TestSuite]
public class CooldownTest
{
    [TestCase]
    public void StartsReady()
    {
        var cd = new Cooldown();
        AssertThat(cd.IsReady).IsTrue();
        AssertThat(cd.Remaining).IsEqual(0);
    }

    [TestCase]
    public void StartBlocksUntilFullyTickedDown()
    {
        var cd = new Cooldown();
        cd.Start(3);
        AssertThat(cd.IsReady).IsFalse();

        cd.Tick(); // 3 -> 2
        cd.Tick(); // 2 -> 1
        AssertThat(cd.IsReady).IsFalse();

        cd.Tick(); // 1 -> 0
        AssertThat(cd.IsReady).IsTrue();

        cd.Tick(); // stays at 0, never underflows
        AssertThat(cd.Remaining).IsEqual(0);
    }

    [TestCase]
    public void FractionReportsRechargeProgress()
    {
        var cd = new Cooldown();
        cd.Start(10);
        AssertThat(cd.Fraction(10)).IsEqualApprox(0f, 0.0001f); // just started

        for (int i = 0; i < 5; i++)
        {
            cd.Tick();
        }

        AssertThat(cd.Fraction(10)).IsEqualApprox(0.5f, 0.0001f);

        for (int i = 0; i < 5; i++)
        {
            cd.Tick();
        }

        AssertThat(cd.Fraction(10)).IsEqualApprox(1f, 0.0001f); // ready
    }

    [TestCase]
    public void ResetForcesReady()
    {
        var cd = new Cooldown();
        cd.Start(50);
        cd.Reset();
        AssertThat(cd.IsReady).IsTrue();
    }
}
