using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>
/// Rules ask a structure's traits, never its kind (0112): no code of the sim, the net or the client compares a cell to a
/// concrete structure code (<c>Structure.Wall</c>, <c>Structure.DoorClosed</c>, …) outside the table that defines them.
/// <c>Structure.Empty</c>, the absence of a structure, is not a kind. Tests may name codes: they build maps from them.
/// </summary>
public class StructureCodeGuardTests
{
    private static string Here([CallerFilePath] string path = "") => path;

    private static readonly string Root = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Here())!, ".."));

    /// <summary>The only files that may name a structure's code: its table.</summary>
    private static readonly string[] Allowed = ["sim/Content/Structures.cs"];

    /// <summary>A code by its constant, every one but <c>Empty</c>; not a member called Structure (a layer's <c>x.Structure.Length</c>).</summary>
    private static readonly Regex Code = new(
        $@"(?<![.\w])Structure\.({string.Join('|', typeof(Stillwake.Content.Structure).GetFields().Select(f => f.Name).Where(n => n != "Empty"))})\b",
        RegexOptions.Compiled);

    private static string Slashed(string path) => path.Replace('\\', '/');

    private static bool Skipped(string relative) =>
        relative.Split('/').Any(p => p is "bin" or "obj" or ".godot" or "Tests")
        || Allowed.Contains(relative);

    /// <summary>Each line of <paramref name="text"/> naming a concrete structure code, as <c>line: text</c>.</summary>
    public static IEnumerable<string> Offenders(string text) =>
        text.Split('\n').Select((line, i) => (line, i)).Where(l => Code.IsMatch(l.line)).Select(l => $"{l.i + 1}: {l.line.Trim()}");

    [Fact]
    public void NoCodeOutsideTheTableNamesAStructuresKind()
    {
        var found = new[] { "sim", "net", "game" }
            .SelectMany(d => Directory.EnumerateFiles(Path.Combine(Root, d), "*.cs", SearchOption.AllDirectories))
            .Select(f => Slashed(Path.GetRelativePath(Root, f)))
            .Where(f => !Skipped(f))
            .SelectMany(f => Offenders(File.ReadAllText(Path.Combine(Root, f))).Select(o => $"{f}:{o}"))
            .ToList();
        Assert.True(found.Count == 0, "Ask the grid's traits (0112) instead of naming a structure code:\n" + string.Join('\n', found));
    }

    [Fact]
    public void TheGuardBites()
    {
        Assert.Single(Offenders("if (grid.StructureAt(c) == Structure.Wall) return;"));
        Assert.Single(Offenders("x is Structure.HatchOpen or Structure.DoorOpen"));
        Assert.Empty(Offenders("if (grid.StructureAt(c) == Structure.Empty) return;"));
        Assert.Empty(Offenders("if (grid.Solid(c)) return; // a StructureSpec, Structures.Of(code)"));
        Assert.Empty(Offenders("int size = State.Structure.Length;"));
        Assert.Single(Offenders("s == Structure.Grate"));
        Assert.True(Skipped("sim/Tests/RoomsTests.cs"));
        Assert.True(Skipped("sim/Content/Structures.cs"));
        Assert.False(Skipped("sim/Rooms/Rooms.cs"));
    }
}
