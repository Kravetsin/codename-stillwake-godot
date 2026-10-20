using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Stillwake.Sim;
using Xunit;
using static Stillwake.Game.I18n.En;

namespace Stillwake.Game.Tests;

/// <summary>English text: apps/game/src/i18n/en.test.ts.</summary>
public class EnTests
{
    private static string Here([CallerFilePath] string path = "") => path;

    /// <summary>Every <c>.cs</c> file of the sim's modules and content, tests and build output left out.</summary>
    private static IEnumerable<string> Sources(string dir) =>
        Directory.EnumerateFiles(dir, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .Any(part => part is "Tests" or "obj" or "bin"));

    [Fact]
    public void HasEveryRefusalReasonTheSimCanGive()
    {
        // A refusal key is any string literal "<moduleId>.<why>" in the sim, however it is handed back.
        // The core's own refusals too, such as a command from a surface its sender has left (0076).
        var ids = new Registry().All.Select(m => m.Id).Append("world");
        var refusal = new Regex($"\"((?:{string.Join('|', ids)})\\.[a-z]\\w*)\"");
        var sim = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Here())!, "..", "sim"));
        var keys = Sources(sim)
            .SelectMany(file => refusal.Matches(File.ReadAllText(file)).Select(m => m.Groups[1].Value))
            .ToHashSet();
        // Checked against keys known to exist, so a broken pattern cannot pass by finding nothing.
        Assert.Contains("grid.occupied", keys);
        Assert.Contains("items.full", keys);
        Assert.Contains("building.noPlan", keys);
        Assert.Contains("world.elsewhere", keys);
        Assert.Empty(keys.Where(key => !Table.ContainsKey(key)).Order(StringComparer.Ordinal));
    }

    [Fact]
    public void TFillsEachNamedValueItIsGivenAndLeavesTheRestAsWritten()
    {
        Assert.Equal("Filling · 3/96 cells", T("air.filling", ("aired", 3), ("cells", 96)));
        Assert.Equal("Filling · 3/{cells} cells", T("air.filling", ("aired", 3)));
        Assert.Equal("no.such.key", T("no.such.key"));
    }

    [Fact]
    public void NamesEveryItemThereIs()
    {
        Assert.All(Content.Items.All, item => Assert.True(Table.ContainsKey($"item.{item.Id}"), item.Id));
    }
}
