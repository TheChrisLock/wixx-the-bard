using Godot;

namespace WixxTheBard;

/// <summary>
/// Thin host controller for the hello-world scene: opens the Options screen on
/// <c>ui_cancel</c> (Esc / Start) so M1's remapper + calibration are reachable
/// from play. The <see cref="GuitarInput"/> autoload persists across the scene
/// change, so bindings stay live.
/// </summary>
public partial class Game : Node2D
{
    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed("ui_cancel"))
        {
            GetTree().ChangeSceneToFile("res://scenes/Options.tscn");
            GetViewport().SetInputAsHandled();
        }
    }
}
