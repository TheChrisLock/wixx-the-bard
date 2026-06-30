namespace WixxTheBard.Tests;

using GdUnit4;
using WixxTheBard.Controls;
using static GdUnit4.Assertions;

/// <summary>
/// A/V offset reduction (CLAUDE.md rule 6 — rhythm is calibration-relative). The
/// recommended offset is a trimmed median so a few mis-taps don't skew it.
/// </summary>
[TestSuite]
public class LatencyCalibrationTest
{
    private static bool Approx(double a, double b) => System.Math.Abs(a - b) < 1e-6;

    [TestCase]
    public void EmptySessionReturnsFallback()
    {
        var calibration = new LatencyCalibration();
        AssertThat(Approx(calibration.RecommendedOffsetMs(42.0), 42.0)).IsTrue();
        AssertThat(calibration.SampleCount).IsEqual(0);
    }

    [TestCase]
    public void OddCountMedian()
    {
        var calibration = new LatencyCalibration();
        calibration.AddSample(10);
        calibration.AddSample(30);
        calibration.AddSample(20);
        // Sorted 10,20,30 → median 20 (too few to trim).
        AssertThat(Approx(calibration.RecommendedOffsetMs(0), 20.0)).IsTrue();
    }

    [TestCase]
    public void EvenCountAveragesMiddlePair()
    {
        var calibration = new LatencyCalibration();
        calibration.AddSample(10);
        calibration.AddSample(20);
        calibration.AddSample(30);
        calibration.AddSample(40);
        // Sorted 10,20,30,40 → middle pair 20,30 → 25.
        AssertThat(Approx(calibration.RecommendedOffsetMs(0), 25.0)).IsTrue();
    }

    [TestCase]
    public void TrimsExtremeOutliers()
    {
        var calibration = new LatencyCalibration();
        // Nine tight taps around 50 plus one wild outlier; trimmed median stays ~50.
        foreach (var v in new double[] { 48, 49, 50, 50, 51, 52, 49, 50, 51, 5000 })
        {
            calibration.AddSample(v);
        }

        double result = calibration.RecommendedOffsetMs(0);
        AssertThat(result > 45 && result < 55).IsTrue();
    }

    [TestCase]
    public void ResetClearsSamples()
    {
        var calibration = new LatencyCalibration();
        calibration.AddSample(10);
        calibration.Reset();
        AssertThat(calibration.SampleCount).IsEqual(0);
    }
}
