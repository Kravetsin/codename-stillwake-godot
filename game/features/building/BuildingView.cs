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
    private static readonly Color GridLine = AsciiArt.Colour(0x9fb4c8);
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
    private readonly Sketch _badges = new(Layers.PlanMarks), _hands = new(Layers.PlanHands);
    /// <summary>The marching dashes round plans; round back walls, behind what is built; round marks, in red.</summary>
    private readonly MarchingDashes _outline = new(Layers.PlanMarks) { Modulate = Dashes }, _backOutline = new(Layers.PlanBacks) { Modulate = Dashes },
        _markOutline = new(Layers.PlanMarks) { Modulate = Mark with { A = 0.95f } },
        _takingOutline = new(Layers.PlanMarks) { Modulate = Mark with { A = 0.95f } };
    /// <summary>What a middle drag would take, before it is let go: drawn as its marks will be, a red ghost in dashes.</summary>
    private readonly PlanTiles _taking = new(tint: 0.6f) { ZIndex = Layers.PlanGhost, Modulate = Mark with { A = GhostAlpha } };

    private string _drawn = "", _outlined = "", _takingKey = "";
    private List<Rect2> _machines = [];
    private List<Edge> _near = [];
    private List<(int Cell, bool Refused)> _removing = [];
    private int _width;
    /// <summary>The drag under way: what it does, the button that began it and the cell it began on.</summary>
    private (Act Act, bool Left, Spot From)? _drag;
    /// <summary>Each plan's work done when last seen, and the sparks owed: work sparks only as it goes on.</summary>
    private Dictionary<int, int> _worked = [];
    private double _owedSparks;
    /// <summary>Sparks off a tick of work on a plan.</summary>
    private const double SparksPerTick = 0.3;
    /// <summary>How far from a plan, in pixels, a hero still turns its sparks toward it: a little past the hands' reach.</summary>
    private const double SparkReach = (Stillwake.Content.Hero.Reach / 1000.0 + 2) * Cell;
    /// <summary>The screwdriver, moved to each plan as it sparks: a few ticks at once, from plans worked side by side.</summary>
    private readonly Heard2D _screwdriver = Cues.Player2D(S.Screwdriver, S.World, voices: 4);

    public BuildingView(Ui ui, PlanHand hand)
    {
        _ui = ui;
        _hand = hand;
        _plans = new(BackAlpha, backZ: Layers.PlanBacks, built: _built) { ZIndex = Layers.Plans, Modulate = Plan };
        foreach (var node in (Node2D[])[_built, _plans, _backOutline, _marked, _outline, _markOutline, _taking, _takingOutline, _badges, _fits, _refused, _hands, _screwdriver]) AddChild(node);
        _badges.Paint = PaintBadges;
        _hands.Paint = PaintHands;
    }

    /// <summary>
    /// One frame: the plans brought up to the world, the buttons pressed since the last read — the
    /// middle one's <paramref name="removing"/>, those the cables did not take — the ghost drawn.
    /// </summary>
    public void Frame(Client client, Registry m, Buttons buttons, IReadOnlyList<ButtonEvent> removing, (double X, double Y)? pointer)
    {
        var grid = m.Grid;
        var building = m.Building;
        int width = _width = grid.Width, height = grid.Height;
        DrawPlans(grid, building, m.Rooms);

        var tool = _hand.Selected.Get();
        var holds = _ui.Hand.Get();
        string? overlay = _ui.OverlayOn;
        int CellAt((double X, double Y) p)
        {
            var s = Drag.SpotAt(width, height, p);
            return s.Y * width + s.X;
        }
        void Send(Act act, Spot from, Spot to)
        {
            var planned = new List<int>();
            foreach (var verdict in Drag.Judge(act, Drag.CellsOf(width, act, from, to, building), building))
                foreach (var c in verdict.Commands)
                {
                    client.Send(c);
                    if (c is PlacePlan p) planned.Add(p.Cell);
                }
            if (planned.Count > 0) _ui.Plan(planned);
        }
        // A drag ends with the hand that began it: a card swapped or put away mid-drag sends nothing.
        if (_drag is { } d && (d.Act is Remove ? holds == Holding.Free : d.Act != tool)) _drag = null;

        void DragWith(ButtonEvent button, Act act, bool left)
        {
            if (button.Down)
            {
                // The first button down has the drag; another pressed meanwhile is let be.
                if (_drag == null && Drag.OnGrid(width, height, button.At)) _drag = (act, left, Drag.SpotAt(width, height, button.At));
                return;
            }
            if (_drag is not { } under || under.Left != left) return;
            Send(Tools.Shaped(under.Act, _hand.Shape.Get())!, under.From, Drag.SpotAt(width, height, button.At));
            _drag = null;
        }
        // The left button is the card's, but on the power tab with no card to place, where it lays the
        // cable or carries pegs and plugs: the power feature's.
        bool wiring = overlay == Tools.Power && tool is not PlaceTool;
        foreach (var button in wiring ? [] : buttons.TakeLeft())
            if (tool is PlaceTool place) DragWith(button, place, left: true);
        // The middle button removes in the tab's layers with the menu open.
        foreach (var button in removing)
            if (holds != Holding.Free) DragWith(button, new Remove(Tools.LayersFor(overlay)), left: false);
        // Q picks the card of what is under the pointer, onto its own tab.
        if (_hand.PickWanted)
        {
            _hand.PickWanted = false;
            if (pointer is { } at && Drag.OnGrid(width, height, at)
                && Tools.CardAt(CellAt(at), overlay, grid, building, m.Power, m.Research) is { } card)
            {
                _hand.Tab.Set(Tools.TabOf(card));
                _hand.Selected.Set(card);
                // A wall is picked with its shape (0069).
                if (card is PlaceTool { Placement.Kind: "wall" }) _hand.Shape.Set(Tools.ShapePicked(CellAt(at), overlay, grid, building));
            }
        }

        tool = _hand.Selected.Get();
        bool onGrid = pointer is { } p0 && Drag.OnGrid(width, height, p0);
        ShowTray(onGrid ? CellAt(pointer!.Value) : null, grid, building);
        Ghost(tool, pointer, onGrid, grid, building);
        _hands.QueueRedraw();
    }

    /// <summary>
    /// The plans as ghosts of their own pictures, pipes, doors and walls joined to what is planned and built
    /// beside them, back walls fainter and behind what is built; a blueprint being built real up to its cut (0063);
    /// what a mark has taken so far, the thing's red ghost from the top (<see cref="MarkCutOf"/>); dashes along the
    /// silhouette of each group (<see cref="Outline"/>). Redrawn only when the plans or the grid change, the dashes
    /// only when what they go round does.
    /// </summary>
    private void DrawPlans(IGridQueries grid, BuildingModule building, IRoomsQueries rooms)
    {
        var plans = building.Plans;
        // The grid's version is in both keys, as what is built or taken down beside a plan changes how it joins; what was
        // put in and the work done only in the drawing's, so plans turn real and marks red as they go on.
        string outlined = grid.Version + "|" + string.Join(";", plans.Select(p => $"{p.Id}:{p.Cell}:{p.Place}:{p.Clear}"));
        string key = outlined + "|" + string.Join(";", plans.Select(p => $"{p.Progress}:{string.Join(",", p.Delivered)}"));
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
        foreach (var p in plans)
        {
            if (p.Place is not { } place) continue;
            int linked = Tools.LinksOf(place, p.Cell, width, PlannedAt, grid);
            _plans.Lay(place, p.Cell, width, linked, CutOf(grid, building, p, place));
            shapes.Add((p, place, p.Cell, linked));
        }
        // Built pipes reach toward planned ones beside them, drawn as plans.
        var conduits = plans.Where(p => p.Place is { Layer: "conduit" }).Select(p => p.Cell).ToHashSet();
        foreach (var (cell, sides) in Tools.StubsOf(conduits, width, c => grid.ConduitAt(c) != Conduit.None)) _plans.Stub(cell, width, sides);
        _marked.Begin();
        foreach (var p in plans)
        {
            if (p.Clear is not { } layer) continue;
            int at = MarkedAt(grid, p);
            if (Tools.BuiltAt(at, layer, grid) is not { } built) continue;
            // Joined as the grid draws it, to what is built beside it, marked or not.
            int joined = Tools.LinksOf(built, at, width, _ => [], grid);
            if (MarkCutOf(grid, building, p, built) is { } cut) _marked.Lay(built, at, width, joined, cut);
            shapes.Add((p, built, at, joined));
        }
        if (outlined != _outlined)
        {
            _outlined = outlined;
            Outline(shapes, grid);
        }
        SparkWork(plans, grid, rooms);
        _worked = plans.ToDictionary(p => p.Id, p => p.Progress);
    }

    /// <summary>
    /// Sparks off every plan whose work went on since it was last seen, from its side facing the nearest
    /// hero and toward it (<see cref="Tools.SparksOf"/>): falling aboard, drifting off in space.
    /// </summary>
    private void SparkWork(IReadOnlyList<Sim.Building.Plan> plans, IGridQueries grid, IRoomsQueries rooms)
    {
        foreach (var p in plans)
        {
            if (!_worked.TryGetValue(p.Id, out int was) || p.Progress <= was) continue;
            _owedSparks += (p.Progress - was) * SparksPerTick;
            int count = (int)Math.Floor(_owedSparks);
            _owedSparks -= count;
            if (count == 0) continue;
            var box = BoxOf(CellsOf(grid, p), grid.Width);
            var (x, y, heading) = Tools.SparksOf((box.Position.X, box.Position.Y, box.Size.X, box.Size.Y), _ui.Heroes, SparkReach);
            Particles.Shared.Emit(Effect.Spark, x, y, count, heading, rooms.RoomAt(p.Cell) == RoomsModule.Space);
            _screwdriver.PlayAt(new Vector2((float)x, (float)y));
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
            if (plan.Place is { Layer: "machine" }) machines.Add(BoxOf(CellsOf(grid, plan), width));
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

    /// <summary>Where a mark's thing is drawn from: a machine's top-left, whichever of its cells is marked; else its cell.</summary>
    private static int MarkedAt(IGridQueries grid, Sim.Building.Plan p) =>
        p.Clear == "machine" && grid.MachineAt(p.Cell) is { } machine ? machine.Cell : p.Cell;

    /// <summary>The cells a plan covers: a machine's footprint, else its cell.</summary>
    private static IReadOnlyList<int> CellsOf(IGridQueries grid, Sim.Building.Plan p)
    {
        var machine = p.Place is { Layer: "machine" } place ? (place.Kind, p.Cell)
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

    /// <summary>
    /// Where blueprint <paramref name="p"/> turns from real into plan: across its box as far as its work has gone, a
    /// pixel at a time; none before any. Work stops where a part is not paid for, so a stalled build sits at its boundary.
    /// </summary>
    private static PlanCut? CutOf(IGridQueries grid, BuildingModule building, Sim.Building.Plan p, Placement place)
    {
        if (p.Progress <= 0 || building.TicksFor(p.Id) is not { } ticks) return null;
        var box = BoxOf(CellsOf(grid, p), grid.Width);
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
        var box = BoxOf(CellsOf(grid, p), grid.Width);
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

    /// <summary>The ghost of the card along a drag, or of a removal: its own pictures, green where the sim would take it and red where not.</summary>
    private void Ghost(Tool? tool, (double X, double Y)? pointer, bool onGrid, IGridQueries grid, BuildingModule building)
    {
        _fits.Begin();
        _refused.Begin();
        _taking.Begin();
        var taken = new List<(string, int, int, IReadOnlyList<Rim>)>();
        var takenKey = new System.Text.StringBuilder();
        try
        {
            GhostOf(tool, pointer, onGrid, grid, building, taken, takenKey);
        }
        finally
        {
            // The dashes are laid out again only when what the drag would take changes.
            if (takenKey.ToString() != _takingKey)
            {
                _takingKey = takenKey.ToString();
                _takingOutline.Show(PlanOutline.Of(taken, SmallestHole));
            }
        }
    }

    private void GhostOf(Tool? tool, (double X, double Y)? pointer, bool onGrid, IGridQueries grid, BuildingModule building,
        List<(string, int, int, IReadOnlyList<Rim>)> taken, System.Text.StringBuilder takenKey)
    {
        int width = grid.Width, height = grid.Height;
        _near = [];
        _removing = [];
        _ui.Reason.Set(null);
        if (tool is PlaceTool && pointer is { } near) _near = NearGrid.Around(near, width, height);
        // A wall's card places walls cut to the shape in hand, R turning it mid-drag too (0069).
        Act? act = Tools.Shaped(_drag?.Act ?? (tool as PlaceTool), _hand.Shape.Get());
        if (act == null || pointer is not { } p || (_drag == null && !onGrid)) return;
        var end = Drag.SpotAt(width, height, p);
        var verdicts = Drag.Judge(act, Drag.CellsOf(width, act, _drag?.From ?? end, end, building), building);
        if (act is PlaceTool place)
        {
            // The ghost joins the rest of its drag, what is planned and what is built, as a plan would.
            var cells = verdicts.Select(v => v.Cell).ToHashSet();
            IEnumerable<Placement> Beside(int c) =>
                building.PlansAt(c).Select(o => o.Place).OfType<Placement>().Concat(cells.Contains(c) ? [place.Placement] : []);
            foreach (var v in verdicts)
                (v.Problem != null ? _refused : _fits).Lay(place.Placement, v.Cell, width, Tools.LinksOf(place.Placement, v.Cell, width, Beside, grid));
            // Built and planned pipes beside it reach toward it, in its colour.
            bool Conduit(int c) => grid.ConduitAt(c) != Sim.Grid.Conduit.None || building.PlansAt(c).Any(o => o.Place is { Layer: "conduit" });
            if (place.Placement.Layer == "conduit")
                foreach (var refused in (bool[])[false, true])
                {
                    var those = verdicts.Where(v => v.Problem != null == refused).Select(v => v.Cell).ToHashSet();
                    foreach (var (cell, sides) in Tools.StubsOf(those, width, Conduit)) (refused ? _refused : _fits).Stub(cell, width, sides);
                }
        }
        else
        {
            // What it would take, as its marks will look: each thing's red ghost and dashes round its silhouette;
            // a cell it cannot take, a red box, its reason by the pointer.
            foreach (var v in verdicts)
                foreach (var c in v.Commands)
                {
                    if (c is not Sim.Building.Deconstruct d) continue;
                    int at = d.Layer == "machine" && grid.MachineAt(d.Cell) is { } machine ? machine.Cell : d.Cell;
                    if (Tools.BuiltAt(at, d.Layer, grid) is not { } built) continue;
                    int joined = Tools.LinksOf(built, at, width, _ => [], grid);
                    _taking.Lay(built, at, width, joined);
                    int x = at % width, y = at / width;
                    string group = d.Layer == "machine" ? $"machine{at}" : d.Layer is "back" or "conduit" ? d.Layer : built.Kind;
                    taken.Add((group, x * Cell, y * Cell, PlanShapes.RimOf(built, x, y, joined)));
                    takenKey.Append(d.Layer).Append(at).Append(',');
                }
            _removing = verdicts.Where(v => v.Problem != null).Select(v => (v.Cell, true)).ToList();
        }
        _ui.Reason.Set(verdicts.FirstOrDefault(v => v.Problem != null)?.Problem);
    }

    private void PaintHands(Sketch g)
    {
        foreach (var e in _near)
            g.DrawLine(new Vector2(e.X1, e.Y1) * Cell, new Vector2(e.X2, e.Y2) * Cell, GridLine with { A = (float)e.Alpha }, 1);
        foreach (var (cell, refused) in _removing)
        {
            var box = new Rect2(cell % _width * Cell, cell / _width * Cell, Cell, Cell);
            var colour = refused ? Refused : Fits;
            g.DrawRect(box, colour with { A = 0.3f });
            // A screen pixel wide at any zoom: a primitive line.
            g.DrawRect(box, colour, false);
        }
    }
}
