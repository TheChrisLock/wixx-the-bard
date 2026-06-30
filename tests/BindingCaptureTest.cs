namespace WixxTheBard.Tests;

using GdUnit4;
using WixxTheBard.Controls;
using static GdUnit4.Assertions;

/// <summary>
/// The press-to-bind detector (SPEC §10, §14; CLAUDE.md rule 2). These cover the
/// load-bearing behaviour: learn each axis's rest at runtime, capture the engaged
/// direction either way, ignore a button held when the binder opens, and never
/// bind on noise or during the baseline window. Godot-free.
/// </summary>
[TestSuite]
public class BindingCaptureTest
{
    // Baseline = 4 frames so tests stay short.
    private static InputTuning Tuning() => new(0.35f, 0.5f, 1.0f, 4, 8, 6);

    private static InputSnapshot Snap(bool[] buttons, float[] axes) => new(buttons, axes);

    private static void Baseline(BindingCapture capture, bool[] buttons, float[] axes, int frames)
    {
        for (int i = 0; i < frames; i++)
        {
            capture.Feed(Snap(buttons, axes));
        }
    }

    [TestCase]
    public void StaysInBaselineUntilSampleTargetReached()
    {
        var capture = new BindingCapture(Tuning());
        capture.Feed(Snap(new[] { true }, new float[0])); // pressed, but still baseline
        AssertThat(capture.State == CaptureState.Baseline).IsTrue();
    }

    [TestCase]
    public void CapturesFreshlyPressedButton()
    {
        var capture = new BindingCapture(Tuning());
        Baseline(capture, new[] { false, false, false }, new float[0], 4);
        AssertThat(capture.State == CaptureState.Listening).IsTrue();

        capture.Feed(Snap(new[] { false, true, false }, new float[0]));
        AssertThat(capture.State == CaptureState.Captured).IsTrue();
        AssertThat(capture.Result!.Kind == BindingKind.Button).IsTrue();
        AssertThat(capture.Result!.Index).IsEqual(1);
    }

    [TestCase]
    public void LearnsWhammyRestAtMinusOneAndUpwardDirection()
    {
        var capture = new BindingCapture(Tuning());
        Baseline(capture, new bool[0], new[] { 0f, 0f, -1.0f }, 4);

        // Sweep whammy toward +1: deflection 0.6 > capture threshold 0.5.
        capture.Feed(Snap(new bool[0], new[] { 0f, 0f, -0.4f }));

        AssertThat(capture.State == CaptureState.Captured).IsTrue();
        var result = capture.Result!;
        AssertThat(result.Kind == BindingKind.Axis).IsTrue();
        AssertThat(result.Index).IsEqual(2);
        AssertThat(result.Direction).IsEqual(1);
        AssertThat(System.Math.Abs(result.RestValue - (-1.0f)) < 0.01f).IsTrue();
    }

    [TestCase]
    public void LearnsTiltRestAndNegativeDirection()
    {
        var capture = new BindingCapture(Tuning());
        Baseline(capture, new bool[0], new[] { 0f, 0f, 0f, 0.3f }, 4);

        // Tilt the neck so axis 3 drops below its 0.3 rest: direction −1.
        capture.Feed(Snap(new bool[0], new[] { 0f, 0f, 0f, -0.3f }));

        AssertThat(capture.State == CaptureState.Captured).IsTrue();
        var result = capture.Result!;
        AssertThat(result.Index).IsEqual(3);
        AssertThat(result.Direction).IsEqual(-1);
        AssertThat(System.Math.Abs(result.RestValue - 0.3f) < 0.01f).IsTrue();
    }

    [TestCase]
    public void IgnoresButtonHeldSinceBinderOpened()
    {
        var capture = new BindingCapture(Tuning());
        Baseline(capture, new[] { true, false, false }, new float[0], 4); // button 0 held throughout

        // Button 0 still held (the menu button); button 2 is the real new press.
        capture.Feed(Snap(new[] { true, false, true }, new float[0]));

        AssertThat(capture.State == CaptureState.Captured).IsTrue();
        AssertThat(capture.Result!.Index).IsEqual(2);
    }

    [TestCase]
    public void HeldButtonBindsAfterReleaseAndRepress()
    {
        var capture = new BindingCapture(Tuning());
        Baseline(capture, new[] { true }, new float[0], 4);

        capture.Feed(Snap(new[] { false }, new float[0])); // release clears the guard
        AssertThat(capture.State == CaptureState.Listening).IsTrue();

        capture.Feed(Snap(new[] { true }, new float[0])); // deliberate re-press binds it
        AssertThat(capture.State == CaptureState.Captured).IsTrue();
        AssertThat(capture.Result!.Index).IsEqual(0);
    }

    [TestCase]
    public void DoesNotCaptureOnSubThresholdNoise()
    {
        var capture = new BindingCapture(Tuning());
        Baseline(capture, new bool[0], new[] { 0f }, 4);

        capture.Feed(Snap(new bool[0], new[] { 0.2f })); // < 0.5 capture threshold
        AssertThat(capture.State == CaptureState.Listening).IsTrue();
        AssertThat(capture.Result == null).IsTrue();
    }
}
