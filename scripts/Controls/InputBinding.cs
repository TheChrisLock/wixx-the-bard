namespace WixxTheBard.Controls;

/// <summary>Whether a verb is driven by a digital button or an analog axis.</summary>
public enum BindingKind
{
    Button,
    Axis,
}

/// <summary>
/// One verb's binding: either a button index, or an axis with a <em>learned</em>
/// rest value. Nothing about a guitar's layout is guessable (SPEC §14): analog
/// inputs rest at −1, +1, or 0, so "engaged" is defined as deflection away from
/// the captured rest — never an assumed polarity (CLAUDE.md rule 2).
///
/// Most axis verbs (whammy) are legitimately <em>directional</em> — pressed means
/// deflection past rest in one captured direction, so two verbs can share an axis
/// in opposite directions (e.g. strum-as-axis presets). Tilt is different: SPEC
/// §14 found its axis polarity unknowable per guitar and calls for engaging on
/// deflection "away from rest in either direction" — so its binding is
/// <see cref="Bidirectional"/> and <see cref="Direction"/> is ignored.
/// </summary>
public sealed class InputBinding
{
    public BindingKind Kind { get; }

    /// <summary>Button index or axis index.</summary>
    public int Index { get; }

    /// <summary>Axis rest value learned at bind time (meaningless for buttons).</summary>
    public float RestValue { get; }

    /// <summary>Axis engaged direction: +1 or −1, the sign of deflection from rest that counts as pressed
    /// (ignored when <see cref="Bidirectional"/> is set).</summary>
    public int Direction { get; }

    /// <summary>When true, deflection either way from rest counts as engaged (SPEC §14 tilt finding);
    /// meaningless for buttons.</summary>
    public bool Bidirectional { get; }

    private InputBinding(BindingKind kind, int index, float restValue, int direction, bool bidirectional)
    {
        Kind = kind;
        Index = index;
        RestValue = restValue;
        Direction = direction;
        Bidirectional = bidirectional;
    }

    public static InputBinding Button(int index) => new(BindingKind.Button, index, 0f, 0, false);

    /// <summary>
    /// Bind to an axis. <paramref name="direction"/> is normalised to ±1 and is
    /// ignored when <paramref name="bidirectional"/> is true (tilt — SPEC §14).
    /// </summary>
    public static InputBinding Axis(int index, float restValue, int direction, bool bidirectional = false)
        => new(BindingKind.Axis, index, restValue, direction < 0 ? -1 : 1, bidirectional);

    /// <summary>Magnitude/sign of deflection away from rest: absolute for a bidirectional
    /// axis (tilt), signed to the engaged direction otherwise (negative if pushed the wrong way).</summary>
    private float Deflection(InputSnapshot snapshot)
    {
        float raw = snapshot.GetAxis(Index) - RestValue;
        return Bidirectional ? System.Math.Abs(raw) : raw * Direction;
    }

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
        : $"Axis {Index}  rest {RestValue.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)}  dir {(Bidirectional ? "±" : Direction >= 0 ? "+" : "-")}";
}
