namespace WixxTheBard.Controls;

/// <summary>
/// Plain-C# carrier for the input thresholds. The running game builds this from
/// the <c>Tunables</c> resource at the Godot boundary (CLAUDE.md rule 1 — the
/// values live in one place); unit tests construct it directly so the pure logic
/// stays Godot-free and runnable under <c>dotnet test</c>.
/// </summary>
public sealed class InputTuning
{
    /// <summary>Deflection from an axis's rest (in axis units) at which it counts as engaged at runtime.</summary>
    public float AxisEngageThreshold { get; }

    /// <summary>Larger deflection required to *capture* an axis during press-to-bind, so a deliberate push binds, not noise.</summary>
    public float AxisCaptureThreshold { get; }

    /// <summary>Deflection from rest treated as a fully-engaged analog value (1.0). Used for crouch depth (M3).</summary>
    public float AxisFullDeflection { get; }

    /// <summary>Frames sampled to learn each axis's rest value before listening for a press-to-bind input.</summary>
    public int BindingBaselineSamples { get; }

    /// <summary>How many raw joypad buttons to scan (guitars expose frets on arbitrary indices — SPEC §14).</summary>
    public int ButtonScanCount { get; }

    /// <summary>How many raw joypad axes to scan (whammy/tilt land on arbitrary axes).</summary>
    public int AxisScanCount { get; }

    public InputTuning(
        float axisEngageThreshold,
        float axisCaptureThreshold,
        float axisFullDeflection,
        int bindingBaselineSamples,
        int buttonScanCount,
        int axisScanCount)
    {
        AxisEngageThreshold = axisEngageThreshold;
        AxisCaptureThreshold = axisCaptureThreshold;
        AxisFullDeflection = axisFullDeflection;
        BindingBaselineSamples = bindingBaselineSamples;
        ButtonScanCount = buttonScanCount;
        AxisScanCount = axisScanCount;
    }
}
