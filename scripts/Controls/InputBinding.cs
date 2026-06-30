namespace WixxTheBard.Controls;

/// <summary>Whether a verb is driven by a digital button or an analog axis.</summary>
public enum BindingKind
{
    Button,
    Axis,
}

/// <summary>
/// One verb's binding: either a button index, or an axis with a <em>learned</em>
/// rest value and an engaged <em>direction</em>. Nothing about a guitar's layout
/// is guessable (SPEC §14): analog inputs rest at −1, +1, or 0, so "engaged" is
/// defined as deflection away from the captured rest in the captured direction —
/// never an assumed polarity (CLAUDE.md rule 2).
/// </summary>
public sealed class InputBinding
{
    public BindingKind Kind { get; }

    /// <summary>Button index or axis index.</summary>
    public int Index { get; }

    /// <summary>Axis rest value learned at bind time (meaningless for buttons).</summary>
    public float RestValue { get; }

    /// <summary>Axis engaged direction: +1 or −1, the sign of deflection from rest that counts as pressed.</summary>
    public int Direction { get; }

    private InputBinding(BindingKind kind, int index, float restValue, int direction)
    {
        Kind = kind;
        Index = index;
        RestValue = restValue;
        Direction = direction;
    }

    public static InputBinding Button(int index) => new(BindingKind.Button, index, 0f, 0);

    /// <summary>Bind to an axis. <paramref name="direction"/> is normalised to ±1.</summary>
    public static InputBinding Axis(int index, float restValue, int direction)
        => new(BindingKind.Axis, index, restValue, direction < 0 ? -1 : 1);

    /// <summary>Deflection of the axis away from rest in the engaged direction (negative if pushed the wrong way).</summary>
    private float Deflection(InputSnapshot snapshot) => (snapshot.GetAxis(Index) - RestValue) * Direction;

    /// <summary>True when the control is currently pressed past the engage threshold.</summary>
    public bool IsEngaged(InputSnapshot snapshot, InputTuning tuning)
    {
        if (Kind == BindingKind.Button)
        {
            return snapshot.GetButton(Index);
        }

        return Deflection(snapshot) >= tuning.AxisEngageThreshold;
    }

    /// <summary>
    /// Normalised 0..1 engagement: 0/1 for a button, and the analog travel past the
    /// engage threshold for an axis (used for whammy crouch depth in M3).
    /// </summary>
    public float EngagedAmount(InputSnapshot snapshot, InputTuning tuning)
    {
        if (Kind == BindingKind.Button)
        {
            return snapshot.GetButton(Index) ? 1f : 0f;
        }

        float deflection = Deflection(snapshot);
        if (deflection <= tuning.AxisEngageThreshold)
        {
            return 0f;
        }

        float span = tuning.AxisFullDeflection - tuning.AxisEngageThreshold;
        if (span <= 0f)
        {
            return 1f;
        }

        float amount = (deflection - tuning.AxisEngageThreshold) / span;
        return amount < 0f ? 0f : amount > 1f ? 1f : amount;
    }

    public string Describe() => Kind == BindingKind.Button
        ? $"Button {Index}"
        : $"Axis {Index}  rest {RestValue.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)}  dir {(Direction >= 0 ? "+" : "-")}";
}
