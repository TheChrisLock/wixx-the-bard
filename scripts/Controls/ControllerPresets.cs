using System.Collections.Generic;

namespace WixxTheBard.Controls;

/// <summary>
/// Common-controller presets as <em>data</em>, not code paths (CLAUDE.md rule 2):
/// every preset is a table of (verb, kind, index, rest, direction) rows fed
/// through one builder. SPEC §10 asks for sensible presets so most players never
/// open the binder, with full press-to-bind remapping as the fallback for
/// everything else.
///
/// The button/axis index literals are allowed to live <em>here</em> — this is the
/// preset data layer. Gameplay never sees them; it only ever reads a
/// <see cref="BindingSet"/>.
///
/// VALIDATED: the "Ardwiino" preset is the real test-guitar map from SPEC §14
/// (frets on buttons 0,1,3,2,9; strum as buttons 11/12; whammy axis 2 resting at
/// −1.0; tilt on axis 3). It is the default so the proven hardware works on first
/// plug-in. PROVISIONAL: the remaining presets are best-effort starting points to
/// be confirmed on real hardware — the press-to-bind binder is the authoritative
/// path for any controller they get wrong.
/// </summary>
public static class ControllerPresets
{
    private readonly struct Row
    {
        public readonly GuitarVerb Verb;
        public readonly BindingKind Kind;
        public readonly int Index;
        public readonly float Rest;
        public readonly int Direction;

        public Row(GuitarVerb verb, int button)
        {
            Verb = verb;
            Kind = BindingKind.Button;
            Index = button;
            Rest = 0f;
            Direction = 0;
        }

        public Row(GuitarVerb verb, int axis, float rest, int direction)
        {
            Verb = verb;
            Kind = BindingKind.Axis;
            Index = axis;
            Rest = rest;
            Direction = direction;
        }
    }

    private static Row Btn(GuitarVerb verb, int button) => new(verb, button);

    private static Row Axis(GuitarVerb verb, int axis, float rest, int direction) => new(verb, axis, rest, direction);

    /// <summary>Validated Ardwiino test guitar (SPEC §14) — used as the out-of-box default.</summary>
    public const string DefaultPresetName = "Ardwiino (validated test guitar)";

    private static readonly Dictionary<string, Row[]> Tables = new()
    {
        [DefaultPresetName] = new[]
        {
            Btn(GuitarVerb.Jump, 0),       // Green
            Btn(GuitarVerb.Sprint, 1),     // Red
            Btn(GuitarVerb.Swing, 3),      // Yellow
            Btn(GuitarVerb.Special1, 2),   // Blue
            Btn(GuitarVerb.Special2, 9),   // Orange
            Btn(GuitarVerb.MoveLeft, 11),  // strum up
            Btn(GuitarVerb.MoveRight, 12), // strum down
            Axis(GuitarVerb.Crouch, 2, -1.0f, 1),    // whammy rests at −1, sweeps toward +1
            Axis(GuitarVerb.SuperJump, 3, 0.0f, 1),  // tilt — rest re-learned by the binder per guitar
        },

        // PROVISIONAL — verify on hardware. Frets on face buttons, strum on a hat
        // axis (rest 0, both directions), whammy/tilt on triggers/sticks.
        ["Xbox 360 Guitar (provisional)"] = new[]
        {
            Btn(GuitarVerb.Jump, 0),
            Btn(GuitarVerb.Sprint, 1),
            Btn(GuitarVerb.Swing, 2),
            Btn(GuitarVerb.Special1, 3),
            Btn(GuitarVerb.Special2, 4),
            Axis(GuitarVerb.MoveLeft, 7, 0.0f, -1),
            Axis(GuitarVerb.MoveRight, 7, 0.0f, 1),
            Axis(GuitarVerb.Crouch, 4, -1.0f, 1),
            Axis(GuitarVerb.SuperJump, 1, 0.0f, -1),
        },

        ["PlayStation 3 Guitar (provisional)"] = new[]
        {
            Btn(GuitarVerb.Jump, 0),
            Btn(GuitarVerb.Sprint, 1),
            Btn(GuitarVerb.Swing, 2),
            Btn(GuitarVerb.Special1, 3),
            Btn(GuitarVerb.Special2, 4),
            Btn(GuitarVerb.MoveLeft, 11),
            Btn(GuitarVerb.MoveRight, 12),
            Axis(GuitarVerb.Crouch, 2, -1.0f, 1),
            Axis(GuitarVerb.SuperJump, 3, 0.0f, 1),
        },

        ["Guitar Hero Live (provisional)"] = new[]
        {
            Btn(GuitarVerb.Jump, 0),
            Btn(GuitarVerb.Sprint, 1),
            Btn(GuitarVerb.Swing, 2),
            Btn(GuitarVerb.Special1, 3),
            Btn(GuitarVerb.Special2, 5),
            Axis(GuitarVerb.MoveLeft, 7, 0.0f, -1),
            Axis(GuitarVerb.MoveRight, 7, 0.0f, 1),
            Axis(GuitarVerb.Crouch, 4, -1.0f, 1),
            Axis(GuitarVerb.SuperJump, 3, 0.0f, 1),
        },

        ["Rock Band 4 Guitar (provisional)"] = new[]
        {
            Btn(GuitarVerb.Jump, 0),
            Btn(GuitarVerb.Sprint, 1),
            Btn(GuitarVerb.Swing, 2),
            Btn(GuitarVerb.Special1, 3),
            Btn(GuitarVerb.Special2, 4),
            Btn(GuitarVerb.MoveLeft, 11),
            Btn(GuitarVerb.MoveRight, 12),
            Axis(GuitarVerb.Crouch, 2, -1.0f, 1),
            Axis(GuitarVerb.SuperJump, 3, 0.0f, 1),
        },
    };

    /// <summary>Preset names in a stable display order (default first).</summary>
    public static IReadOnlyList<string> Names { get; } = BuildNames();

    public static bool Has(string name) => Tables.ContainsKey(name);

    /// <summary>Build a fresh <see cref="BindingSet"/> for a preset, or <c>null</c> if unknown.</summary>
    public static BindingSet? Build(string name)
    {
        if (!Tables.TryGetValue(name, out var rows))
        {
            return null;
        }

        var set = new BindingSet();
        foreach (var row in rows)
        {
            var binding = row.Kind == BindingKind.Button
                ? InputBinding.Button(row.Index)
                : InputBinding.Axis(row.Index, row.Rest, row.Direction);
            set.Set(row.Verb, binding);
        }

        return set;
    }

    private static List<string> BuildNames()
    {
        var names = new List<string> { DefaultPresetName };
        foreach (var key in Tables.Keys)
        {
            if (key != DefaultPresetName)
            {
                names.Add(key);
            }
        }

        return names;
    }
}
