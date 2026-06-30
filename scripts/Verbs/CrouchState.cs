namespace WixxTheBard.Verbs;

/// <summary>
/// The whammy crouch / slide derivation as a pure, Godot-free value (SPEC §2.4).
/// Holding the whammy crouches while grounded; crouching <i>while carrying
/// momentum</i> becomes a slide (pass under low hazards, knock down enemies). It is
/// a pure function of this tick's inputs — no internal state — so it is trivially
/// unit-tested and the node simply reads <see cref="Crouching"/> / <see cref="Sliding"/>
/// to drive the collision shape and animation (CLAUDE.md rule 7).
/// </summary>
public readonly struct CrouchState
{
    private CrouchState(bool crouching, bool sliding)
    {
        Crouching = crouching;
        Sliding = sliding;
    }

    /// <summary>Whammy held and grounded — duck under projectiles / drop through one-way platforms.</summary>
    public bool Crouching { get; }

    /// <summary>Crouching while moving fast enough — a slide (SPEC §2.4).</summary>
    public bool Sliding { get; }

    /// <summary>
    /// Resolve crouch/slide for this tick. <paramref name="crouchEngaged"/> is the
    /// resolved whammy verb (analog depth already thresholded by the input layer —
    /// rule 2); <paramref name="speed"/> is horizontal speed in px/tick.
    /// </summary>
    public static CrouchState Evaluate(bool crouchEngaged, bool onFloor, float speed, VerbTunables t)
    {
        bool crouching = crouchEngaged && onFloor;
        float absSpeed = speed < 0f ? -speed : speed;
        bool sliding = crouching && absSpeed > t.SlideSpeedThreshold;
        return new CrouchState(crouching, sliding);
    }
}
