using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>
/// Creatures are composed (2026-10-26-creatures-are-composed): no code of the sim, the net or the client names a species, by its
/// id (<c>"loper"</c>) or its record (<c>Creatures.Loper</c>), outside the looks keyed by it. What a creature does comes from its
/// drives and acts, so a new species is content and art. Tests, dev scripts and demos may name one.
/// </summary>
public class CreatureSpeciesGuardTests
{
    private static string Here([CallerFilePath] string path = "") => path;

    private static readonly string Root = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Here())!, ".."));

    /// <summary>The looks and sounds keyed by species, and the content that defines them.</summary>
    private static readonly string[] Allowed =
    [
        "sim/Content/Creatures.cs",
        "sim/Content/PlanetTypes.cs",
        "sim/Content/Ruins.cs",
        "game/Content/logic/CreatureArt.cs",
        "game/Content/logic/CreatureInk.cs",
        "game/Content/logic/CreatureLooks.cs",
        "game/Content/logic/HitLooks.cs",
        "game/Content/logic/Sound.cs",
    ];

    /// <summary>Every species' record, by its field in <c>Content.Creatures</c>: named through any alias (<c>Spec.Loper</c>, <c>Tune.Tapir</c>).</summary>
    private static readonly string[] Records = typeof(Stillwake.Content.Creatures).GetFields(BindingFlags.Public | BindingFlags.Static)
        .Where(f => f.FieldType == typeof(Stillwake.Content.Creatures.Species)).Select(f => f.Name).ToArray();

    private static readonly Regex Named = new(
        $@"""(?:{string.Join('|', Stillwake.Content.Creatures.All.Select(s => s.Id))})""|\.(?:{string.Join('|', Records)})\b",
        RegexOptions.Compiled);

    private static string Slashed(string path) => path.Replace('\\', '/');

    private static bool Skipped(string relative) =>
        relative.Split('/').Any(p => p is "bin" or "obj" or ".godot" or "Tests" or "dev")
        || relative.EndsWith("Demo.cs", StringComparison.Ordinal)
        || Allowed.Contains(relative);

    public static IEnumerable<string> Offenders(string text) =>
        text.Split('\n').Select((line, i) => (line, i)).Where(l => Named.IsMatch(l.line)).Select(l => $"{l.i + 1}: {l.line.Trim()}");

    [Fact]
    public void NoSystemNamesASpecies()
    {
        var found = new[] { "sim", "net", "game" }
            .SelectMany(d => Directory.EnumerateFiles(Path.Combine(Root, d), "*.cs", SearchOption.AllDirectories))
            .Select(f => Slashed(Path.GetRelativePath(Root, f)))
            .Where(f => !Skipped(f))
            .SelectMany(f => Offenders(File.ReadAllText(Path.Combine(Root, f))).Select(o => $"{f}:{o}"))
            .ToList();
        Assert.True(found.Count == 0, "Read the creature's drives and acts instead of naming its species:\n" + string.Join('\n', found));
    }

    [Fact]
    public void TheGuardBites()
    {
        Assert.Single(Offenders("if (c.Species == \"loper\") return;"));
        Assert.Single(Offenders("var ram = Spec.Creatures.Tapir.Moves[0];"));
        Assert.Single(Offenders("if (Spec.Get(c.Species) == Spec.Loper) return;"));
        Assert.Single(Offenders("int hp = Tune.Sixleg.Life.Hp;"));
        Assert.Equal(Stillwake.Content.Creatures.All.Count, Records.Length);
        Assert.Empty(Offenders("var spec = Spec.Creatures.Get(c.Species);"));
        Assert.True(Skipped("game/dev/CreaturesDemo.cs"));
        Assert.True(Skipped("game/Content/logic/CreatureLooks.cs"));
        Assert.False(Skipped("game/features/creatures/CreaturesView.cs"));
    }
}
