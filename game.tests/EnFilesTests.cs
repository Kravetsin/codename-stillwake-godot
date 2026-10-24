using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Stillwake.Game.I18n;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>Each feature's text is in its own <c>logic/&lt;Name&gt;.En.cs</c>, every key in one of them, and <c>En.Table</c> holds them all.</summary>
public class EnFilesTests
{
    private static string Here([CallerFilePath] string path = "") => path;

    private static readonly Regex Key = new("""^\s*\["([^"]+)"\]\s*=""", RegexOptions.Multiline);

    /// <summary>Every feature file under <c>game/</c>, by the name its <c>Strings</c> field must have.</summary>
    private static Dictionary<string, string> Files()
    {
        var game = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Here())!, "..", "game"));
        return Directory.EnumerateFiles(game, "*.En.cs", SearchOption.AllDirectories)
            .Where(f => !f.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Any(p => p is "bin" or "obj" or ".godot"))
            .ToDictionary(f => Path.GetFileName(f)[..^".En.cs".Length], f => f);
    }

    [Fact]
    public void EveryKeyIsInExactlyOneFeatureFileAndTheTableHoldsThemAll()
    {
        var written = Files().SelectMany(f => Key.Matches(File.ReadAllText(f.Value)).Select(m => (File: f.Key, Key: m.Groups[1].Value))).ToList();
        Assert.True(written.Count > 500, "the files hold too few keys to be the whole table");
        var twice = written.GroupBy(w => w.Key).Where(g => g.Count() > 1).Select(g => $"{g.Key} in {string.Join(" and ", g.Select(w => w.File))}").ToList();
        Assert.True(twice.Count == 0, "keys written twice: " + string.Join(", ", twice));
        Assert.Equal(written.Select(w => w.Key).Order(StringComparer.Ordinal), En.Table.Keys.Order(StringComparer.Ordinal));
    }

    [Fact]
    public void EveryFeatureFileHasItsOwnStringsField()
    {
        var fields = typeof(En).GetFields(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
            .Where(f => f.FieldType.Name == "Strings")
            .Select(f => f.Name)
            .Order(StringComparer.Ordinal);
        Assert.Equal(Files().Keys.Order(StringComparer.Ordinal), fields);
    }
}
