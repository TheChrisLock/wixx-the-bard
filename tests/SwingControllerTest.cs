namespace WixxTheBard.Tests;

using GdUnit4;
using WixxTheBard.Verbs;
using static GdUnit4.Assertions;

/// <summary>
/// Contract tests for <see cref="SwingController"/> — the Yellow lute swing's active
/// hitbox window (SPEC §2.3). Pure and Godot-free.
/// </summary>
[TestSuite]
public class SwingControllerTest
{
    // SwingActiveTicks = 12 (reference attack_timer).
    private static VerbTunables Tuning() => new(
        swingActiveTicks: 12,
        superJumpVelocity: 21f,
        superJumpLaunchTicks: 50,
        superJumpCooldownTicks: 90,
        slideSpeedThreshold: 1.5f,
        crouchHeightFactor: 0.5f,
        enemyKnockbackSpeed: 3f,
        contactInvulnTicks: 30);

    [TestCase]
    public void PressOpensTheActiveWindowFacingTheMoveDirection()
    {
        var t = Tuning();
        var swing = new SwingController();

        AssertThat(swing.IsActive).IsFalse();

        swing.Tick(swingJustPressed: true, facing: -1, suppressed: false, t);
        AssertThat(swing.IsActive).IsTrue();
        AssertThat(swing.Facing).IsEqual(-1);
    }

    [TestCase]
    public void WindowExpiresAfterSwingActiveTicks()
    {
        var t = Tuning();
        var swing = new SwingController();
        swing.Tick(true, 1, false, t); // active = 12 this tick

        for (int i = 0; i < t.SwingActiveTicks - 1; i++)
        {
            swing.Tick(false, 1, false, t);
            AssertThat(swing.IsActive).IsTrue();
        }

        swing.Tick(false, 1, false, t); // 12th decay -> expired
        AssertThat(swing.IsActive).IsFalse();
    }

    [TestCase]
    public void FreshPressReArmsTheWindow()
    {
        var t = Tuning();
        var swing = new SwingController();
        swing.Tick(true, 1, false, t);
        swing.Tick(false, 1, false, t); // partway through

        swing.Tick(true, 1, false, t); // re-arm to full
        for (int i = 0; i < t.SwingActiveTicks - 1; i++)
        {
            swing.Tick(false, 1, false, t);
        }

        AssertThat(swing.IsActive).IsTrue(); // full window restored
    }

    [TestCase]
    public void SuppressedPressDoesNotSwing()
    {
        var t = Tuning();
        var swing = new SwingController();

        swing.Tick(swingJustPressed: true, facing: 1, suppressed: true, t);

        AssertThat(swing.IsActive).IsFalse(); // strum-mode discipline: no swing in tar (rule 5)
    }
}
