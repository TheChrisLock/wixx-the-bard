namespace WixxTheBard.Controls;

/// <summary>
/// The complete set of bindable gameplay verbs (SPEC §2.3 / §10). Every one is
/// rebindable through the data-driven input layer — gameplay never reads a raw
/// button or axis index (CLAUDE.md rule 2).
/// </summary>
public enum GuitarVerb
{
    /// <summary>Hold strum up (Scheme B) — move left.</summary>
    MoveLeft,

    /// <summary>Hold strum down (Scheme B) — move right.</summary>
    MoveRight,

    /// <summary>Green fret — jump (variable height in M2).</summary>
    Jump,

    /// <summary>Red fret — sprint / build speed.</summary>
    Sprint,

    /// <summary>Yellow fret — lute swing (melee).</summary>
    Swing,

    /// <summary>Blue fret — special attack slot 1.</summary>
    Special1,

    /// <summary>Orange fret — special attack slot 2.</summary>
    Special2,

    /// <summary>Whammy — crouch / slide (analog depth).</summary>
    Crouch,

    /// <summary>Tilt — super jump.</summary>
    SuperJump,
}

/// <summary>Iteration helper that avoids <c>Enum.GetValues</c> allocations in hot paths.</summary>
public static class GuitarVerbs
{
    /// <summary>Every verb, in a fixed order, indexable by <c>(int)verb</c>.</summary>
    public static readonly GuitarVerb[] All =
    {
        GuitarVerb.MoveLeft,
        GuitarVerb.MoveRight,
        GuitarVerb.Jump,
        GuitarVerb.Sprint,
        GuitarVerb.Swing,
        GuitarVerb.Special1,
        GuitarVerb.Special2,
        GuitarVerb.Crouch,
        GuitarVerb.SuperJump,
    };

    /// <summary>Number of verbs — array length for resolver state buffers.</summary>
    public static int Count => All.Length;
}
