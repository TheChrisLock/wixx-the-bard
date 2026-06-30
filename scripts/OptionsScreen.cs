using System.Collections.Generic;
using Godot;
using WixxTheBard.Controls;

namespace WixxTheBard;

/// <summary>
/// The single Options screen housing <em>both</em> control remapping and A/V
/// latency calibration (SPEC §10, §12). It is a thin view over
/// <see cref="GuitarInput"/>: press-to-bind every verb, apply common-controller
/// presets, watch a live input monitor (every verb lights when engaged — the
/// on-guitar verification surface), and calibrate latency by tapping a metronome.
/// Gameplay state is authoritative; this screen only reads it and drives the
/// binder/persistence (CLAUDE.md rule 7). UI is built in code so the scene file
/// stays a trivial host.
/// </summary>
public partial class OptionsScreen : Control
{
    private static readonly Color IdleColor = new("26344a");
    private static readonly Color EngagedColor = new("3ad17a");
    private static readonly Color BeatColor = new("ffcf4d");

    private readonly Dictionary<GuitarVerb, Label> _bindingLabels = new();
    private readonly Dictionary<GuitarVerb, ColorRect> _indicators = new();

    private readonly LatencyCalibration _calibration = new();

    private GuitarInput _input = null!;
    private Label _deviceLabel = null!;
    private Label _rebindStatus = null!;
    private OptionButton _presetPicker = null!;

    private ColorRect _beatPanel = null!;
    private Button _metronomeButton = null!;
    private Label _calibrationStatus = null!;
    private Label _offsetLabel = null!;
    private HSlider _offsetSlider = null!;
    private AudioStreamPlayer _click = null!;

    private bool _metronomeOn;
    private double _metronomeStartMs;
    private int _lastBeatIndex = -1;
    private float _beatFlash;
    private bool _syncingSlider;

    public override void _Ready()
    {
        _input = GuitarInput.Instance!;
        BuildUi();

        if (_input != null)
        {
            _input.BindingChanged += OnBindingChanged;
            _input.BindingsReloaded += RefreshAllBindings;
            _offsetSlider.Value = _input.LatencyOffsetMs;
            UpdateOffsetLabel(_input.LatencyOffsetMs);
        }

        RefreshAllBindings();
        SelectActivePreset();
    }

    public override void _ExitTree()
    {
        if (_input != null)
        {
            _input.BindingChanged -= OnBindingChanged;
            _input.BindingsReloaded -= RefreshAllBindings;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        // Tap capture runs on the fixed tick where verb edges are valid for exactly
        // one frame; offsets are measured against the same wall clock the metronome uses.
        if (!_metronomeOn || _input == null)
        {
            return;
        }

        if (_input.JustPressed(GuitarVerb.Jump)
            || _input.JustPressed(GuitarVerb.MoveLeft)
            || _input.JustPressed(GuitarVerb.MoveRight))
        {
            RecordTap();
        }
    }

    public override void _Process(double delta)
    {
        if (_input != null)
        {
            foreach (var verb in GuitarVerbs.All)
            {
                if (_indicators.TryGetValue(verb, out var rect))
                {
                    rect.Color = _input.IsPressed(verb) ? EngagedColor : IdleColor;
                }
            }

            _deviceLabel.Text = _input.HasDevice
                ? $"Guitar #{_input.Device} connected"
                : "No guitar detected — keyboard fallback active";
        }

        if (_metronomeOn && _input != null)
        {
            DriveMetronome(delta);
        }
    }

    // ---- Metronome / calibration ----

    private void DriveMetronome(double delta)
    {
        double beat = _input.Tunables.CalibrationBeatMs;
        double now = Time.GetTicksMsec();
        int beatIndex = (int)((now - _metronomeStartMs) / beat);
        if (beatIndex > _lastBeatIndex)
        {
            _lastBeatIndex = beatIndex;
            _beatFlash = 1f;
            _click?.Play();
        }

        _beatFlash = Mathf.Max(0f, _beatFlash - (float)delta * 5f);
        _beatPanel.Color = IdleColor.Lerp(BeatColor, _beatFlash);
    }

    private void RecordTap()
    {
        double beat = _input.Tunables.CalibrationBeatMs;
        double rel = Time.GetTicksMsec() - _metronomeStartMs;
        double phase = rel - Mathf.Floor(rel / beat) * beat; // 0..beat
        double offset = phase <= beat / 2.0 ? phase : phase - beat; // signed; negative = early
        _calibration.AddSample(offset);

        int target = _input.Tunables.CalibrationSampleTarget;
        double recommended = _calibration.RecommendedOffsetMs(_input.LatencyOffsetMs);
        _calibrationStatus.Text =
            $"Taps: {_calibration.SampleCount}/{target}   ·   last {offset:0} ms   ·   suggested {recommended:0} ms";
    }

    private void ToggleMetronome()
    {
        _metronomeOn = !_metronomeOn;
        if (_metronomeOn)
        {
            _metronomeStartMs = Time.GetTicksMsec();
            _lastBeatIndex = -1;
            _calibration.Reset();
            _metronomeButton.Text = "Stop metronome";
            _calibrationStatus.Text = "Tap (strum / Green / Space) on each beat…";
        }
        else
        {
            _metronomeButton.Text = "Start metronome";
            _beatPanel.Color = IdleColor;
        }
    }

    private void AcceptCalibration()
    {
        if (_input == null)
        {
            return;
        }

        double recommended = _calibration.RecommendedOffsetMs(_input.LatencyOffsetMs);
        _input.LatencyOffsetMs = recommended;
        _input.SaveSettings();
        _syncingSlider = true;
        _offsetSlider.Value = recommended;
        _syncingSlider = false;
        UpdateOffsetLabel(recommended);
        _calibrationStatus.Text = $"Saved offset: {recommended:0} ms";
    }

    private void OnSliderChanged(double value)
    {
        if (_syncingSlider || _input == null)
        {
            return;
        }

        _input.LatencyOffsetMs = value;
        _input.SaveSettings();
        UpdateOffsetLabel(value);
    }

    private void UpdateOffsetLabel(double ms) => _offsetLabel.Text = $"A/V offset: {ms:0} ms";

    // ---- Bindings ----

    private void OnBindingChanged(int verb)
    {
        RefreshBindingLabel((GuitarVerb)verb);
        _rebindStatus.Text = "Bound.";
        SelectActivePreset();
    }

    private void StartRebind(GuitarVerb verb)
    {
        _input?.BeginCapture(verb);
        _rebindStatus.Text = $"Press the control for {Friendly(verb)} … (hold still first)";
    }

    private void RefreshAllBindings()
    {
        foreach (var verb in GuitarVerbs.All)
        {
            RefreshBindingLabel(verb);
        }

        SelectActivePreset();
    }

    private void RefreshBindingLabel(GuitarVerb verb)
    {
        if (!_bindingLabels.TryGetValue(verb, out var label) || _input == null)
        {
            return;
        }

        label.Text = _input.Bindings.TryGet(verb, out var binding) ? binding.Describe() : "— unbound —";
    }

    private void OnPresetSelected(long index)
    {
        if (_input == null || index < 0 || index >= ControllerPresets.Names.Count)
        {
            return;
        }

        _input.ApplyPreset(ControllerPresets.Names[(int)index]);
        _rebindStatus.Text = $"Applied preset: {ControllerPresets.Names[(int)index]}";
    }

    private void SelectActivePreset()
    {
        if (_input == null)
        {
            return;
        }

        for (int i = 0; i < ControllerPresets.Names.Count; i++)
        {
            if (ControllerPresets.Names[i] == _input.ActivePresetName)
            {
                _presetPicker.Selected = i;
                return;
            }
        }

        // Custom bindings — show no preset selected.
        _presetPicker.Selected = -1;
    }

    // ---- UI construction ----

    private void BuildUi()
    {
        SetAnchorsPreset(LayoutPreset.FullRect);

        var bg = new ColorRect { Color = new Color("0e1116") };
        bg.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(bg);

        var margin = new MarginContainer();
        margin.SetAnchorsPreset(LayoutPreset.FullRect);
        margin.AddThemeConstantOverride("margin_left", 24);
        margin.AddThemeConstantOverride("margin_top", 16);
        margin.AddThemeConstantOverride("margin_right", 24);
        margin.AddThemeConstantOverride("margin_bottom", 16);
        AddChild(margin);

        var root = new VBoxContainer();
        root.AddThemeConstantOverride("separation", 6);
        margin.AddChild(root);

        root.AddChild(Heading("OPTIONS — Controls & A/V Calibration", 22));
        _deviceLabel = Dim("Detecting controller…");
        root.AddChild(_deviceLabel);

        // Presets
        root.AddChild(Heading("Controller preset", 16));
        var presetRow = new HBoxContainer();
        presetRow.AddThemeConstantOverride("separation", 8);
        _presetPicker = new OptionButton { CustomMinimumSize = new Vector2(360, 0) };
        foreach (var name in ControllerPresets.Names)
        {
            _presetPicker.AddItem(name);
        }

        _presetPicker.ItemSelected += OnPresetSelected;
        presetRow.AddChild(_presetPicker);
        root.AddChild(presetRow);

        // Per-verb rebind rows + live monitor
        root.AddChild(Heading("Bindings  (press Rebind, then press the control)", 16));
        foreach (var verb in GuitarVerbs.All)
        {
            root.AddChild(BuildVerbRow(verb));
        }

        _rebindStatus = Dim("Indicators light when a verb is engaged — check every one on the guitar.");
        root.AddChild(_rebindStatus);

        // Calibration
        root.AddChild(Heading("A/V latency calibration", 16));
        _beatPanel = new ColorRect { Color = IdleColor, CustomMinimumSize = new Vector2(0, 28) };
        root.AddChild(_beatPanel);

        var calRow = new HBoxContainer();
        calRow.AddThemeConstantOverride("separation", 8);
        _metronomeButton = new Button { Text = "Start metronome" };
        _metronomeButton.Pressed += ToggleMetronome;
        calRow.AddChild(_metronomeButton);

        var acceptButton = new Button { Text = "Accept suggested offset" };
        acceptButton.Pressed += AcceptCalibration;
        calRow.AddChild(acceptButton);
        root.AddChild(calRow);

        _calibrationStatus = Dim("Tap on the beat to measure your latency.");
        root.AddChild(_calibrationStatus);

        var sliderRow = new HBoxContainer();
        sliderRow.AddThemeConstantOverride("separation", 8);
        _offsetLabel = Dim("A/V offset: 0 ms");
        _offsetLabel.CustomMinimumSize = new Vector2(160, 0);
        sliderRow.AddChild(_offsetLabel);
        _offsetSlider = new HSlider
        {
            MinValue = -250,
            MaxValue = 250,
            Step = 1,
            CustomMinimumSize = new Vector2(360, 0),
            SizeFlagsVertical = SizeFlags.ShrinkCenter,
        };
        _offsetSlider.ValueChanged += OnSliderChanged;
        sliderRow.AddChild(_offsetSlider);
        root.AddChild(sliderRow);

        // Back
        var backButton = new Button { Text = "Back (Esc)" };
        backButton.Pressed += GoBack;
        root.AddChild(backButton);

        _click = new AudioStreamPlayer { Stream = MakeClick() };
        AddChild(_click);
    }

    private Control BuildVerbRow(GuitarVerb verb)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 8);

        var indicator = new ColorRect { Color = IdleColor, CustomMinimumSize = new Vector2(16, 16) };
        indicator.SizeFlagsVertical = SizeFlags.ShrinkCenter;
        _indicators[verb] = indicator;
        row.AddChild(indicator);

        var name = new Label { Text = Friendly(verb), CustomMinimumSize = new Vector2(150, 0) };
        row.AddChild(name);

        var bindingLabel = new Label { Text = "—", CustomMinimumSize = new Vector2(260, 0) };
        _bindingLabels[verb] = bindingLabel;
        row.AddChild(bindingLabel);

        var rebind = new Button { Text = "Rebind" };
        rebind.Pressed += () => StartRebind(verb);
        row.AddChild(rebind);

        return row;
    }

    private void GoBack() => GetTree().ChangeSceneToFile("res://scenes/Main.tscn");

    private static Label Heading(string text, int size)
    {
        var label = new Label { Text = text };
        label.AddThemeFontSizeOverride("font_size", size);
        label.AddThemeColorOverride("font_color", new Color("9fb3c8"));
        return label;
    }

    private static Label Dim(string text)
    {
        var label = new Label { Text = text };
        label.AddThemeColorOverride("font_color", new Color("6b7c92"));
        return label;
    }

    private static string Friendly(GuitarVerb verb) => verb switch
    {
        GuitarVerb.MoveLeft => "Move left (strum up)",
        GuitarVerb.MoveRight => "Move right (strum down)",
        GuitarVerb.Jump => "Jump (Green)",
        GuitarVerb.Sprint => "Sprint (Red)",
        GuitarVerb.Swing => "Lute swing (Yellow)",
        GuitarVerb.Special1 => "Special 1 (Blue)",
        GuitarVerb.Special2 => "Special 2 (Orange)",
        GuitarVerb.Crouch => "Crouch / slide (Whammy)",
        GuitarVerb.SuperJump => "Super jump (Tilt)",
        _ => verb.ToString(),
    };

    /// <summary>Synthesise a short metronome click so calibration measures audio + video + input.</summary>
    private static AudioStreamWav MakeClick()
    {
        const int rate = 44100;
        const double durationSec = 0.03;
        int frames = (int)(rate * durationSec);
        var data = new byte[frames * 2];
        for (int i = 0; i < frames; i++)
        {
            double t = i / (double)rate;
            double envelope = 1.0 - (i / (double)frames);
            double sample = Mathf.Sin((float)(2.0 * Mathf.Pi * 1000.0 * t)) * envelope * 0.6;
            short value = (short)(sample * short.MaxValue);
            data[i * 2] = (byte)(value & 0xff);
            data[i * 2 + 1] = (byte)((value >> 8) & 0xff);
        }

        return new AudioStreamWav
        {
            Format = AudioStreamWav.FormatEnum.Format16Bits,
            MixRate = rate,
            Stereo = false,
            Data = data,
        };
    }
}
