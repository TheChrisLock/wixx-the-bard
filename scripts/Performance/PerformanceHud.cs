using Godot;

namespace WixxTheBard.Performance;

/// <summary>
/// The spell-performance note highway (SPEC §5.2) — pure presentation. It draws the
/// five lanes, the strike zone, and the notes scrolling <b>right → left</b> (mirrored
/// Guitar Hero), plus the success/fail banner. It only ever <i>reads</i> the
/// authoritative <see cref="Player"/> performance state (CLAUDE.md rule 7) — it never
/// drives a mechanic. Redrawing lives in <see cref="_Process"/>, which is fine for
/// visuals (rule 3 keeps only gameplay on the fixed tick).
///
/// All numbers here are <b>layout pixels</b>, not gameplay tunables — the rhythm
/// windows themselves come from <c>Tunables</c> via the performance state.
/// </summary>
public partial class PerformanceHud : Control
{
    private const float StrikeX = 96f;
    private const float PxPerMs = 0.16f;
    private const float LaneHeight = 16f;
    private const float LaneGap = 4f;
    private const float TopY = 44f;
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

        if (_player.IsPerforming)
        {
            DrawHighway(_player.Performance);
        }

        if (_player.ShowingResult)
        {
            DrawBanner();
        }
    }

    private void DrawHighway(SpellPerformance perf)
    {
        NoteChart? chart = perf.Chart;
        if (chart == null)
        {
            return;
        }

        float width = GetViewportRect().Size.X;
        float laneSpan = NoteLanes.Count * LaneHeight + (NoteLanes.Count - 1) * LaneGap;

        // Lane backgrounds + the strike zone (sized from the live Good window).
        float goodHalf = (float)perf.GoodWindowMs * PxPerMs;
        DrawRect(new Rect2(StrikeX - goodHalf, TopY - 6f, goodHalf * 2f, laneSpan + 12f), new Color(1f, 1f, 1f, 0.06f));
        for (int i = 0; i < NoteLanes.Count; i++)
        {
            float y = TopY + i * (LaneHeight + LaneGap);
            DrawRect(new Rect2(StrikeX, y, width - StrikeX - 16f, LaneHeight), new Color(LaneColors[i], 0.10f));
            DrawRect(new Rect2(StrikeX - 3f, y, 3f, LaneHeight), new Color(LaneColors[i], 0.8f));
        }

        DrawLine(new Vector2(StrikeX, TopY - 6f), new Vector2(StrikeX, TopY + laneSpan + 6f), new Color("e6edf5"), 2f);

        // Notes scroll right→left: a note sits on the strike line at its target time.
        PerformanceJudge? judge = perf.Judge;
        double chartMs = perf.ChartMs;
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

        DrawString(_font, new Vector2(StrikeX, TopY - 14f), $"♪ {chart.Name} — strum + fret in time", HorizontalAlignment.Left, -1, 13, new Color("d7c0ff"));
    }

    private void DrawBanner()
    {
        if (_player == null)
        {
            return;
        }

        PerformanceResult r = _player.LastResult;
        string text = r.IsPerfect
            ? $"{_player.SpellName} — PERFECT!"
            : r.Success
                ? $"{_player.SpellName} — SPELL FIRED"
                : $"{_player.SpellName} — FAILED";
        Color col = r.Success ? new Color("8ff0a4") : new Color("ff8f8f");

        var pos = new Vector2(StrikeX, TopY + NoteLanes.Count * (LaneHeight + LaneGap) + 22f);
        DrawString(_font, pos, text, HorizontalAlignment.Left, -1, 20, col);
        DrawString(_font, pos + new Vector2(0f, 18f), $"hits {r.Hits}/{r.Total}  ·  perfect {r.Perfect}  ·  miss {r.Miss}", HorizontalAlignment.Left, -1, 12, new Color("9fb3c8"));
    }
}
