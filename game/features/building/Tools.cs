using System.Collections.Generic;
using System.Linq;
using Stillwake.Content;
using Stillwake.Game.I18n;
using Stillwake.Game.Shared;
using Stillwake.Sim.Building;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Research;

namespace Stillwake.Game.Features.Building;

/// <summary>What a drag does: a card's placement on the left button, or a removal on the right (0030, 0108).</summary>
public abstract record Act;

/// <summary>A card in hand: what the left button plans in the world with it, the right marking its layer for removal; <c>Id</c> names its text.</summary>
public abstract record Tool(string Id, string Label) : Act;

/// <summary>A card that places <paramref name="Placement"/> along a drag of the left button, and marks its layer along one of the right (0108).</summary>
public sealed record PlaceTool(string Id, Placement Placement, string Label) : Tool(Id, Label);

/// <summary>
/// A card of a hand tool's menu (0110): what the stripper's or the wrench's left button lays, paid from the inventory as it
/// is laid; <paramref name="Item"/> is the one whose count the card shows.
/// </summary>
public sealed record HandCard(string Id, string Label, string Item, IReadOnlyList<(string Item, int Count)> Cost) : Tool(Id, Label);

/// <summary>
/// A removal from <paramref name="Layers"/>, in the order they are taken (0110).
/// </summary>
public sealed record Remove(IReadOnlyList<string> Layers) : Act;

/// <summary>The layer the open plan works in (0110): its right button removes there, the other drawn faded.</summary>
public enum Focus
{
    /// <summary>What cannot stack in a cell: walls, machines, doors, hatches, ladders, floors and the other pieces.</summary>
    Front,
    Back,
}

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

    /// <summary>
    /// A card for each material a blueprint builds a wall of (0077, 0084: no bedrock), its id and its text's key followed by
    /// the material's name; then one for each it builds a back wall of (0108), alike.
    /// </summary>
    private static IEnumerable<Tool> OfEveryMaterial(string kind, System.Func<string, Placement> placement) =>
        Materials.All.Where(m => Blueprints.Has(placement(m.Id))).Select(m => new PlaceTool(
            kind + char.ToUpperInvariant(m.Id[0]) + m.Id[1..],
            placement(m.Id),
            $"{En.T($"tool.{kind}")}: {En.T($"material.{m.Id}")}"));

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
            Place("vent", new Placement("machine", "vent")),
            Place("tank", new Placement("machine", "tank")),
            Place("generator", new Placement("machine", "generator")),
        ]),
        new("power",
        [
            Place("solarPanel", new Placement("machine", "solarPanel")),
            Place("burnerGenerator", new Placement("machine", "burnerGenerator")),
            Place("lamp", new Placement("machine", "lamp")),
            Place("arcLamp", new Placement("machine", "arcLamp")),
            Place("charger", new Placement("machine", "charger")),
            Place("capacitorBank", new Placement("machine", "capacitorBank")),
        ]),
        new("production",
        [
            Place("furnace", new Placement("machine", "furnace")),
            Place("analyzer", new Placement("machine", "analyzer")),
            Place("former", new Placement("machine", "former")),
            Place("smelter", new Placement("machine", "smelter")),
            Place("assembler", new Placement("machine", "assembler")),
            Place("growCapsule", new Placement("machine", "growCapsule")),
            Place("arcBench", new Placement("machine", "arcBench")),
        ]),
        new("storage", [Place("crate", new Placement("machine", "crate")), Place("droneBay", new Placement("machine", "droneBay"))]),
    ];

    /// <summary>Every card, tab by tab.</summary>
    public static readonly IReadOnlyList<Tool> All = Tabs.SelectMany(t => t.Tools).ToArray();

    /// <summary>What a card costs, by <paramref name="costFor"/> (the sim's own rule) for what it places.</summary>
    public static IReadOnlyList<(string Item, int Count)> CostOf(Tool tool, System.Func<Placement, IReadOnlyList<(string Item, int Count)>> costFor) =>
        tool switch
        {
            PlaceTool p => costFor(p.Placement),
            HandCard h => h.Cost,
            _ => [],
        };

    /// <summary>The layer a card's placement is in (0110): a back wall's the back, every other the front.</summary>
    public static Focus FocusOf(Tool tool) => tool is PlaceTool { Placement.Layer: "back" } ? Focus.Back : Focus.Front;

    /// <summary>The layer the middle button turns to from <paramref name="focus"/>: the other one (0110).</summary>
    public static Focus Toggled(Focus focus) => focus == Focus.Front ? Focus.Back : Focus.Front;

    /// <summary>What the right button takes in <paramref name="focus"/>: back walls (those behind machines too), or all of the front.</summary>
    public static Remove RemovalIn(Focus focus) => new(focus == Focus.Back ? ["back"] : FrontLayers);

    /// <summary>
    /// What a drag in the open plan does (0110): the left button places what the card in hand places, if one is of the
    /// <paramref name="focus"/>ed layer; the <paramref name="right"/> marks or cancels in that layer, a card in hand or not.
    /// </summary>
    public static Act? ActOf(Tool? tool, bool right, Focus focus) =>
        right ? RemovalIn(focus) : tool is PlaceTool place && FocusOf(place) == focus ? place : null;

    /// <summary>How opaque a layer out of focus is drawn while the plan is open (0110).</summary>
    public const float Faded = 0.35f;

    /// <summary>How opaque the front and the back walls are drawn: the focused layer whole, the other <see cref="Faded"/>; both whole with no focus.</summary>
    public static (float Front, float Back) AlphasOf(Focus? focus) => focus switch
    {
        Focus.Front => (1, Faded),
        Focus.Back => (Faded, 1),
        _ => (1, 1),
    };

    /// <summary>The stripper's card (0110): a cable, a wire each.</summary>
    public static readonly HandCard Cable = new("cable", En.T("tool.cable"), "wire", Content.Power.CableCost);

    /// <summary>The wrench's cards (0110): a pipe, a tube per started <see cref="Piping.CellsPerItem"/> cells; a valve on a joint.</summary>
    public static readonly HandCard Pipe = new("pipe", En.T("tool.pipe"), Piping.Item, [(Piping.Item, 1)]),
        Valve = new("valve", En.T("tool.valve"), "gear", Piping.ValveCost);

    /// <summary>The cards of the menu the item <paramref name="wielded"/> opens: the stripper's cable, the wrench's pipe and valve; none for any other.</summary>
    public static IReadOnlyList<HandCard> CardsOf(string? wielded) =>
        wielded == HandTools.Stripper ? [Cable] : wielded == HandTools.Wrench ? [Pipe, Valve] : [];

    /// <summary>
    /// The overlay that shows: the open tab's, if the menu is open and the tab has one; else the one the item
    /// <paramref name="wielded"/> works in (the stripper's cables, 0108; the wrench's pipes, 0109); else air if the player wants
    /// it; else none.
    /// </summary>
    public static string? OverlayShown(bool planOut, string tab, bool airOn, string? wielded = null) =>
        planOut ? Tabs.FirstOrDefault(t => t.Id == tab)?.Overlay ?? (airOn ? Air : null)
        : wielded == HandTools.Stripper ? Power
        : wielded == HandTools.Wrench ? Pipes
        : airOn ? Air : null;

    /// <summary>The cards among <paramref name="cards"/> whose names hold <paramref name="query"/>, case ignored, in their order: the menu's search, across every tab.</summary>
    public static List<Tool> Matching(IEnumerable<Tool> cards, string query)
    {
        string words = query.Trim();
        return cards.Where(each => each.Label.Contains(words, System.StringComparison.OrdinalIgnoreCase)).ToList();
    }

    /// <summary>The tab a card is on.</summary>
    public static string TabOf(Tool tool) => Tabs.FirstOrDefault(t => t.Tools.Contains(tool))?.Id ?? "";

    /// <summary>What the buttons do, an i18n key: a hand tool's card its own, the plan's the same on every tab.</summary>
    public static string HintOf(Tool? held = null) => held is HandCard h ? $"hint.{h.Id}" : "hint.build";

    /// <summary>The layers Q picks in, in the order they are taken: what the ship is built of.</summary>
    public static readonly IReadOnlyList<string> Layers = ["machine", "structure", "back"];

    /// <summary>The layers the right button marks in the front's focus: <see cref="Layers"/> but the back walls (0110).</summary>
    public static readonly IReadOnlyList<string> FrontLayers = ["machine", "structure"];

    /// <summary>Whether <paramref name="tool"/> is a card the world knows (0024): its blueprint.</summary>
    public static bool Known(Tool tool, IResearchQueries research) => tool is not PlaceTool p || research.Knows(p.Placement);

    /// <summary>Whether <paramref name="tool"/> builds what the kind of surface it is on allows (0076).</summary>
    public static bool Allowed(Tool tool, Surfaces.Kind rules) => tool is not PlaceTool p || rules.Allows(p.Placement.Kind);

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
    /// wall running on into every wall, a door along its doorway's straight run.
    /// </summary>
    public static int LinksOf(Placement place, int cell, int width, System.Func<int, IEnumerable<Placement>> planned, IGridQueries grid)
    {
        if (!Render.Blueprint.Joins(place, place)) return 0;
        return Render.Blueprint.LinksOf(place, cell, width, width * grid.Height, c =>
            planned(c).Any(o => Render.Blueprint.Joins(place, o)) || BuiltAt(c, place.Layer, grid) is { } built && Render.Blueprint.Joins(place, built));
    }

    /// <summary>
    /// The pipette's card for <paramref name="cell"/>: the card that places what is there, planned or built, in
    /// <see cref="Layers"/>, if its blueprint is known.
    /// </summary>
    public static Tool? CardAt(int cell, IGridQueries grid, BuildingModule building, IResearchQueries research)
    {
        if (PlacementAt(cell, Layers, grid, building) is not { } placed) return null;
        bool Places(Placement p) => p.Layer == placed.Layer && p.Kind == placed.Kind && p.Material == placed.Material;
        var card = All.OfType<PlaceTool>().FirstOrDefault(each => Places(each.Placement));
        // What is not known, or not built here, is not picked: it could not be planned.
        return card != null && Known(card, research) && Allowed(card, building.Rules) ? card : null;
    }
}
