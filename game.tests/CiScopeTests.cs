using System.Runtime.CompilerServices;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>What CI takes for granted when it leaves tests out of a PR's run (<c>tools/ci-scope.sh</c>).</summary>
public class CiScopeTests
{
    private static string Here([CallerFilePath] string path = "") => path;

    private static string At(params string[] parts) => Path.Combine([Path.GetDirectoryName(Here())!, "..", .. parts]);

    /// <summary>A PR of the client skips these two projects, so neither may compile a file of its.</summary>
    [Theory]
    [InlineData("sim")]
    [InlineData("net")]
    public void TheSimsAndNetsTestsCompileNothingOfTheClient(string folder) =>
        Assert.DoesNotContain("game/", File.ReadAllText(At(folder, "Tests", "Tests.csproj")).Replace('\\', '/'));

    /// <summary>A PR of docs runs this class by name, and a filter that matches nothing passes with nothing run.</summary>
    [Fact]
    public void APullOfDocsRunsTheDocsGuard() =>
        Assert.Contains($"FullyQualifiedName~{nameof(DocsGuardTests)}", File.ReadAllText(At(".github", "workflows", "ci.yml")));
}
