using System.Collections.Generic;

namespace WixxTheBard.Controls;

/// <summary>
/// The active map of verb → binding. This is the data-driven layer gameplay
/// resolves through; no gameplay code ever names a button or axis index
/// (CLAUDE.md rule 2). Presets, the press-to-bind remapper, and persistence all
/// produce or mutate one of these.
/// </summary>
public sealed class BindingSet
{
    private readonly Dictionary<GuitarVerb, InputBinding> _bindings = new();

    public IReadOnlyDictionary<GuitarVerb, InputBinding> Bindings => _bindings;

    public void Set(GuitarVerb verb, InputBinding binding) => _bindings[verb] = binding;

    public void Unbind(GuitarVerb verb) => _bindings.Remove(verb);

    public bool IsBound(GuitarVerb verb) => _bindings.ContainsKey(verb);

    public bool TryGet(GuitarVerb verb, out InputBinding binding) => _bindings.TryGetValue(verb, out binding!);

    public bool IsEngaged(GuitarVerb verb, InputSnapshot snapshot, InputTuning tuning)
        => _bindings.TryGetValue(verb, out var binding) && binding.IsEngaged(snapshot, tuning);

    public float EngagedAmount(GuitarVerb verb, InputSnapshot snapshot, InputTuning tuning)
        => _bindings.TryGetValue(verb, out var binding) ? binding.EngagedAmount(snapshot, tuning) : 0f;

    public BindingSet Clone()
    {
        var copy = new BindingSet();
        foreach (var pair in _bindings)
        {
            copy._bindings[pair.Key] = pair.Value;
        }

        return copy;
    }
}
