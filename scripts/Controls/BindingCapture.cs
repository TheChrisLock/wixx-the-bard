using System;

namespace WixxTheBard.Controls;

/// <summary>Progress of a single press-to-bind capture.</summary>
public enum CaptureState
{
    /// <summary>Learning each axis's rest value; the player should hold the controls still.</summary>
    Baseline,

    /// <summary>Rest values learned; waiting for the player to press the control to bind.</summary>
    Listening,

    /// <summary>A control fired; <see cref="BindingCapture.Result"/> holds the binding.</summary>
    Captured,
}

/// <summary>
/// The press-to-bind detector (SPEC §10, CLAUDE.md rule 2). It is fed one
/// <see cref="InputSnapshot"/> per fixed tick and:
/// <list type="bullet">
///   <item>spends a short baseline window learning each axis's <em>rest</em> value
///   at runtime (whammy rests at −1, tilt at an unknown value — none guessable);</item>
///   <item>then captures the first deliberate input — a freshly-pressed button, or an
///   axis deflected past the capture threshold, recording the rest value and the
///   <em>direction</em> of deflection so "engaged = away from rest, either way".</item>
/// </list>
/// Buttons already held when capture begins (e.g. the control used to open the
/// binder) are ignored until released, so they can't bind themselves.
/// Pure and Godot-free, so the detector is fully unit-testable.
/// </summary>
public sealed class BindingCapture
{
    private readonly InputTuning _tuning;

    private float[] _restSum = Array.Empty<float>();
    private float[] _rest = Array.Empty<float>();
    private bool[] _heldSinceStart = Array.Empty<bool>();
    private int _baselineSeen;

    public CaptureState State { get; private set; } = CaptureState.Baseline;

    public InputBinding? Result { get; private set; }

    public BindingCapture(InputTuning tuning)
    {
        _tuning = tuning;
    }

    public void Reset()
    {
        State = CaptureState.Baseline;
        Result = null;
        _baselineSeen = 0;
        _restSum = Array.Empty<float>();
        _rest = Array.Empty<float>();
        _heldSinceStart = Array.Empty<bool>();
    }

    /// <summary>Feed one tick of input; returns the (possibly advanced) state.</summary>
    public CaptureState Feed(InputSnapshot snapshot)
    {
        if (State == CaptureState.Captured)
        {
            return State;
        }

        EnsureBuffers(snapshot);

        if (State == CaptureState.Baseline)
        {
            AccumulateBaseline(snapshot);
            return State;
        }

        return Listen(snapshot);
    }

    private void EnsureBuffers(InputSnapshot snapshot)
    {
        if (_restSum.Length != snapshot.AxisCount)
        {
            _restSum = new float[snapshot.AxisCount];
        }

        if (_heldSinceStart.Length != snapshot.ButtonCount)
        {
            _heldSinceStart = new bool[snapshot.ButtonCount];
        }
    }

    private void AccumulateBaseline(InputSnapshot snapshot)
    {
        for (int i = 0; i < snapshot.AxisCount; i++)
        {
            _restSum[i] += snapshot.GetAxis(i);
        }

        // A button counts as "held since start" only if it has been down on every
        // baseline frame; releasing it at any point clears the flag so a later press
        // is treated as a fresh, bindable input.
        for (int b = 0; b < snapshot.ButtonCount; b++)
        {
            bool down = snapshot.GetButton(b);
            _heldSinceStart[b] = _baselineSeen == 0 ? down : _heldSinceStart[b] && down;
        }

        _baselineSeen++;
        if (_baselineSeen >= _tuning.BindingBaselineSamples)
        {
            _rest = new float[snapshot.AxisCount];
            for (int i = 0; i < _rest.Length; i++)
            {
                _rest[i] = _restSum[i] / _baselineSeen;
            }

            State = CaptureState.Listening;
        }
    }

    private CaptureState Listen(InputSnapshot snapshot)
    {
        // Keep clearing the "held since start" guard as those buttons are released.
        for (int b = 0; b < _heldSinceStart.Length && b < snapshot.ButtonCount; b++)
        {
            if (_heldSinceStart[b] && !snapshot.GetButton(b))
            {
                _heldSinceStart[b] = false;
            }
        }

        // A freshly-pressed button wins — it's the most unambiguous input.
        for (int b = 0; b < snapshot.ButtonCount; b++)
        {
            bool guarded = b < _heldSinceStart.Length && _heldSinceStart[b];
            if (snapshot.GetButton(b) && !guarded)
            {
                Result = InputBinding.Button(b);
                State = CaptureState.Captured;
                return State;
            }
        }

        // Otherwise, the axis with the largest deflection past the capture threshold.
        int bestAxis = -1;
        float bestDeflection = _tuning.AxisCaptureThreshold;
        for (int i = 0; i < snapshot.AxisCount; i++)
        {
            float rest = i < _rest.Length ? _rest[i] : 0f;
            float deflection = Math.Abs(snapshot.GetAxis(i) - rest);
            if (deflection > bestDeflection)
            {
                bestDeflection = deflection;
                bestAxis = i;
            }
        }

        if (bestAxis >= 0)
        {
            float rest = _rest[bestAxis];
            int direction = snapshot.GetAxis(bestAxis) - rest >= 0f ? 1 : -1;
            Result = InputBinding.Axis(bestAxis, rest, direction);
            State = CaptureState.Captured;
        }

        return State;
    }
}
