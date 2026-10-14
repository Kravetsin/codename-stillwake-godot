using System.Text.Json.Nodes;
using Stillwake.Sim;

namespace Stillwake.Game.Tests;

/// <summary>
/// Worlds with items in them for the client's tests: put into a world's save and loaded, as no command
/// makes items out of nothing and the items' slice is the sim's own (TypeScript tests wrote the slice).
/// </summary>
public static class ItemsWorlds
{
    /// <summary>A world from <paramref name="layout"/>, its items' slice changed by <paramref name="edit"/> before it loads.</summary>
    public static (Registry M, World World) With(Layout layout, Action<JsonNode> edit)
    {
        var made = new Registry();
        var root = JsonNode.Parse(World.Create(1, layout, made.All).Save())!;
        edit(root["slices"]!["items"]!);
        var m = new Registry();
        return (m, World.Load(root.ToJsonString(), m.All));
    }

    /// <summary>A stack as the slice writes one.</summary>
    public static JsonObject Stack(string item, int count) => new() { ["item"] = item, ["count"] = count };

    /// <summary>A stack at rest on the floor of cell (x, y), its bottom middle, dropped by <paramref name="by"/> on tick <paramref name="at"/>.</summary>
    public static JsonObject Lying(int id, string item, int count, int x, int y, int width, int by, int at) => new()
    {
        ["id"] = id,
        ["item"] = item,
        ["count"] = count,
        ["cell"] = y * width + x,
        ["x"] = x * 1000 + 500,
        ["y"] = (y + 1) * 1000,
        ["vx"] = 0,
        ["vy"] = 0,
        ["by"] = by,
        ["at"] = at,
    };
}
