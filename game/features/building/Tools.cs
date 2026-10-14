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

/// <summary>What a drag does: a card's work, or the right button's removal (0022).</summary>
public abstract record Act;

/// <summary>A card in hand: what the left button does in the world with it; <c>Id</c> names its text.</summary>
public abstract record Tool(string Id, string Label) : Act;

/// <summary>A card that places <paramref name="Placement"/> along a drag.</summary>
public sealed record PlaceTool(string Id, Placement Placement, string Label) : Tool(Id, Label);

/// <summary>The cable card: laid by the power feature, from press to release.</summary>
public sealed record CableTool(string Id, string Label) : Tool(Id, Label);

/// <summary>The right button's removal from <paramref name="Layers"/>, in the order they are taken.</summary>
public sealed record Remove(IReadOnlyList<string> Layers) : Act;

/// <summary>A tab of the build menu and its cards.</summary>
public sealed record Tab(string Id, IReadOnlyList<Tool> Tools);

/// <summary>
/// The build menu's cards and the rules of the hand around them (features/building/tools.ts): which
/// overlay a card is used in, which tab an overlay opens the menu on, which layers removal and the
/// pipette take, and the pipette's card for a cell.
/// </summary>
public static class Tools
{
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
        ]),
        new("power",
        [
            new CableTool("cable", En.T("tool.cable")),
            Place("solarPanel", new Placement("machine", "solarPanel")),
            Place("lamp", new Placement("machine", "lamp")),
        ]),
        new("production",
        [
            Place("furnace", new Placement("machine", "furnace")),
            Place("analyzer", new Placement("machine", "analyzer")),
        ]),
        new("storage", [Place("crate", new Placement("machine", "crate"))]),
    ];

    /// <summary>Every card, tab by tab.</summary>
    public static readonly IReadOnlyList<Tool> All = Tabs.SelectMany(t => t.Tools).ToArray();

    /// <summary>The cable card.</summary>
    public static readonly Tool Cable = All.OfType<CableTool>().Single();

    /// <summary>What a card costs, by <paramref name="costFor"/> (the sim's own rule) for what it places; a cable its own.</summary>
    public static IReadOnlyList<(string Item, int Count)> CostOf(Tool tool, System.Func<Placement, IReadOnlyList<(string Item, int Count)>> costFor) =>
        tool is PlaceTool p ? costFor(p.Placement) : Content.Power.CableCost;

    /// <summary>
    /// The overlay a card is used in, where what it joins is in sight: pipes for a pipe, a valve and a
    /// machine with ports; power for the cable and a machine with a socket and no ports; none for the rest.
    /// </summary>
    public static string? OverlayFor(Tool tool)
    {
        if (tool is not PlaceTool { Placement: var placement }) return Power;
        if (placement.Layer == "conduit") return Pipes;
        if (placement.Layer != "machine") return null;
        var spec = Machines.Get(placement.Kind);
        return spec.Ports.Count > 0 ? Pipes : spec.Plug != null ? Power : null;
    }

    /// <summary>The menu's tab an overlay opens it on: what is built in what it shows.</summary>
    public static string? TabFor(string? overlay) => overlay switch
    {
        Power => "power",
        Pipes or Air => "air",
        _ => null,
    };

    /// <summary>
    /// The layers the right button removes from and the middle one picks in, under <paramref name="overlay"/>,
    /// in the order they are taken: pipes and valves under the pipes overlay, else what the ship is built
    /// of. Cables, under the power overlay, are the power feature's.
    /// </summary>
    public static IReadOnlyList<string> LayersFor(string? overlay) =>
        overlay == Pipes ? ["conduit"] : ["machine", "structure", "back"];

    /// <summary>Whether <paramref name="tool"/> is a card the world knows (0024): the cable always, else its blueprint.</summary>
    public static bool Known(Tool tool, IResearchQueries research) =>
        tool is not PlaceTool p || research.Knows(p.Placement);

    /// <summary>What is placed at <paramref name="cell"/> in <paramref name="layers"/>: a plan first, then what is built, in their order.</summary>
    private static Placement? PlacementAt(int cell, IReadOnlyList<string> layers, IGridQueries grid, BuildingModule building)
    {
        foreach (var plan in building.PlansAt(cell))
            if (plan.Place is { } place && layers.Contains(place.Layer)) return place;
        foreach (var layer in layers)
        {
            switch (layer)
            {
                case "machine":
                    if (grid.MachineAt(cell) is { } m) return new Placement(layer, m.Kind);
                    break;
                case "conduit":
                    int c = grid.ConduitAt(cell);
                    if (c != Conduit.None) return new Placement(layer, c == Conduit.Pipe ? "pipe" : "valve");
                    break;
                case "structure":
                    switch (grid.StructureAt(cell))
                    {
                        case Structure.Wall: return Placement.Wall(grid.WallAt(cell)!.Id);
                        case Structure.Floor: return new Placement(layer, "floor");
                        case Structure.Ladder: return new Placement(layer, "ladder");
                        case Structure.DoorClosed or Structure.DoorOpen: return new Placement(layer, "door");
                    }
                    break;
                default:
                    if (grid.BackAt(cell) is { } material) return Placement.BackWall(material.Id);
                    break;
            }
        }
        return null;
    }

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
