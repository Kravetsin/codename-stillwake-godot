using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>
/// The navigation docs kept true to the tree: <c>docs/map.md</c> names every module folder, every README lists its
/// files, and <c>docs/decisions/index.md</c> is what <c>tools/adr-index.ps1</c> would write now.
/// </summary>
public class DocsGuardTests
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

    /// <summary>One ADR: its id (<c>0081</c>, or a dated one's slug), its file, title and status line.</summary>
    private sealed record Adr(string Id, string File, string Title, string Status);

    /// <summary>
    /// A numbered ADR's file (<c>0081-…</c>, the ones before dated names), and a dated one's (<c>2026-10-24-two-words.md</c>): a slug of
    /// two words or more, so no plain word of a status line is taken for one.
    /// </summary>
    private static readonly Regex Numbered = new(@"^(\d{4})-(?!\d{2}-\d{2}-)"), Dated = new(@"^\d{4}-\d{2}-\d{2}-([a-z0-9]+(?:-[a-z0-9]+)+)\.md$");

    /// <summary>The ids of <paramref name="known"/> that <paramref name="text"/> names, each as a whole word.</summary>
    private static IEnumerable<string> IdsIn(string text, IEnumerable<string> known) =>
        known.Where(id => Regex.IsMatch(text, $@"(?<![\w-]){Regex.Escape(id)}(?![\w-])"));

    /// <summary>
    /// The ADR index, from each ADR's title line and its <c>- Status:</c> line with its indented continuation: superseded by
    /// what its own status names after "superseded" and by every later ADR whose status says it "supersedes" it. A numbered ADR
    /// is titled <c># 0081 — Title</c>, a dated one <c># Title</c> and named by its slug; numbered ones come first, then by date.
    /// </summary>
    public static string AdrIndex(string decisions)
    {
        var numberedTitle = new Regex(@"^# (\d{4}) — (.+)$");
        var files = Directory.EnumerateFiles(decisions, "*.md").Select(Path.GetFileName).OfType<string>().Order(StringComparer.Ordinal).ToList();
        var stray = files.Where(f => f != "index.md" && !Numbered.IsMatch(f) && !Dated.IsMatch(f)).ToList();
        if (stray.Count > 0) throw new InvalidOperationException($"not an ADR's name, NNNN-slug.md or YYYY-MM-DD-slug.md: {string.Join(", ", stray)}");
        var adrFiles = files.Where(f => Numbered.IsMatch(f)).Concat(files.Where(f => Dated.IsMatch(f))).ToList();
        string IdOf(string file) => Numbered.Match(file) is { Success: true } n ? n.Groups[1].Value : Dated.Match(file).Groups[1].Value;
        var adrs = adrFiles
            .Select(f =>
            {
                var head = File.ReadLines(Path.Combine(decisions, f)).Take(15).ToList();
                string title;
                if (Numbered.IsMatch(f))
                {
                    var t = numberedTitle.Match(head[0]);
                    if (!t.Success || t.Groups[1].Value != IdOf(f)) throw new InvalidOperationException($"{f}: the first line is not \"# {IdOf(f)} — Title\"");
                    title = t.Groups[2].Value;
                }
                else
                {
                    if (!head[0].StartsWith("# ", StringComparison.Ordinal) || numberedTitle.IsMatch(head[0]))
                        throw new InvalidOperationException($"{f}: the first line is not \"# Title\", with no number");
                    title = head[0][2..];
                }
                var at = head.FindIndex(l => l.StartsWith("- Status:", StringComparison.Ordinal));
                if (at < 0) throw new InvalidOperationException($"{f}: no \"- Status:\" line in its first 15");
                var status = string.Join(' ', head.Skip(at).Take(1 + head.Skip(at + 1).TakeWhile(l => l.StartsWith("  ")).Count())
                    .Select(l => l.Trim()))["- Status:".Length..].Trim();
                // A link names the ADR it points to, whatever its text says.
                status = Regex.Replace(status, @"\[[^\]]*\]\(([^)#]+\.md)[^)]*\)", m => adrFiles.Contains(m.Groups[1].Value) ? IdOf(m.Groups[1].Value) : m.Value);
                status = Regex.Replace(status, @"\]\([^)]*\)", "").Replace("[", "");
                return new Adr(IdOf(f), f, title.Trim(), status);
            })
            .ToList();
        var duplicate = adrs.GroupBy(a => a.Id).FirstOrDefault(g => g.Count() > 1);
        if (duplicate != null) throw new InvalidOperationException($"two ADRs named {duplicate.Key}: {string.Join(", ", duplicate.Select(a => a.File))}");

        var ids = adrs.Select(a => a.Id).ToList();
        var by = adrs.ToDictionary(a => a.Id, _ => new List<string>());
        foreach (var a in adrs)
        {
            var own = a.Status.IndexOf("superseded", StringComparison.OrdinalIgnoreCase);
            if (own >= 0)
                foreach (var part in a.Status[own..].Split(';').Where(p => !p.Contains("supersedes") && !p.Contains("amend")))
                    foreach (var n in IdsIn(part, ids).Where(n => n != a.Id)) by[a.Id].Add(n);
            foreach (var part in a.Status.Split(';').Where(p => p.Contains("supersedes")))
                foreach (var n in IdsIn(part, ids).Where(n => ids.IndexOf(n) < ids.IndexOf(a.Id)))
                    by[n].Add(a.Id);
        }

        var md = new StringBuilder();
        md.Append("# Decisions\n\n");
        md.Append("Generated by `tools/adr-index.ps1` from each ADR's title and status lines; `DocsGuardTests` fails when it is\n");
        md.Append("stale. Read an ADR only when a README, plan or this list points you to it. A new ADR is\n");
        md.Append("`YYYY-MM-DD-slug.md`, titled `# Title` and named by its slug; the numbered ones keep their numbers.\n\n");
        md.Append("| ADR | Title | Status | Superseded by |\n| --- | --- | --- | --- |\n");
        foreach (var a in adrs)
        {
            var word = Regex.Match(a.Status, @"^\w+").Value.ToLowerInvariant();
            var later = by[a.Id].Distinct().OrderBy(ids.IndexOf).ToList();
            var after = later.Count == 0 ? "" : string.Join(", ", later) + (word == "accepted" ? " (in part)" : "");
            md.Append($"| [{a.Id}]({a.File}) | {a.Title.Replace("|", "\\|")} | {word} | {after} |\n");
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

    /// <summary>Every README of the code, and of the tools' pipelines: its rules and files under 8 KB, history in ADRs.</summary>
    [Fact]
    public void EveryReadmeStaysUnderEightKb()
    {
        var tools = Directory.EnumerateFiles(At("tools"), "README.md", SearchOption.AllDirectories)
            .Where(f => !Built(f) && !f.Contains("node_modules", StringComparison.Ordinal));
        var large = Readmes(Root).Concat(tools)
            .Where(r => new FileInfo(r).Length > ReadmeMostBytes)
            .Select(r => $"{Slashed(Path.GetRelativePath(Root, r))} ({new FileInfo(r).Length} bytes)")
            .ToList();
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

            // A dated ADR is named by its slug, after the numbered ones, and supersedes one by a link to it.
            File.WriteAllText(Path.Combine(decisions, "2026-10-24-sliding-hatches.md"), "# Sliding hatches\n\n- Status: accepted; supersedes [0002](0002-hatches.md)\n");
            var dated = AdrIndex(decisions);
            Assert.Contains("| [0002](0002-hatches.md) | Hatches | accepted | sliding-hatches (in part) |", dated);
            Assert.EndsWith("| [sliding-hatches](2026-10-24-sliding-hatches.md) | Sliding hatches | accepted |  |\n", dated);
            // A dated one titled with a number, a one-word slug, and a name of neither form are refused.
            File.WriteAllText(Path.Combine(decisions, "2026-10-25-tiles.md"), "# Tiles\n\n- Status: accepted\n");
            Assert.Throws<InvalidOperationException>(() => AdrIndex(decisions));
            File.Delete(Path.Combine(decisions, "2026-10-25-tiles.md"));
            File.WriteAllText(Path.Combine(decisions, "2026-10-25-more-tiles.md"), "# 0003 — More tiles\n\n- Status: accepted\n");
            Assert.Throws<InvalidOperationException>(() => AdrIndex(decisions));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
