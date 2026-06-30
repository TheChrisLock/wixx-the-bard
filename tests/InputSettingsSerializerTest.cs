namespace WixxTheBard.Tests;

using GdUnit4;
using WixxTheBard.Controls;
using static GdUnit4.Assertions;

/// <summary>
/// Persistence round-trip for the Options state (bindings + latency + preset).
/// JSON (de)serialisation is Godot-free; the engine layer only does the file I/O.
/// </summary>
[TestSuite]
public class InputSettingsSerializerTest
{
    [TestCase]
    public void RoundTripsButtonsAxesLatencyAndPreset()
    {
        var settings = new InputSettings { LatencyOffsetMs = 33.0, PresetName = "Custom" };
        settings.Bindings.Set(GuitarVerb.Jump, InputBinding.Button(0));
        settings.Bindings.Set(GuitarVerb.Crouch, InputBinding.Axis(2, -1.0f, 1));
        settings.Bindings.Set(GuitarVerb.SuperJump, InputBinding.Axis(3, 0.3f, -1));

        string json = InputSettingsSerializer.Serialize(settings);
        var loaded = InputSettingsSerializer.Deserialize(json);

        AssertThat(System.Math.Abs(loaded.LatencyOffsetMs - 33.0) < 1e-6).IsTrue();
        AssertThat(loaded.PresetName).IsEqual("Custom");

        AssertThat(loaded.Bindings.TryGet(GuitarVerb.Jump, out var jump)).IsTrue();
        AssertThat(jump.Kind == BindingKind.Button).IsTrue();
        AssertThat(jump.Index).IsEqual(0);

        AssertThat(loaded.Bindings.TryGet(GuitarVerb.Crouch, out var crouch)).IsTrue();
        AssertThat(crouch.Kind == BindingKind.Axis).IsTrue();
        AssertThat(crouch.Index).IsEqual(2);
        AssertThat(crouch.Direction).IsEqual(1);
        AssertThat(System.Math.Abs(crouch.RestValue - (-1.0f)) < 0.01f).IsTrue();

        AssertThat(loaded.Bindings.TryGet(GuitarVerb.SuperJump, out var tilt)).IsTrue();
        AssertThat(tilt.Direction).IsEqual(-1);
        AssertThat(System.Math.Abs(tilt.RestValue - 0.3f) < 0.01f).IsTrue();
    }

    [TestCase]
    public void DeserializeToleratesGarbage()
    {
        var loaded = InputSettingsSerializer.Deserialize("not json {{{");
        AssertThat(loaded.Bindings.Bindings.Count).IsEqual(0);
        AssertThat(System.Math.Abs(loaded.LatencyOffsetMs) < 1e-9).IsTrue();
    }

    [TestCase]
    public void DeserializeEmptyReturnsDefaults()
    {
        var loaded = InputSettingsSerializer.Deserialize("");
        AssertThat(loaded.Bindings.Bindings.Count).IsEqual(0);
        AssertThat(loaded.PresetName).IsEqual(string.Empty);
    }
}
