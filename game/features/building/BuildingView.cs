using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using Stillwake.Net;
using Stillwake.Sim;
using Stillwake.Sim.Building;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Power;
using Stillwake.Sim.Rooms;
using S = Stillwake.Game.Sound;

namespace Stillwake.Game.Features.Building;

/// <summary>
/// Plans drawn over the ship as ghosts of their own pictures in marching dashes along their silhouettes, a "+" on each
/// machine, turning into the real thing as they are worked, a part paid an item (0063); what is marked for taking down
/// as it is, in red dashes, turning into its red ghost from the top as it is worked (0064); and the hand at work
/// (features/building/index.ts, 0022, 0030): a card's ghost along a left drag, a removal's along a middle
/// one in the open tab's layers, coloured by what the sim would say, with its reason by the pointer; a
/// release sends the commands. Q picks a card. With no card out, no grid: space stays space. Work on a
/// plan sparks and ticks with the screwdriver, heard from the plan.
/// </summary>
public partial class BuildingView : Node2D
{
    private const int Cell = ShipCamera.Cell;
    /// <summary>How strongly a ghost shows: a plan's, a mark's taken share, the same.</summary>
    private const float GhostAlpha = 0.88f;
    /// <summary>A plan's tint: its own picture as a pale ghost, a little blue, not built yet.</summary>
    private static readonly Color Plan = AsciiArt.Colour(0xc8f6ff, GhostAlpha);
    /// <summary>A planned back wall shows fainter than the rest, as it is behind.</summary>
    private const float BackAlpha = 0.65f / GhostAlpha;
    /// <summary>The dashes round each group of plans and the badge on a planned machine.</summary>
    private static readonly Color Dashes = AsciiArt.Colour(0xc8f6ff, 0.95f), Badge = AsciiArt.Colour(0x6fe3ff), BadgeBack = AsciiArt.Colour(0x0b1a24);
    private static readonly Color Mark = AsciiArt.Colour(0xe05555), Fits = AsciiArt.Colour(0x7fe08a), Refused = AsciiArt.Colour(0xe05555);
    /// <summary>A hole in a group smaller than a cell is part of its silhouette, not outlined: a ladder's rungs, a grille.</summary>
    private const int SmallestHole = Cell * Cell;

    private readonly Ui _ui;
    private readonly PlanHand _hand;
    /// <summary>What of the plans being built is real already, drawn and lit as the ship is.</summary>
    private readonly BuiltParts _built = new(ShipTiles.Shared);
    // Planned back walls behind what is built in front of them; the rest over the ship.
    private readonly PlanTiles _plans;
    /// <summary>What a mark has taken out so far, from the top: its picture's red ghost over the thing, which the grid still draws.</summary>
    private readonly PlanTiles _marked = new(tint: 0.6f) { ZIndex = Layers.Plans, Modulate = Mark with { A = GhostAlpha } };
    // The ghost in hand as a plan is drawn, but plainly green or red.
    private readonly PlanTiles _fits = new(tint: 0.45f) { ZIndex = Layers.PlanGhost, Modulate = Fits with { A = 0.8f } };
    private readonly PlanTiles _refused = new(tint: 0.45f) { ZIndex = Layers.PlanGhost, Modulate = Refused with { A = 0.8f } };
    private readonly Sketch _badges = new(Layers.PlanMarks);
    /// <summary>The marching dashes round plans; round back walls, behind what is built; round marks, in red.</summary>
    private readonly MarchingDashes _outline = new(Layers.PlanMarks) { Modulate = Dashes }, _backOutline = new(Layers.PlanBacks) { Modulate = Dashes },
        _markOutline = new(Layers.PlanMarks) { Modulate = Mark with { A = 0.95f } },
        _takingOutline = new(Layers.PlanMarks) { Modulate = Mark with { A = 0.95f } },
        _keptOutline = new(Layers.PlanMarks) { Modulate = Mark with { A = 0.4f } },
        _cancelOutline = new(Layers.PlanMarks) { Modulate = AsciiArt.Colour(0xffc94d, 0.95f) };
    /// <summary>What a middle drag would take, before it is let go: drawn as its marks will be, a red ghost in dashes.</summary>
    private readonly PlanTiles _taking = new(tint: 0.6f) { ZIndex = Layers.PlanGhost, Modulate = Mark with { A = GhostAlpha } };
    /// <summary>The shafts under launch platforms, kept clear for the pod's fall, while building on the plan.</summary>
    private readonly Shafts _shafts = new();
    /// <summary>The cells of the platform card's ghost, gathered as it is laid; the platforms built and planned, by the grid's version and the plans' count.</summary>
    private readonly List<int> _ghostPlatform = [];
    private (int Version, int Plans) _platformsOf = (-1, -1);
    private int[] _platforms = [];

    private string _drawn = "", _outlined = "", _takingKey = "";
    private List<Rect2> _machines = [];
    private int _width;
    /// <summary>The drag under way: what it does, whether the right button began it, the point it began at, in cells, and the card it began with.</summary>
    private (Act Act, bool Right, (double X, double Y) From, Tool? Card)? _drag;

    /// <summary>Each plan's work done when last seen, and the sparks owed: work sparks only as it goes on.</summary>
    private Dictionary<int, int> _worked = [];
    private double _owedBursts;
    /// <summary>Bursts of sparks off a tick of work on a plan (0075).</summary>
    private const double BurstsPerTick = 0.42;
    /// <summary>How far below a spark a floor is looked for, in cells: past it, sparks fall out of sight.</summary>
    private const int FloorDepth = 12;
    /// <summary>Each blueprint being built: its ticks of work in all, to tell one finished from one cancelled as it goes.</summary>
    private readonly Dictionary<int, int> _ticks = [];
    private readonly BuildGlow _glow = new();
    /// <summary>How far from a plan, in pixels, a hero still turns its sparks toward it: a little past the hands' reach.</summary>
    private const double SparkReach = (Stillwake.Content.Hero.Reach / 1000.0 + 4) * Cell;
    /// <summary>The screwdriver, moved to each plan as it sparks: a few ticks at once, from plans worked side by side.</summary>
    private readonly Heard2D _screwdriver = Cues.Player2D(S.Screwdriver, S.World, voices: 4);

    public BuildingView(Ui ui, PlanHand hand)
    {
        _ui = ui;
        _hand = hand;
        _plans = new(BackAlpha, backZ: Layers.PlanBacks, built: _built) { ZIndex = Layers.Plans, Modulate = Plan };
        foreach (var node in (Node2D[])[_built, _plans, _backOutline, _marked, _outline, _markOutline, _taking, _takingOutline, _keptOutline, _cancelOutline, _badges, _shafts, _fits, _refused, _screwdriver, _glow]) AddChild(node);
        _badges.Paint = PaintBadges;
    }

    /// <summary>
    /// One frame: the plans brought up to the world, the presses since the last read the cables did not take, the
    /// <paramref name="left"/> button's placing the card and the <paramref name="right"/> one's removing in the focused layer (0110), the
    /// ghost drawn, the removal's while the right button is held (<paramref name="holdsRight"/>).
    /// </summary>
    public void Frame(Client client, Registry m, IReadOnlyList<ButtonEvent> left, IReadOnlyList<ButtonEvent> right, bool holdsRight,
        (double X, double Y)? pointer)
    {
        var grid = m.Grid;
        var building = m.Building;
        int width = _width = grid.Width, height = grid.Height;
        DrawPlans(grid, building, m.Rooms, m.Power);

        var tool = _hand.Selected.Get();
        string? overlay = _ui.OverlayOn;
        int CellAt((double X, double Y) p)
        {
            var s = Drag.SpotAt(width, height, p);
            return s.Y * width + s.X;
        }

        void Send(Act act, (double X, double Y) from, (double X, double Y) to)
        {
            var planned = new List<int>();
            foreach (var verdict in Drag.Judge(act, Drag.StrokeOf(width, height, act, from, to), building))
                foreach (var c in verdict.Commands)
                {
                    client.Send(c);
                    if (c is PlacePlan p) planned.Add(p.Cell);
                }
            if (planned.Count > 0) _ui.Plan(planned);
        }
        // A drag ends with the card that began it: a card swapped or put away mid-drag sends nothing.
        if (_drag is { } d && d.Card != tool) _drag = null;

        void DragWith(ButtonEvent button, bool back)
        {
            if (button.Down)
            {
                // The first button down has the drag; another pressed meanwhile is let be. The right removes in the focused layer, card or none (0110).
                if (_drag == null && _hand.PlanOut.Get() && Tools.ActOf(tool, back, _hand.Focus.Get()) is { } act && Drag.OnGrid(width, height, button.At))
                    _drag = (act, back, button.At, tool);
                return;
            }
            if (_drag is not { } under || under.Right != back) return;
            Send(under.Act, under.From, button.At);
            _drag = null;
        }
        foreach (var button in left) DragWith(button, back: false);
        foreach (var button in right) DragWith(button, back: true);
        // Q picks the card of what is under the pointer, onto its own tab.
        if (_hand.PickWanted)
        {
            _hand.PickWanted = false;
            if (pointer is { } at && Drag.OnGrid(width, height, at)
                && Tools.CardAt(CellAt(at), grid, building, m.Research) is { } card)
            {
                _hand.Tab.Set(Tools.TabOf(card));
                _hand.Selected.Set(card);
            }
        }

        tool = _hand.Selected.Get();
        bool onGrid = pointer is { } p0 && Drag.OnGrid(width, height, p0);
        ShowTray(onGrid ? CellAt(pointer!.Value) : null, grid, building);
        Ghost(tool, pointer, onGrid, grid, building, _hand.PlanOut.Get() ? Tools.ActOf(tool, holdsRight, _hand.Focus.Get()) : null);
        bool shafts = _hand.PlanOut.Get() && _hand.Builds;
        _shafts.Show(shafts ? grid : null, shafts ? PlatformsOf(grid, building) : [], shafts ? _ghostPlatform : []);
    }

    /// <summary>
    /// The plans as ghosts of their own pictures, pipes, doors and walls joined to what is planned and built
    /// beside them, back walls fainter and behind what is built; a blueprint being built real up to its cut (0063);
    /// what a mark has taken so far, the thing's red ghost from the top (<see cref="MarkCutOf"/>); dashes along the
    /// silhouette of each group (<see cref="Outline"/>). Redrawn only when the plans or the grid change, the dashes
    /// only when what they go round does.
    /// </summary>
    private void DrawPlans(IGridQueries grid, BuildingModule building, IRoomsQueries rooms, IPowerQueries power)
    {
        var plans = building.Plans;
        // The grid's version is in both keys, as what is built or taken down beside a plan changes how it joins; what was
        // put in and the work done only in the drawing's, so plans turn real and marks red as they go on; repairs too.
        string outlined = grid.Version + "|" + string.Join(";", plans.Select(p => $"{p.Id}:{p.Cell}:{p.Place}:{p.Clear}"));
        string key = outlined + "|" + string.Join(";", plans.Select(p => $"{p.Progress}:{string.Join(",", p.Delivered)}"))
            + "|" + string.Join(";", power.Broken.Select(id => $"{id}:{building.RepairProgress(id)}"));
        if (key == _drawn) return;
        _drawn = key;
        int width = grid.Width;
        _plans.Begin();
        var placed = new Dictionary<int, List<Placement>>();
        foreach (var p in plans)
            if (p.Place is { } place)
            {
                if (!placed.TryGetValue(p.Cell, out var list)) placed[p.Cell] = list = [];
                list.Add(place);
            }
        IEnumerable<Placement> PlannedAt(int c) => placed.TryGetValue(c, out var list) ? list : [];
        var shapes = new List<(Sim.Building.Plan Plan, Placement Shown, int At, int Linked)>();
        var doorCodes = new Dictionary<int, int>();
        foreach (var p in plans)
            if (p.Place is { } place && Tools.LaysDoor(place))
                foreach (int c in CellsOf(grid, p)) doorCodes[c] = Tools.CodeOf(place.Kind);
        var doorLinks = Tools.DoorLinks(grid, doorCodes);
        foreach (var p in plans)
        {
            if (p.Place is not { } place) continue;
            int linked = Tools.LinksOf(place, p.Cell, width, PlannedAt, grid);
            var cut = CutOf(grid, building, p, place);
            if (Tools.LaysDoor(place))
                foreach (int c in CellsOf(grid, p)) _plans.LayDoor(c, width, doorLinks, Doorways.Lies(Tools.CodeOf(place.Kind)), cut);
            else _plans.Lay(place, p.Cell, width, linked, cut);
            if (cut != null) _glow.Cut(p.Id, BoxOf(CellsOf(grid, p), width), cut);
            shapes.Add((p, place, p.Cell, linked));
        }
        var repairing = LayRepairs(grid, building, power);
        _marked.Begin();
        foreach (var p in plans)
        {
            if (p.Clear is not { } layer) continue;
            int at = MarkedAt(grid, p);
            if (Tools.BuiltAt(at, layer, grid) is not { } built) continue;
            // Joined as the grid draws it, to what is built beside it, marked or not.
            int joined = Tools.LinksOf(built, at, width, _ => [], grid);
            if (MarkCutOf(grid, building, p, built) is { } cut) LayBuilt(_marked, built, at, joined, grid, cut);
            shapes.Add((p, built, at, joined));
        }
        if (outlined != _outlined)
        {
            _outlined = outlined;
            Outline(shapes, grid);
        }
        SparkWork(plans, grid, rooms);
        Finish(plans, building);
        _worked = plans.ToDictionary(p => p.Id, p => p.Progress);
        SparkRepairs(repairing, grid, rooms, power);
        _repairing = repairing;
    }

    /// <summary>Each broken machine whose repair is begun, by id: its work done, of all it takes, and its box. Kept as last drawn.</summary>
    private Dictionary<int, (int Done, int Of, Rect2 Box)> _repairing = [];

    /// <summary>A repair's key in <see cref="BuildGlow"/>, apart from every plan's id, which only grows from 1.</summary>
    private static int RepairGlow(int machine) => -1 - machine;

    /// <summary>
    /// Each broken machine under repair (magnetic-flare) laid as a blueprint being built: its pale ghost over it, turning
    /// real as the work goes on, from where its plan would (<see cref="PlanCut"/>). None before the repair is paid.
    /// </summary>
    private Dictionary<int, (int Done, int Of, Rect2 Box)> LayRepairs(IGridQueries grid, BuildingModule building, IPowerQueries power)
    {
        var now = new Dictionary<int, (int Done, int Of, Rect2 Box)>();
        foreach (int id in power.Broken)
        {
            if (grid.MachineById(id) is not { } machine || building.RepairProgress(id) is not ( > 0 and var done, var of)) continue;
            var place = new Placement("machine", machine.Kind);
            var box = BoxOf(grid.Footprint(machine.Kind, machine.Cell) ?? [machine.Cell], grid.Width);
            var cut = PlanCut.Of(((int)box.Position.X, (int)box.Position.Y, (int)box.Size.X, (int)box.Size.Y),
                PlanCut.FromLeftFor(place.Layer, place.Kind), done, of);
            _plans.Lay(place, machine.Cell, grid.Width, Tools.LinksOf(place, machine.Cell, grid.Width, _ => [], grid), cut);
            _glow.Cut(RepairGlow(id), box, cut);
            now[id] = (done, of, box);
        }
        return now;
    }

    /// <summary>
    /// Sparks and the screwdriver off each repair whose work went on, as off a plan's (<see cref="SparkWork"/>); a repair gone
    /// since, its machine mended, swept with light as a plan built is, else let go.
    /// </summary>
    private void SparkRepairs(Dictionary<int, (int Done, int Of, Rect2 Box)> now, IGridQueries grid, IRoomsQueries rooms, IPowerQueries power)
    {
        foreach (var (id, (done, _, box)) in now)
        {
            int was = _repairing.TryGetValue(id, out var before) ? before.Done : 0;
            if (done > was) Spark(RepairGlow(id), done - was, box, grid, rooms);
        }
        foreach (var (id, _) in _repairing)
            if (!now.ContainsKey(id)) _glow.Gone(RepairGlow(id), finished: grid.MachineById(id) != null && !power.IsBroken(id));
    }

    /// <summary>
    /// Bursts of sparks off every plan whose work went on since it was last seen, from its side facing the nearest
    /// hero and toward it (<see cref="Tools.SparksOf"/>): falling aboard and bouncing off the floor, drifting off in
    /// space (<see cref="BuildGlow"/>).
    /// </summary>
    private void SparkWork(IReadOnlyList<Sim.Building.Plan> plans, IGridQueries grid, IRoomsQueries rooms)
    {
        foreach (var p in plans)
        {
            if (!_worked.TryGetValue(p.Id, out int was) || p.Progress <= was) continue;
            Spark(p.Id, p.Progress - was, BoxOf(CellsOf(grid, p), grid.Width), grid, rooms);
        }
    }

    /// <summary>The sparks <paramref name="ticks"/> more of work owe off <paramref name="box"/>, glow <paramref name="glow"/>'s, and the screwdriver heard there.</summary>
    private void Spark(int glow, int ticks, Rect2 box, IGridQueries grid, IRoomsQueries rooms)
    {
        _owedBursts += ticks * BurstsPerTick * Fx.Count;
        int bursts = (int)Math.Floor(_owedBursts);
        _owedBursts -= bursts;
        if (bursts == 0) return;
        var (x, y, heading) = Tools.SparksOf((box.Position.X, box.Position.Y, box.Size.X, box.Size.Y), _ui.Heroes, SparkReach);
        var tip = new Vector2((float)x, (float)y);
        int cell = (int)(box.Position.Y / Cell) * grid.Width + (int)(box.Position.X / Cell);
        _glow.Sparked(glow, tip, heading, bursts, rooms.Afloat(cell), FloorUnder(grid, tip));
        _screwdriver.PlayAt(tip);
    }

    /// <summary>The top of the first solid cell under <paramref name="at"/>, in pixels, within <see cref="FloorDepth"/> cells; none past it.</summary>
    private static float? FloorUnder(IGridQueries grid, Vector2 at)
    {
        int x = (int)(at.X / Cell), y = (int)(at.Y / Cell);
        if (x < 0 || x >= grid.Width) return null;
        for (int row = y + 1; row <= y + FloorDepth && row < grid.Height; row++)
            if (row >= 0 && grid.Solid(row * grid.Width + x)) return row * Cell;
        return null;
    }

    /// <summary>
    /// Blueprints gone since the plans were last seen: swept with light if they were all but built when last seen,
    /// a hand's ticks of work short at most, rather than cancelled (<see cref="BuildGlow.Gone"/>).
    /// </summary>
    private void Finish(IReadOnlyList<Sim.Building.Plan> plans, BuildingModule building)
    {
        var now = new HashSet<int>();
        foreach (var p in plans)
        {
            now.Add(p.Id);
            if (p.Place != null && building.TicksFor(p.Id) is { } ticks) _ticks[p.Id] = ticks;
        }
        foreach (int id in _worked.Keys)
        {
            if (now.Contains(id)) continue;
            bool built = _ticks.Remove(id, out int all) && all - _worked[id] <= Math.Max(10, all / 10);
            _glow.Gone(id, built);
        }
    }

    /// <summary>
    /// The dashes along the silhouette of each group (<see cref="PlanOutline"/>, <see cref="PlanShapes"/>), a group's
    /// shapes joined where they meet: planned walls, back walls (behind what is built), ladders, doors, floors, each
    /// planned machine, which gets a badge; and in red, marked walls, back walls, ladders, doors, floors, conduits, each
    /// marked machine. Planned pipes and valves are lines of their own and not outlined; a marked one is, as nothing
    /// else shows it marked.
    /// </summary>
    private void Outline(List<(Sim.Building.Plan Plan, Placement Shown, int At, int Linked)> shapes, IGridQueries grid)
    {
        int width = grid.Width;
        var front = new List<(string, int, int, IReadOnlyList<Rim>)>();
        var backs = new List<(string, int, int, IReadOnlyList<Rim>)>();
        var marks = new List<(string, int, int, IReadOnlyList<Rim>)>();
        var machines = new List<Rect2>();
        foreach (var (plan, shown, at, linked) in shapes)
        {
            if (plan.Place != null && shown.Layer == "conduit") continue;
            int x = at % width, y = at / width;
            var shape = (GroupOf(plan, shown), x * Cell, y * Cell, PlanShapes.RimOf(shown, x, y, linked));
            if (plan.Clear != null) marks.Add(shape);
            else if (shown.Layer == "back") backs.Add(shape);
            else front.Add(shape);
            if (plan.Place is { Layer: "machine" } place) machines.Add(PictureOf(BoxOf(CellsOf(grid, plan), width), place.Kind));
        }
        _outline.Show(PlanOutline.Of(front, SmallestHole));
        _backOutline.Show(PlanOutline.Of(backs, SmallestHole));
        _markOutline.Show(PlanOutline.Of(marks, SmallestHole));
        _machines = machines;
        _badges.QueueRedraw();
    }

    /// <summary>Which outline a plan joins: a machine its own; else those of its kind, its conduits all one. Marks are outlined apart.</summary>
    private static string GroupOf(Sim.Building.Plan plan, Placement shown) => shown.Layer switch
    {
        "machine" => $"machine{plan.Id}",
        "back" or "conduit" => shown.Layer,
        _ => shown.Kind,
    };

    /// <summary>A "+" on the top right corner of each planned machine.</summary>
    private void PaintBadges(Sketch g)
    {
        foreach (var box in _machines)
        {
            // Seven pixels square, a pixel of it past the machine's edge.
            var badge = new Rect2(box.End.X - 6, box.Position.Y - 1, 7, 7);
            g.DrawRect(badge, Badge);
            g.DrawRect(badge.Grow(-1), BadgeBack);
            g.DrawRect(new Rect2(badge.Position + new Vector2(2, 3), 3, 1), Badge);
            g.DrawRect(new Rect2(badge.Position + new Vector2(3, 2), 1, 3), Badge);
        }
    }

    /// <summary>What is built at <paramref name="at"/> as <paramref name="into"/>'s ghost: a doorway's piece by its joins as the grid draws it.</summary>
    private static void LayBuilt(PlanTiles into, Placement built, int at, int joined, IGridQueries grid, PlanCut? cut = null)
    {
        if (!Tools.LaysDoor(built))
        {
            into.Lay(built, at, grid.Width, joined, cut);
            return;
        }
        var links = Tools.DoorLinks(grid, new Dictionary<int, int>());
        foreach (int c in grid.PieceOf(at)) into.LayDoor(c, grid.Width, links, Doorways.Lies(Tools.CodeOf(built.Kind)), cut);
    }

    /// <summary>Where a mark's thing is drawn from: a machine's top-left, whichever of its cells is marked; else its cell.</summary>
    private static int MarkedAt(IGridQueries grid, Sim.Building.Plan p) =>
        p.Clear == "machine" && grid.MachineAt(p.Cell) is { } machine ? machine.Cell : p.Cell;

    /// <summary>The cells a plan covers: a machine's footprint, a piece's (a ladder, a doorway, 0104), else its cell.</summary>
    private static IReadOnlyList<int> CellsOf(IGridQueries grid, Sim.Building.Plan p)
    {
        if (p.Clear == "structure" && grid.PieceOf(p.Cell) is { Count: > 0 } piece) return piece;
        var machine = p.Place is { } place && (place.Layer == "machine" || Stillwake.Content.Builds.PieceOf(place.Kind) != null) ? (place.Kind, p.Cell)
            : p.Clear == "machine" && grid.MachineAt(p.Cell) is { } built ? (built.Kind, built.Cell)
            : ((string Kind, int Cell)?)null;
        return machine is { } mc ? grid.Footprint(mc.Kind, mc.Cell) ?? [p.Cell] : [p.Cell];
    }

    /// <summary>The rectangle around <paramref name="cells"/>, in pixels of the world.</summary>
    private static Rect2 BoxOf(IReadOnlyList<int> cells, int width)
    {
        int x = cells.Min(c => c % width), y = cells.Min(c => c / width);
        int right = cells.Max(c => c % width), bottom = cells.Max(c => c / width);
        return new Rect2(x * Cell, y * Cell, (right - x + 1) * Cell, (bottom - y + 1) * Cell);
    }

    /// <summary>What <paramref name="box"/>, the footprint of a thing of <paramref name="kind"/>, is as drawn: a picture may rise over it or reach out of it.</summary>
    private static Rect2 PictureOf(Rect2 box, string kind)
    {
        var (x, y, w, h) = PlanCut.Picture(((int)box.Position.X, (int)box.Position.Y, (int)box.Size.X, (int)box.Size.Y), ShipTiles.RiseOf(kind), ShipTiles.ReachOf(kind), Cell);
        return new Rect2(x, y, w, h);
    }

    /// <summary>
    /// Where blueprint <paramref name="p"/> turns from real into plan: across its box as far as its work has gone, a
    /// pixel at a time; none before any. Work stops where a part is not paid for, so a stalled build sits at its boundary.
    /// </summary>
    private static PlanCut? CutOf(IGridQueries grid, BuildingModule building, Sim.Building.Plan p, Placement place)
    {
        if (p.Progress <= 0 || building.TicksFor(p.Id) is not { } ticks) return null;
        var box = PictureOf(BoxOf(CellsOf(grid, p), grid.Width), place.Kind);
        var pixels = ((int)box.Position.X, (int)box.Position.Y, (int)box.Size.X, (int)box.Size.Y);
        return PlanCut.Of(pixels, PlanCut.FromLeftFor(place.Layer, place.Kind), p.Progress, ticks);
    }

    /// <summary>
    /// How far mark <paramref name="p"/> has taken <paramref name="built"/> out: the mirror of a blueprint being built,
    /// what stands turning into its red ghost from the top down, a floor or a conduit from the right, the rest still the
    /// thing as the grid draws it; none before any work.
    /// </summary>
    private static PlanCut? MarkCutOf(IGridQueries grid, BuildingModule building, Sim.Building.Plan p, Placement built)
    {
        if (p.Progress <= 0 || building.TicksFor(p.Id) is not { } ticks) return null;
        var box = PictureOf(BoxOf(CellsOf(grid, p), grid.Width), built.Kind);
        var pixels = ((int)box.Position.X, (int)box.Position.Y, (int)box.Size.X, (int)box.Size.Y);
        return PlanCut.Of(pixels, PlanCut.FromLeftFor(built.Layer, built.Kind), ticks - p.Progress, ticks);
    }

    /// <summary>
    /// Beside the pointer (<see cref="Ui.CostTip"/>), what the blueprint at <paramref name="cell"/> builds and,
    /// row by row, what this player carries against what it still needs, counting down a part at a time as it
    /// is paid, while it lacks something (once all is paid the plan, real all over, says the rest); else what a
    /// mark there takes down and what that gives back (<see cref="BuildingModule.RefundOf"/>); nothing anywhere else.
    /// </summary>
    private void ShowTray(int? cell, IGridQueries grid, BuildingModule building)
    {
        var plans = cell is int c ? building.PlansAt(c) : [];
        var plan = plans.FirstOrDefault(p => p.Place != null && building.Lacking(p.Id).Count > 0);
        Tip? tip = null;
        if (plan?.Place is { } place && building.CostOf(plan.Id) is { } cost)
            tip = new PlanTip(place, CostLine.Lacking(cost, plan.Delivered.Select(s => (s.Item, s.Count)).ToList(), _ui.Carried)).Tip;
        else if (plans.FirstOrDefault(p => p.Clear != null) is { Clear: { } layer } mark && building.RefundOf(mark.Id) is { } refund
            && Tools.BuiltAt(MarkedAt(grid, mark), layer, grid) is { } built)
            tip = new MarkTip(built, refund).Tip;
        _ui.CostTip.Set(tip);
    }

    /// <summary>Every launch platform cell built or planned, scanned again only when the grid or the plans change.</summary>
    private int[] PlatformsOf(IGridQueries grid, BuildingModule building)
    {
        if ((grid.Version, building.Plans.Count) == _platformsOf) return _platforms;
        _platformsOf = (grid.Version, building.Plans.Count);
        var cells = new List<int>();
        var structure = grid.StructureLayer;
        for (int c = 0; c < structure.Length; c++) if (Doorways.IsPlatform(structure[c])) cells.Add(c);
        foreach (var plan in building.Plans)
            if (plan.Place is { } place && Tools.IsPlatform(place)) cells.AddRange(grid.Footprint(place.Kind, plan.Cell) ?? [plan.Cell]);
        return _platforms = [.. cells];
    }

    /// <summary>The ghost of the card along a drag, or of a removal: its own pictures, green where the sim would take it and red where not.</summary>
    private void Ghost(Tool? tool, (double X, double Y)? pointer, bool onGrid, IGridQueries grid, BuildingModule building, Act? hovered)
    {
        _fits.Begin();
        _refused.Begin();
        _taking.Begin();
        var taken = new List<(string, int, int, IReadOnlyList<Rim>)>();
        var kept = new List<(string, int, int, IReadOnlyList<Rim>)>();
        var cancelled = new List<(string, int, int, IReadOnlyList<Rim>)>();
        var takenKey = new System.Text.StringBuilder();
        try
        {
            GhostOf(tool, pointer, onGrid, grid, building, hovered, taken, kept, cancelled, takenKey);
        }
        finally
        {
            // The dashes are laid out again only when what the drag would take changes.
            if (takenKey.ToString() != _takingKey)
            {
                _takingKey = takenKey.ToString();
                _takingOutline.Show(PlanOutline.Of(taken, SmallestHole));
                _keptOutline.Show(PlanOutline.Of(kept, SmallestHole));
                _cancelOutline.Show(PlanOutline.Of(cancelled, SmallestHole));
            }
        }
    }

    private void GhostOf(Tool? tool, (double X, double Y)? pointer, bool onGrid, IGridQueries grid, BuildingModule building, Act? hovered,
        List<(string, int, int, IReadOnlyList<Rim>)> taken, List<(string, int, int, IReadOnlyList<Rim>)> kept,
        List<(string, int, int, IReadOnlyList<Rim>)> cancelled, System.Text.StringBuilder takenKey)
    {
        int width = grid.Width, height = grid.Height;
        _ui.Reason.Set(null);
        _ghostPlatform.Clear();
        // Before a press, what the button held would do: the card's, the focused layer's removal while the right is held (0110).
        Act? act = _drag?.Act ?? hovered;
        if (act == null || pointer is not { } p || (_drag == null && !onGrid)) return;
        var verdicts = Drag.Judge(act, Drag.StrokeOf(width, height, act, _drag?.From ?? p, p), building);
        if (act is PlaceTool place)
        {
            // The ghost joins the rest of its drag, what is planned and what is built, as a plan would.
            var cells = verdicts.Select(v => v.Cell).ToHashSet();
            IEnumerable<Placement> Beside(int c) =>
                building.PlansAt(c).Select(o => o.Place).OfType<Placement>().Concat(cells.Contains(c) ? [place.Placement] : []);
            // A doorway's ghost joins as it will be built, a door to doors and a hatch to hatches (0104, 0111).
            Dictionary<int, int>? doorCodes = null;
            if (Tools.LaysDoor(place.Placement))
            {
                doorCodes = new Dictionary<int, int>();
                foreach (var o in building.Plans)
                    if (o.Place is { } other && Tools.LaysDoor(other))
                        foreach (int c in CellsOf(grid, o)) doorCodes[c] = Tools.CodeOf(other.Kind);
                foreach (var v in verdicts)
                    foreach (int c in grid.Footprint(place.Placement.Kind, v.Cell) ?? [v.Cell]) doorCodes[c] = Tools.CodeOf(place.Placement.Kind);
            }
            var doorLinks = doorCodes != null ? Tools.DoorLinks(grid, doorCodes) : null;
            if (Tools.IsPlatform(place.Placement))
                foreach (var v in verdicts) _ghostPlatform.AddRange(grid.Footprint(place.Placement.Kind, v.Cell) ?? [v.Cell]);
            foreach (var v in verdicts)
            {
                var into = v.Problem != null ? _refused : _fits;
                if (doorLinks != null)
                    foreach (int c in grid.Footprint(place.Placement.Kind, v.Cell) ?? [v.Cell])
                        into.LayDoor(c, width, doorLinks, Doorways.Lies(Tools.CodeOf(place.Placement.Kind)));
                else into.Lay(place.Placement, v.Cell, width, Tools.LinksOf(place.Placement, v.Cell, width, Beside, grid));
            }
        }
        else
        {
            // What it would take, as its marks will look: each thing's red ghost and dashes round its silhouette.
            // What it may not take (a crate not empty), its silhouette dim, its reason by the pointer.
            var seen = new HashSet<(string, int)>();
            void Silhouette(string layer, int cell, bool takes)
            {
                // A machine's top-left, a ladder's or a doorway's first cell (0104), whichever of their cells it is.
                int at = layer == "machine" && grid.MachineAt(cell) is { } machine ? machine.Cell
                    : layer == "structure" && grid.PieceOf(cell) is { Count: > 1 } piece ? piece[0] : cell;
                if (!seen.Add((layer, at)) || Tools.BuiltAt(at, layer, grid) is not { } built) return;
                int joined = Tools.LinksOf(built, at, width, _ => [], grid);
                if (takes) LayBuilt(_taking, built, at, joined, grid);
                int x = at % width, y = at / width;
                string group = layer == "machine" ? $"machine{at}" : layer is "back" or "conduit" ? layer : built.Kind;
                (takes ? taken : kept).Add((group, x * Cell, y * Cell, PlanShapes.RimOf(built, x, y, joined)));
                takenKey.Append(takes ? '+' : '-').Append(layer).Append(at).Append(',');
            }
            // A drag begun on plans cancels them: each plan or mark it would cancel, amber dashes round its silhouette.
            void Cancelled(int cell, int id)
            {
                if (building.PlansAt(cell).FirstOrDefault(p => p.Id == id) is not { } plan) return;
                int at = plan.Clear != null ? MarkedAt(grid, plan) : plan.Cell;
                var shown = plan.Place ?? (plan.Clear is { } layer ? Tools.BuiltAt(at, layer, grid) : null);
                if (shown is null || !seen.Add(("plan", plan.Id))) return;
                int joined = Tools.LinksOf(shown, at, width, _ => [], grid);
                int x = at % width, y = at / width;
                cancelled.Add((shown.Layer == "machine" ? $"machine{at}" : shown.Layer is "back" or "conduit" ? shown.Layer : shown.Kind,
                    x * Cell, y * Cell, PlanShapes.RimOf(shown, x, y, joined)));
                takenKey.Append('x').Append(plan.Id).Append(',');
            }
            foreach (var v in verdicts)
            {
                foreach (var c in v.Commands)
                    if (c is Sim.Building.Deconstruct d) Silhouette(d.Layer, d.Cell, takes: true);
                    else if (c is CancelPlan cancel) Cancelled(v.Cell, cancel.Id);
                if (v.Problem != null && act is Remove remove)
                    foreach (var (layer, at) in Drag.ThingsIn(remove, v.Cell, building)) Silhouette(layer, at, takes: false);
            }
        }
        // A removal's rectangle over open cells as well as things says nothing of the open ones while it takes something.
        bool takes = verdicts.Any(v => v.Commands.Count > 0);
        _ui.Reason.Set(verdicts.FirstOrDefault(v => v.Problem != null && !(takes && v.Problem == "building.nothingThere"))?.Problem);
    }
}
