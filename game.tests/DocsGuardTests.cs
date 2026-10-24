using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;

namespace Stillwake.Game.Tests;

/// <summary>
/// The navigation docs kept true to the tree: <c>docs/map.md</c> names every module folder, every README lists its
/// files, and <c>docs/decisions/index.md</c> is what <c>tools/adr-index.ps1</c> would write now.
/// </summary>
public class DocsGuardTests(ITestOutputHelper output)
{
    /// <summary>Set to 1 by <c>tools/adr-index.ps1</c>: the index is written instead of checked.</summary>
    public const string WriteIndex = "STILLWAKE_WRITE_ADR_INDEX";

    public const int ReadmeMostBytes = 8 * 1024;

    private static string Here([CallerFilePath] string path = "") => path;

    private static readonly string Root = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Here())!, ".."));

    private static string At(params string[] parts) => Path.Combine([Root, .. parts]);

    private static string Slashed(string path) => path.Replace('\\', '/');

    private static bool Built(string path) =>
        path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Any(p => p is "bin" or "obj" or ".godot");

    /// <summary>Every README of the code: the sim's modules, the client's features and folders, the net.</summary>
    public static IEnumerable<string> Readmes(string root) =>
        new[] { "sim", "game", "net" }
            .SelectMany(d => Directory.EnumerateFiles(Path.Combine(root, d), "README.md", SearchOption.AllDirectories))
            .Where(f => !Built(Path.GetRelativePath(root, f)))
            .Order(StringComparer.Ordinal);

    /// <summary>The <c>.cs</c> files a README's folder holds, its subfolders' too, as paths relative to it.</summary>
    public static SortedSet<string> FilesBeside(string readme)
    {
        var dir = Path.GetDirectoryName(readme)!;
        return new(Directory.EnumerateFiles(dir, "*.cs", SearchOption.AllDirectories)
            .Select(f => Path.GetRelativePath(dir, f))
            .Where(f => f.EndsWith(".cs", StringComparison.Ordinal) && !Built(f))
            .Select(Slashed), StringComparer.Ordinal);
    }

    /// <summary>What a README's <c>## Files</c> table lists, or null when its first section is not one.</summary>
    public static List<string>? FilesListed(string text)
    {
        var lines = text.Replace("\r\n", "\n").Split('\n');
        var first = Array.FindIndex(lines, l => l.StartsWith("## ", StringComparison.Ordinal));
        if (first < 0 || lines[first].Trim() != "## Files") return null;
        var row = new Regex(@"^\|\s*`([^`]+)`\s*\|");
        // The section's first table, and only it: a README may have other tables of files before its next heading.
        return lines.Skip(first + 1)
            .TakeWhile(l => !l.StartsWith('#'))
            .SkipWhile(l => !l.StartsWith('|'))
            .TakeWhile(l => l.StartsWith('|'))
            .Select(l => row.Match(l))
            .Where(m => m.Success)
            .Select(m => m.Groups[1].Value)
            .ToList();
    }

    /// <summary>What is wrong with one README's Files section; empty when nothing is.</summary>
    public static IEnumerable<string> FilesProblems(string readme, string root)
    {
        var name = Slashed(Path.GetRelativePath(root, readme));
        var listed = FilesListed(File.ReadAllText(readme));
        if (listed is null)
        {
            yield return $"{name}: no `## Files` section right after the title";
            yield break;
        }
        var there = FilesBeside(readme);
        foreach (var f in listed.Where(f => !there.Contains(f))) yield return $"{name}: lists {f}, which does not exist";
        foreach (var f in there.Where(f => !listed.Contains(f))) yield return $"{name}: misses {f}";
        foreach (var f in listed.GroupBy(f => f).Where(g => g.Count() > 1)) yield return $"{name}: lists {f.Key} twice";
    }

    /// <summary>The module and feature folders <c>docs/map.md</c> does not name.</summary>
    public static IEnumerable<string> MapMisses(string root, string map)
    {
        var sim = Directory.EnumerateDirectories(Path.Combine(root, "sim"))
            .Where(d => Directory.EnumerateFiles(d, "*.csproj").Any())
            .Select(d => Path.GetFileName(d))
            .Where(n => !Regex.IsMatch(map, $@"\bsim/{Regex.Escape(n)}\b"))
            .Select(n => $"sim/{n}");
        var features = Directory.EnumerateDirectories(Path.Combine(root, "game", "features"))
            .Select(d => Path.GetFileName(d))
            .Where(n => !Regex.IsMatch(map, $@"\b(features|f)/{Regex.Escape(n)}(?![\w-])"))
            .Select(n => $"game/features/{n}");
        return sim.Concat(features).Order(StringComparer.Ordinal);
    }

    private sealed record Adr(string Number, string File, string Title, string Status);

    private static readonly Regex Number = new(@"\b\d{4}\b");

    private static IEnumerable<string> NumbersIn(string text) => Number.Matches(text).Select(m => m.Value);

    /// <summary>
    /// The ADR index, from each ADR's title line and its <c>- Status:</c> line with its indented continuation: superseded by
    /// what its own status names after "superseded" and by every later ADR whose status says it "supersedes" it.
    /// </summary>
    public static string AdrIndex(string decisions)
    {
        var title = new Regex(@"^# (\d{4}) — (.+)$");
        var adrs = Directory.EnumerateFiles(decisions, "*.md")
            .Select(Path.GetFileName)
            .Where(f => Regex.IsMatch(f!, @"^\d{4}-"))
            .Order(StringComparer.Ordinal)
            .Select(f =>
            {
                var head = File.ReadLines(Path.Combine(decisions, f!)).Take(15).ToList();
                var t = title.Match(head[0]);
                if (!t.Success) throw new InvalidOperationException($"{f}: the first line is not \"# NNNN — Title\"");
                var at = head.FindIndex(l => l.StartsWith("- Status:", StringComparison.Ordinal));
                if (at < 0) throw new InvalidOperationException($"{f}: no \"- Status:\" line in its first 15");
                var status = string.Join(' ', head.Skip(at).Take(1 + head.Skip(at + 1).TakeWhile(l => l.StartsWith("  ")).Count())
                    .Select(l => l.Trim()))["- Status:".Length..].Trim();
                // A link names the ADR it points to, whatever its text says.
                status = Regex.Replace(status, @"\[[^\]]*\]\((\d{4})[^)]*\)", "$1");
                status = Regex.Replace(status, @"\]\([^)]*\)", "").Replace("[", "");
                return new Adr(t.Groups[1].Value, f!, t.Groups[2].Value.Trim(), status);
            })
            .ToList();

        var by = adrs.ToDictionary(a => a.Number, _ => new SortedSet<string>(StringComparer.Ordinal));
        foreach (var a in adrs)
        {
            var own = a.Status.IndexOf("superseded", StringComparison.OrdinalIgnoreCase);
            if (own >= 0)
                foreach (var part in a.Status[own..].Split(';').Where(p => !p.Contains("supersedes") && !p.Contains("amend")))
                    foreach (var n in NumbersIn(part).Where(n => by.ContainsKey(n) && n != a.Number)) by[a.Number].Add(n);
            foreach (var part in a.Status.Split(';').Where(p => p.Contains("supersedes")))
                foreach (var n in NumbersIn(part).Where(n => by.ContainsKey(n) && string.CompareOrdinal(n, a.Number) < 0))
                    by[n].Add(a.Number);
        }

        var md = new StringBuilder();
        md.Append("# Decisions\n\n");
        md.Append("Generated by `tools/adr-index.ps1` from each ADR's title and status lines; `DocsGuardTests` fails when it is\n");
        md.Append("stale. Read an ADR only when a README, plan or this list points you to it.\n\n");
        md.Append("| ADR | Title | Status | Superseded by |\n| --- | --- | --- | --- |\n");
        foreach (var a in adrs)
        {
            var word = Regex.Match(a.Status, @"^\w+").Value.ToLowerInvariant();
            var after = by[a.Number].Count == 0 ? "" : string.Join(", ", by[a.Number]) + (word == "accepted" ? " (in part)" : "");
            md.Append($"| [{a.Number}]({a.File}) | {a.Title.Replace("|", "\\|")} | {word} | {after} |\n");
        }
        return md.ToString();
    }

    [Fact]
    public void MapNamesEveryModuleAndFeatureFolder()
    {
        var misses = MapMisses(Root, File.ReadAllText(At("docs", "map.md"))).ToList();
        Assert.True(misses.Count == 0, "docs/map.md does not name: " + string.Join(", ", misses));
    }

    [Fact]
    public void EveryReadmeListsExactlyItsFiles()
    {
        var problems = Readmes(Root).SelectMany(r => FilesProblems(r, Root)).ToList();
        Assert.True(problems.Count == 0, "README Files sections out of date:\n" + string.Join('\n', problems));
    }

    [Fact]
    public void AdrIndexMatchesTheDecisions()
    {
        var index = At("docs", "decisions", "index.md");
        var expected = AdrIndex(At("docs", "decisions"));
        if (Environment.GetEnvironmentVariable(WriteIndex) == "1")
        {
            File.WriteAllText(index, expected);
            return;
        }
        Assert.True(File.Exists(index), "docs/decisions/index.md is missing: run tools/adr-index.ps1");
        Assert.True(expected == File.ReadAllText(index).Replace("\r\n", "\n"),
            "docs/decisions/index.md is stale: run tools/adr-index.ps1");
    }

    /// <summary>READMEs trimmed to their rules; each fails the guard once over 8 KB. Add one as it is trimmed.</summary>
    public static readonly string[] Trimmed =
    [
        "game/features/crew/README.md", "game/features/items/README.md", "sim/Items/README.md",
        "sim/Debris/README.md", "sim/Building/README.md",
    ];

    /// <summary>Warns, never fails, for a README not yet trimmed: it is trimmed the next time a task touches it.</summary>
    [Fact]
    public void ReadmesOverEightKbAreListed()
    {
        var large = Readmes(Root)
            .Select(r => (Name: Slashed(Path.GetRelativePath(Root, r)), Bytes: new FileInfo(r).Length))
            .Where(r => r.Bytes > ReadmeMostBytes && !Trimmed.Contains(r.Name))
            .OrderByDescending(r => r.Bytes)
            .Select(r => $"WARN README over 8 KB: {r.Name} ({r.Bytes / 1024} KB)")
            .ToList();
        foreach (var line in large) output.WriteLine(line);
    }

    [Fact]
    public void TrimmedReadmesStayUnderEightKb()
    {
        var large = Trimmed.Where(n => new FileInfo(At(n.Split('/'))).Length > ReadmeMostBytes).ToList();
        Assert.True(large.Count == 0, "README over 8 KB; history goes to ADRs: " + string.Join(", ", large));
    }

    [Fact]
    public void TheGuardBites()
    {
        var root = Path.Combine(Path.GetTempPath(), "docs-guard-" + Guid.NewGuid().ToString("N"));
        try
        {
            var module = Directory.CreateDirectory(Path.Combine(root, "sim", "Doors")).FullName;
            File.WriteAllText(Path.Combine(module, "Doors.csproj"), "");
            File.WriteAllText(Path.Combine(module, "Doors.cs"), "");
            File.WriteAllText(Path.Combine(module, "Hatch.cs"), "");
            Directory.CreateDirectory(Path.Combine(root, "game", "features", "doors"));
            Directory.CreateDirectory(Path.Combine(root, "net"));
            var readme = Path.Combine(module, "README.md");

            // A map without the folders, then with them.
            Assert.Equal(["game/features/doors", "sim/Doors"], MapMisses(root, "- Grid — sim/Grid · f/grid"));
            Assert.Empty(MapMisses(root, "- Doors — sim/Doors · f/doors"));

            // No Files section; one listing a file that is not there and missing one that is; one exact.
            File.WriteAllText(readme, "# Doors\n\nIntro.\n\n## Owns\n");
            Assert.Single(FilesProblems(readme, root));
            File.WriteAllText(readme, "# Doors\n\n## Files\n\n| File | Purpose |\n| --- | --- |\n| `Doors.cs` | x |\n| `Gone.cs` | y |\n");
            Assert.Equal(["sim/Doors/README.md: lists Gone.cs, which does not exist", "sim/Doors/README.md: misses Hatch.cs"],
                FilesProblems(readme, root).Select(Slashed));
            File.WriteAllText(readme, "# Doors\n\n## Files\n\n| File | Purpose |\n| --- | --- |\n| `Doors.cs` | x |\n| `Hatch.cs` | y |\n");
            Assert.Empty(FilesProblems(readme, root));

            // The index follows the folder: a new ADR changes it, and its "supersedes" marks the older one.
            var decisions = Directory.CreateDirectory(Path.Combine(root, "decisions")).FullName;
            File.WriteAllText(Path.Combine(decisions, "0001-doors.md"), "# 0001 — Doors\n\n- Status: accepted\n");
            var before = AdrIndex(decisions);
            File.WriteAllText(Path.Combine(decisions, "0002-hatches.md"), "# 0002 — Hatches\n\n- Status: accepted; supersedes\n  [0001](0001-doors.md)'s doors\n");
            var after = AdrIndex(decisions);
            Assert.NotEqual(before, after);
            Assert.Contains("| [0001](0001-doors.md) | Doors | accepted | 0002 (in part) |", after);
            Assert.Contains("| [0002](0002-hatches.md) | Hatches | accepted |  |", after);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
