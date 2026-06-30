namespace WixxTheBard.Controls;

/// <summary>
/// The persisted Options state: the active bindings, the stored A/V latency
/// offset, and the name of the preset they came from (empty once the player has
/// custom-bound anything). Plain data so it can be (de)serialised without Godot.
/// </summary>
public sealed class InputSettings
{
    public BindingSet Bindings { get; set; } = new();

    public double LatencyOffsetMs { get; set; }

    public string PresetName { get; set; } = string.Empty;
}
