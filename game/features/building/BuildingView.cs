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
/// Plans drawn over the ship as pale ghosts of their own pictures inside marching dashes, a "+" on each
/// machine; marks as projections of what they take out in red (<see cref="PlanTiles"/>); and the hand at work
/// (features/building/index.ts, 0022, 0030): a card's ghost along a left drag, a removal's along a middle
/// one in the open tab's layers, coloured by what the sim would say, with its reason by the pointer; a
/// release sends the commands. Q picks a card. With no card out, no grid: space stays space. Work on a
/// plan sparks and ticks with the screwdriver, heard from the plan.
/// </summary>
public partial class BuildingView : Node2D
{
    private const int Cell = ShipCamera.Cell;
    /// <summary>A plan's tint and how strongly it shows: its own picture as a pale ghost, a little blue, not built yet.</summary>
    private static readonly Color Plan = AsciiArt.Colour(0xc8f6ff, 0.7f);
    /// <summary>A planned back wall shows fainter than the rest, as it is behind.</summary>
    private const float BackAlpha = 0.45f / 0.7f;
    /// <summary>The dashes round each group of plans and the badge on a planned machine.</summary>
    private static readonly Color Dashes = AsciiArt.Colour(0xc8f6ff, 0.95f), Badge = AsciiArt.Colour(0x6fe3ff), BadgeBack = AsciiArt.Colour(0x0b1a24);
    /// <summary>A dash and its gap, in pixels of the world, and how many pixels a second they march round.</summary>
    private const int Dash = 3, DashPeriod = 5;
    private const double March = 5;
    private static readonly Color Mark = AsciiArt.Colour(0xe05555), Fits = AsciiArt.Colour(0x7fe08a), Refused = AsciiArt.Colour(0xe05555);
    private static readonly Color Bar = AsciiArt.Colour(0xf2f5fa), BarBack = AsciiArt.Colour(0x0b0f17), GridLine = AsciiArt.Colour(0x9fb4c8);
    /// <summary>The bar of a blueprint still being delivered to: what it has, in the colour of materials.</summary>
    private static readonly Color Delivered = AsciiArt.Colour(0xe8b04a);

    private readonly Ui _ui;
    private readonly PlanHand _hand;
    // Planned back walls behind what is built in front of them; the rest over the ship.
    private readonly PlanTiles _plans = new(PlanLook.Ghost, BackAlpha, backZ: Layers.PlanBacks) { ZIndex = Layers.Plans, Modulate = Plan };
    /// <summary>What a mark takes out, its own picture again in red over it.</summary>
    private readonly PlanTiles _marked = new(PlanLook.Projection) { ZIndex = Layers.Plans, Modulate = Mark with { A = 0.7f } };
    // The ghost in hand as a plan is drawn, but plainly green or red.
    private readonly PlanTiles _fits = new(PlanLook.Ghost, tint: 0.45f) { ZIndex = Layers.PlanGhost, Modulate = Fits with { A = 0.8f } };
    private readonly PlanTiles _refused = new(PlanLook.Ghost, tint: 0.45f) { ZIndex = Layers.PlanGhost, Modulate = Refused with { A = 0.8f } };
    private readonly Sketch _marks = new(Layers.PlanMarks), _hands = new(Layers.PlanHands);
    /// <summary>The marching dashes round plans and the badges on machines; round back walls, behind what is built.</summary>
    private readonly Sketch _outline = new(Layers.PlanMarks), _backOutline = new(Layers.PlanBacks);

    private string _drawn = "";
    private List<(Rect2 Box, float Share, bool Lacks)> _shownPlans = [];
    private List<OutlineRun> _runs = [], _backRuns = [];
    private List<Rect2> _machines = [];
    /// <summary>Seconds the dashes have marched.</summary>
    private double _marched;
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
        foreach (var node in (Node2D[])[_plans, _backOutline, _marked, _outline, _marks, _fits, _refused, _hands, _screwdriver]) AddChild(node);
        _marks.Paint = PaintPlans;
        _hands.Paint = PaintHands;
        _outline.Paint = g => PaintOutline(g, _runs, _machines);
        _backOutline.Paint = g => PaintOutline(g, _backRuns, []);
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
        _marched += GetProcessDeltaTime();
        if (_runs.Count > 0) _outline.QueueRedraw();
        if (_backRuns.Count > 0) _backOutline.QueueRedraw();

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
            foreach (var verdict in Drag.Judge(act, Drag.LineOf(width, act, from, to), building))
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
            Send(under.Act, under.From, Drag.SpotAt(width, height, button.At));
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
            }
        }

        tool = _hand.Selected.Get();
        bool onGrid = pointer is { } p0 && Drag.OnGrid(width, height, p0);
        ShowTray(onGrid ? CellAt(pointer!.Value) : null, building);
        Ghost(tool, pointer, onGrid, grid, building);
        _hands.QueueRedraw();
    }

    /// <summary>
    /// The plans as ghosts of their own pictures, pipes, doors and walls joined to what is planned and built
    /// beside them, back walls fainter and behind what is built; dashes round each group; marks as what they take out in red; and a bar along the bottom of each plan: a
    /// blueprint's share delivered while it lacks something, in amber, then the work done, in white.
    /// Redrawn only when the plans or the grid change.
    /// </summary>
    private void DrawPlans(IGridQueries grid, BuildingModule building, IRoomsQueries rooms)
    {
        var plans = building.Plans;
        // What was delivered and the work done are in the key, so bars grow as they go on; the grid's version,
        // as what is built or taken down beside a plan changes how it joins.
        string key = grid.Version + "|" + string.Join(";", plans.Select(p => $"{p.Id}:{p.Cell}:{p.Place?.Kind}:{p.Clear}:{p.Progress}:{string.Join(",", p.Delivered)}"));
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
        foreach (var p in plans)
        {
            if (p.Place is not { } place) continue;
            _plans.Lay(place, p.Cell, width, Tools.LinksOf(place, p.Cell, width, PlannedAt, grid));
        }
        Outline(plans, grid);
        _marked.Begin();
        foreach (var p in plans)
        {
            if (p.Clear is not { } layer) continue;
            int at = layer == "machine" && grid.MachineAt(p.Cell) is { } machine ? machine.Cell : p.Cell;
            if (Tools.BuiltAt(at, layer, grid) is not { } built) continue;
            int joined = Blueprint.LinksOf(at, width, c => Tools.BuiltAt(c, layer, grid) is { } other && Blueprint.Joins(built, other));
            _marked.Lay(built, at, width, joined);
        }
        _shownPlans = plans.Select(p =>
        {
            var box = BoxOf(CellsOf(grid, p), width);
            bool lacks = p.Place != null && building.Lacking(p.Id).Count > 0;
            float share = lacks
                ? (float)p.Delivered.Sum(s => s.Count) / Math.Max(1, building.CostOf(p.Id)?.Sum(c => c.Count) ?? 1)
                : (float)p.Progress / (building.TicksFor(p.Id) ?? 1);
            return (box, share, lacks);
        }).ToList();
        _marks.QueueRedraw();
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
    /// The dashes round each group of blueprints (<see cref="PlanOutline"/>): walls, back walls, ladders, doors,
    /// each row of floor along its strip, each machine round its footprint, which gets a badge. Pipes and valves
    /// are lines of their own, marks red over what they take out: neither is outlined.
    /// </summary>
    private void Outline(IReadOnlyList<Sim.Building.Plan> plans, IGridQueries grid)
    {
        int floor = PixelArt.Picture("floor").GetHeight();
        var cells = new List<(int Cell, string Group, int Height)>();
        var backs = new List<(int Cell, string Group, int Height)>();
        var machines = new List<Rect2>();
        foreach (var p in plans)
            switch (p.Place)
            {
                case null or { Layer: "conduit" }:
                    break;
                case { Layer: "back" }:
                    backs.Add((p.Cell, "back", Cell));
                    break;
                case { Layer: "machine" }:
                    var footprint = CellsOf(grid, p);
                    foreach (int c in footprint) cells.Add((c, $"machine{p.Id}", Cell));
                    machines.Add(BoxOf(footprint, grid.Width));
                    break;
                case { Kind: "floor" }:
                    cells.Add((p.Cell, "floor", floor));
                    break;
                case { } place:
                    cells.Add((p.Cell, place.Kind, Cell));
                    break;
            }
        _runs = PlanOutline.Of(cells, grid.Width, Cell);
        _backRuns = PlanOutline.Of(backs, grid.Width, Cell);
        _machines = machines;
        _outline.QueueRedraw();
        _backOutline.QueueRedraw();
    }

    /// <summary>The dashes of <paramref name="runs"/>, marching, and a "+" on the corner of each of <paramref name="machines"/>.</summary>
    private void PaintOutline(Sketch g, List<OutlineRun> runs, List<Rect2> machines)
    {
        double offset = _marched * March;
        foreach (var run in runs)
            foreach (var (x, y, w, h) in PlanOutline.Dashes(run, offset, Dash, DashPeriod))
                g.DrawRect(new Rect2(x, y, w, h), Dashes);
        foreach (var box in machines)
        {
            // Seven pixels square on the top right corner, a pixel of it past the machine's edge.
            var badge = new Rect2(box.End.X - 6, box.Position.Y - 1, 7, 7);
            g.DrawRect(badge, Badge);
            g.DrawRect(badge.Grow(-1), BadgeBack);
            g.DrawRect(new Rect2(badge.Position + new Vector2(2, 3), 3, 1), Badge);
            g.DrawRect(new Rect2(badge.Position + new Vector2(3, 2), 1, 3), Badge);
        }
    }

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

    private void PaintPlans(Sketch g)
    {
        foreach (var (box, share, lacks) in _shownPlans)
        {
            if (share <= 0) continue;
            var bar = new Rect2(box.Position.X + 2, box.End.Y - 5, box.Size.X - 4, 3);
            g.DrawRect(bar, BarBack);
            g.DrawRect(bar with { Size = new Vector2(bar.Size.X * Math.Min(1, share), 3) }, lacks ? Delivered : Bar);
        }
    }

    /// <summary>
    /// Beside the pointer (<see cref="Ui.CostTip"/>), what the blueprint at <paramref name="cell"/> builds and,
    /// row by row, what this player carries against what it still needs, while it lacks something;
    /// nothing anywhere else. Once all is delivered the work bar says the rest, so the tip goes.
    /// </summary>
    private void ShowTray(int? cell, BuildingModule building)
    {
        var plan = cell is int c ? building.PlansAt(c).FirstOrDefault(p => p.Place != null && building.Lacking(p.Id).Count > 0) : null;
        var cost = plan != null ? building.CostOf(plan.Id) : null;
        _ui.CostTip.Set(plan?.Place is { } place && cost != null
            ? new PlanTip(place, CostLine.Lacking(cost, plan.Delivered.Select(s => (s.Item, s.Count)).ToList(), _ui.Carried))
            : null);
    }

    /// <summary>The ghost of the card along a drag, or of a removal: its own pictures, green where the sim would take it and red where not.</summary>
    private void Ghost(Tool? tool, (double X, double Y)? pointer, bool onGrid, IGridQueries grid, BuildingModule building)
    {
        int width = grid.Width, height = grid.Height;
        _fits.Begin();
        _refused.Begin();
        _near = [];
        _removing = [];
        _ui.Reason.Set(null);
        if (tool is PlaceTool && pointer is { } near) _near = NearGrid.Around(near, width, height);
        Act? act = _drag?.Act ?? (tool as PlaceTool);
        if (act == null || pointer is not { } p || (_drag == null && !onGrid)) return;
        var end = Drag.SpotAt(width, height, p);
        var verdicts = Drag.Judge(act, Drag.LineOf(width, act, _drag?.From ?? end, end), building);
        if (act is PlaceTool place)
        {
            var cells = verdicts.Select(v => v.Cell).ToHashSet();
            bool together = Blueprint.Joins(place.Placement, place.Placement);
            foreach (var v in verdicts)
            {
                int linked = together ? Blueprint.LinksOf(v.Cell, width, cells.Contains) : 0;
                (v.Problem != null ? _refused : _fits).Lay(place.Placement, v.Cell, width, linked);
            }
        }
        else _removing = verdicts.Select(v => (v.Cell, v.Problem != null)).ToList();
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
