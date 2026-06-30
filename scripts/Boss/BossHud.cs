using Godot;
using WixxTheBard.Performance;

namespace WixxTheBard.Boss;

/// <summary>
/// The Rock Off duel HUD (SPEC §6) — pure presentation, kept as its own CanvasLayer
/// rather than folded into M4's <see cref="PerformanceHud"/> (CLAUDE.md rule 8 — a
/// new milestone doesn't refactor an existing one's file just to share some
/// lane-drawing code). It only ever reads the authoritative
/// <see cref="Player.BossFight"/>/<see cref="Player.ShowingBossResult"/> state
/// (rule 7): the phase name/number, the Resolve meter, the boss's cumulative
/// Highway accuracy as an HP bar, the Call &amp; Response telegraph preview, the
/// live highway while performing, and the win/lose banner.
/// </summary>
public partial class BossHud : Control
{
    private const float StrikeX = 96f;
    private const float PxPerMs = 0.16f;
    private const float LaneHeight = 16f;
    private const float LaneGap = 4f;
    private const float TopY = 64f;
    private const float NoteW = 14f;

    private static readonly Color[] LaneColors =
    {
        new("3fd17a"), // Green
        new("e0484d"), // Red
        new("ffcf4d"), // Yellow
        new("3aa0ff"), // Blue
        new("ff7a3a"), // Orange
    };

    private Player? _player;
    private Font _font = null!;

    public override void _Ready()
    {
        _font = ThemeDB.FallbackFont;
        MouseFilter = MouseFilterEnum.Ignore;
        SetAnchorsPreset(LayoutPreset.FullRect);
    }

    public override void _Process(double delta)
    {
        _player ??= GetTree().GetFirstNodeInGroup("player") as Player;
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (_player == null)
        {
            return;
        }

        if (_player.IsInBossFight)
        {
            DrawStatusBar();
            DrawCurrentPhase();
        }

        if (_player.ShowingBossResult)
        {
            DrawBanner();
        }
    }

    private void DrawStatusBar()
    {
        BossFight fight = _player!.BossFight;
        string phaseLabel = $"{BossCharts.ChoirbreakerName} — phase {fight.PhaseNumber}/{fight.PhaseCount}: {fight.CurrentPhase?.Name}";
        DrawString(_font, new Vector2(StrikeX, 18f), phaseLabel, HorizontalAlignment.Left, -1, 14, new Color("ffd7ea"));

        // Boss "HP" — cumulative Highway accuracy against the shared success threshold (rule 1: no new number).
        const float hpWidth = 160f;
        float damageDealt = Mathf.Clamp((float)(1.0 - fight.BossAccuracy), 0f, 1f);
        DrawRect(new Rect2(StrikeX, 4f, hpWidth, 8f), new Color(1f, 1f, 1f, 0.12f));
        DrawRect(new Rect2(StrikeX, 4f, hpWidth * (1f - damageDealt), 8f), new Color("ff5c6a"));

        // Resolve pips — Wixx's risk in this duel (SPEC §6 "miss lets the boss land a hit").
        float pipX = StrikeX + hpWidth + 16f;
        for (int i = 0; i < fight.ResolveMax; i++)
        {
            Color c = i < fight.Resolve ? new Color("8ff0a4") : new Color(1f, 1f, 1f, 0.15f);
            DrawRect(new Rect2(pipX + i * 12f, 4f, 8f, 8f), c);
        }
    }

    private void DrawCurrentPhase()
    {
        BossFight fight = _player!.BossFight;

        NoteChart? telegraphChart = fight.TelegraphChart;
        if (fight.Stage == BossStage.Telegraph && telegraphChart != null)
        {
            DrawHighway(telegraphChart, fight.TelegraphMs, judge: null, goodWindowMs: 0.0, "BOSS'S TURN — watch the phrase", new Color("ff9a3a"));
            return;
        }

        SpellPerformance? perf = fight.Performance;
        NoteChart? chart = perf?.Chart;
        if (perf != null && chart != null)
        {
            DrawHighway(chart, perf.ChartMs, perf.Judge, perf.GoodWindowMs, "YOUR TURN — strum + fret it back", new Color("8fe0ff"));
        }
    }

    private void DrawHighway(NoteChart chart, double chartMs, PerformanceJudge? judge, double goodWindowMs, string label, Color labelColor)
    {
        float width = GetViewportRect().Size.X;
        float laneSpan = NoteLanes.Count * LaneHeight + (NoteLanes.Count - 1) * LaneGap;

        float goodHalf = (float)goodWindowMs * PxPerMs;
        if (goodHalf > 0f)
        {
            DrawRect(new Rect2(StrikeX - goodHalf, TopY - 6f, goodHalf * 2f, laneSpan + 12f), new Color(1f, 1f, 1f, 0.06f));
        }

        for (int i = 0; i < NoteLanes.Count; i++)
        {
            float y = TopY + i * (LaneHeight + LaneGap);
            DrawRect(new Rect2(StrikeX, y, width - StrikeX - 16f, LaneHeight), new Color(LaneColors[i], 0.10f));
            DrawRect(new Rect2(StrikeX - 3f, y, 3f, LaneHeight), new Color(LaneColors[i], 0.8f));
        }

        DrawLine(new Vector2(StrikeX, TopY - 6f), new Vector2(StrikeX, TopY + laneSpan + 6f), new Color("e6edf5"), 2f);

        for (int i = 0; i < chart.Notes.Count; i++)
        {
            Note note = chart.Notes[i];
            int lane = (int)note.Lane;
            if (lane < 0 || lane >= NoteLanes.Count)
            {
                continue;
            }

            float x = StrikeX + (float)(note.TargetMs - chartMs) * PxPerMs;
            if (x < -NoteW || x > width)
            {
                continue;
            }

            float y = TopY + lane * (LaneHeight + LaneGap);
            Color c = LaneColors[lane];
            Judgment? verdict = judge?.JudgmentOf(i);
            float alpha = 1f;
            if (verdict == Judgment.Perfect)
            {
                c = c.Lightened(0.35f);
            }
            else if (verdict == Judgment.Good)
            {
                c = c.Darkened(0.2f);
            }
            else if (verdict == Judgment.Miss)
            {
                c = new Color("5a2730");
                alpha = 0.7f;
            }

            DrawRect(new Rect2(x - NoteW * 0.5f, y, NoteW, LaneHeight), new Color(c, alpha));
        }

        DrawString(_font, new Vector2(StrikeX, TopY - 14f), $"♪ {label}", HorizontalAlignment.Left, -1, 13, labelColor);
    }

    private void DrawBanner()
    {
        bool victory = _player!.BossVictory;
        string text = victory
            ? $"{BossCharts.ChoirbreakerName} SILENCED — the song returns"
            : $"{BossCharts.ChoirbreakerName} — ROCK OFF LOST, try again";
        Color col = victory ? new Color("8ff0a4") : new Color("ff8f8f");

        var pos = new Vector2(StrikeX, TopY + NoteLanes.Count * (LaneHeight + LaneGap) + 22f);
        DrawString(_font, pos, text, HorizontalAlignment.Left, -1, 20, col);
    }
}
