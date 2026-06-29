namespace WixxTheBard.Tests;

using GdUnit4;
using static GdUnit4.Assertions;

/// <summary>
/// M0 test-harness smoke test. One trivial passing assertion proves gdUnit4 runs
/// via <c>dotnet test</c>; a second confirms the Tunables resource instantiates
/// with sane defaults (the single source of truth is wired and loadable).
/// </summary>
[TestSuite]
public class TunablesTest
{
    [TestCase]
    public void ScaffoldSanity()
    {
        AssertThat(2 + 2).IsEqual(4);
    }

    [TestCase]
    public void TunablesHavePositiveDefaults()
    {
        var tunables = new Tunables();
        AssertThat(tunables.MoveSpeed).IsGreater(0.0f);
        AssertThat(tunables.JumpVelocity).IsGreater(0.0f);
        AssertThat(tunables.Gravity).IsGreater(0.0f);
    }
}
