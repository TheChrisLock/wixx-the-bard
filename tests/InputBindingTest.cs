namespace WixxTheBard.Tests;

using GdUnit4;
using WixxTheBard.Controls;
using static GdUnit4.Assertions;

/// <summary>
/// Engage / amount math for <see cref="InputBinding"/> — the runtime resolution
/// rule that "engaged = deflection from rest in the captured direction"
/// (CLAUDE.md rule 2). Godot-free, runs under plain <c>dotnet test</c>.
/// </summary>
[TestSuite]
public class InputBindingTest
{
    private static InputTuning Tuning() => new(0.35f, 0.5f, 1.0f, 4, 8, 6);

    private static InputSnapshot Snap(bool[] buttons, float[] axes) => new(buttons, axes);

    [TestCase]
    public void ButtonEngagesWhenDown()
    {
        var binding = InputBinding.Button(2);
        AssertThat(binding.IsEngaged(Snap(new[] { false, false, true }, new float[0]), Tuning())).IsTrue();
        AssertThat(binding.IsEngaged(Snap(new[] { false, false, false }, new float[0]), Tuning())).IsFalse();
    }

    [TestCase]
    public void WhammyAxisRestingAtMinusOneEngagesUpward()
    {
        // Whammy rests at -1, sweeps toward +1 (SPEC §14): direction +1.
        var binding = InputBinding.Axis(2, -1.0f, 1);
        // At rest: not engaged.
        AssertThat(binding.IsEngaged(Snap(new bool[0], new[] { 0f, 0f, -1.0f }), Tuning())).IsFalse();
        // Pushed to -0.5 → deflection 0.5 > 0.35: engaged.
        AssertThat(binding.IsEngaged(Snap(new bool[0], new[] { 0f, 0f, -0.5f }), Tuning())).IsTrue();
    }

    [TestCase]
    public void AxisWithNegativeDirectionEngagesWhenPushedBelowRest()
    {
        // Tilt resting at 0.3, captured pushing negative: direction -1.
        var binding = InputBinding.Axis(3, 0.3f, -1);
        AssertThat(binding.IsEngaged(Snap(new bool[0], new[] { 0f, 0f, 0f, 0.3f }), Tuning())).IsFalse();
        // Push to -0.2 → deflection (−0.2−0.3)*−1 = 0.5 > 0.35: engaged.
        AssertThat(binding.IsEngaged(Snap(new bool[0], new[] { 0f, 0f, 0f, -0.2f }), Tuning())).IsTrue();
        // Pushing the WRONG way (toward +1) must not engage.
        AssertThat(binding.IsEngaged(Snap(new bool[0], new[] { 0f, 0f, 0f, 0.9f }), Tuning())).IsFalse();
    }

    [TestCase]
    public void BidirectionalAxisEngagesEitherWayFromRest()
    {
        // Tilt's polarity is unknowable per guitar (SPEC §14) — engagement must not
        // depend on which way the axis happens to deflect from its learned rest.
        var binding = InputBinding.Axis(3, 0.0f, direction: 1, bidirectional: true);

        // At rest: not engaged.
        AssertThat(binding.IsEngaged(Snap(new bool[0], new[] { 0f, 0f, 0f, 0f }), Tuning())).IsFalse();
        // Deflected in the nominal "captured" direction: engaged.
        AssertThat(binding.IsEngaged(Snap(new bool[0], new[] { 0f, 0f, 0f, 0.5f }), Tuning())).IsTrue();
        // Deflected the OTHER way — a directional binding would reject this, but
        // bidirectional must still engage.
        AssertThat(binding.IsEngaged(Snap(new bool[0], new[] { 0f, 0f, 0f, -0.5f }), Tuning())).IsTrue();
    }

    [TestCase]
    public void OutOfRangeIndexIsSafe()
    {
        var binding = InputBinding.Button(99);
        AssertThat(binding.IsEngaged(Snap(new[] { true }, new float[0]), Tuning())).IsFalse();
        var axis = InputBinding.Axis(99, 0f, 1);
        AssertThat(axis.IsEngaged(Snap(new bool[0], new[] { 1f }), Tuning())).IsFalse();
    }

    [TestCase]
    public void EngagedAmountClampsBetweenZeroAndOne()
    {
        var binding = InputBinding.Axis(0, -1.0f, 1); // full deflection 1.0
        // Below threshold → 0.
        AssertThat(binding.EngagedAmount(Snap(new bool[0], new[] { -1.0f }), Tuning())).IsEqual(0f);
        // Fully swept to +1 → deflection 2.0, well past full deflection → clamps to 1.
        AssertThat(binding.EngagedAmount(Snap(new bool[0], new[] { 1.0f }), Tuning())).IsEqual(1f);
    }
}
