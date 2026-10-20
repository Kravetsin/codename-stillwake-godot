using System.Text.Json;

namespace Stillwake.Net;

/// <summary>
/// One module whose state differed, with the first paths that did, as <c>a.b[3].c</c>: the ship's by its id, another
/// surface's as <c>1:grid</c> (0076); <c>surfaces</c> and <c>where</c> for which surfaces there are and who is on them.
/// </summary>
public sealed record ModuleDiff(string Id, IReadOnlyList<string> Paths);

/// <summary>How one peer's world differed from the host's, module by module, on one tick.</summary>
public sealed record DesyncReport(int Tick, bool Rng, IReadOnlyList<ModuleDiff> Modules)
{
    public override string ToString() =>
        $"tick {Tick}{(Rng ? ", rng drifted" : "")}: " +
        string.Join("; ", Modules.Select(m => $"{m.Id} at {string.Join(", ", m.Paths)}"));
}

/// <summary>Desync reports from two saves (packages/net/src/diff.ts).</summary>
public static class Diff
{
    /// <summary>Paths listed per module: enough to find the bug, not a dump of a drifted grid.</summary>
    private const int MaxPaths = 10;

    /// <summary>How the world saved in <paramref name="mine"/> differs from the one in <paramref name="theirs"/>: two saves of one tick.</summary>
    public static DesyncReport Saves(string mine, string theirs)
    {
        using var a = JsonDocument.Parse(mine);
        using var b = JsonDocument.Parse(theirs);
        var modules = new List<ModuleDiff>();
        Slices(a.RootElement.GetProperty("slices"), b.RootElement.GetProperty("slices"), "", modules);
        var surfacesA = Property(a.RootElement, "surfaces");
        var surfacesB = Property(b.RootElement, "surfaces");
        int na = surfacesA?.GetArrayLength() ?? 0, nb = surfacesB?.GetArrayLength() ?? 0;
        if (na != nb) modules.Add(new ModuleDiff("surfaces", ["surfaces.length"]));
        for (int i = 0; i < Math.Min(na, nb); i++)
        {
            var (x, y) = (surfacesA!.Value[i], surfacesB!.Value[i]);
            string at = $"{x.GetProperty("id").GetInt32()}:";
            var head = new List<string>();
            foreach (var key in new[] { "id", "kind", "seed" }) Walk(x.GetProperty(key), y.GetProperty(key), $"surfaces[{i}].{key}", head);
            if (head.Count > 0) modules.Add(new ModuleDiff("surfaces", head));
            Slices(x.GetProperty("slices"), y.GetProperty("slices"), at, modules);
        }
        var where = new List<string>();
        Walk(Property(a.RootElement, "where"), Property(b.RootElement, "where"), "where", where);
        if (where.Count > 0) modules.Add(new ModuleDiff("where", where));
        bool rng = a.RootElement.GetProperty("rng").GetRawText() != b.RootElement.GetProperty("rng").GetRawText();
        return new DesyncReport(b.RootElement.GetProperty("tick").GetInt32(), rng, modules);
    }

    /// <summary>Adds each module of one surface's slices that differs, its id after <paramref name="prefix"/>.</summary>
    private static void Slices(JsonElement sa, JsonElement sb, string prefix, List<ModuleDiff> modules)
    {
        foreach (var id in Keys(sa, sb))
        {
            var paths = new List<string>();
            Walk(Property(sa, id), Property(sb, id), id, paths);
            if (paths.Count > 0) modules.Add(new ModuleDiff(prefix + id, paths));
        }
    }

    /// <summary>Adds to <paramref name="paths"/> where <paramref name="a"/> and <paramref name="b"/> differ, depth first, until there are enough.</summary>
    private static void Walk(JsonElement? a, JsonElement? b, string at, List<string> paths)
    {
        if (paths.Count >= MaxPaths) return;
        if (!IsContainer(a) || !IsContainer(b))
        {
            if (!Same(a, b)) paths.Add(at);
            return;
        }
        var x = a!.Value;
        var y = b!.Value;
        if (x.ValueKind != y.ValueKind)
        {
            paths.Add(at);
            return;
        }
        if (x.ValueKind == JsonValueKind.Array)
        {
            int na = x.GetArrayLength(), nb = y.GetArrayLength();
            if (na != nb) paths.Add($"{at}.length");
            for (int i = 0; i < Math.Min(na, nb); i++) Walk(x[i], y[i], $"{at}[{i}]", paths);
            return;
        }
        foreach (var key in Keys(x, y)) Walk(Property(x, key), Property(y, key), $"{at}.{key}", paths);
    }

    private static bool IsContainer(JsonElement? e) => e is { ValueKind: JsonValueKind.Object or JsonValueKind.Array };

    private static bool Same(JsonElement? a, JsonElement? b) =>
        a is null || b is null ? a is null && b is null : a.Value.ValueKind == b.Value.ValueKind && a.Value.GetRawText() == b.Value.GetRawText();

    private static JsonElement? Property(JsonElement e, string name) => e.TryGetProperty(name, out var v) ? v : null;

    private static List<string> Keys(JsonElement a, JsonElement b) =>
        a.EnumerateObject().Select(p => p.Name).Union(b.EnumerateObject().Select(p => p.Name)).Order(StringComparer.Ordinal).ToList();
}
