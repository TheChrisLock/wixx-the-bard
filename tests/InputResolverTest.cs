namespace WixxTheBard.Tests;

using GdUnit4;
using WixxTheBard.Controls;
using static GdUnit4.Assertions;

/// <summary>
/// Edge detection for <see cref="InputResolver"/>: pressed / just-pressed /
/// just-released must be correct across ticks (the contract gameplay reads).
/// </summary>
[TestSuite]
public class InputResolverTest
{
    private static InputTuning Tuning() => new(0.35f, 0.5f, 1.0f, 4, 8, 6);

    private static InputSnapshot Buttons(params bool[] buttons) => new(buttons, new float[0]);

    [TestCase]
    public void RisingAndFallingEdges()
    {
        var bindings = new BindingSet();
        bindings.Set(GuitarVerb.Jump, InputBinding.Button(0));
        var resolver = new InputResolver(bindings, Tuning());

        resolver.Update(Buttons(false));
        AssertThat(resolver.IsPressed(GuitarVerb.Jump)).IsFalse();
        AssertThat(resolver.JustPressed(GuitarVerb.Jump)).IsFalse();

        resolver.Update(Buttons(true));
        AssertThat(resolver.IsPressed(GuitarVerb.Jump)).IsTrue();
        AssertThat(resolver.JustPressed(GuitarVerb.Jump)).IsTrue();
        AssertThat(resolver.JustReleased(GuitarVerb.Jump)).IsFalse();

        // Held — pressed stays true, just-pressed clears.
        resolver.Update(Buttons(true));
        AssertThat(resolver.IsPressed(GuitarVerb.Jump)).IsTrue();
        AssertThat(resolver.JustPressed(GuitarVerb.Jump)).IsFalse();

        resolver.Update(Buttons(false));
        AssertThat(resolver.IsPressed(GuitarVerb.Jump)).IsFalse();
        AssertThat(resolver.JustReleased(GuitarVerb.Jump)).IsTrue();

        resolver.Update(Buttons(false));
        AssertThat(resolver.JustReleased(GuitarVerb.Jump)).IsFalse();
    }

    [TestCase]
    public void UnboundVerbNeverEngages()
    {
        var resolver = new InputResolver(new BindingSet(), Tuning());
        resolver.Update(Buttons(true, true, true));
        AssertThat(resolver.IsPressed(GuitarVerb.SuperJump)).IsFalse();
    }
}
