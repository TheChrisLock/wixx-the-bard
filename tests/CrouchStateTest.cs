namespace WixxTheBard.Tests;

using GdUnit4;
using WixxTheBard.Verbs;
using static GdUnit4.Assertions;

/// <summary>
/// Contract tests for <see cref="CrouchState"/> — whammy crouch and the
/// crouch-with-momentum slide (SPEC §2.4). Pure and Godot-free.
/// </summary>
[TestSuite]
public class CrouchStateTest
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
    public void CrouchesOnlyWhenGrounded()
    {
        var t = Tuning();

        var grounded = CrouchState.Evaluate(crouchEngaged: true, onFloor: true, speed: 0f, t);
        AssertThat(grounded.Crouching).IsTrue();

        var airborne = CrouchState.Evaluate(crouchEngaged: true, onFloor: false, speed: 0f, t);
        AssertThat(airborne.Crouching).IsFalse();
    }

    [TestCase]
    public void NotCrouchingWhenWhammyReleased()
    {
        var t = Tuning();
        var state = CrouchState.Evaluate(crouchEngaged: false, onFloor: true, speed: 5f, t);
        AssertThat(state.Crouching).IsFalse();
        AssertThat(state.Sliding).IsFalse();
    }

    [TestCase]
    public void SlidesWhenCrouchingWithSpeedPastThreshold()
    {
        var t = Tuning();

        var fast = CrouchState.Evaluate(true, true, speed: 3f, t);
        AssertThat(fast.Sliding).IsTrue();

        var slow = CrouchState.Evaluate(true, true, speed: 1f, t);
        AssertThat(slow.Sliding).IsFalse(); // crouching, but below the slide threshold
        AssertThat(slow.Crouching).IsTrue();
    }

    [TestCase]
    public void SlideUsesSpeedMagnitudeInBothDirections()
    {
        var t = Tuning();
        var leftFast = CrouchState.Evaluate(true, true, speed: -3f, t);
        AssertThat(leftFast.Sliding).IsTrue();
    }
}
