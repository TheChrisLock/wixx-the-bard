namespace WixxTheBard.Verbs;

/// <summary>
/// The whammy slide as a pure, Godot-free <b>committed</b> state (SPEC §2.4). A
/// slide is not a per-tick condition: it is triggered once by crouching with
/// momentum, then runs its course — gliding a short distance and decaying to a stop
/// even if the player keeps holding the strum (the movement core applies
/// <c>SlideFriction</c> while it is active and ignores the held direction). It does
/// not re-trigger while the whammy stays held; releasing and re-pressing the whammy
/// re-arms it, so a held crouch can't stutter-loop into repeated slides.
///
/// It holds the latch + a "consumed" flag and no Godot types, so it is unit-tested
/// directly; all thresholds come from <see cref="VerbTunables"/> (CLAUDE.md rule 1).
/// </summary>
public sealed class SlideController
{
    private bool _active;
    private bool _consumed; // a slide has already run during this crouch-hold

    /// <summary>True while a committed slide is gliding to a stop — combat/visuals read this.</summary>
    public bool Active => _active;

    /// <summary>
    /// Advance one fixed tick. Releasing the crouch (<paramref name="crouchEngaged"/>
    /// false) re-arms the next slide; otherwise, while grounded, a slide triggers when
    /// speed clears <c>SlideSpeedThreshold</c> and ends once it bleeds below
    /// <c>SlideStopSpeed</c>. Returns whether a slide is active this tick.
    /// </summary>
    public bool Tick(bool crouchEngaged, bool onFloor, float speed, VerbTunables t)
    {
        if (!crouchEngaged)
        {
            _active = false;
            _consumed = false; // whammy released — re-arm for the next slide
            return false;
        }

        if (!onFloor)
        {
            _active = false; // airborne can't slide; stays consumed until the whammy is released
            return false;
        }

        float absSpeed = speed < 0f ? -speed : speed;
        if (_active)
        {
            if (absSpeed <= t.SlideStopSpeed)
            {
                _active = false;
                _consumed = true; // ran its course — no auto re-slide while still crouched
            }
        }
        else if (!_consumed && absSpeed > t.SlideSpeedThreshold)
        {
            _active = true;
        }

        return _active;
    }

    /// <summary>Clear the slide latch (respawn / scene reset).</summary>
    public void Reset()
    {
        _active = false;
        _consumed = false;
    }
}
