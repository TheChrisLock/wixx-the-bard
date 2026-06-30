namespace WixxTheBard.Verbs;

/// <summary>What a single <see cref="TarState.Tick"/> resolved to this fixed step.</summary>
public enum TarOutcome
{
    /// <summary>Still in the tar — keep struggling.</summary>
    Struggling,

    /// <summary>Climbed back to the surface this tick — the caller fires the breach leap.</summary>
    Breached,

    /// <summary>Fully submerged this tick — the caller kills and respawns Wixx.</summary>
    Drowned,
}

/// <summary>One tick's resolved tar result: the outcome, the forward nudge, and the current depth.</summary>
public readonly struct TarStep
{
    public TarStep(TarOutcome outcome, float forwardStep, float depth)
    {
        Outcome = outcome;
        ForwardStep = forwardStep;
        Depth = depth;
    }

    /// <summary>Whether Wixx is still struggling, breached the surface, or drowned this tick.</summary>
    public TarOutcome Outcome { get; }

    /// <summary>Horizontal nudge (px) earned this tick, signed by the entry facing — a clean alternating strum.</summary>
    public float ForwardStep { get; }

    /// <summary>Depth below the surface (px) after this tick — visuals/position read it (CLAUDE.md rule 7).</summary>
    public float Depth { get; }
}

/// <summary>
/// The tar / quicksand struggle as a pure, Godot-free state machine (SPEC §4.4,
/// validated on real hardware). A constant downward pull sinks Wixx;
/// <b>clean alternate</b> up/down strumming climbs him (single-direction mashing
/// barely helps — that ties the hazard to "play it properly", not "spam the bar");
/// reaching the surface breaches him out with a leap, and full submersion drowns
/// him. The strum is consumed <i>only</i> by tar while submerged (rule 5).
///
/// It holds no Godot types and runs in the fixed step (rule 3); all numbers come
/// from <see cref="TarTunables"/> (rule 1). The Godot node owns position and fires
/// the forced-launch breach leap (rule 4) from the <see cref="TarStep"/> this returns.
/// </summary>
public sealed class TarState
{
    private int _lastStrum; // −1 = last clean kick was strum-up, +1 = strum-down, 0 = none yet

    /// <summary>True while Wixx is submerged and the tar owns the strum.</summary>
    public bool Submerged { get; private set; }

    /// <summary>Current depth below the surface in px (0 = at the surface).</summary>
    public float Depth { get; private set; }

    /// <summary>Entry facing (+1 / −1) — the breach leap and forward nudges carry this way.</summary>
    public int Facing { get; private set; } = 1;

    /// <summary>
    /// Plunge into the tar to the entry depth (SPEC §4.4 — "a few kicks deep, or you
    /// breach instantly"). <paramref name="facing"/> is the direction of travel on
    /// contact, used for the forward carry and the breach leap.
    /// </summary>
    public void Enter(int facing, TarTunables t)
    {
        Submerged = true;
        Depth = t.EntryDepth;
        _lastStrum = 0;
        Facing = facing < 0 ? -1 : 1;
    }

    /// <summary>
    /// Advance one fixed tick of struggling. <paramref name="kickUp"/> /
    /// <paramref name="kickDown"/> are the strum <b>edges</b> this tick (a flick, not
    /// a hold) — clean alternation between them is what climbs. Returns the resolved
    /// <see cref="TarStep"/>; on <see cref="TarOutcome.Breached"/> or
    /// <see cref="TarOutcome.Drowned"/> the machine has left the tar and the caller
    /// must act (leap out / respawn).
    /// </summary>
    public TarStep Tick(bool kickUp, bool kickDown, TarTunables t)
    {
        if (!Submerged)
        {
            return new TarStep(TarOutcome.Struggling, 0f, Depth);
        }

        // Constant downward pull.
        Depth += t.SinkPerTick;

        // A strum edge this tick: +1 (down) or −1 (up); 0 if none or both at once.
        int kick = (kickDown ? 1 : 0) - (kickUp ? 1 : 0);
        float forwardStep = 0f;
        if (kick != 0)
        {
            if (kick != _lastStrum)
            {
                // Clean alternation — a real climb plus a forward nudge.
                Depth = Depth - t.KickRise;
                if (Depth < 0f)
                {
                    Depth = 0f;
                }

                forwardStep = Facing * t.KickForward;
            }
            else
            {
                // Same direction again — barely helps (spamming one way is punished).
                Depth = Depth - t.KickRise * t.WeakKickFactor;
                if (Depth < 0f)
                {
                    Depth = 0f;
                }
            }

            _lastStrum = kick;
        }

        if (Depth >= t.DeathDepth)
        {
            Submerged = false;
            return new TarStep(TarOutcome.Drowned, forwardStep, Depth);
        }

        if (Depth <= 0f)
        {
            Submerged = false;
            return new TarStep(TarOutcome.Breached, forwardStep, 0f);
        }

        return new TarStep(TarOutcome.Struggling, forwardStep, Depth);
    }

    /// <summary>Force-clear tar state (respawn / scene reset / waded out horizontally).</summary>
    public void Reset()
    {
        Submerged = false;
        Depth = 0f;
        _lastStrum = 0;
    }
}
