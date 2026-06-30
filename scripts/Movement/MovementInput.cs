namespace WixxTheBard.Movement;

/// <summary>
/// One fixed tick's worth of resolved movement intent, handed to
/// <see cref="MovementCore"/>. The fields are already verb-resolved (Hold scheme:
/// <see cref="MoveLeft"/>/<see cref="MoveRight"/> come from the strum), so the
/// pure physics never touches a raw button or axis index (CLAUDE.md rule 2) and
/// never references Godot — it runs under plain <c>dotnet test</c>.
/// </summary>
public readonly struct MovementInput
{
    public MovementInput(
        bool moveLeft,
        bool moveRight,
        bool jumpHeld,
        bool jumpJustPressed,
        bool sprint,
        bool onFloor,
        bool crouching = false,
        bool sliding = false)
    {
        MoveLeft = moveLeft;
        MoveRight = moveRight;
        JumpHeld = jumpHeld;
        JumpJustPressed = jumpJustPressed;
        Sprint = sprint;
        OnFloor = onFloor;
        Crouching = crouching;
        Sliding = sliding;
    }

    /// <summary>Hold strum up (Scheme B) — drive left this tick.</summary>
    public bool MoveLeft { get; }

    /// <summary>Hold strum down (Scheme B) — drive right this tick.</summary>
    public bool MoveRight { get; }

    /// <summary>Green fret currently held — sustains a button jump's rise (variable height).</summary>
    public bool JumpHeld { get; }

    /// <summary>Green fret pressed this tick — triggers the button jump off the floor.</summary>
    public bool JumpJustPressed { get; }

    /// <summary>Red fret held — builds sprint charge.</summary>
    public bool Sprint { get; }

    /// <summary>Grounded state from the previous <c>MoveAndSlide</c> (the Godot collision result).</summary>
    public bool OnFloor { get; }

    /// <summary>Whammy held while grounded — ducking. Suppresses the Hold-scheme run so you stop to crouch.</summary>
    public bool Crouching { get; }

    /// <summary>A committed slide is in progress — decay via <c>SlideFriction</c> to a stop, ignoring held strum.</summary>
    public bool Sliding { get; }
}
