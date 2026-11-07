using System;
using System.Linq;
using System.Text.Json.Nodes;
using Stillwake.Game.Features.Crew;
using Stillwake.Net;
using Stillwake.Sim;

namespace Stillwake.Game.Dev;

/// <summary>
/// A planet opened and the hero beamed down at once (0077), or the test strip (0076). After the hero's own scenarios, whose
/// scripts (--ladders, --feel, --hills) win over the strip's.
/// </summary>
[Dev("--planet", "a planet grown from its seed, the hero beamed down with the axe, the drill and the helmet lamp (0077)", Order = 5)]
[Dev("--strip", "the test strip (0076); with --autopilot played by a script (HeroAutopilot.PlanetRun)", Order = 5)]
public sealed class PlanetScenario : DevScenario
{
    private bool _beamedBack;

    private bool Strip => Args.Has("--strip");

    // The hero carries the axe and the drill and wears the helmet lamp, to try the caves; --ground seeds the same.
    public override World Seed(World world, DevSeed seed) =>
        Args.Value("--ground") == null && (Args.Has("--planet") || Args.Given("--creatures") != null) ? GroundDemo.Seed(world, seed.Modules, ArcDrill) : world;

    public override HeroAutopilot? Script => Strip && Args.Has("--autopilot") ? HeroAutopilot.PlanetRun() : null;

    public override void Started(Client client)
    {
        client.Send(new OpenSurface((Strip ? Content.Surfaces.TestStrip : Content.PlanetTypes.First).Id));
        client.Send(new Transfer(client.World.Surfaces.Count));
    }

    /// <summary><c>--beam-back</c>: back aboard two seconds in, to see the switch both ways.</summary>
    public override void Frame(Client client, Registry m, Action<string> log)
    {
        if (_beamedBack || !Args.Has("--beam-back") || client.World.Tick < 2 * World.TicksPerSecond) return;
        _beamedBack = true;
        client.Send(new Transfer(0));
    }
}

/// <summary>Creatures let out by the hero, with --planet or --strip, hunted by script unless --manual (0102).</summary>
[Dev("--hitboxes", "with whatever shows creatures, the box each is hit in drawn over it (HitBoxDemo)")]
public sealed class HitBoxScenario : DevScenario
{
    public override void Attach(DevContext c) => c.Add(new HitBoxDemo(c.World));
}

[Dev("--creatures", "creatures of every kind let out by the hero, or =kind one kind, hunted by script (CreatureHands, 0102)", Shape = DevShape.Either)]
public sealed class CreaturesScenario : DevScenario
{
    private CreaturesDemo? _demo;

    public override void Attach(DevContext c)
    {
        // =tapir (a species or its look): only that kind let out.
        string? kind = Args.Given("--creatures") is { Length: > 0 } k ? k : null;
        string? ram = Args.Given("--creatures-ram") is { } r ? r.Length > 0 ? r : "hit" : null;
        bool drill = Args.Has("--creatures-drill"), course = Args.Has("--creatures-course");
        _demo = new CreaturesDemo(drill: drill, course: course, kind: kind, herd: Args.Has("--creatures-herd"), ram: ram) { Kill = Args.Has("--creatures-kill") };
        if (!Args.Has("--manual")) c.Add(new CreatureHands(_demo, c.Hero, still: drill, quick: course));
    }

    public override void Frame(Client client, Registry m, Action<string> log) => _demo?.Frame(client, log);
}

/// <summary>The nailer fired by script (shots-are-the-sims): on the strip with --strip, aboard without.</summary>
[Dev("--nailer", "the nailer and nails in hand, fired up at 45°, at a loper and level, by script (NailerDemo)")]
public sealed class NailerScenario : DevScenario
{
    private NailerDemo? _demo;

    public override World Seed(World world, DevSeed seed) => NailerDemo.Seed(world, seed.Modules);

    // With --manual the player fires: the loper let out, nothing else played.
    public override void Attach(DevContext c)
    {
        _demo = new NailerDemo(c.Hero);
        if (!Args.Has("--manual")) c.Add(_demo);
    }

    public override void Frame(Client client, Registry m, Action<string> log) => _demo?.Frame(client, m);
}

/// <summary>The railgun charged and fired by script (2026-10-27-the-railgun): on the strip with --strip, aboard without.</summary>
[Dev("--railgun", "the railgun held to charge and fired at two lopers, straight up and into the ground, once let go early (RailgunDemo)")]
public sealed class RailgunScenario : DevScenario
{
    private RailgunDemo? _demo;

    public override World Seed(World world, DevSeed seed) => RailgunDemo.Seed(world, seed.Modules);

    // With --manual the player fires: the lopers let out, nothing else played.
    public override void Attach(DevContext c)
    {
        _demo = new RailgunDemo(c.Hero);
        if (!Args.Has("--manual")) c.Add(_demo);
    }

    public override void Frame(Client client, Registry m, Action<string> log) => _demo?.Frame(client, m);
}

/// <summary>
/// The pickaxe (the-pickaxe) to try by hand beside the drill and the axe: the hero beamed into a cave under the spawn, the pickaxe in the first
/// slot, the drill on a full battery in the second, the axe in the third, the helmet lamp worn.
/// </summary>
[Dev("--pickaxe", "a planet's cave with the pickaxe, the drill on a full battery and the axe in slots 1 to 3, to dig stone by hand (the-pickaxe)")]
public sealed class PickaxeScenario : DevScenario
{
    private bool _beamed;

    public override World Seed(World world, DevSeed seed)
    {
        var root = JsonNode.Parse(GroundDemo.Seed(world, seed.Modules).Save())!;
        var slots = root["slices"]!["items"]!["inventories"]![0]!["slots"]!.AsArray();
        string[] tools = [Content.Weapons.Pickaxe.Item, Content.Weapons.Drill.Item, Content.Weapons.Axe.Item];
        for (int i = 0; i < tools.Length; i++) slots[i] = new JsonObject { ["item"] = tools[i], ["count"] = 1 };
        return World.Load(root.ToJsonString(), seed.Modules());
    }

    public override void Started(Client client) => client.Send(new OpenSurface(Content.PlanetTypes.First.Id));

    public override void Frame(Client client, Registry m, Action<string> log)
    {
        if (_beamed || client.World.Surfaces.Count < 2) return;
        _beamed = true;
        int planet = client.World.Surfaces.Count - 1;
        var ground = Registry.On(client.World, planet);
        int? cave = GroundDemo.CaveUnder(ground);
        client.Send(cave is int at ? new Transfer(planet, at) : new Transfer(planet));
        log($"pickaxe: beamed down to {(cave is int c ? $"({c % ground.Grid.Width}, {c / ground.Grid.Width})" : "the spawn")}");
    }
}

/// <summary>
/// Ore shining in lamp light (2026-11-07-ore-pockets): the first planet, the hero beamed onto a dark cave's floor facing the cave's wall
/// richest in copper and iron at head height, the helmet lamp on, creative so the air never runs out; <c>=off</c> without the lamp, to
/// see the same ore as dark as its rock.
/// </summary>
[Dev("--ore-shine", "a dark cave's floor facing copper and iron veins, the helmet lamp on (=off without), creative (2026-11-07-ore-pockets)", Shape = DevShape.Either)]
public sealed class OreShineScenario : DevScenario
{
    private bool _beamed, _creative;
    private int _planet;

    public override World Seed(World world, DevSeed seed)
    {
        var root = JsonNode.Parse(world.Save())!;
        var slots = new JsonArray();
        int Worn(string slot) => Content.Inventory.Slots + Content.Inventory.Equipment.ToList().IndexOf(slot);
        // A lamp shines only on a helmet: both worn, or the helmet alone.
        for (int i = 0; i < Content.Inventory.Total; i++)
            slots.Add(i == Worn(Content.Helmet.Slot) ? new JsonObject { ["item"] = Content.Helmet.Basic, ["count"] = 1 }
                : i == Worn(Content.Helmet.Mount) && Args.Given("--ore-shine") != "off" ? new JsonObject { ["item"] = Content.HelmetLamp.Item, ["count"] = 1 }
                : null);
        root["slices"]!["items"]!["inventories"] = new JsonArray(new JsonObject { ["id"] = 0, ["slots"] = slots });
        return World.Load(root.ToJsonString(), seed.Modules());
    }

    public override void Started(Client client) => client.Send(new OpenSurface(Content.PlanetTypes.First.Id));

    public override void Frame(Client client, Registry m, Action<string> log)
    {
        var world = client.World;
        if (_beamed && !_creative && world.SurfaceOf(client.Id) == _planet)
        {
            _creative = true;
            client.Send(new Sim.Items.SetCreative(true));
        }
        if (_beamed || world.Surfaces.Count < 2) return;
        _beamed = true;
        _planet = world.Surfaces.Count - 1;
        var ground = Registry.On(world, _planet);
        if (Spot(ground) is not var (at, copper, iron))
        {
            client.Send(new Transfer(_planet));
            log("ore shine: no cave wall with ore found; beamed to the spawn");
            return;
        }
        client.Send(new Transfer(_planet, at));
        log($"ore shine: beamed to ({at % ground.Grid.Width}, {at / ground.Grid.Width}), facing {copper} copper and {iron} iron cells");
    }

    /// <summary>
    /// A floor cell with room for the hero over it, at least 20 rows under the sky, whose wall to the right holds the most copper and iron
    /// vein cells within reach of the beam (both kinds counted first); null if no such wall has any.
    /// </summary>
    private static (int At, int Copper, int Iron)? Spot(Registry m)
    {
        var grid = m.Grid;
        int w = grid.Width, h = grid.Height, fog = ((Content.PlanetType)m.Rules).Ground.Fog + 8;
        bool Open(int x, int y) => x >= 0 && y >= 0 && x < w && y < h && grid.StructureAt(y * w + x) == Content.Structure.Empty;
        var sky = new int[w];
        for (int x = 0; x < w; x++) sky[x] = Enumerable.Range(0, h).FirstOrDefault(y => !Open(x, y));
        (int At, int Copper, int Iron, int Score)? best = null;
        for (int y = 8; y < h - 1; y++)
            for (int x = fog; x < w - fog; x++)
            {
                if (y < sky[x] + 20 || Open(x, y + 1) || Enumerable.Range(0, 6).Any(k => !Open(x, y - k))) continue;
                int copper = 0, iron = 0;
                for (int dy = -7; dy <= 1; dy++)
                    for (int dx = 1; dx <= 5; dx++)
                        if (grid.WallAt((y + dy) * w + x + dx)?.Id is { } id) (copper, iron) = id == "copperVein" ? (copper + 1, iron) : id == "ironVein" ? (copper, iron + 1) : (copper, iron);
                int score = Math.Min(copper, iron) * 100 + copper + iron;
                if (score > 0 && (best == null || score > best.Value.Score)) best = (y * w + x, copper, iron, score);
            }
        return best is var (at, c, i, _) ? (at, c, i) : null;
    }
}

/// <summary>
/// A grown lake to swim in and dig open (docs/plans/liquids.md): the first planet, the hero beamed onto the floor beside the lake
/// nearest the middle, the pickaxe and the drill in hand's reach, the helmet lamp on, creative so the air never runs out.
/// </summary>
[Dev("--lake", "the first planet's lake nearest the middle, the hero on its bank with the pickaxe, the drill and the lamp, creative (liquids)")]
public sealed class LakeScenario : DevScenario
{
    private bool _beamed, _creative;
    private int _planet;

    public override World Seed(World world, DevSeed seed)
    {
        var root = JsonNode.Parse(world.Save())!;
        var slots = new JsonArray();
        int Worn(string slot) => Content.Inventory.Slots + Content.Inventory.Equipment.ToList().IndexOf(slot);
        string[] tools = [Content.Weapons.Pickaxe.Item, Content.Weapons.Drill.Item];
        for (int i = 0; i < Content.Inventory.Total; i++)
            slots.Add(i < tools.Length ? new JsonObject { ["item"] = tools[i], ["count"] = 1 }
                : i == Worn(Content.Helmet.Slot) ? new JsonObject { ["item"] = Content.Helmet.Basic, ["count"] = 1 }
                : i == Worn(Content.Helmet.Mount) ? new JsonObject { ["item"] = Content.HelmetLamp.Item, ["count"] = 1 }
                : null);
        root["slices"]!["items"]!["inventories"] = new JsonArray(new JsonObject { ["id"] = 0, ["slots"] = slots });
        return World.Load(root.ToJsonString(), seed.Modules());
    }

    public override void Started(Client client) => client.Send(new OpenSurface(Content.PlanetTypes.First.Id));

    public override void Frame(Client client, Registry m, Action<string> log)
    {
        var world = client.World;
        if (_beamed && !_creative && world.SurfaceOf(client.Id) == _planet)
        {
            _creative = true;
            client.Send(new Sim.Items.SetCreative(true));
        }
        if (_beamed || world.Surfaces.Count < 2) return;
        _beamed = true;
        _planet = world.Surfaces.Count - 1;
        var ground = Registry.On(world, _planet);
        if (Bank(ground) is not int at)
        {
            client.Send(new Transfer(_planet));
            log("lake: no lake with a bank found; beamed to the spawn");
            return;
        }
        client.Send(new Transfer(_planet, at));
        log($"lake: beamed to ({at % ground.Grid.Width}, {at / ground.Grid.Width})");
    }

    /// <summary>
    /// A dry floor cell with room for the hero over it beside the surface of the lake nearest the middle's column, within 16 columns
    /// of the surface's end and a few rows of it; null for none.
    /// </summary>
    private static int? Bank(Registry m)
    {
        var grid = m.Grid;
        var layer = m.Liquids.Layer.ToArray();
        if (layer.Length == 0) return null;
        int w = grid.Width, h = grid.Height;
        bool Open(int x, int y) => x >= 0 && y >= 0 && x < w && y < h && grid.StructureAt(y * w + x) == Content.Structure.Empty;
        bool Wet(int x, int y) => x >= 0 && y >= 0 && x < w && y < h && m.Liquids.AmountAt(y * w + x) > 0;
        var surfaces = Enumerable.Range(0, layer.Length).Where(c => layer[c] != 0 && !Wet(c % w, c / w - 1) && Open(c % w, c / w - 1));
        foreach (int top in surfaces.OrderBy(c => Math.Abs(c % w - w / 2)).Take(64))
        {
            int x0 = top % w, y0 = top / w;
            foreach (int way in (int[])[-1, 1])
            {
                int end = x0;
                while (Wet(end + way, y0)) end += way;
                for (int x = end + way; Math.Abs(x - end) <= 16; x += way)
                    for (int y = y0 - 6; y <= y0; y++)
                        if (Open(x, y) && !Wet(x, y) && !Open(x, y + 1) && Enumerable.Range(1, 6).All(k => Open(x, y - k))) return y * w + x;
            }
        }
        return null;
    }
}
