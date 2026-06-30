using System;

namespace WixxTheBard.Controls;

/// <summary>
/// An immutable, read-only view over one tick's raw joypad state: button down/up
/// flags and axis values. It wraps caller-owned arrays without copying, so the
/// Godot layer can refill reusable buffers each <c>_PhysicsProcess</c> and create
/// a snapshot with no heap allocation (CLAUDE.md rule 9). Out-of-range reads are
/// safe, so binding indices never have to be range-checked at the call site.
/// </summary>
public readonly struct InputSnapshot
{
    private readonly bool[] _buttons;
    private readonly float[] _axes;

    public InputSnapshot(bool[] buttons, float[] axes)
    {
        _buttons = buttons ?? Array.Empty<bool>();
        _axes = axes ?? Array.Empty<float>();
    }

    public int ButtonCount => _buttons.Length;

    public int AxisCount => _axes.Length;

    /// <summary>Button state, or <c>false</c> for an out-of-range index.</summary>
    public bool GetButton(int index) => index >= 0 && index < _buttons.Length && _buttons[index];

    /// <summary>Axis value, or <c>0</c> for an out-of-range index.</summary>
    public float GetAxis(int index) => index >= 0 && index < _axes.Length ? _axes[index] : 0f;
}
