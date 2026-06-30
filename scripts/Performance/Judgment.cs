namespace WixxTheBard.Performance;

/// <summary>
/// The verdict on a single input or note (SPEC §5.2/§5.4). <see cref="Perfect"/> and
/// <see cref="Good"/> both count as hits; <see cref="Miss"/> is a note that timed out
/// unhit; <see cref="Stray"/> is a strum that matched no note in its window (wrong
/// fret or wrong time) — it scores nothing and breaks a perfect run.
/// </summary>
public enum Judgment
{
    Perfect,
    Good,
    Miss,
    Stray,
}
