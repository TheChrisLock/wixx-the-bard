namespace WixxTheBard.Verbs;

/// <summary>
/// Plain-C# carrier for the tar / quicksand struggle constants (SPEC §4.4), ported
/// from the validated <c>/reference/WixxMovement.gd</c> grey-box (SPEC §14). Built
/// from the one <c>Tunables</c> resource at the Godot boundary (CLAUDE.md rule 1)
/// and constructed directly by the unit tests so <see cref="TarState"/> stays
/// Godot-free.
///
/// Depths are in <b>pixels below the tar surface</b>; the per-tick rates assume the
/// fixed 60 Hz step (rule 3). Tuning that worked (SPEC §4.4): plunge-in depth must
/// be a few kicks deep or you breach instantly; the exit leap needs real height and
/// forward carry.
/// </summary>
public sealed class TarTunables
{
    public TarTunables(
        float sinkPerTick,
        float kickRise,
        float weakKickFactor,
        float kickForward,
        float deathDepth,
        float entryDepth,
        float exitJumpVelocity,
        float exitForwardVelocity,
        int exitLaunchTicks)
    {
        SinkPerTick = sinkPerTick;
        KickRise = kickRise;
        WeakKickFactor = weakKickFactor;
        KickForward = kickForward;
        DeathDepth = deathDepth;
        EntryDepth = entryDepth;
        ExitJumpVelocity = exitJumpVelocity;
        ExitForwardVelocity = exitForwardVelocity;
        ExitLaunchTicks = exitLaunchTicks;
    }

    /// <summary>Constant downward pull each tick, in px (reference TAR_SINK = 0.55).</summary>
    public float SinkPerTick { get; }

    /// <summary>Depth (px) a <b>clean</b> alternating strum climbs (reference TAR_KICK_RISE = 7).</summary>
    public float KickRise { get; }

    /// <summary>Fraction of <see cref="KickRise"/> a same-direction (non-alternating) strum earns —
    /// single-direction mashing barely helps (reference 0.15).</summary>
    public float WeakKickFactor { get; }

    /// <summary>Forward nudge (px) per clean alternating strum (reference TAR_KICK_FWD = 8).</summary>
    public float KickForward { get; }

    /// <summary>Depth (px) at which full submersion kills (reference TAR_DEATH = 58).</summary>
    public float DeathDepth { get; }

    /// <summary>Depth (px) Wixx plunges to on contact — must exceed a couple of kicks (reference TAR_ENTRY_DEPTH = 26).</summary>
    public float EntryDepth { get; }

    /// <summary>Upward launch speed (px/tick) of the surface-breach leap (reference TAR_EXIT_JUMP = 12).</summary>
    public float ExitJumpVelocity { get; }

    /// <summary>Forward launch speed (px/tick) carried out of the pit on breach (reference TAR_EXIT_FWD = 4).</summary>
    public float ExitForwardVelocity { get; }

    /// <summary>Ticks the breach leap floats uncut, so it clears the pit (reference TAR_LAUNCH_FRAMES = 14; rule 4).</summary>
    public int ExitLaunchTicks { get; }
}
