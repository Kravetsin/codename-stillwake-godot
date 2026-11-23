using System;
using System.Collections.Generic;
using System.Linq;
using Stillwake.Game.Shared;
using Stillwake.Sim;
using Stillwake.Sim.Building;
using Stillwake.Sim.Creatures;
using Stillwake.Sim.Debris;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// A tool's hit as a fan of rays from the shoulder (tool-reach): each ray of the weapon's fan (<c>Arc</c>, <c>Rays</c>, <c>Range</c>)
/// walked over the grid as the sim walks one (<see cref="Rays"/>), stopping at its first cell that stops a body; it strikes the cells
/// it walked and every wreck whose drawn pixels and every creature it crosses before it stops: a wreck shields none behind it.
/// </summary>
public static class ToolRays
{
    /// <summary>
    /// What <paramref name="tool"/>'s fan from <paramref name="hero"/>'s shoulder toward <paramref name="heading"/> touches on
    /// <paramref name="m"/>: the cells each ray walks, its stopping one included; while the hero is <paramref name="outside"/>, the
    /// wrecks whose pictures <paramref name="solid"/> says are opaque where a ray crosses them, as drawn <paramref name="progress"/>
    /// of the way to their next tick, each touched where a ray first meets it; the creatures crossed. The rays are walked from the
    /// middle out, so the first ground touch is the middle ray's, and the end is where the middle ray first meets a wreck or stops.
    /// </summary>
    public static EdgeHit Fan(Registry m, Hero hero, string tool, int heading, bool outside, double progress, Func<Piece, double, double, bool> solid)
    {
        var weapon = Spec.Weapons.Of(tool) ?? throw new InvalidOperationException($"{tool} deals no damage");
        var grid = m.Grid;
        int width = grid.Width, height = grid.Height;
        var hand = Aiming.HandOf(hero);
        (double X, double Y) from = (hand.X, hand.Y);
        var wrecks = WrecksNear(m, outside, progress, (from.X - weapon.Range, from.Y - weapon.Range, from.X + weapon.Range, from.Y + weapon.Range));
        var met = new Crossed(everyGround: false);
        (double X, double Y)? end = null;
        var walked = new List<(int Cell, double Enter)>();
        int n = weapon.Rays;
        foreach (int i in Enumerable.Range(0, n).OrderBy(i => Math.Abs(2 * i - (n - 1))).ThenBy(i => i))
        {
            var (dx, dy) = Rays.Direction(Rays.HeadingOf(weapon, heading, i));
            double ux = dx / (double)Rays.Cell, uy = dy / (double)Rays.Cell;
            walked.Clear();
            int? stopped = null;
            Rays.Cast(width, height, hand.X, hand.Y, Rays.HeadingOf(weapon, heading, i), weapon.Range, cell =>
            {
                walked.Add((cell, EnterOf(cell % width, cell / width, from.X, from.Y, ux, uy)));
                bool stops = Stops(grid, cell);
                if (stops) stopped ??= cell;
                return stops;
            });
            double length = Math.Sqrt(ux * ux + uy * uy) * weapon.Range;
            var (first, reached) = Cross(m, from, (ux, uy), length, walked, stopped, wrecks, solid, met);
            end ??= first ?? reached;
        }
        return new EdgeHit(met.Cells, met.Struck, met.Touched, met.Touches, end ?? from);
    }

    /// <summary>
    /// What a thrown thing touches from <paramref name="from"/> to <paramref name="to"/> on <paramref name="m"/> (companion-fights),
    /// as a fan's ray does: the cells the sim's walk crosses (<see cref="Rays.Walk"/>), the wrecks and creatures before the first
    /// that stops a ray; each ground cell a touch, the thrower knowing which it struck already; and whether one stopped it, the end there.
    /// </summary>
    public static (EdgeHit Hit, bool Stopped) Segment(Registry m, (double X, double Y) from, (double X, double Y) to, bool outside, double progress,
        Func<Piece, double, double, bool> solid)
    {
        double dx = to.X - from.X, dy = to.Y - from.Y, length = Math.Sqrt(dx * dx + dy * dy);
        var (ux, uy) = length > 0 ? (dx / length, dy / length) : (1.0, 0.0);
        var walked = new List<(int Cell, double Enter)>();
        var grid = m.Grid;
        int width = grid.Width, x = (int)Math.Floor(from.X), y = (int)Math.Floor(from.Y);
        int? stopped = null;
        // Both ends rounded down, as a cell is: none starts in a cell the last step had not entered.
        Rays.Walk(width, grid.Height, x, y, (long)Math.Floor(to.X) - x, (long)Math.Floor(to.Y) - y, cell =>
        {
            walked.Add((cell, Math.Clamp(EnterOf(cell % width, cell / width, from.X, from.Y, ux, uy), 0, length)));
            bool stops = Stops(grid, cell);
            if (stops) stopped ??= cell;
            return stops;
        });
        var wrecks = WrecksNear(m, outside, progress, (Math.Min(from.X, to.X), Math.Min(from.Y, to.Y), Math.Max(from.X, to.X), Math.Max(from.Y, to.Y)));
        var met = new Crossed(everyGround: true);
        var (_, reached) = Cross(m, from, (ux, uy), length, walked, stopped, wrecks, solid, met);
        return (new EdgeHit(met.Cells, met.Struck, met.Touched, met.Touches, reached), stopped != null);
    }

    /// <summary>What stops a tool's ray.</summary>
    private static bool Stops(Sim.Grid.IGridQueries grid, int cell) => grid.WallAt(cell) != null || grid.Blocks(cell);

    /// <summary>What the rays of one hit met so far, each once; the ground's first touch, or with <paramref name="everyGround"/> every one.</summary>
    private sealed class Crossed(bool everyGround)
    {
        public bool TakesGround => everyGround || !Grounded;
        public readonly List<int> Cells = [];
        public readonly List<Piece> Struck = [];
        public readonly List<Creature> Touched = [];
        public readonly List<EdgeContact> Touches = [];
        public bool Grounded;
    }

    /// <summary>
    /// The one lookup along a line: into <paramref name="met"/> what a ray <paramref name="length"/> long meets over the cells it
    /// <paramref name="walked"/>, up to the one that <paramref name="stopped"/> it; answers where it first met a wreck and where it ends.
    /// </summary>
    private static ((double X, double Y)? First, (double X, double Y) End) Cross(Registry m, (double X, double Y) from, (double X, double Y) unit, double length,
        List<(int Cell, double Enter)> walked, int? stopped, List<(Piece Piece, double X, double Y, double Reach2)> wrecks, Func<Piece, double, double, bool> solid, Crossed met)
    {
        var (ux, uy) = unit;
        double reach = stopped != null ? walked[^1].Enter : length;
        // Along what the grid lets it walk, every wreck whose pixels it crosses, touched where it first meets each.
        (double X, double Y)? first = null;
        if (wrecks.Count > 0)
            for (double s = 0; s <= reach; s += ToolEdges.Step)
            {
                double px = from.X + ux * s, py = from.Y + uy * s;
                foreach (var w in wrecks)
                    if (!met.Struck.Contains(w.Piece) && (px - w.X) * (px - w.X) + (py - w.Y) * (py - w.Y) <= w.Reach2 && solid(w.Piece, px, py))
                    {
                        met.Struck.Add(w.Piece);
                        met.Touches.Add(new((px, py), null, w.Piece));
                        first ??= (px, py);
                    }
            }
        var (endX, endY) = (from.X + ux * reach, from.Y + uy * reach);
        foreach (var (cell, enter) in walked)
        {
            if (!met.Cells.Contains(cell)) met.Cells.Add(cell);
            if (met.TakesGround && (cell == stopped || m.Building.PlantAt(cell) != null || m.Building.TargetAt(cell) is { Kind: TargetKind.Machine }))
            {
                double at = Math.Max(enter, 0);
                met.Touches.Add(new((from.X + ux * at, from.Y + uy * at), cell, null));
                met.Grounded = true;
            }
        }
        foreach (var c in m.Creatures.Creatures)
        {
            if (!met.Touched.Contains(c) && Aiming.Entered((from.X, from.Y, endX, endY), Aiming.BoxOf(m, c)) != null) met.Touched.Add(c);
        }
        return (first, (endX, endY));
    }

    /// <summary>The wrecks in space whose picture's circle may reach into <paramref name="box"/>, as drawn; none while not <paramref name="outside"/>.</summary>
    private static List<(Piece Piece, double X, double Y, double Reach2)> WrecksNear(Registry m, bool outside, double progress,
        (double Left, double Top, double Right, double Bottom) box)
    {
        var wrecks = new List<(Piece Piece, double X, double Y, double Reach2)>();
        if (!outside) return wrecks;
        foreach (var w in m.Debris.Wrecks())
        {
            if (m.Debris.PieceById(w.Piece) is not { } piece) continue;
            var (x, y) = PieceDrawn.At(piece, progress);
            var (halfW, halfH) = Aiming.PictureOf(piece.Item);
            // A cell over: a direction's length is a hair off one.
            double reach2 = (double)halfW * halfW + (double)halfH * halfH, near = Math.Sqrt(reach2) + Rays.Cell;
            if (x >= box.Left - near && x <= box.Right + near && y >= box.Top - near && y <= box.Bottom + near) wrecks.Add((piece, x, y, reach2));
        }
        return wrecks;
    }

    /// <summary>
    /// How far along a ray from (<paramref name="x"/>, <paramref name="y"/>) by (<paramref name="ux"/>, <paramref name="uy"/>) a unit
    /// it enters cell (<paramref name="cx"/>, <paramref name="cy"/>), in thousandths of a cell; under zero for the cell it starts in.
    /// </summary>
    private static double EnterOf(int cx, int cy, double x, double y, double ux, double uy)
    {
        double Slab(double origin, double toward, double low) =>
            toward == 0 ? double.NegativeInfinity : Math.Min((low - origin) / toward, (low + Rays.Cell - origin) / toward);
        return Math.Max(Slab(x, ux, cx * (double)Rays.Cell), Slab(y, uy, cy * (double)Rays.Cell));
    }

    /// <summary>The heading of <paramref name="hero"/>'s fan toward <paramref name="toward"/>, a point in thousandths of a cell, from its shoulder.</summary>
    public static int HeadingOf(Hero hero, (double X, double Y) toward) =>
        Aiming.HeadingOf(Aiming.HandOf(hero), new Point((int)Math.Round(toward.X), (int)Math.Round(toward.Y)));
}
