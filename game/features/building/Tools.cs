using System.Collections.Generic;
using System.Linq;
using Stillwake.Content;
using Stillwake.Game.I18n;
using Stillwake.Game.Shared;
using Stillwake.Sim.Building;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Power;
using Stillwake.Sim.Research;

namespace Stillwake.Game.Features.Building;

/// <summary>What a drag does: a card's work, or the middle button's removal (0030).</summary>
public abstract record Act;

/// <summary>A card in hand: what the left button does in the world with it; <c>Id</c> names its text.</summary>
public abstract record Tool(string Id, string Label) : Act;

/// <summary>A card that places <paramref name="Placement"/> along a drag.</summary>
public sealed record PlaceTool(string Id, Placement Placement, string Label) : Tool(Id, Label);

/// <summary>The cable card: laid by the power feature, from press to release.</summary>
public sealed record CableTool(string Id, string Label) : Tool(Id, Label);

/// <summary>The middle button's removal from <paramref name="Layers"/>, in the order they are taken.</summary>
public sealed record Remove(IReadOnlyList<string> Layers) : Act;

/// <summary>A tab of the build menu, its cards, and the overlay it shows while open, if any: the tab is the mode (0030).</summary>
public sealed record Tab(string Id, IReadOnlyList<Tool> Tools, string? Overlay = null);

/// <summary>
/// The build menu's cards and the rules of the hand around them (features/building/tools.ts, 0030):
/// which overlay shows for the open tab, which layers removal and the pipette take, the pipette's card
/// for a cell and the hint each tab gives.
/// </summary>
public static class Tools
{
    /// <summary>
    /// Where a plan's sparks fly from and which way, in radians with y down: from the point of its
    /// <paramref name="box"/> nearest the nearest of <paramref name="heroes"/> within <paramref name="reach"/>
    /// pixels, toward that hero — the side being worked on; with none near, or one inside the box, from
    /// its middle, upward.
    /// </summary>
    public static (double X, double Y, double Heading) SparksOf(
        (double X, double Y, double W, double H) box, IReadOnlyList<(float X, float Y)> heroes, double reach)
    {
        (double X, double Y)? nearest = null;
        double best = reach * reach;
        foreach (var (hx, hy) in heroes)
        {
            double dx = hx - System.Math.Clamp(hx, box.X, box.X + box.W), dy = hy - System.Math.Clamp(hy, box.Y, box.Y + box.H);
            if (dx * dx + dy * dy <= best) (best, nearest) = (dx * dx + dy * dy, (hx, hy));
        }
        if (nearest is not var (x, y) || best == 0) return (box.X + box.W / 2, box.Y + box.H / 2, -System.Math.PI / 2);
        // A pixel inside its edge, so they leave the thing itself.
        double sx = System.Math.Clamp(x, box.X + 1, box.X + box.W - 1), sy = System.Math.Clamp(y, box.Y + 1, box.Y + box.H - 1);
        return (sx, sy, System.Math.Atan2(y - sy, x - sx));
    }

    /// <summary>The overlays by their features' ids.</summary>
    public const string Power = "power", Pipes = "pipes", Air = "atmosphere";

    private static PlaceTool Place(string id, Placement placement, string? label = null) =>
        new(id, placement, label ?? En.T($"tool.{id}"));

    /// <summary>A card for each material, its id and its text's key followed by the material's name.</summary>
    private static IEnumerable<Tool> OfEveryMaterial(string id, System.Func<string, Placement> placement) =>
        Materials.All.Select(m => Place(
            id + char.ToUpperInvariant(m.Id[0]) + m.Id[1..],
            placement(m.Id),
            $"{En.T($"tool.{id}")}: {En.T($"material.{m.Id}")}"));

    /// <summary>The build menu's tabs, in order, each with its cards: what the player builds, grouped by use.</summary>
    public static readonly IReadOnlyList<Tab> Tabs =
    [
        new("structure",
        [
            .. OfEveryMaterial("wall", Placement.Wall),
            .. OfEveryMaterial("backWall", Placement.BackWall),
            Place("floor", new Placement("structure", "floor")),
            Place("ladder", new Placement("structure", "ladder")),
            Place("door", new Placement("structure", "door")),
        ]),
        new("air",
        [
            Place("pipe", new Placement("conduit", "pipe")),
            Place("valve", new Placement("conduit", "valve")),
            Place("vent", new Placement("machine", "vent")),
            Place("tank", new Placement("machine", "tank")),
            Place("generator", new Placement("machine", "generator")),
        ], Pipes),
        new("power",
        [
            new CableTool("cable", En.T("tool.cable")),
            Place("solarPanel", new Placement("machine", "solarPanel")),
            Place("burnerGenerator", new Placement("machine", "burnerGenerator")),
            Place("lamp", new Placement("machine", "lamp")),
            Place("charger", new Placement("machine", "charger")),
        ], Power),
        new("production",
        [
            Place("furnace", new Placement("machine", "furnace")),
            Place("analyzer", new Placement("machine", "analyzer")),
            Place("former", new Placement("machine", "former")),
            Place("smelter", new Placement("machine", "smelter")),
            Place("assembler", new Placement("machine", "assembler")),
            Place("growCapsule", new Placement("machine", "growCapsule")),
        ]),
        new("storage", [Place("crate", new Placement("machine", "crate")), Place("droneBay", new Placement("machine", "droneBay"))]),
    ];

    /// <summary>Every card, tab by tab.</summary>
    public static readonly IReadOnlyList<Tool> All = Tabs.SelectMany(t => t.Tools).ToArray();

    /// <summary>The cable card.</summary>
    public static readonly Tool Cable = All.OfType<CableTool>().Single();

    /// <summary>What a card costs, by <paramref name="costFor"/> (the sim's own rule) for what it places; a cable its own.</summary>
    public static IReadOnlyList<(string Item, int Count)> CostOf(Tool tool, System.Func<Placement, IReadOnlyList<(string Item, int Count)>> costFor) =>
        tool is PlaceTool p ? costFor(p.Placement) : Content.Power.CableCost;

    /// <summary>
    /// The overlay that shows: the open tab's, if the menu is open and the tab has one; else air if the
    /// player wants it; else none. With the menu closed, air or nothing.
    /// </summary>
    public static string? OverlayShown(bool planOut, string tab, bool airOn) =>
        planOut && Tabs.FirstOrDefault(t => t.Id == tab)?.Overlay is { } overlay ? overlay : airOn ? Air : null;

    /// <summary>The tab a card is on.</summary>
    public static string TabOf(Tool tool) => Tabs.First(t => t.Tools.Contains(tool)).Id;

    /// <summary>What the buttons do on a tab, an i18n key: the pipes' and the cables' tabs say their own.</summary>
    public static string HintOf(string tab) => tab switch
    {
        "air" => "hint.air",
        "power" => "hint.power",
        _ => "hint.build",
    };

    /// <summary>
    /// The layers the middle button removes from and Q picks in, under <paramref name="overlay"/>, in the
    /// order they are taken: pipes, valves and the machines on them under the pipes overlay, else what the
    /// ship is built of. Cables, under the power overlay, are the power feature's.
    /// </summary>
    public static IReadOnlyList<string> LayersFor(string? overlay) =>
        overlay == Pipes ? ["conduit", "machine"] : ["machine", "structure", "back"];

    /// <summary>Whether <paramref name="tool"/> is a card the world knows (0024): the cable always, else its blueprint.</summary>
    public static bool Known(Tool tool, IResearchQueries research) =>
        tool is not PlaceTool p || research.Knows(p.Placement);

    /// <summary>What is placed at <paramref name="cell"/> in <paramref name="layers"/>: a plan first, then what is built, in their order.</summary>
    private static Placement? PlacementAt(int cell, IReadOnlyList<string> layers, IGridQueries grid, BuildingModule building)
    {
        foreach (var plan in building.PlansAt(cell))
            if (plan.Place is { } place && layers.Contains(place.Layer)) return place;
        foreach (var layer in layers)
            if (BuiltAt(cell, layer, grid) is { } built) return built;
        return null;
    }

    /// <summary>What is built at <paramref name="cell"/> in <paramref name="layer"/>, as the card that places it would: what a mark takes out.</summary>
    public static Placement? BuiltAt(int cell, string layer, IGridQueries grid)
    {
        switch (layer)
        {
            case "machine":
                return grid.MachineAt(cell) is { } m ? new Placement(layer, m.Kind) : null;
            case "conduit":
                int c = grid.ConduitAt(cell);
                return c != Conduit.None ? new Placement(layer, c == Conduit.Pipe ? "pipe" : "valve") : null;
            case "structure":
                return grid.StructureAt(cell) switch
                {
                    Structure.Wall => Placement.Wall(grid.WallAt(cell)!.Id),
                    Structure.Floor => new Placement(layer, "floor"),
                    Structure.Ladder => new Placement(layer, "ladder"),
                    Structure.DoorClosed or Structure.DoorOpen => new Placement(layer, "door"),
                    _ => null,
                };
            default:
                return grid.BackAt(cell) is { } material ? Placement.BackWall(material.Id) : null;
        }
    }

    /// <summary>
    /// The sides of <paramref name="cell"/> that <paramref name="place"/>, planned or in hand there, joins when
    /// drawn: what is <paramref name="planned"/> beside it and what is built, as the grid would draw it built — a
    /// pipe reaching every conduit, a wall running on into every wall, a door into the door above or below.
    /// </summary>
    public static int LinksOf(Placement place, int cell, int width, System.Func<int, IEnumerable<Placement>> planned, IGridQueries grid) =>
        !Render.Blueprint.Joins(place, place) ? 0 : Render.Blueprint.LinksOf(cell, width, c =>
            planned(c).Any(o => Render.Blueprint.Joins(place, o)) || BuiltAt(c, place.Layer, grid) is { } built && Render.Blueprint.Joins(place, built));

    /// <summary>
    /// The pipette's card for <paramref name="cell"/> under <paramref name="overlay"/>: under the power
    /// overlay the cable, on a node a cable ends at; else the card that places what is there, planned or
    /// built, in <see cref="LayersFor"/>, if its blueprint is known.
    /// </summary>
    public static Tool? CardAt(int cell, string? overlay, IGridQueries grid, BuildingModule building, IPowerQueries power, IResearchQueries research)
    {
        if (overlay == Power)
            return power.NodeAt(cell) is int node && power.Cabled(node) ? Cable : null;
        if (PlacementAt(cell, LayersFor(overlay), grid, building) is not { } placed) return null;
        var card = All.OfType<PlaceTool>().FirstOrDefault(each =>
            each.Placement.Layer == placed.Layer && each.Placement.Kind == placed.Kind && each.Placement.Material == placed.Material);
        // What is not known is not picked: it could not be planned.
        return card != null && Known(card, research) ? card : null;
    }
}
