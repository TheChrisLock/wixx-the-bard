using System.Collections.Generic;
using Godot;
using WixxTheBard.Controls;

namespace WixxTheBard;

/// <summary>
/// The data-driven guitar input layer (autoload singleton). Each fixed tick it
/// reads the connected joypad's <em>raw</em> button/axis state into a reusable
/// snapshot, resolves it against the active <see cref="BindingSet"/>, and exposes
/// per-verb pressed / just-pressed / just-released / amount to gameplay. Gameplay
/// asks for verbs, never indices (CLAUDE.md rule 2). All polling runs in
/// <see cref="_PhysicsProcess"/> at the fixed tick (rule 3); buffers are reused so
/// the hot path allocates nothing (rule 9).
///
/// It also owns press-to-bind capture, preset application, the stored A/V latency
/// offset, and persistence to <c>user://</c> — everything the Options screen drives.
/// A keyboard fallback is OR'd in so the game is playable without a guitar in dev
/// (the labelled standard-pad Accessibility Mode proper is phase-2 scope).
/// </summary>
public partial class GuitarInput : Node
{
    [Signal]
    public delegate void BindingChangedEventHandler(int verb);

    [Signal]
    public delegate void BindingsReloadedEventHandler();

    private const string SettingsPath = "user://wixx_input.json";

    public static GuitarInput? Instance { get; private set; }

    [Export] public Tunables Tunables { get; set; } = null!;

    private InputTuning _tuning = null!;
    private BindingSet _bindings = new();
    private InputResolver _resolver = null!;

    private bool[] _buttons = System.Array.Empty<bool>();
    private float[] _axes = System.Array.Empty<float>();

    private readonly bool[] _current = new bool[GuitarVerbs.Count];
    private readonly bool[] _previous = new bool[GuitarVerbs.Count];

    private int _device = -1;
    private BindingCapture? _capture;
    private GuitarVerb _captureVerb;

    public double LatencyOffsetMs { get; set; }

    public string ActivePresetName { get; private set; } = string.Empty;

    public BindingSet Bindings => _bindings;

    public InputTuning Tuning => _tuning;

    public int Device => _device;

    public bool HasDevice => _device >= 0;

    public bool IsCapturing => _capture != null && _capture.State != CaptureState.Captured;

    public CaptureState? CapturePhase => _capture?.State;

    public GuitarVerb CapturingVerb => _captureVerb;

    // Keyboard fallback action per verb (dev convenience; the guitar uses bindings).
    private static readonly Dictionary<GuitarVerb, string> KeyboardFallback = new()
    {
        [GuitarVerb.MoveLeft] = "move_left",
        [GuitarVerb.MoveRight] = "move_right",
        [GuitarVerb.Jump] = "jump",
        [GuitarVerb.Sprint] = "sprint",
        [GuitarVerb.Swing] = "swing",
        [GuitarVerb.Special1] = "special_1",
        [GuitarVerb.Special2] = "special_2",
        [GuitarVerb.Crouch] = "crouch",
        [GuitarVerb.SuperJump] = "super_jump",
    };

    public override void _Ready()
    {
        Instance = this;
        Tunables ??= GD.Load<Tunables>("res://config/Tunables.tres");

        _tuning = Tunables.BuildInputTuning();
        _buttons = new bool[_tuning.ButtonScanCount];
        _axes = new float[_tuning.AxisScanCount];
        _resolver = new InputResolver(_bindings, _tuning);
        LatencyOffsetMs = Tunables.DefaultLatencyOffsetMs;

        // Keep resolving through scene changes and any future time-slow/pause.
        ProcessMode = ProcessModeEnum.Always;

        LoadSettings();
    }

    public override void _ExitTree()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        RefreshDevice();
        BuildSnapshot();
        var snapshot = new InputSnapshot(_buttons, _axes);

        if (_capture != null)
        {
            _capture.Feed(snapshot);
            if (_capture.State == CaptureState.Captured && _capture.Result != null)
            {
                CompleteCapture(_capture.Result);
            }
        }

        _resolver.Update(snapshot);

        // Fold the keyboard fallback in and track combined edges here, so callers
        // get consistent just-pressed/-released whether the input was guitar or key.
        var verbs = GuitarVerbs.All;
        for (int i = 0; i < verbs.Length; i++)
        {
            _previous[i] = _current[i];
            _current[i] = _resolver.IsPressed(verbs[i]) || KeyboardPressed(verbs[i]);
        }
    }

    private void RefreshDevice()
    {
        var pads = Input.GetConnectedJoypads();
        _device = pads.Count > 0 ? pads[0] : -1;
    }

    private void BuildSnapshot()
    {
        if (_device >= 0)
        {
            for (int b = 0; b < _buttons.Length; b++)
            {
                _buttons[b] = Input.IsJoyButtonPressed(_device, (JoyButton)b);
            }

            for (int a = 0; a < _axes.Length; a++)
            {
                _axes[a] = Input.GetJoyAxis(_device, (JoyAxis)a);
            }
        }
        else
        {
            System.Array.Clear(_buttons, 0, _buttons.Length);
            System.Array.Clear(_axes, 0, _axes.Length);
        }
    }

    private static bool KeyboardPressed(GuitarVerb verb)
        => KeyboardFallback.TryGetValue(verb, out var action)
            && InputMap.HasAction(action)
            && Input.IsActionPressed(action);

    // ---- Verb queries (the gameplay-facing API) ----

    public bool IsPressed(GuitarVerb verb) => _current[(int)verb];

    public bool JustPressed(GuitarVerb verb) => _current[(int)verb] && !_previous[(int)verb];

    public bool JustReleased(GuitarVerb verb) => !_current[(int)verb] && _previous[(int)verb];

    /// <summary>Analog engagement 0..1 (guitar), or 1 when held on the keyboard fallback.</summary>
    public float Amount(GuitarVerb verb)
    {
        float guitar = _resolver.Amount(verb);
        return guitar > 0f ? guitar : (KeyboardPressed(verb) ? 1f : 0f);
    }

    // ---- Press-to-bind ----

    public void BeginCapture(GuitarVerb verb)
    {
        _captureVerb = verb;
        _capture = new BindingCapture(_tuning);
    }

    public void CancelCapture() => _capture = null;

    private void CompleteCapture(InputBinding binding)
    {
        // Tilt's polarity is unknowable per guitar (SPEC §14) — whichever direction the
        // player happened to tilt during capture must not become the only one that
        // fires later; lock it to engage either way from the learned rest.
        if (binding.Kind == BindingKind.Axis && _captureVerb == GuitarVerb.SuperJump)
        {
            binding = InputBinding.Axis(binding.Index, binding.RestValue, binding.Direction, bidirectional: true);
        }

        _bindings.Set(_captureVerb, binding);
        ActivePresetName = string.Empty; // bindings are now custom
        var verb = _captureVerb;
        _capture = null;
        SaveSettings();
        EmitSignal(SignalName.BindingChanged, (int)verb);
    }

    // ---- Presets ----

    public void ApplyPreset(string name)
    {
        var set = ControllerPresets.Build(name);
        if (set == null)
        {
            return;
        }

        _bindings = set;
        _resolver.Bindings = _bindings;
        ActivePresetName = name;
        SaveSettings();
        EmitSignal(SignalName.BindingsReloaded);
    }

    // ---- Persistence ----

    public void SaveSettings()
    {
        var settings = new InputSettings
        {
            Bindings = _bindings,
            LatencyOffsetMs = LatencyOffsetMs,
            PresetName = ActivePresetName,
        };

        string json = InputSettingsSerializer.Serialize(settings);
        using var file = FileAccess.Open(SettingsPath, FileAccess.ModeFlags.Write);
        if (file != null)
        {
            file.StoreString(json);
        }
        else
        {
            GD.PushWarning($"GuitarInput: could not write {SettingsPath}: {FileAccess.GetOpenError()}");
        }
    }

    private void LoadSettings()
    {
        if (!FileAccess.FileExists(SettingsPath))
        {
            ApplyPreset(ControllerPresets.DefaultPresetName); // first run — proven test-guitar map
            return;
        }

        using var file = FileAccess.Open(SettingsPath, FileAccess.ModeFlags.Read);
        if (file == null)
        {
            ApplyPreset(ControllerPresets.DefaultPresetName);
            return;
        }

        var settings = InputSettingsSerializer.Deserialize(file.GetAsText());
        _bindings = settings.Bindings;
        _resolver.Bindings = _bindings;
        LatencyOffsetMs = settings.LatencyOffsetMs;
        ActivePresetName = settings.PresetName;
        EmitSignal(SignalName.BindingsReloaded);
    }
}
