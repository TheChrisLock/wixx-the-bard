using Godot;
using WixxTheBard.Controls;
using WixxTheBard.Movement;
using WixxTheBard.Verbs;

namespace WixxTheBard;

/// <summary>
/// Wixx's <see cref="CharacterBody2D"/>. M2 gave it the Hold-scheme movement core;
/// M3 adds the verbs (SPEC §2.3/§2.4/§4.4): the Yellow lute swing + basic enemies,
/// the tilt super-jump, the whammy crouch/slide, and the tar struggle hazard.
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

    private MovementTunables _moveTunables = null!;
    private VerbTunables _verbTunables = null!;
    private TarTunables _tarTunables = null!;
    private float _ticksPerSecond;

    // Authoritative verb state visuals read (rule 7).
    private bool _crouching;
    private bool _sliding;

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
        _ticksPerSecond = (float)Engine.PhysicsTicksPerSecond;

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

    public override void _PhysicsProcess(double delta)
    {
        var input = GuitarInput.Instance;

        // Cooldowns advance every tick regardless of mode.
        _superJump.Tick();
        _contactInvuln.Tick();

        if (_tar.Submerged)
        {
            PhysicsTickTar(input);
            return;
        }

        bool onFloor = IsOnFloor();

        // --- Whammy crouch / slide (resolved before the core tick: the slide latch
        //     reads last tick's speed and feeds the core, which then decays it). A
        //     committed slide glides to a stop on SlideFriction even with the strum
        //     held; a settled crouch ducks in place. ---
        bool crouchEngaged = input?.IsPressed(GuitarVerb.Crouch) ?? false;
        _crouching = CrouchState.Evaluate(crouchEngaged, onFloor, _core.VelocityX, _verbTunables).Crouching;
        _sliding = _slide.Tick(crouchEngaged, onFloor, _core.VelocityX, _verbTunables);

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
        if (_tar.Submerged)
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
