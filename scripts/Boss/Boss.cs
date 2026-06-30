using Godot;

namespace WixxTheBard.Boss;

/// <summary>
/// World-space visual placeholder for the Choirbreaker (SPEC §6/§7/§8 — art
/// production deferred post-M6). It only ever <i>reads</i> the authoritative
/// <see cref="Player.BossFight"/>/<see cref="Player.BossDefeated"/> state
/// (CLAUDE.md rule 7) to flash on a landed Highway hit and fade out on victory —
/// it never drives the duel itself, exactly like <see cref="Performance.PerformanceHud"/>
/// only ever reads a spell performance.
/// </summary>
public partial class Boss : Node2D
{
    private Player? _player;
    private ColorRect? _box;
    private BossStage _lastStage = BossStage.Idle;
    private bool _playedDefeat;

    public override void _Ready()
    {
        AddToGroup("boss");
        _box = GetNodeOrNull<ColorRect>("Box");
    }

    public override void _Process(double delta)
    {
        _player ??= GetTree().GetFirstNodeInGroup("player") as Player;
        if (_player == null || _playedDefeat)
        {
            return;
        }

        if (_player.BossDefeated)
        {
            PlayDefeatTween();
            return;
        }

        BossStage stage = _player.BossFight.Stage;

        // A phase just resolved (Performing -> anything else): flash if it was a
        // Highway hit landing damage (SPEC §6 — Call & Response hits don't damage
        // the boss, so no flash there).
        bool justResolvedAPhase = _lastStage == BossStage.Performing && stage != BossStage.Performing;
        if (justResolvedAPhase && _player.BossFight.LastPhaseResult.Hits > 0)
        {
            FlashHurt();
        }

        _lastStage = stage;
    }

    private void FlashHurt()
    {
        if (_box == null)
        {
            return;
        }

        var tween = CreateTween();
        tween.TweenProperty(_box, "modulate", new Color("ff9a9a"), 0.04f);
        tween.TweenProperty(_box, "modulate", Colors.White, 0.18f);
    }

    private void PlayDefeatTween()
    {
        _playedDefeat = true;
        var tween = CreateTween();
        tween.TweenProperty(this, "modulate:a", 0.25f, 0.6f);
        tween.TweenProperty(this, "scale", new Vector2(1.2f, 0.6f), 0.6f);
    }
}
