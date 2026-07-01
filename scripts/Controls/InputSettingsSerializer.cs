using System;
using System.Collections.Generic;
using System.Text.Json;

namespace WixxTheBard.Controls;

/// <summary>
/// JSON (de)serialisation for <see cref="InputSettings"/>. Kept separate from the
/// domain types and Godot-free, so persistence round-trips can be unit-tested.
/// The Godot layer only supplies/consumes the string and handles the
/// <c>user://</c> file I/O.
/// </summary>
public static class InputSettingsSerializer
{
    private sealed class BindingDto
    {
        public string Verb { get; set; } = string.Empty;
        public string Kind { get; set; } = string.Empty;
        public int Index { get; set; }
        public float Rest { get; set; }
        public int Dir { get; set; }
        public bool Bidi { get; set; }
    }

    private sealed class RootDto
    {
        public List<BindingDto> Bindings { get; set; } = new();
        public double LatencyOffsetMs { get; set; }
        public string PresetName { get; set; } = string.Empty;
    }

    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    public static string Serialize(InputSettings settings)
    {
        var root = new RootDto
        {
            LatencyOffsetMs = settings.LatencyOffsetMs,
            PresetName = settings.PresetName,
        };

        foreach (var pair in settings.Bindings.Bindings)
        {
            var binding = pair.Value;
            root.Bindings.Add(new BindingDto
            {
                Verb = pair.Key.ToString(),
                Kind = binding.Kind.ToString(),
                Index = binding.Index,
                Rest = binding.RestValue,
                Dir = binding.Direction,
                Bidi = binding.Bidirectional,
            });
        }

        return JsonSerializer.Serialize(root, Options);
    }

    /// <summary>Tolerant of empty/garbage input — returns defaults rather than throwing.</summary>
    public static InputSettings Deserialize(string json)
    {
        var settings = new InputSettings();
        if (string.IsNullOrWhiteSpace(json))
        {
            return settings;
        }

        RootDto? root;
        try
        {
            root = JsonSerializer.Deserialize<RootDto>(json);
        }
        catch (JsonException)
        {
            return settings;
        }

        if (root == null)
        {
            return settings;
        }

        settings.LatencyOffsetMs = root.LatencyOffsetMs;
        settings.PresetName = root.PresetName ?? string.Empty;

        foreach (var dto in root.Bindings)
        {
            if (!Enum.TryParse<GuitarVerb>(dto.Verb, out var verb))
            {
                continue;
            }

            if (!Enum.TryParse<BindingKind>(dto.Kind, out var kind))
            {
                continue;
            }

            var binding = kind == BindingKind.Button
                ? InputBinding.Button(dto.Index)
                : InputBinding.Axis(dto.Index, dto.Rest, dto.Dir, dto.Bidi);
            settings.Bindings.Set(verb, binding);
        }

        return settings;
    }
}
