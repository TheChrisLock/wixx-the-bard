namespace WixxTheBard.Verbs;

/// <summary>
/// Plain-C# carrier for the M3 verb-feel constants (lute swing, tilt super-jump,
/// whammy crouch/slide, basic-enemy feedback), mirroring
/// <c>WixxTheBard.Movement.MovementTunables</c>. The running game builds this from
/// the one <c>Tunables</c> resource at the Godot boundary (CLAUDE.md rule 1 — the
/// numbers live in a single place); unit tests construct it directly so the verb
/// logic stays Godot-free and runnable under plain <c>dotnet test</c>.
///
/// All values are in <b>per-60 Hz-tick units</b>, ported from the validated
/// <c>/reference/WixxMovement.gd</c> grey-box (SPEC §14); they assume the fixed
/// 60 Hz physics step (rule 3).
/// </summary>
public sealed class VerbTunables
{
    public VerbTunables(
        int swingActiveTicks,
        float superJumpVelocity,
        int superJumpLaunchTicks,
        int superJumpCooldownTicks,
        float slideSpeedThreshold,
        float slideStopSpeed,
        float crouchHeightFactor,
        float enemyKnockbackSpeed,
        int contactInvulnTicks)
    {
        SwingActiveTicks = swingActiveTicks;
        SuperJumpVelocity = superJumpVelocity;
        SuperJumpLaunchTicks = superJumpLaunchTicks;
        SuperJumpCooldownTicks = superJumpCooldownTicks;
        SlideSpeedThreshold = slideSpeedThreshold;
        SlideStopSpeed = slideStopSpeed;
        CrouchHeightFactor = crouchHeightFactor;
        EnemyKnockbackSpeed = enemyKnockbackSpeed;
        ContactInvulnTicks = contactInvulnTicks;
    }

    /// <summary>Ticks the lute-swing hitbox stays live after a Yellow press (reference attack_timer = 12).</summary>
    public int SwingActiveTicks { get; }

    /// <summary>Upward launch speed of the tilt super-jump in px/tick (reference SUPER_JUMP_V = 21, ~5× a normal jump).</summary>
    public float SuperJumpVelocity { get; }

    /// <summary>Ticks the super-jump floats uncut so it reaches full height (reference SUPER_LAUNCH = 50; rule 4).</summary>
    public int SuperJumpLaunchTicks { get; }

    /// <summary>Cooldown ticks before the super-jump can fire again (reference SUPER_CD = 90, ~1.5 s @ 60 Hz).</summary>
    public int SuperJumpCooldownTicks { get; }

    /// <summary>Horizontal speed (px/tick) a crouch must carry to trigger a slide (reference 1.5).</summary>
    public float SlideSpeedThreshold { get; }

    /// <summary>Horizontal speed (px/tick) at or below which a committed slide ends and Wixx settles into a crouch.</summary>
    public float SlideStopSpeed { get; }

    /// <summary>Crouch collision height as a fraction of standing height — how low a slide ducks
    /// (reference body shrinks ~30→16, ~0.5).</summary>
    public float CrouchHeightFactor { get; }

    /// <summary>Horizontal speed (px/tick) an enemy contact pushes Wixx back (light feedback, not a damage system).</summary>
    public float EnemyKnockbackSpeed { get; }

    /// <summary>Ticks of contact-immunity after an enemy knockback so a single touch can't repeat-stunlock.</summary>
    public int ContactInvulnTicks { get; }
}
