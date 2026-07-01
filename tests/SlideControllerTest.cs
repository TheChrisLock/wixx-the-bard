namespace WixxTheBard.Tests;

using GdUnit4;
using WixxTheBard.Verbs;
using static GdUnit4.Assertions;

/// <summary>
/// Contract tests for <see cref="SlideController"/> — the committed whammy slide
/// (SPEC §2.4). The behaviour the player asked for: a slide is triggered once by
/// crouching with momentum <em>while sprinting</em>, then runs to a stop on its
/// own (the movement core decays it) regardless of the held strum, and it does
/// not auto re-trigger while the whammy stays held. Pure and Godot-free; speed is
/// supplied by the caller (the core owns the friction), so the tests feed a
/// decaying speed sequence.
/// </summary>
[TestSuite]
public class SlideControllerTest
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
    public void TriggersWhenCrouchingWithMomentumWhileSprinting()
    {
        var t = Tuning();
        var slide = new SlideController();

        AssertThat(slide.Tick(crouchEngaged: true, sprintEngaged: true, onFloor: true, speed: 4f, t)).IsTrue();
        AssertThat(slide.Active).IsTrue();
    }

    [TestCase]
    public void DoesNotTriggerWithoutSprint()
    {
        var t = Tuning();
        var slide = new SlideController();

        // Same crouch + momentum as the triggering case, but Sprint isn't held.
        AssertThat(slide.Tick(true, sprintEngaged: false, true, speed: 4f, t)).IsFalse();
        AssertThat(slide.Active).IsFalse();
    }

    [TestCase]
    public void DoesNotTriggerWhenTooSlow()
    {
        var t = Tuning();
        var slide = new SlideController();

        AssertThat(slide.Tick(true, true, true, speed: 1f, t)).IsFalse(); // below slide threshold
    }

    [TestCase]
    public void DoesNotTriggerWhileAirborne()
    {
        var t = Tuning();
        var slide = new SlideController();

        AssertThat(slide.Tick(crouchEngaged: true, sprintEngaged: true, onFloor: false, speed: 4f, t)).IsFalse();
    }

    [TestCase]
    public void StaysActiveAsItGlidesThenEndsBelowStopSpeed()
    {
        var t = Tuning();
        var slide = new SlideController();

        slide.Tick(true, true, true, 4f, t); // trigger

        // The strum is irrelevant; the core bleeds speed down. The slide stays
        // committed all the way until it drops to the stop speed.
        AssertThat(slide.Tick(true, true, true, 3f, t)).IsTrue();
        AssertThat(slide.Tick(true, true, true, 2f, t)).IsTrue();
        AssertThat(slide.Tick(true, true, true, 1f, t)).IsTrue();
        AssertThat(slide.Tick(true, true, true, 0.4f, t)).IsFalse(); // <= stop speed → settles
        AssertThat(slide.Active).IsFalse();
    }

    [TestCase]
    public void StaysActiveEvenIfSprintIsReleasedMidSlide()
    {
        var t = Tuning();
        var slide = new SlideController();

        slide.Tick(true, true, true, 4f, t); // trigger while sprinting

        // Sprint let go mid-slide — already committed, so it keeps gliding to a stop
        // just like it ignores the strum (the slide, once triggered, runs its course).
        AssertThat(slide.Tick(true, sprintEngaged: false, true, 3f, t)).IsTrue();
        AssertThat(slide.Tick(true, false, true, 0.4f, t)).IsFalse(); // settles as usual
    }

    [TestCase]
    public void DoesNotReTriggerWhileTheWhammyStaysHeld()
    {
        var t = Tuning();
        var slide = new SlideController();

        slide.Tick(true, true, true, 4f, t);   // slide
        slide.Tick(true, true, true, 0.3f, t); // ends (consumed)

        // Speed climbs again (player still crouched, still sprinting) — must NOT re-slide.
        AssertThat(slide.Tick(true, true, true, 4f, t)).IsFalse();
        AssertThat(slide.Tick(true, true, true, 5f, t)).IsFalse();
    }

    [TestCase]
    public void ReleasingTheWhammyReArmsTheNextSlide()
    {
        var t = Tuning();
        var slide = new SlideController();

        slide.Tick(true, true, true, 4f, t);
        slide.Tick(true, true, true, 0.3f, t); // consumed

        slide.Tick(crouchEngaged: false, sprintEngaged: true, onFloor: true, speed: 4f, t); // release → re-arm

        AssertThat(slide.Tick(true, true, true, 4f, t)).IsTrue(); // crouch again → fresh slide
    }
}
