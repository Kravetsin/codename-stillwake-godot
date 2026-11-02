using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using Godot;
using Stillwake.Game.Features.Crew;
using Stillwake.Net;
using Stillwake.Sim;
using Stillwake.Sim.Building;
using Spec = Stillwake.Content;
using Structure = Stillwake.Content.Structure;
using World = Stillwake.Sim.World;

namespace Stillwake.Game.Dev;

/// <summary>
/// The anime slice (tools/anime-art): the testbed's hero by the smelter in its suit, the slice's items on the hotbar and in the bag.
/// <c>=bare</c> without the suit; <c>=bag</c> the inventory open, for the tenth item; <c>=walk</c> (or <c>=walk-bare</c>) it walks a few
/// steps left and back; <c>=door</c> every door opened and shut; <c>=colours</c> (lit) and <c>=colours-dark</c> a row of players in each colour; <c>=cave</c> it stands by moss and a crystal in the first planet's caves, its helmet lamp on.
/// </summary>
[Dev("--anime", "the anime slice by the smelter; =bare no suit, =bag inventory open, =walk or =walk-bare a stroll left and back, =door every door opened and shut, =colours or =colours-dark a row of players, =cave by moss and a crystal", Shape = DevShape.Either, Order = 1)]
public sealed class AnimeScenario : DevScenario
{
    private static readonly string[] Carried = ["motor", "transceiver", "accumulator", "drone", "crystal", "ironOre", "iron", "scrap", "biomass", "circuit"];

    private bool _done;
    private int _door;
    private Node2D? _row;

    private string Mode => Args.Value("--anime") ?? "";

    private bool Cave => Mode is "cave" or "colours-dark";

    public override Layout? Map => Cave ? null : Testbed.Map;

    public override World? Create(IReadOnlyList<Module> modules) => Cave ? null : Testbed.Create(modules);

    public override World Seed(World world, DevSeed seed)
    {
        var root = JsonNode.Parse((Cave ? GroundDemo.Seed(world, seed.Modules) : world).Save())!;
        var items = root["slices"]!["items"]!;
        JsonNode? Stack(string item) => new JsonObject { ["item"] = item, ["count"] = 1 };
        var slots = Cave ? items["inventories"]![0]!["slots"]!.AsArray() : new JsonArray([.. Enumerable.Range(0, Spec.Inventory.Total).Select(i => i < Carried.Length ? Stack(Carried[i]) : null)]);
        if (!Mode.Contains("bare"))
        {
            slots[Spec.Inventory.Slots] = Stack(Spec.Helmet.Basic);
            slots[Spec.Inventory.Slots + Spec.Inventory.Equipment.ToList().IndexOf(Spec.Suit.Slot)] = Stack(Spec.Suit.Basic);
        }
        items["inventories"] = new JsonArray(new JsonObject { ["id"] = 0, ["slots"] = slots.DeepClone() });
        return World.Load(root.ToJsonString(), seed.Modules());
    }

    public override HeroAutopilot? Script => Mode.StartsWith("walk", StringComparison.Ordinal) ? HeroAutopilot.Stroll() : null;

    public override Func<Registry, int>? Start => Cave ? null : m => OpenDemo.StartOf(m, "smelter");

    public override void Attach(DevContext c)
    {
        if (Mode.StartsWith("colours", StringComparison.Ordinal)) c.Add(_row = new Node2D { ZIndex = Render.Layers.OtherHeroes });
    }

    /// <summary>A figure of every suit colour in a row by the hero, the two ends facing away from the others, to tell players apart and to see a turned hero lit alike.</summary>
    private void Colours(Node2D row, int column, int floor, int spacing)
    {
        for (int i = 0; i < Features.Crew.Suits.All.Length; i++)
        {
            int colour = Features.Crew.Suits.All[i];
            var figure = new Figure(colour, own: false);
            figure.Wear(colour, i != 3, i != 2);
            row.AddChild(figure);
            var hero = new Hero(0, 0, 0, 0, false, i % 2 == 0 ? 1 : -1);
            for (int t = 0; t < 40; t++) figure.Show(hero, Pose.Stand, 0.15, false, 1000.0 / 60);
            figure.Position = new Vector2((column + i * spacing) * (float)Rig.Cell, floor * (float)Rig.Cell);
        }
    }

    public override void Frame(Client client, Registry m, Action<string> log)
    {
        if (Mode == "colours" && _row is { } row && row.GetChildCount() == 0)
        {
            int at = Start!(m), w = m.Grid.Width;
            Colours(row, at % w + 5, at / w + 1, 5);
        }
        if (_done) return;
        if (Mode == "bag" && client.World.Tick > Testbed.AirTicks + 20)
        {
            _done = true;
            foreach (bool pressed in new[] { true, false })
                Input.ParseInputEvent(new InputEventKey { Keycode = Key.Tab, PhysicalKeycode = Key.Tab, Pressed = pressed });
        }
        if (Cave) Beam(client, log);
        if (Mode == "door") Doors(client, m);
    }

    /// <summary>Every door opened and, a little later, shut: <see cref="Autopilot.At"/> counts from a world's start, this one from its air.</summary>
    private void Doors(Client client, Registry m)
    {
        long tick = client.World.Tick;
        if (_door == 0 && tick >= Testbed.AirTicks + 20)
        {
            _door = 1;
            foreach (var c in Dev.Autopilot.At(20, m.Grid)) client.Send(c);
        }
        if (_door == 1 && tick >= Testbed.AirTicks + 60)
        {
            _door = 2;
            foreach (var c in Dev.Autopilot.At(40, m.Grid)) client.Send(c);
        }
    }

    /// <summary>The planet opened, then the hero beamed to its corner.</summary>
    private void Beam(Client client, Action<string> log)
    {
        var world = client.World;
        if (world.Surfaces.Count == 1) client.Send(new OpenSurface(Spec.PlanetTypes.First.Id));
        if (world.Surfaces.Count < 2) return;
        _done = true;
        var planet = Registry.On(world, world.Surfaces.Count - 1);
        int at = CornerOf(planet), w = planet.Grid.Width;
        client.Send(new Transfer(world.Surfaces.Count - 1, at));
        log($"anime: beamed down to ({at % w}, {at / w})");
        if (_row is { } row) Colours(row, at % w - 30, at / w + 1, 5);
    }

    /// <summary>A cell to stand on with a crystal on the ground beside it and moss in sight, the nearest the surface there is.</summary>
    private static int CornerOf(Registry m)
    {
        var grid = m.Grid;
        int w = grid.Width;
        bool Open(int cell) => cell >= 0 && grid.StructureAt(cell) == Structure.Empty;
        var moss = m.Building.Plants.Where(p => p.Kind == "moss").Select(p => p.Cell).ToList();
        var crystals = m.Building.Plants.Where(p => p.Kind.StartsWith("crystal", StringComparison.Ordinal) && !p.Kind.EndsWith("Hang", StringComparison.Ordinal));
        foreach (var crystal in crystals.OrderBy(p => p.Cell / w).ThenBy(p => Math.Abs(p.Cell % w - m.Crew.Spawn % w)))
            for (int dx = 2; dx <= 8; dx++)
                foreach (int side in (int[])[-1, 1])
                    for (int dy = -3; dy <= 3; dy++)
                    {
                        int stand = crystal.Cell + side * dx + dy * w;
                        if (!Open(stand) || !Open(stand - w) || !Open(stand - 2 * w) || Open(stand + w)) continue;
                        if (moss.Count(c => Math.Abs(c % w - stand % w) <= 14 && Math.Abs(c / w - stand / w) <= 8) >= 6) return stand;
                    }
        return m.Crew.Spawn;
    }
}
