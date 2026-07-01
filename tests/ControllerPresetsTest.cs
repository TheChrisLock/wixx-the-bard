namespace WixxTheBard.Tests;

using GdUnit4;
using WixxTheBard.Controls;
using static GdUnit4.Assertions;

/// <summary>
/// Presets are data, not code paths (CLAUDE.md rule 2). The validated Ardwiino
/// map (SPEC §14) must bind every verb with the exact layout the test guitar
/// reported, and every preset must cover the full verb set.
/// </summary>
[TestSuite]
public class ControllerPresetsTest
{
    [TestCase]
    public void DefaultPresetResolves()
    {
        AssertThat(ControllerPresets.Has(ControllerPresets.DefaultPresetName)).IsTrue();
        AssertThat(ControllerPresets.Build(ControllerPresets.DefaultPresetName) != null).IsTrue();
        AssertThat(ControllerPresets.Build("nope") == null).IsTrue();
    }

    [TestCase]
    public void EveryPresetBindsEveryVerb()
    {
        foreach (var name in ControllerPresets.Names)
        {
            var set = ControllerPresets.Build(name)!;
            foreach (var verb in GuitarVerbs.All)
            {
                AssertThat(set.IsBound(verb)).IsTrue();
            }
        }
    }

    [TestCase]
    public void ArdwiinoMatchesValidatedHardwareMap()
    {
        var set = ControllerPresets.Build(ControllerPresets.DefaultPresetName)!;

        // Frets on buttons 0,1,3,2,9 (green,red,yellow,blue,orange) per SPEC §14.
        AssertButton(set, GuitarVerb.Jump, 0);
        AssertButton(set, GuitarVerb.Sprint, 1);
        AssertButton(set, GuitarVerb.Swing, 3);
        AssertButton(set, GuitarVerb.Special1, 2);
        AssertButton(set, GuitarVerb.Special2, 9);

        // Strum as two buttons 11/12.
        AssertButton(set, GuitarVerb.MoveLeft, 11);
        AssertButton(set, GuitarVerb.MoveRight, 12);

        // Whammy: axis 2 resting at −1, engaging upward.
        AssertThat(set.TryGet(GuitarVerb.Crouch, out var whammy)).IsTrue();
        AssertThat(whammy.Kind == BindingKind.Axis).IsTrue();
        AssertThat(whammy.Index).IsEqual(2);
        AssertThat(whammy.Direction).IsEqual(1);
        AssertThat(System.Math.Abs(whammy.RestValue - (-1.0f)) < 0.01f).IsTrue();

        // Tilt: axis 3, engaging on deflection either way (SPEC §14 — polarity unknowable).
        AssertThat(set.TryGet(GuitarVerb.SuperJump, out var tilt)).IsTrue();
        AssertThat(tilt.Kind == BindingKind.Axis).IsTrue();
        AssertThat(tilt.Index).IsEqual(3);
        AssertThat(tilt.Bidirectional).IsTrue();
    }

    [TestCase]
    public void EveryPresetBindsTiltBidirectionally()
    {
        foreach (var name in ControllerPresets.Names)
        {
            var set = ControllerPresets.Build(name)!;
            AssertThat(set.TryGet(GuitarVerb.SuperJump, out var tilt)).IsTrue();
            AssertThat(tilt.Bidirectional).IsTrue();
        }
    }

    private static void AssertButton(BindingSet set, GuitarVerb verb, int index)
    {
        AssertThat(set.TryGet(verb, out var binding)).IsTrue();
        AssertThat(binding.Kind == BindingKind.Button).IsTrue();
        AssertThat(binding.Index).IsEqual(index);
    }
}
