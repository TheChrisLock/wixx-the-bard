using System.Collections.Generic;

namespace WixxTheBard.Controls;

/// <summary>
/// Collects A/V calibration taps and reduces them to a single latency offset
/// (milliseconds). Rhythm judgement is calibration-relative (CLAUDE.md rule 6),
/// and the stored offset is what every note window is measured against. Each
/// sample is the signed error of a tap relative to the beat it was aimed at
/// (positive = late). The recommendation is the median of the samples after
/// trimming the most extreme ones, so a few mis-taps don't skew it. Pure logic,
/// unit-tested.
/// </summary>
public sealed class LatencyCalibration
{
    private readonly List<double> _samples = new();

    public int SampleCount => _samples.Count;

    public IReadOnlyList<double> Samples => _samples;

    public void AddSample(double offsetMs) => _samples.Add(offsetMs);

    public void Reset() => _samples.Clear();

    /// <summary>
    /// Recommended offset in ms: trimmed median of the collected taps, or
    /// <paramref name="fallback"/> when no samples have been gathered yet.
    /// </summary>
    public double RecommendedOffsetMs(double fallback)
    {
        if (_samples.Count == 0)
        {
            return fallback;
        }

        var sorted = new List<double>(_samples);
        sorted.Sort();

        // Drop ~10% from each end once there's enough data to make trimming meaningful.
        int trim = sorted.Count >= 5 ? sorted.Count / 10 : 0;
        int low = trim;
        int high = sorted.Count - trim;
        int count = high - low;
        if (count <= 0)
        {
            low = 0;
            high = sorted.Count;
            count = sorted.Count;
        }

        int mid = low + count / 2;
        if (count % 2 == 1)
        {
            return sorted[mid];
        }

        return (sorted[mid - 1] + sorted[mid]) / 2.0;
    }
}
