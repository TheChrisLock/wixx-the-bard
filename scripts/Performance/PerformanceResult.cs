namespace WixxTheBard.Performance;

/// <summary>
/// The tally of a finished performance (SPEC §5.4). <see cref="Success"/> decides
/// whether the spell fires; <see cref="IsPerfect"/> flags the stretch "perfect
/// performance" empowered version (SPEC §5.4 stretch). Pure data, unit-tested.
/// </summary>
public readonly struct PerformanceResult
{
    public PerformanceResult(int total, int perfect, int good, int miss, int stray, double successThreshold)
    {
        Total = total;
        Perfect = perfect;
        Good = good;
        Miss = miss;
        Stray = stray;
        SuccessThreshold = successThreshold;
    }

    /// <summary>Number of notes in the chart.</summary>
    public int Total { get; }

    public int Perfect { get; }

    public int Good { get; }

    public int Miss { get; }

    /// <summary>Strums that matched no note (wrong fret/time).</summary>
    public int Stray { get; }

    public double SuccessThreshold { get; }

    /// <summary>Perfect + Good — the notes that landed.</summary>
    public int Hits => Perfect + Good;

    /// <summary>Hit ratio in [0,1]; 1 for an empty chart (nothing to miss).</summary>
    public double Accuracy => Total <= 0 ? 1.0 : Hits / (double)Total;

    /// <summary>The spell fires when accuracy meets the threshold (SPEC §5.4).</summary>
    public bool Success => Accuracy >= SuccessThreshold;

    /// <summary>Every note Perfect, with no strays — the empowered stretch tier (SPEC §5.4).</summary>
    public bool IsPerfect => Total > 0 && Perfect == Total && Stray == 0;
}
