namespace WixxTheBard.Tests;

using GdUnit4;
using WixxTheBard.Movement;
using static GdUnit4.Assertions;

/// <summary>
/// Contract tests for <see cref="MovementCore"/> — the M2 physics that gameplay
/// and (later) visuals read. Pure and Godot-free, so they run under plain
/// <c>dotnet test</c>. The load-bearing one is <see cref="ForcedLaunchIsExemptFromJumpCut"/>:
/// CLAUDE.md rule 4 (the documented footgun) demands forced launches never be cut.
/// </summary>
[TestSuite]
public class MovementCoreTest
{
    // Ported 1:1 from /reference/WixxMovement.gd (per-60 Hz-tick units).
    private static MovementTunables Tuning() => new(
        moveAccel: 0.9f,
        maxSpeed: 4.2f,
        frictionHold: 0.78f,
        sprintMaxMultiplier: 1.9f,
        sprintChargeRate: 0.03f,
        sprintDecayRate: 0.05f,
        gravity: 0.8f,
        terminalFallSpeed: 15.0f,
        jumpVelocity: 13.0f,
        jumpCutFactor: 0.85f,
        risingGravityFactor: 0.6f);

    private static MovementInput Hold(bool left, bool right, bool onFloor) =>
        new(left, right, jumpHeld: false, jumpJustPressed: false, sprint: false, onFloor: onFloor);

    private static MovementInput Idle(bool onFloor) =>
        new(false, false, false, false, false, onFloor);

    // ---- Horizontal (Hold scheme B) ----

    [TestCase]
    public void HeldDirectionAcceleratesAndSetsFacing()
    {
        var t = Tuning();
        var core = new MovementCore();

        core.Tick(Hold(left: false, right: true, onFloor: true), t);

        AssertThat(core.VelocityX).IsEqual(0.9f); // one tick of MoveAccel
        AssertThat(core.Facing).IsEqual(1);

        core.Tick(Hold(left: true, right: false, onFloor: true), t);
        AssertThat(core.Facing).IsEqual(-1);
    }

    [TestCase]
    public void HorizontalSpeedClampsToMaxSpeed()
    {
        var t = Tuning();
        var core = new MovementCore();

        for (int i = 0; i < 100; i++)
        {
            core.Tick(Hold(left: false, right: true, onFloor: true), t);
        }

        AssertThat(core.VelocityX).IsEqualApprox(t.MaxSpeed, 0.0001f);
    }

    [TestCase]
    public void ReleasingDirectionDecaysByFriction()
    {
        var t = Tuning();
        var core = new MovementCore();
        core.Tick(Hold(left: false, right: true, onFloor: true), t); // vx = 0.9

        core.Tick(Hold(left: false, right: false, onFloor: true), t);

        AssertThat(core.VelocityX).IsEqualApprox(0.9f * 0.78f, 0.0001f);
    }

    // ---- Sprint ----

    [TestCase]
    public void SprintRaisesTheSpeedCapAboveBase()
    {
        var t = Tuning();
        var core = new MovementCore();

        // Build sprint charge while driving right; speed should exceed base MaxSpeed.
        for (int i = 0; i < 200; i++)
        {
            core.Tick(new MovementInput(false, true, false, false, sprint: true, onFloor: true), t);
        }

        AssertThat(core.SprintCharge).IsEqualApprox(t.SprintMaxMultiplier, 0.0001f);
        AssertThat(core.VelocityX).IsGreater(t.MaxSpeed);
        AssertThat(core.VelocityX).IsEqualApprox(t.MaxSpeed * t.SprintMaxMultiplier, 0.0001f);
    }

    [TestCase]
    public void SprintChargeBleedsBackToOneWhenReleased()
    {
        var t = Tuning();
        var core = new MovementCore();
        for (int i = 0; i < 200; i++)
        {
            core.Tick(new MovementInput(false, false, false, false, sprint: true, onFloor: true), t);
        }

        AssertThat(core.SprintCharge).IsGreater(1f);

        for (int i = 0; i < 200; i++)
        {
            core.Tick(Idle(onFloor: true), t);
        }

        AssertThat(core.SprintCharge).IsEqualApprox(1f, 0.0001f);
    }

    // ---- Jump ----

    [TestCase]
    public void JumpLaunchesUpwardOnlyFromTheFloor()
    {
        var t = Tuning();
        var core = new MovementCore();

        // Pressed while airborne — must not jump.
        core.Tick(new MovementInput(false, false, true, true, false, onFloor: false), t);
        AssertThat(core.VelocityY).IsGreaterEqual(0f); // gravity only, no launch

        // Pressed on the floor — launches up (negative Y).
        var grounded = new MovementCore();
        grounded.Tick(new MovementInput(false, false, true, true, false, onFloor: true), t);
        AssertThat(grounded.VelocityY).IsEqual(-t.JumpVelocity);
    }

    [TestCase]
    public void RestingOnFloorZeroesFallSpeed()
    {
        var t = Tuning();
        var core = new MovementCore();

        // Fall a few ticks airborne, then land.
        core.Tick(Idle(onFloor: false), t);
        core.Tick(Idle(onFloor: false), t);
        AssertThat(core.VelocityY).IsGreater(0f);

        core.Tick(Idle(onFloor: true), t);
        AssertThat(core.VelocityY).IsEqual(0f);
    }

    [TestCase]
    public void FallSpeedIsCappedAtTerminal()
    {
        var t = Tuning();
        var core = new MovementCore();

        for (int i = 0; i < 200; i++)
        {
            core.Tick(Idle(onFloor: false), t);
        }

        AssertThat(core.VelocityY).IsEqualApprox(t.TerminalFallSpeed, 0.0001f);
    }

    // ---- Variable-height jump + the rule-4 exemption (the footgun) ----

    [TestCase]
    public void ReleasingJumpEarlyCutsTheRise()
    {
        var t = Tuning();

        float heldPeak = SimulatePeak(jumpHeldWhileRising: true);
        float cutPeak = SimulatePeak(jumpHeldWhileRising: false);

        // Releasing early must reach a strictly lower (less-negative) apex.
        AssertThat(cutPeak).IsGreater(heldPeak); // peak stored as min Y; less negative = greater
        return;

        float SimulatePeak(bool jumpHeldWhileRising)
        {
            var core = new MovementCore();
            core.Tick(new MovementInput(false, false, true, true, false, onFloor: true), t); // jump off floor
            float minY = 0f;
            float y = 0f;
            for (int i = 0; i < 60; i++)
            {
                core.Tick(new MovementInput(false, false, jumpHeldWhileRising, false, false, onFloor: false), t);
                y += core.VelocityY;
                if (y < minY)
                {
                    minY = y;
                }

                if (core.VelocityY > 0f)
                {
                    break; // started descending — apex reached
                }
            }

            return minY;
        }
    }

    [TestCase]
    public void ForcedLaunchIsExemptFromJumpCut()
    {
        var t = Tuning();

        // A forced launch with the jump button NEVER held must still float to full
        // height — the exact footgun rule 4 forbids. Compare against a button jump
        // of the same launch speed that gets cut by an immediate release.
        float forcedPeak = SimulateForced();
        float cutButtonPeak = SimulateCutButton();

        AssertThat(core_isLaunchExemptDeeper(forcedPeak, cutButtonPeak)).IsTrue();
        return;

        // Forced launch: never press/hold jump; LaunchFrames keeps the rise whole.
        float SimulateForced()
        {
            var core = new MovementCore();
            core.ForcedLaunch(t.JumpVelocity, frames: 50);
            return RisePeak(core, jumpHeld: false);
        }

        // Button jump, released immediately: the cut chops the rise.
        float SimulateCutButton()
        {
            var core = new MovementCore();
            core.Tick(new MovementInput(false, false, true, true, false, onFloor: true), t);
            return RisePeak(core, jumpHeld: false);
        }

        float RisePeak(MovementCore core, bool jumpHeld)
        {
            float minY = 0f;
            float y = 0f;
            for (int i = 0; i < 120; i++)
            {
                core.Tick(new MovementInput(false, false, jumpHeld, false, false, onFloor: false), t);
                y += core.VelocityY;
                if (y < minY)
                {
                    minY = y;
                }

                if (core.VelocityY > 0f)
                {
                    break;
                }
            }

            return minY;
        }

        // Forced launch (deeper/min Y more negative) must out-climb the cut jump.
        static bool core_isLaunchExemptDeeper(float forced, float cut) => forced < cut;
    }

    [TestCase]
    public void LaunchTimerCountsDownAndExpires()
    {
        var t = Tuning();
        var core = new MovementCore();

        core.ForcedLaunch(t.JumpVelocity, frames: 3);
        AssertThat(core.InLaunch).IsTrue();

        core.Tick(Idle(onFloor: false), t); // 3 -> 2
        core.Tick(Idle(onFloor: false), t); // 2 -> 1
        AssertThat(core.InLaunch).IsTrue();

        core.Tick(Idle(onFloor: false), t); // 1 -> 0
        AssertThat(core.InLaunch).IsFalse();
    }
}
