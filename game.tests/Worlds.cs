using System.Text.Json.Nodes;
using Stillwake.Sim;

namespace Stillwake.Game.Tests;

/// <summary>A world of every registered module, as the client reads it: <c>stillwake.create</c> in the TypeScript tests.</summary>
public sealed class TestWorld
{
    public World World { get; private set; }
    public Registry M { get; private set; } = null!;

    public TestWorld(Layout layout, uint seed = 1)
    {
        World = World.Create(seed, layout, Modules());
    }

    public TestWorld(string map) : this(Layout.Parse(map)) { }

    private IReadOnlyList<Module> Modules()
    {
        M = new Registry();
        return M.All;
    }

    public TestWorld Step(params Command[] commands)
    {
        World.Step(commands);
        return this;
    }

    /// <summary>
    /// Puts <paramref name="items"/> into peer <paramref name="peer"/>'s inventory, through a save: no
    /// command makes items out of nothing, and the client's tests cannot reach the sim's internals.
    /// </summary>
    public TestWorld Give(int peer, params (string Item, int Count)[] items)
    {
        var root = JsonNode.Parse(World.Save())!;
        var slots = new JsonArray();
        foreach (var (item, count) in items) slots.Add(new JsonObject { ["item"] = item, ["count"] = count });
        while (slots.Count < Content.Inventory.Total) slots.Add(null);
        root["slices"]!["items"]!["inventories"] = new JsonArray(new JsonObject { ["id"] = peer, ["slots"] = slots });
        World = World.Load(root.ToJsonString(), Modules());
        return this;
    }

    /// <summary>The world with its save changed by <paramref name="edit"/>: what no command makes, put in as <see cref="Give"/> does.</summary>
    public TestWorld Edit(Action<JsonNode> edit)
    {
        var root = JsonNode.Parse(World.Save())!;
        edit(root);
        World = World.Load(root.ToJsonString(), Modules());
        return this;
    }
}

/// <summary>A link whose host is not here: a test releases ticks by hand (<c>guest</c> of predicted.test.ts).</summary>
public sealed class Fed : Stillwake.Net.IChannel<Stillwake.Net.ToClient, Stillwake.Net.ToHost>
{
    private Action<Stillwake.Net.ToClient> _receive = _ => { };
    public void Send(Stillwake.Net.ToHost message) { }
    public void OnMessage(Action<Stillwake.Net.ToClient> handler) => _receive = handler;
    public void Receive(Stillwake.Net.ToClient message) => _receive(message);
}
