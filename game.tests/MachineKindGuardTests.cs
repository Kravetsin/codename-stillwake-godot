using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>
/// Systems ask a machine's traits, not its kind (0113): no code of the sim, the net or the client compares a kind to a
/// machine's name (<c>m.Kind == "crate"</c>), walks one kind by name (<c>MachinesOf("vent")</c>) or reads one kind's spec by
/// name (<c>Machines.Get("furnace")</c>), outside the files that work one machine's own rule. Tests, dev scripts and demos may.
/// </summary>
public class MachineKindGuardTests
{
    private static string Here([CallerFilePath] string path = "") => path;

    private static readonly string Root = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Here())!, ".."));

    /// <summary>The files that work one machine's own rule, and may name it.</summary>
    private static readonly string[] Allowed =
    [
        // The smelter's heat glow and the former's press: their own looks and sounds.
        "game/features/crafting/StationView.cs",
        "game/features/crafting/StationSounds.cs",
    ];

    /// <summary>A machine's kind by name, compared, walked or looked up; not a plant's, a placement's or an item's of another name.</summary>
    private static readonly Regex Named = new(
        $@"(?:\b[Kk]ind\s*(?:==|!=|is(?:\s+not)?)\s*\(?|MachinesOf\(|Machines\.Get\()""(?:{string.Join('|', Stillwake.Content.Machines.All.Select(m => m.Kind))})""",
        RegexOptions.Compiled);

    private static string Slashed(string path) => path.Replace('\\', '/');

    private static bool Skipped(string relative) =>
        relative.Split('/').Any(p => p is "bin" or "obj" or ".godot" or "Tests" or "dev")
        || relative.EndsWith("Demo.cs", StringComparison.Ordinal)
        || Allowed.Contains(relative);

    /// <summary>Each line of <paramref name="text"/> naming a machine's kind, as <c>line: text</c>.</summary>
    public static IEnumerable<string> Offenders(string text) =>
        text.Split('\n').Select((line, i) => (line, i)).Where(l => Named.IsMatch(l.line)).Select(l => $"{l.i + 1}: {l.line.Trim()}");

    [Fact]
    public void NoSystemNamesAMachinesKindWhereATraitIsMeant()
    {
        var found = new[] { "sim", "net", "game" }
            .SelectMany(d => Directory.EnumerateFiles(Path.Combine(Root, d), "*.cs", SearchOption.AllDirectories))
            .Select(f => Slashed(Path.GetRelativePath(Root, f)))
            .Where(f => !Skipped(f))
            .SelectMany(f => Offenders(File.ReadAllText(Path.Combine(Root, f))).Select(o => $"{f}:{o}"))
            .ToList();
        Assert.True(found.Count == 0, "Ask the machine's traits (0113) instead of naming its kind:\n" + string.Join('\n', found));
    }

    [Fact]
    public void TheGuardBites()
    {
        Assert.Single(Offenders("if (m.Kind == \"crate\") return;"));
        Assert.Single(Offenders("if (kind != \"tank\") continue;"));
        Assert.Single(Offenders("if (kind is not (\"former\" or \"smelter\")) return;"));
        Assert.Single(Offenders("foreach (var m in _grid.MachinesOf(\"vent\"))"));
        Assert.Single(Offenders("int fuel = Machines.Get(\"furnace\").FuelSlot!.Value;"));
        Assert.Empty(Offenders("if (plant.Kind == \"moss\") return;"));
        Assert.Empty(Offenders("foreach (var m in _grid.MachinesWith(MachineTrait.Vents))"));
        Assert.Empty(Offenders("placement.Kind == \"wall\""));
        Assert.True(Skipped("sim/Tests/AirTests.cs"));
        Assert.True(Skipped("game/dev/logic/Testbed.cs"));
        Assert.True(Skipped("game/features/power/StoreDemo.cs"));
        Assert.True(Skipped("game/features/crafting/StationView.cs"));
        Assert.False(Skipped("sim/Atmosphere/Air.cs"));
    }
}
