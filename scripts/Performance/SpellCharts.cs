namespace WixxTheBard.Performance;

/// <summary>
/// The vertical slice's ability charts (SPEC §12 — "one special ability with its
/// spell performance, one fixed chart"). M4 ships exactly one: <see cref="Kindle"/>,
/// a tier-1 single-note song (SPEC §5.3 — difficulty starts at single notes). These
/// note times are the melody — content, fixed and learnable, not feel-tunables
/// (CLAUDE.md rule 1). Further abilities and the difficulty ladder are phase-2 scope.
/// </summary>
public static class SpellCharts
{
    /// <summary>
    /// "Kindle" — Wixx's first spell, a small spark of restored colour (SPEC §7/§8).
    /// A ~3-second tier-1 chart: six single notes across three frets, spaced for a
    /// learnable little phrase the player strums in time.
    /// </summary>
    public static NoteChart Kindle() => new(
        "Kindle",
        new[]
        {
            new Note(700.0, NoteLane.Green),
            new Note(1150.0, NoteLane.Green),
            new Note(1600.0, NoteLane.Yellow),
            new Note(2050.0, NoteLane.Red),
            new Note(2500.0, NoteLane.Yellow),
            new Note(2950.0, NoteLane.Blue),
        });
}
