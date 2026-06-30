namespace WixxTheBard.Controls;

/// <summary>
/// Turns a stream of raw snapshots into per-verb pressed / just-pressed /
/// just-released states plus analog amounts, against the active
/// <see cref="BindingSet"/>. State is kept in fixed arrays indexed by
/// <c>(int)verb</c> so <see cref="Update"/> allocates nothing on the hot path
/// (CLAUDE.md rule 9). It must be driven once per fixed tick (rule 3).
/// </summary>
public sealed class InputResolver
{
    private readonly bool[] _current = new bool[GuitarVerbs.Count];
    private readonly bool[] _previous = new bool[GuitarVerbs.Count];
    private readonly float[] _amount = new float[GuitarVerbs.Count];

    public BindingSet Bindings { get; set; }

    public InputTuning Tuning { get; set; }

    public InputResolver(BindingSet bindings, InputTuning tuning)
    {
        Bindings = bindings;
        Tuning = tuning;
    }

    /// <summary>Advance one fixed tick: roll current→previous and re-resolve every verb.</summary>
    public void Update(InputSnapshot snapshot)
    {
        var verbs = GuitarVerbs.All;
        for (int i = 0; i < verbs.Length; i++)
        {
            _previous[i] = _current[i];
            bool engaged = Bindings.IsEngaged(verbs[i], snapshot, Tuning);
            _current[i] = engaged;
            _amount[i] = engaged ? Bindings.EngagedAmount(verbs[i], snapshot, Tuning) : 0f;
        }
    }

    public bool IsPressed(GuitarVerb verb) => _current[(int)verb];

    public bool JustPressed(GuitarVerb verb) => _current[(int)verb] && !_previous[(int)verb];

    public bool JustReleased(GuitarVerb verb) => !_current[(int)verb] && _previous[(int)verb];

    public float Amount(GuitarVerb verb) => _amount[(int)verb];
}
