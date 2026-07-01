using Godot;
using WixxTheBard.Boss;
using WixxTheBard.Controls;
using WixxTheBard.Movement;
using WixxTheBard.Performance;
using WixxTheBard.Verbs;

namespace WixxTheBard;

/// <summary>
/// Wixx's <see cref="CharacterBody2D"/>. M2 gave it the Hold-scheme movement core;
/// M3 adds the verbs (SPEC §2.3/§2.4/§4.4): the Yellow lute swing + basic enemies,
/// the tilt super-jump, the whammy crouch/slide, and the tar struggle hazard. M5
/// adds the Rock Off boss duel (SPEC §6): Wixx stands his ground and the strum/frets
/// are handed entirely to <see cref="WixxTheBard.Boss.BossFight"/> for the duel,
/// the same "exactly one strum consumer" discipline (rule 5) M4's spell performance
/// already uses.
///
/// All four are driven from the data-driven <see cref="GuitarInput"/> verbs — never
/// raw indices (CLAUDE.md rule 2) — and from the pure, authoritative gameplay
/// modules in <c>WixxTheBard.Verbs</c> (rule 7: state drives, visuals only read).
/// Every number comes from <see cref="Tunables"/> (rule 1), and the whole step runs
/// in <see cref="_PhysicsProcess"/> at the fixed 60 Hz tick (rule 3). The super-jump
/// and tar-exit leap go through <see cref="MovementCore.ForcedLaunch"/> so they
/// inherit the jump-cut exemption (rule 4) and reach full height.
/// </summary>
public partial class Player : CharacterBody2D
{
    [Export] public Tunables Tunables { get; set; } = null!;

    private readonly MovementCore _core = new();
    private readonly SwingController _swing = new();
    private readonly SuperJumpController _superJump = new();
    private readonly SlideController _slide = new();
    private readonly TarState _tar = new();
    private readonly Cooldown _contactInvuln = new();
    private readonly SpellPerformance _performance = new();
    private readonly Cooldown _spellCooldown = new();
    private readonly BossFight _bossFight = new();

    private MovementTunables _moveTunables = null!;
    private VerbTunables _verbTunables = null!;
    private TarTunables _tarTunables = null!;
    private PerformanceTunables _perfTunables = null!;
    private BossTunables _bossTunables = null!;
    private NoteChart _spellChart = null!;
    private float _ticksPerSecond;
    private double _msPerTick;

    // Authoritative verb state visuals read (rule 7).
    private bool _crouching;
    private bool _sliding;

    // Spell-performance state (M4): the world-slow budget and the result banner hold.
    private double _worldTickBudget;
    private PerformanceResult _lastResult;
    private int _resultBannerTicks;

    // Rock Off state (M5): persists across the Idle/Telegraph/.../Victory cycle —
    // once defeated, the Choirbreaker stays defeated (rule 7's authoritative source,
    // not the visual Boss node) — plus the result banner hold and the arena's entry
    // point for a defeat respawn (mirrors the tar hazard's "respawn at the entry edge").
    private bool _bossDefeated;
    private bool _bossVictory;
    private int _bossResultBannerTicks;
    private Vector2 _bossArenaEntry;

    // Scene wiring (combat areas + the shape that shrinks on a crouch).
    private CollisionShape2D _bodyShape = null!;
    private Control? _box;
    private Area2D? _swingArea;
    private Area2D? _bodyArea;
    private float _swingReachX;
    private float _swingAreaY;
    private Vector2 _standShapeSize;
    private Vector2 _standShapePos;

    // Tar context for the current pit (set on entry by TarPit).
    private float _tarSurfaceY;
    private float _tarLeftX;
    private float _tarRightX;

    public override void _Ready()
    {
        Tunables ??= GD.Load<Tunables>("res://config/Tunables.tres");
        _moveTunables = Tunables.BuildMovementTunables();
        _verbTunables = Tunables.BuildVerbTunables();
        _tarTunables = Tunables.BuildTarTunables();
        _perfTunables = Tunables.BuildPerformanceTunables();
        _bossTunables = Tunables.BuildBossTunables();
        _spellChart = SpellCharts.Kindle();
        _ticksPerSecond = (float)Engine.PhysicsTicksPerSecond;
        _msPerTick = 1000.0 / Engine.PhysicsTicksPerSecond;

        // The HUD highway and desaturation overlay locate Wixx by group (rule 7 — they
        // observe this authoritative state, never drive it).
        AddToGroup("player");

        _bodyShape = GetNode<CollisionShape2D>("CollisionShape2D");
        if (_bodyShape.Shape is RectangleShape2D rect)
        {
            _standShapeSize = rect.Size;
        }

        _standShapePos = _bodyShape.Position;
        _box = GetNodeOrNull<Control>("Box");
        _swingArea = GetNodeOrNull<Area2D>("SwingArea");
        _bodyArea = GetNodeOrNull<Area2D>("BodyArea");

        if (_swingArea != null)
        {
            // Keep the configured reach as level geometry; we only flip its sign by facing.
            _swingReachX = Mathf.Abs(_swingArea.Position.X);
            _swingAreaY = _swingArea.Position.Y;
        }
    }

    // ---- Spell-performance state the HUD / desaturation overlay read (rule 7) ----

    /// <summary>True while a spell performance is on screen (SPEC §5.2).</summary>
    public bool IsPerforming => _performance.IsActive;

    /// <summary>The authoritative performance state machine the highway HUD draws from.</summary>
    public SpellPerformance Performance => _performance;

    /// <summary>Lute-glow recharge in [0,1] after a performance — 1 = the spell is ready (SPEC §5.4).</summary>
    public float SpellChargeFraction => _spellCooldown.Fraction(_perfTunables.SpellCooldownTicks);

    /// <summary>True while the success/fail banner should show after a performance.</summary>
    public bool ShowingResult => _resultBannerTicks > 0;

    /// <summary>The most recent performance tally — valid while <see cref="ShowingResult"/> (SPEC §5.4).</summary>
    public PerformanceResult LastResult => _lastResult;

    /// <summary>The equipped ability's name, for the HUD (SPEC §7).</summary>
    public string SpellName => _spellChart?.Name ?? string.Empty;

    // ---- Rock Off state the BossHud / Boss visual read (rule 7) ----

    /// <summary>True while a Rock Off is running and owns the strum (SPEC §6).</summary>
    public bool IsInBossFight => _bossFight.IsActive;

    /// <summary>The authoritative duel state the boss HUD/visual draw from.</summary>
    public BossFight BossFight => _bossFight;

    /// <summary>True once the Choirbreaker has been beaten — persists past any single fight (rule 7).</summary>
    public bool BossDefeated => _bossDefeated;

    /// <summary>True while the win/lose banner should show after a Rock Off ends.</summary>
    public bool ShowingBossResult => _bossResultBannerTicks > 0;

    /// <summary>The outcome of the most recently finished Rock Off, valid while <see cref="ShowingBossResult"/>.</summary>
    public bool BossVictory => _bossVictory;

    /// <summary>
    /// Begin the Rock Off (SPEC §6). Called by <see cref="WixxTheBard.Boss.BossArena"/>
    /// on arena entry; ignored if the Choirbreaker is already defeated or another mode
    /// already owns the strum (rule 5). <paramref name="arenaEntry"/> is where a defeat
    /// respawns Wixx, mirroring the tar hazard's "respawn at the entry edge".
    /// </summary>
    public void EnterBossFight(Vector2 arenaEntry)
    {
        if (_bossDefeated || _bossFight.IsActive || _performance.IsActive || _tar.Submerged)
        {
            return;
        }

        _bossArenaEntry = arenaEntry;
        _core.ZeroVelocity();
        _crouching = false;
        _sliding = false;
        ApplyCrouchShape(false);
        Velocity = Vector2.Zero;
        _bossFight.Begin(BossCharts.Choirbreaker(), GuitarInput.Instance?.LatencyOffsetMs ?? 0.0, _bossTunables, _perfTunables);
    }

    public override void _PhysicsProcess(double delta)
    {
        var input = GuitarInput.Instance;

        // Cooldowns advance every tick regardless of mode.
        _superJump.Tick();
        _contactInvuln.Tick();
        _spellCooldown.Tick();
        if (_resultBannerTicks > 0)
        {
            _resultBannerTicks--;
        }

        if (_bossResultBannerTicks > 0)
        {
            _bossResultBannerTicks--;
        }

        // Spell performance owns the strum while active (rule 5) — movement is suspended.
        if (_performance.IsActive)
        {
            PhysicsTickPerformance(input);
            return;
        }

        if (_tar.Submerged)
        {
            PhysicsTickTar(input);
            return;
        }

        if (_bossFight.IsActive)
        {
            PhysicsTickBossFight(input);
            return;
        }

        bool onFloor = IsOnFloor();

        // --- Spell trigger (Blue / Special1, SPEC §5.1): grounded + off-cooldown drops
        //     the world into the performance. The strum becomes "play" mode for the
        //     duration (rule 5); Wixx settles and is handed to the rhythm loop. ---
        if ((input?.JustPressed(GuitarVerb.Special1) ?? false) && onFloor && _spellCooldown.IsReady)
        {
            _performance.Begin(_spellChart, input?.LatencyOffsetMs ?? 0.0, _perfTunables);
            _core.ZeroVelocity();
            _crouching = false;
            _sliding = false;
            _worldTickBudget = 0.0;
            ApplyCrouchShape(false);
            Velocity = Vector2.Zero;
            return;
        }

        // --- Whammy crouch / slide (resolved before the core tick: the slide latch
        //     reads last tick's speed and feeds the core, which then decays it). A
        //     slide only triggers while Sprint is held — a crouch-walk alone can't
        //     slide. A committed slide glides to a stop on SlideFriction even with the
        //     strum held; a settled crouch still moves, but only at a slow crouch-walk. ---
        bool crouchEngaged = input?.IsPressed(GuitarVerb.Crouch) ?? false;
        bool sprintEngaged = input?.IsPressed(GuitarVerb.Sprint) ?? false;
        _crouching = CrouchState.Evaluate(crouchEngaged, onFloor, _core.VelocityX, _verbTunables).Crouching;
        _sliding = _slide.Tick(crouchEngaged, sprintEngaged, onFloor, _core.VelocityX, _verbTunables);

        // --- Lute swing (Yellow): opens a short active hitbox window. ---
        _swing.Tick(
            input?.JustPressed(GuitarVerb.Swing) ?? false,
            _core.Facing,
            suppressed: false,
            _verbTunables);

        // --- Core movement (Hold scheme, variable jump, sprint; crouch/slide override). ---
        _core.Tick(BuildInput(input, onFloor, _crouching, _sliding), _moveTunables);

        // --- Tilt super-jump: level-fire + cooldown + on-floor → forced (uncut) launch. ---
        bool tiltEngaged = input?.IsPressed(GuitarVerb.SuperJump) ?? false;
        if (_superJump.TryFire(tiltEngaged, onFloor, _verbTunables))
        {
            _core.ForcedLaunch(_verbTunables.SuperJumpVelocity, _verbTunables.SuperJumpLaunchTicks);
        }

        ApplyCrouchShape(_crouching);

        // Core works in px/tick; MoveAndSlide integrates px/second * delta (60 Hz → 1:1).
        Velocity = new Vector2(_core.VelocityX, _core.VelocityY) * _ticksPerSecond;
        MoveAndSlide();

        ResolveCombat();
    }

    /// <summary>
    /// A tick of the spell performance (SPEC §5.2). The note clock advances at real
    /// time so the song keeps tempo and input stays 60 Hz-sampled (rule 3); the strum
    /// is consumed here as a strike and the held fret picks the lane — read through
    /// data-driven verbs only (rule 2) and as the sole strum consumer (rule 5). The
    /// world creeps on the <see cref="PerformanceTunables.TimeSlowFactor"/> budget
    /// (SPEC §5.2 global slow), which any world entity reads (rule 7).
    /// </summary>
    private void PhysicsTickPerformance(GuitarInput? input)
    {
        bool strumEdge =
            (input?.JustPressed(GuitarVerb.MoveLeft) ?? false) ||
            (input?.JustPressed(GuitarVerb.MoveRight) ?? false);
        NoteLane heldLane = ResolveHeldLane(input);

        PerformanceTick tick = _performance.Tick(_msPerTick, strumEdge, heldLane);

        // Wixx's own physics advance on the slowed budget — he hangs in slow motion,
        // the visible side of the global time-slow. No directional/jump input flows in
        // (the strum is "play" mode), so he just settles and falls slowly.
        _worldTickBudget += _perfTunables.TimeSlowFactor;
        if (_worldTickBudget >= 1.0)
        {
            _worldTickBudget -= 1.0;
            _core.Tick(BuildInput(null, IsOnFloor(), crouching: false, sliding: false), _moveTunables);
            Velocity = new Vector2(_core.VelocityX, _core.VelocityY) * _ticksPerSecond;
            MoveAndSlide();
        }
        else
        {
            Velocity = Vector2.Zero;
        }

        if (tick.Completed)
        {
            ResolvePerformance(tick.Result);
        }
    }

    /// <summary>
    /// Resolve a finished performance (SPEC §5.4): the spell cooldown begins whether the
    /// player succeeded or failed; only a success fires the spell. A perfect run flags
    /// the empowered stretch tier for the banner (§5.4) — the bigger AoE is phase-2.
    /// </summary>
    private void ResolvePerformance(PerformanceResult result)
    {
        _spellCooldown.Start(_perfTunables.SpellCooldownTicks);
        _lastResult = result;
        _resultBannerTicks = _perfTunables.ResultBannerTicks;
        if (result.Success)
        {
            FireSpell();
        }

        _worldTickBudget = 0.0;
        ApplyCrouchShape(false);
    }

    /// <summary>
    /// The spell's effect — grey-box (art deferred post-M6, SPEC §8): a chord-blast that
    /// dispatches the enemies on screen. Enough to show "success fires" for the slice.
    /// </summary>
    private void FireSpell()
    {
        foreach (Node node in GetTree().GetNodesInGroup("enemies"))
        {
            if (node is Enemy enemy)
            {
                enemy.Defeat();
            }
        }
    }

    /// <summary>
    /// Which fret lane is currently held, read through the data-driven verbs (rule 2).
    /// Lane order resolves ties; tier-1 single-note charts hold one fret at a time.
    /// </summary>
    private static NoteLane ResolveHeldLane(GuitarInput? input)
    {
        if (input == null)
        {
            return NoteLane.None;
        }

        foreach (NoteLane lane in NoteLanes.All)
        {
            if (input.IsPressed(NoteLanes.ToVerb(lane)))
            {
                return lane;
            }
        }

        return NoteLane.None;
    }

    /// <summary>
    /// A tick of the Rock Off (SPEC §6). Wixx stands his ground for the duel — frozen
    /// like a spell performance, but at full real time: the global time-slow is a
    /// spell-only effect (SPEC §5.2), not part of a boss duel. The strum/frets are the
    /// <see cref="BossFight"/>'s alone for the duration (rule 5), read through the same
    /// data-driven verbs as everywhere else (rule 2).
    /// </summary>
    private void PhysicsTickBossFight(GuitarInput? input)
    {
        bool strumEdge =
            (input?.JustPressed(GuitarVerb.MoveLeft) ?? false) ||
            (input?.JustPressed(GuitarVerb.MoveRight) ?? false);
        NoteLane heldLane = ResolveHeldLane(input);

        BossFightTick tick = _bossFight.Tick(_msPerTick, strumEdge, heldLane);

        Velocity = Vector2.Zero;
        MoveAndSlide();

        if (tick.FightCompleted)
        {
            ResolveBossFight(tick.Victory);
        }
    }

    /// <summary>
    /// Resolve a finished Rock Off (SPEC §6): a win permanently defeats the
    /// Choirbreaker (rule 7 — the authoritative flag lives here, not on the visual
    /// Boss node); a loss respawns Wixx at the arena's entry point, same "unforgiving
    /// on purpose" flavour as a tar drowning (SPEC §4.4), and the duel can be retried
    /// by walking back in.
    /// </summary>
    private void ResolveBossFight(bool victory)
    {
        _bossVictory = victory;
        _bossResultBannerTicks = _perfTunables.ResultBannerTicks;

        if (victory)
        {
            _bossDefeated = true;
        }
        else
        {
            RespawnFromBossFight();
        }

        _bossFight.Reset();
    }

    private void RespawnFromBossFight()
    {
        _core.Reset();
        _swing.Reset();
        _superJump.Reset();
        _slide.Reset();
        _contactInvuln.Reset();
        GlobalPosition = _bossArenaEntry;
        Velocity = Vector2.Zero;
    }

    /// <summary>
    /// A tick of the tar struggle (SPEC §4.4): <c>MoveAndSlide</c> is suspended and
    /// Wixx's position is driven from the authoritative <see cref="TarState"/> depth
    /// (rule 7). The strum is consumed here as alternating <i>edges</i> — and only
    /// here — honouring strum-mode discipline (rule 5).
    /// </summary>
    private void PhysicsTickTar(GuitarInput? input)
    {
        bool kickUp = input?.JustPressed(GuitarVerb.MoveLeft) ?? false;   // strum up
        bool kickDown = input?.JustPressed(GuitarVerb.MoveRight) ?? false; // strum down

        TarStep step = _tar.Tick(kickUp, kickDown, _tarTunables);

        GlobalPosition = new Vector2(GlobalPosition.X + step.ForwardStep, _tarSurfaceY + step.Depth);
        Velocity = Vector2.Zero;
        _crouching = false;
        _sliding = false;
        ApplyCrouchShape(false);

        switch (step.Outcome)
        {
            case TarOutcome.Breached:
                ExitTarLeap();
                break;

            case TarOutcome.Drowned:
                RespawnFromTar();
                break;

            default:
                // Safety: the forward nudges carried him clear of the pit's far edge —
                // leap out anyway (SPEC §4.4 "waded clear"). Only the forward edge is
                // checked (by facing), so the entry lip can never false-trigger this.
                bool wadedOut =
                    (_tar.Facing > 0 && GlobalPosition.X >= _tarRightX) ||
                    (_tar.Facing < 0 && GlobalPosition.X <= _tarLeftX);
                if (wadedOut)
                {
                    ExitTarLeap();
                }

                break;
        }
    }

    /// <summary>
    /// Begin the tar struggle. Called by <see cref="TarPit"/> on body entry; ignored
    /// if already submerged. Facing-of-travel sets the forward carry and breach
    /// direction. The drop-in depth comes from <see cref="TarTunables"/>.
    /// </summary>
    public void EnterTar(float surfaceY, float leftX, float rightX)
    {
        if (_tar.Submerged || _bossFight.IsActive)
        {
            return;
        }

        _tarSurfaceY = surfaceY;
        _tarLeftX = leftX;
        _tarRightX = rightX;

        int facing = _core.VelocityX >= 0f ? 1 : -1;
        _core.ZeroVelocity();
        _slide.Reset();
        _tar.Enter(facing, _tarTunables);
        GlobalPosition = new Vector2(GlobalPosition.X, surfaceY + _tar.Depth);
        Velocity = Vector2.Zero;
    }

    private void ExitTarLeap()
    {
        int facing = _tar.Facing;
        _tar.Reset();
        _core.ZeroVelocity();
        // Vertical via ForcedLaunch (uncut, rule 4); horizontal carry alongside it.
        _core.ForcedLaunch(_tarTunables.ExitJumpVelocity, _tarTunables.ExitLaunchTicks);
        _core.ApplyHorizontalImpulse(facing * _tarTunables.ExitForwardVelocity, facing);
    }

    private void RespawnFromTar()
    {
        // Respawn on solid ground just clear of the pit, on the side Wixx entered
        // from (SPEC §4.4 — "respawn at the entry edge"). A full body-width margin
        // keeps him off the lip so he doesn't fall straight back in; the margin is
        // collision geometry, not a gameplay-feel number.
        int facing = _tar.Facing;
        float margin = _standShapeSize.X;
        float x = facing > 0 ? _tarLeftX - margin : _tarRightX + margin;

        _tar.Reset();
        _core.Reset();
        _swing.Reset();
        _superJump.Reset();
        _slide.Reset();
        _contactInvuln.Reset();
        GlobalPosition = new Vector2(x, _tarSurfaceY);
        Velocity = Vector2.Zero;
    }

    private void ResolveCombat()
    {
        // The swing hitbox leads with Wixx's facing — geometry flips, no new numbers.
        if (_swingArea != null)
        {
            _swingArea.Position = new Vector2(_core.Facing * _swingReachX, _swingAreaY);

            if (_swing.IsActive)
            {
                foreach (Node2D body in _swingArea.GetOverlappingBodies())
                {
                    if (body is Enemy enemy)
                    {
                        enemy.Defeat();
                    }
                }
            }
        }

        if (_bodyArea == null)
        {
            return;
        }

        foreach (Node2D body in _bodyArea.GetOverlappingBodies())
        {
            if (body is not Enemy enemy || enemy.Defeated)
            {
                continue;
            }

            if (_sliding)
            {
                enemy.Defeat(); // slide knockdown (SPEC §2.4)
            }
            else if (_contactInvuln.IsReady)
            {
                int away = GlobalPosition.X >= enemy.GlobalPosition.X ? 1 : -1;
                _core.ApplyHorizontalImpulse(away * _verbTunables.EnemyKnockbackSpeed, away);
                _contactInvuln.Start(_verbTunables.ContactInvulnTicks);
            }
        }
    }

    private void ApplyCrouchShape(bool crouching)
    {
        if (_bodyShape.Shape is not RectangleShape2D rect)
        {
            return;
        }

        if (crouching)
        {
            float crouchHeight = _standShapeSize.Y * _verbTunables.CrouchHeightFactor;
            float drop = (_standShapeSize.Y - crouchHeight) * 0.5f; // keep the feet line fixed
            rect.Size = new Vector2(_standShapeSize.X, crouchHeight);
            _bodyShape.Position = new Vector2(_standShapePos.X, _standShapePos.Y + drop);
        }
        else
        {
            rect.Size = _standShapeSize;
            _bodyShape.Position = _standShapePos;
        }

        UpdateBoxVisual(crouching);
    }

    private void UpdateBoxVisual(bool crouching)
    {
        if (_box == null)
        {
            return;
        }

        // Visuals read gameplay state, never drive it (rule 7).
        Color tint =
            _performance.IsActive ? new Color("c84bd6") :
            _bossFight.IsActive ? new Color("ffae3a") :
            _tar.Submerged ? new Color("6b4a1c") :
            _sliding ? new Color("4de1ff") :
            crouching ? new Color("2f6fb0") :
            !_superJump.IsReady ? new Color("2a4a6a") :
            new Color("3aa0ff");

        float half = _standShapeSize.Y * 0.5f;
        if (_box is ColorRect rectBox)
        {
            rectBox.Color = tint;
            rectBox.OffsetTop = crouching ? 0f : -half;
            rectBox.OffsetBottom = half;
        }
        else
        {
            _box.Modulate = tint;
        }
    }

    private static MovementInput BuildInput(GuitarInput? input, bool onFloor, bool crouching, bool sliding)
    {
        if (input == null)
        {
            return new MovementInput(false, false, false, false, false, onFloor, crouching, sliding);
        }

        return new MovementInput(
            moveLeft: input.IsPressed(GuitarVerb.MoveLeft),
            moveRight: input.IsPressed(GuitarVerb.MoveRight),
            jumpHeld: input.IsPressed(GuitarVerb.Jump),
            jumpJustPressed: input.JustPressed(GuitarVerb.Jump),
            sprint: input.IsPressed(GuitarVerb.Sprint),
            onFloor: onFloor,
            crouching: crouching,
            sliding: sliding);
    }
}
