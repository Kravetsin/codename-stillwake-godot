using System;
using System.Linq;
using System.Text.Json.Nodes;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using Stillwake.Sim;
using Stillwake.Sim.Grid;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Features.Debris;

/// <summary>
/// The scoop with a debris net hung on its bow (<c>--net</c>): holding as many pieces as asked, the hero out beside
/// it; <c>--net-press</c> moves the pointer onto it and presses E, emptying it. On the new game's scoop the right face of the column on its roof is
/// the one place the sim lets a net hang.
/// </summary>
public partial class NetDemo(Func<(Stillwake.Net.Client Client, Registry M)?> world) : CanvasLayer
{
    /// <summary>What the held pieces are, in catch order: the pick page's, then more of the same.</summary>
    private static readonly string[] Kinds = ["scrapChunk", "cargoCrate", "scrapChunk", "hullPlate", "supplyPod", "scrapChunk", "hullPlate", "scrapChunk", "cargoCrate", "scrapChunk"];

    /// <summary>The ids the held pieces drifted by: what picks each one's look.</summary>
    private const int FirstId = 9001;

    /// <summary>Seconds into the run the pointer is on the net, then E pressed.</summary>
    private const double PointAt = 1.5, PressAt = 2.2;

    /// <summary>
    /// The net's top-left cell, once hung; before, the bow's: the rightmost place the sim lets a net hang (its rows against the hull's wall,
    /// its cells in space), the lowest of those.
    /// </summary>
    public static int SpotOf(Registry m)
    {
        var grid = m.Grid;
        if (grid.MachinesOf("net").FirstOrDefault() is { } hung) return hung.Cell;
        var net = new Placement("machine", "net");
        int best = -1;
        for (int cell = 0; cell < grid.Width * grid.Height; cell++)
        {
            int x = cell % grid.Width, y = cell / grid.Width;
            bool further = best < 0 || x > best % grid.Width || x == best % grid.Width && y > best / grid.Width;
            if (further && grid.PlacementProblem(cell, net) == null) best = cell;
        }
        return best >= 0 ? best : throw new InvalidOperationException("No place on the bow for a net");
    }

    /// <summary>Where the hero stands: out beside the net, on what is under the cell a step past its outer side.</summary>
    public static int StartOf(Registry m)
    {
        int spot = SpotOf(m), w = m.Grid.Width, cell = spot + Spec.Machines.Get("net").Width + 1;
        while (cell + w < w * m.Grid.Height && !Spec.Structures.Is(m.Grid.StructureAt(cell + w), Spec.Trait.Terrain)) cell += w;
        return cell;
    }

    /// <summary><paramref name="world"/> with the net hung and holding <paramref name="held"/> pieces, through the save.</summary>
    public static World Seed(World world, Registry m, Func<System.Collections.Generic.IReadOnlyList<Module>> modules, int held)
    {
        int spot = SpotOf(m), w = m.Grid.Width;
        var root = JsonNode.Parse(world.Save())!;
        var grid = root["slices"]!["grid"]!;
        int id = (int)grid["nextMachineId"]!;
        grid["machines"]!.AsArray().Add(new JsonObject { ["id"] = id, ["kind"] = "net", ["cell"] = spot });
        grid["nextMachineId"] = id + 1;
        var entries = new JsonArray();
        for (int k = 0; k < Math.Min(held, Spec.Debris.NetHolds); k++)
        {
            string kind = Kinds[k];
            // A salvage piece gives itself; a container its loot, here a little scrap.
            var gives = Spec.Items.TryGet(kind, out _) ? (kind, 1) : ("scrap", 3);
            entries.Add(new JsonObject
            {
                ["net"] = id,
                ["id"] = FirstId + k * 7,
                ["kind"] = kind,
                ["cell"] = spot + (1 + k % 4) * w,
                ["items"] = new JsonArray(new JsonObject { ["item"] = gives.Item1, ["count"] = gives.Item2 }),
            });
        }
        if (entries.Count > 0) root["slices"]!["debris"]!["held"] = entries;
        return World.Load(root.ToJsonString(), modules());
    }

    private double _t;
    private bool _pressed;

    public override void _Process(double delta)
    {
        _t += delta;
        if (_t < PointAt || _pressed || world() is not { } now) return;
        var net = now.M.Grid.MachinesOf("net").FirstOrDefault();
        if (net == null) return;
        // The net's lowest row, nearest the hero inside.
        int w = now.M.Grid.Width, cell = net.Cell + 5 * w;
        var at = GetViewport().GetCanvasTransform() * (new Vector2(cell % w + 0.5f, cell / w + 0.5f) * ShipCamera.Cell);
        Input.ParseInputEvent(new InputEventMouseMotion { Position = at, GlobalPosition = at });
        if (_t < PressAt) return;
        Input.ParseInputEvent(new InputEventKey { Keycode = Key.E, PhysicalKeycode = Key.E, Pressed = true });
        Input.ParseInputEvent(new InputEventKey { Keycode = Key.E, PhysicalKeycode = Key.E, Pressed = false });
        _pressed = true;
        GD.Print($"[demo] E on net {net.Id} at {_t:0.00} s");
    }
}
