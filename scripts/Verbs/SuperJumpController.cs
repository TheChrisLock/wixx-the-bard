namespace WixxTheBard.Verbs;

/// <summary>
/// The tilt super-jump gate as pure, Godot-free logic (SPEC §2.4, §13 — "locked: a
/// cooldown"). Two findings from the validated prototype are baked in here:
/// <list type="bullet">
/// <item><b>Fire on tilt <i>level</i>, not a clean rising edge</b> — edge-detection
/// on a coarse analog sensor dropped inputs and read as unreliable (SPEC §14). So
/// <see cref="TryFire"/> takes the tilt's current <i>engaged</i> level, and the
/// cooldown (not an edge) is what prevents a held tilt from re-firing.</item>
/// <item><b>Cooldown-gated</b> so it can't trivialise platforming.</item>
/// </list>
/// The actual launch is delegated to <c>MovementCore.ForcedLaunch</c> so the jump is
/// exempt from the variable-height cut (CLAUDE.md rule 4) and reaches full height.
/// The tilt's per-guitar rest value and direction are resolved upstream by the M1
/// press-to-bind binder — this gate never touches a raw axis (rule 2).
/// </summary>
public sealed class SuperJumpController
{
    private readonly Cooldown _cooldown = new();

    /// <summary>Recharge progress in [0,1] for the dimming/refilling lute glow (1 = ready).</summary>
    public float ChargeFraction(VerbTunables t) => _cooldown.Fraction(t.SuperJumpCooldownTicks);

    /// <summary>True when the super-jump has recharged and may fire.</summary>
    public bool IsReady => _cooldown.IsReady;

    /// <summary>Advance the cooldown one fixed tick.</summary>
    public void Tick() => _cooldown.Tick();

    /// <summary>
    /// Decide whether the super-jump fires this tick. Fires when the tilt is engaged
    /// (level, not edge), Wixx is on the floor, and the cooldown is ready; firing
    /// starts the cooldown so a sustained tilt won't re-launch until it recharges and
    /// he is grounded again. Call <see cref="Tick"/> first so the cooldown is current.
    /// </summary>
    /// <returns><c>true</c> if the caller should issue a forced launch this tick.</returns>
    public bool TryFire(bool tiltEngaged, bool onFloor, VerbTunables t)
    {
        if (tiltEngaged && onFloor && _cooldown.IsReady)
        {
            _cooldown.Start(t.SuperJumpCooldownTicks);
            return true;
        }

        return false;
    }

    /// <summary>Clear the cooldown (respawn / scene reset).</summary>
    public void Reset() => _cooldown.Reset();
}
