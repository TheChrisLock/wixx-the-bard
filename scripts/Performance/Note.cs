namespace WixxTheBard.Performance;

/// <summary>
/// One charted note: the time (ms from the performance start) it should be struck,
/// and the lane (fret) it lives in. Immutable content — part of the fixed song
/// (SPEC §5.2 "each ability always uses the same song"), not a tunable feel number.
/// </summary>
public readonly struct Note
{
    public Note(double targetMs, NoteLane lane)
    {
        TargetMs = targetMs;
        Lane = lane;
    }

    /// <summary>When the note should be struck, in ms from performance start.</summary>
    public double TargetMs { get; }

    /// <summary>The fret lane the player must hold to hit it.</summary>
    public NoteLane Lane { get; }
}
