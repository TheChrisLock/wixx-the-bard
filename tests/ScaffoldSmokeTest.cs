namespace WixxTheBard.Tests;

using GdUnit4;
using static GdUnit4.Assertions;

/// <summary>
/// M0 test-harness smoke test: one trivial passing assertion that proves gdUnit4
/// is wired and runs via <c>dotnet test</c>.
///
/// It deliberately touches no Godot types. Instantiating a <see cref="Godot.Resource"/>
/// (e.g. <c>new Tunables()</c>) requires the test process to be hosted inside the
/// Godot runtime; under a plain <c>dotnet test</c> host the native interop call
/// crashes the run. Godot-type tests arrive in M1 alongside the Godot-hosted test
/// runner (set <c>GODOT_BIN</c> and run with the project's <c>.runsettings</c>).
/// </summary>
[TestSuite]
public class ScaffoldSmokeTest
{
    [TestCase]
    public void ScaffoldSanity()
    {
        AssertThat(2 + 2).IsEqual(4);
    }
}
