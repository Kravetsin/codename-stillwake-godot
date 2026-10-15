using System;
using System.Linq;
using System.Text.Json.Nodes;

namespace Stillwake.Game.Platform;

/// <summary>
/// Which build this is, and whether a newer one is out (docs/releasing.md): a release is built by CI
/// from a tag <c>vX.Y</c> and published on GitHub; the game asks GitHub for the latest and, offered it,
/// fetches it and puts it over itself. Plain .NET, tested without Godot.
/// </summary>
public static class Updates
{
    /// <summary>Where GitHub says which release is the latest.</summary>
    public const string LatestUrl = "https://api.github.com/repos/Kravetsin/codename-stillwake-godot/releases/latest";

    /// <summary>A build: its release, none for one built at a desk, and its commit, short.</summary>
    public readonly record struct Version(string? Release, string Commit)
    {
        /// <summary>As the menu shows it: <c>v0.3 · 67019ff</c>, or <c>dev · 67019ff</c>.</summary>
        public override string ToString() => $"{(Release is { } r ? $"v{r}" : "dev")} · {Commit}";
    }

    /// <summary>A release on GitHub: its tag and where its Windows zip downloads from.</summary>
    public readonly record struct Release(string Tag, string Zip);

    /// <summary>
    /// The build an assembly's informational version names, <c>0.3+67019ff…</c>: a release when CI set
    /// the version from a tag, a desk build at the project's 0.0.0.
    /// </summary>
    public static Version Of(string informational)
    {
        var parts = informational.Split('+', 2);
        string commit = parts.Length > 1 ? parts[1][..Math.Min(7, parts[1].Length)] : "unknown";
        string number = parts[0];
        return new Version(number is "0.0.0" or "1.0.0" or "" ? null : number, commit);
    }

    /// <summary>The latest release as GitHub's API answers it, and its zip; none without a tag or a zip.</summary>
    public static Release? Read(string json)
    {
        JsonNode? root;
        try
        {
            root = JsonNode.Parse(json);
        }
        catch (System.Text.Json.JsonException)
        {
            return null;
        }
        if (root?["tag_name"]?.GetValue<string>() is not { } tag) return null;
        var zip = (root["assets"] as JsonArray)?
            .Select(a => a?["browser_download_url"]?.GetValue<string>())
            .FirstOrDefault(u => u != null && u.EndsWith(".zip", StringComparison.OrdinalIgnoreCase));
        return zip == null ? null : new Release(tag, zip);
    }

    /// <summary>
    /// Whether <paramref name="tag"/> is newer than this build's <paramref name="release"/>, number by
    /// number. A desk build is offered nothing: it is newer than any release, or a work in progress.
    /// </summary>
    public static bool Newer(string tag, string? release)
    {
        if (release == null) return false;
        static int[] Numbers(string v) => v.TrimStart('v').Split('.').Select(n => int.TryParse(n, out int i) ? i : 0).ToArray();
        int[] a = Numbers(tag), b = Numbers(release);
        for (int i = 0; i < Math.Max(a.Length, b.Length); i++)
        {
            int x = i < a.Length ? a[i] : 0, y = i < b.Length ? b[i] : 0;
            if (x != y) return x > y;
        }
        return false;
    }

    /// <summary>
    /// A Windows script that waits for process <paramref name="pid"/>, the game, to close, copies the new
    /// build from <paramref name="from"/> over the folder <paramref name="to"/>, and starts
    /// <paramref name="exe"/> again. Saves are elsewhere, in the user's folder: nothing of them is touched.
    /// </summary>
    public static string Script(int pid, string from, string to, string exe) => string.Join("\r\n",
        "@echo off",
        "rem Stillwake's updater: puts the new build over the old once the game has closed.",
        ":wait",
        $"tasklist /FI \"PID eq {pid}\" | find \"{pid}\" >nul && (timeout /t 1 /nobreak >nul & goto wait)",
        $"robocopy \"{from}\" \"{to}\" /E /NFL /NDL /NJH /NJS /NP >nul",
        $"start \"\" \"{exe}\"",
        "");
}
