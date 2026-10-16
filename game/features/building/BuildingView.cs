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
/// Plans drawn over the ship as their own pictures in blue, marks as what they take out in red, both as
/// projections (<see cref="PlanTiles"/>), and the hand at work
/// (features/building/index.ts, 0022, 0030): a card's ghost along a left drag, a removal's along a middle
/// one in the open tab's layers, coloured by what the sim would say, with its reason by the pointer; a
/// release sends the commands. Q picks a card. With no card out, no grid: space stays space. Work on a
/// plan sparks and ticks with the screwdriver, heard from the plan.
/// </summary>
public partial class BuildingView : Node2D
{
    private const int Cell = ShipCamera.Cell;
    /// <summary>A plan's tint: its own picture, blue, not built yet.</summary>
    private static readonly Color Plan = AsciiArt.Colour(0x5fd0ff, 0.75f);
    private static readonly Color Mark = AsciiArt.Colour(0xe05555), Fits = AsciiArt.Colour(0x7fe08a), Refused = AsciiArt.Colour(0xe05555);
    private static readonly Color Bar = AsciiArt.Colour(0xf2f5fa), BarBack = AsciiArt.Colour(0x0b0f17), GridLine = AsciiArt.Colour(0x9fb4c8);
    /// <summary>The bar of a blueprint still being delivered to: what it has, in the colour of materials.</summary>
    private static readonly Color Delivered = AsciiArt.Colour(0xe8b04a);

    private readonly Ui _ui;
    private readonly PlanHand _hand;
    // Back walls at 0.35 against the rest's 0.75, as the TypeScript game drew them.
    private readonly PlanTiles _plans = new(backAlpha: 0.35f / 0.75f, projected: true) { ZIndex = Layers.Plans, Modulate = Plan };
    /// <summary>What a mark takes out, its own picture again in red over it.</summary>
    private readonly PlanTiles _marked = new(projected: true) { ZIndex = Layers.Plans, Modulate = Mark with { A = 0.7f } };
    private readonly PlanTiles _fits = new() { ZIndex = Layers.PlanGhost, Modulate = Fits with { A = 0.75f } };
    private readonly PlanTiles _refused = new() { ZIndex = Layers.PlanGhost, Modulate = Refused with { A = 0.75f } };
    private readonly Sketch _marks = new(Layers.PlanMarks), _hands = new(Layers.PlanHands);

    private string _drawn = "";
    private List<(Rect2 Box, float Share, bool Lacks)> _shownPlans = [];
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
        foreach (var node in (Node2D[])[_plans, _marked, _marks, _fits, _refused, _hands, _screwdriver]) AddChild(node);
        _marks.Paint = PaintPlans;
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
        Ghost(tool, pointer, onGrid, width, height, building);
        _hands.QueueRedraw();
    }

    /// <summary>
    /// The plans as their own pictures in blue, pipes and doors joined to the plans beside them, back walls
    /// fainter; marks as what they take out in red; and a bar along the bottom of each plan: a
    /// blueprint's share delivered while it lacks something, in amber, then the work done, in white.
    /// Redrawn only when the plans change.
    /// </summary>
    private void DrawPlans(IGridQueries grid, BuildingModule building, IRoomsQueries rooms)
    {
        var plans = building.Plans;
        // What was delivered and the work done are in the key, so bars grow as they go on.
        string key = string.Join(";", plans.Select(p => $"{p.Id}:{p.Cell}:{p.Place?.Kind}:{p.Clear}:{p.Progress}:{string.Join(",", p.Delivered)}"));
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
        foreach (var p in plans)
        {
            if (p.Place is not { } place) continue;
            int linked = Blueprint.LinksOf(p.Cell, width, c => placed.TryGetValue(c, out var others) && others.Any(o => Blueprint.Joins(place, o)));
            _plans.Lay(place, p.Cell, width, linked);
        }
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
    /// Beside the pointer (<see cref="Ui.CostTip"/>), what the blueprint at <paramref name="cell"/> has
    /// against what it costs, while it still lacks something; nothing anywhere else.
    /// </summary>
    private void ShowTray(int? cell, BuildingModule building)
    {
        var plan = cell is int c ? building.PlansAt(c).FirstOrDefault(p => p.Place != null && building.Lacking(p.Id).Count > 0) : null;
        var cost = plan != null ? building.CostOf(plan.Id) : null;
        _ui.CostTip.SetLines(plan != null && cost != null ? CostLine.Of(cost, plan.Delivered.Select(s => (s.Item, s.Count)).ToList()) : null);
    }

    /// <summary>The ghost of the card along a drag, or of a removal: its own pictures, green where the sim would take it and red where not.</summary>
    private void Ghost(Tool? tool, (double X, double Y)? pointer, bool onGrid, int width, int height, BuildingModule building)
    {
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
