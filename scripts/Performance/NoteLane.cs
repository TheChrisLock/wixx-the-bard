using WixxTheBard.Controls;

namespace WixxTheBard.Performance;

/// <summary>
/// The five fret lanes of the note highway (SPEC §5.2/§5.3), in neck order. A note
/// belongs to one lane; the player frets that lane and strums to hit it.
/// <see cref="NoteLane.None"/> represents "no fret held" — it never matches a charted
/// note, so a strum with no fret is a stray.
/// </summary>
public enum NoteLane
{
    None = -1,
    Green = 0,
    Red = 1,
    Yellow = 2,
    Blue = 3,
    Orange = 4,
}

/// <summary>
/// Lane helpers. The lane→verb map is how the Godot layer reads "which fret is held"
/// during a performance: it asks the data-driven input layer for a <b>verb</b>, never
/// a raw button index (CLAUDE.md rule 2). The frets are re-purposed as note lanes for
/// the duration of the performance (rule 5 — exactly one strum/fret consumer at a time),
/// so the same fret verbs that drive movement read the chart here.
/// </summary>
public static class NoteLanes
{
    /// <summary>Every playable lane, in fixed neck order, indexable by <c>(int)lane</c>.</summary>
    public static readonly NoteLane[] All =
    {
        NoteLane.Green,
        NoteLane.Red,
        NoteLane.Yellow,
        NoteLane.Blue,
        NoteLane.Orange,
    };

    /// <summary>Number of playable lanes.</summary>
    public static int Count => All.Length;

    /// <summary>The bindable fret verb that frets a given lane (data-driven — rule 2).</summary>
    public static GuitarVerb ToVerb(NoteLane lane) => lane switch
    {
        NoteLane.Green => GuitarVerb.Jump,
        NoteLane.Red => GuitarVerb.Sprint,
        NoteLane.Yellow => GuitarVerb.Swing,
        NoteLane.Blue => GuitarVerb.Special1,
        NoteLane.Orange => GuitarVerb.Special2,
        _ => GuitarVerb.Jump,
    };
}
