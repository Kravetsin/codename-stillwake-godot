using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using Stillwake.Net;
using Stillwake.Sim;
using Stillwake.Sim.Building;
using Stillwake.Sim.Grid;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Dev;

/// <summary>
/// A script for the power driver (<c>-- --driver-demo=MODE</c>, power-driver) in a lit room: a 4×4 run of iron wall plans
/// beside the hero, the driver first in the hotbar, plates to pay, and the left button held on the run from tick 68.
/// <c>square</c> with a battery on the belt: the 2×2 square outlined and four walls going up at once; <c>cell</c> the same,
/// switched to one cell by a right click first; <c>flat</c> with no battery: one cell, as the screwdriver; <c>short</c> the square
/// with two plates for its four walls: two wait, and its tip says what all four still need.
/// </summary>
public sealed class DriverDemo(Hands hands, string mode)
{
    /// <summary>The room: lamps under its ceiling, the hero waking on its floor.</summary>
    public static readonly Layout Map = Layout.Parse(
        """
        ##############
        #L....L.....L#
        #............#
        #............#
        #....S.......#
        ##############
        """).Doubled().InWorld();

    private (double X, double Y)? _pointer;
    private int _done = -1;

    /// <summary>The run's top-left cell from the first lamp's, and its side.</summary>
    private const int Dx = 14, Dy = 2, Side = 4;

    /// <summary>Plates enough for the whole run, an iron wall a plate.</summary>
    public const int Plates = Side * Side;

    /// <summary>The world with the driver first in peer 0's hotbar, <paramref name="plates"/> and, with a <paramref name="battery"/>, a full one on the belt.</summary>
    public static World Seed(World world, Func<IReadOnlyList<Module>> modules, bool battery, int plates)
    {
        var root = JsonNode.Parse(world.Save())!;
        var slots = new JsonArray();
        int belt = Spec.Inventory.Slots + 2;
        for (int i = 0; i < Spec.Inventory.Total; i++)
            slots.Add(i == 0 ? new JsonObject { ["item"] = Spec.Screwdrivers.PowerDriver.Item, ["count"] = 1 }
                : i == 1 ? new JsonObject { ["item"] = "plate", ["count"] = plates }
                : i == belt && battery ? new JsonObject { ["item"] = "battery", ["count"] = 1, ["charge"] = Spec.Charge.Battery }
                : null);
        root["slices"]!["items"]!["inventories"] = new JsonArray(new JsonObject { ["id"] = 0, ["slots"] = slots });
        return World.Load(root.ToJsonString(), modules());
    }

    public void Start() => hands.Mouse.Script = () => _pointer;

    /// <summary>A step of the script on world tick <c>client.World.Tick</c>; it says what it does to the log.</summary>
    public void Frame(Client client, Registry m, Action<string> log)
    {
        var grid = m.Grid;
        if (grid.MachinesOf("lamp").Select(l => l.Cell).DefaultIfEmpty(-1).Min() is not (>= 0 and var lamp)) return;
        int w = grid.Width, x0 = lamp % w + Dx, y0 = lamp / w + Dy;
        // Each step once, on the first frame of its tick or after: frames outnumber ticks.
        int tick = client.World.Tick switch { >= 68 => 68, >= 60 => 60, >= 50 => 50, >= 10 => 10, _ => -1 };
        if (tick <= _done) return;
        _done = tick;
        switch (tick)
        {
            case 10:
                log($"a {Side}×{Side} run of iron walls planned");
                for (int y = y0; y < y0 + Side; y++)
                    for (int x = x0; x < x0 + Side; x++) client.Send(new PlacePlan(y * w + x, Placement.Wall("iron")));
                // On the corner inside the run's bottom-left quarter: the square is that quarter.
                _pointer = (x0 + 1.1, y0 + Side - 0.9);
                hands.Mouse.Buttons.OnPointer(false, 0, _pointer.Value);
                break;
            case 50 when mode == "cell":
                log("a right click: one cell");
                hands.Mouse.Buttons.OnPointer(true, Buttons.Right, _pointer!.Value);
                break;
            case 60:
                hands.Mouse.Buttons.OnPointer(false, 0, _pointer!.Value);
                break;
            case 68:
                log("the left button held on the run");
                hands.Mouse.Buttons.OnPointer(true, Buttons.Left, _pointer!.Value);
                break;
        }
    }
}
