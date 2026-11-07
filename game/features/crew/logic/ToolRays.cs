using System;
using System.Collections.Generic;
using System.Linq;
using Stillwake.Game.Shared;
using Stillwake.Sim;
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
        var from = Aiming.HandOf(hero);
        var wrecks = new List<(Piece Piece, double X, double Y, double Reach2)>();
        if (outside)
            foreach (var w in m.Debris.Wrecks())
                if (m.Debris.Pieces.FirstOrDefault(p => p.Id == w.Piece) is { } piece)
                {
                    var (x, y) = PieceDrawn.At(piece, progress);
                    var (halfW, halfH) = Aiming.PictureOf(piece.Item);
                    wrecks.Add((piece, x, y, (double)halfW * halfW + (double)halfH * halfH));
                }
        var cells = new List<int>();
        var struck = new List<Piece>();
        var touched = new List<Creature>();
        var touches = new List<EdgeContact>();
        bool grounded = false;
        (double X, double Y)? end = null;
        var walked = new List<(int Cell, double Enter)>();
        int n = weapon.Rays;
        foreach (int i in Enumerable.Range(0, n).OrderBy(i => Math.Abs(2 * i - (n - 1))).ThenBy(i => i))
        {
            var (dx, dy) = Rays.Direction(Rays.HeadingOf(weapon, heading, i));
            double ux = dx / (double)Rays.Cell, uy = dy / (double)Rays.Cell;
            walked.Clear();
            int? stopped = null;
            Rays.Cast(width, height, from.X, from.Y, Rays.HeadingOf(weapon, heading, i), weapon.Range, cell =>
            {
                walked.Add((cell, EnterOf(cell % width, cell / width, from.X, from.Y, ux, uy)));
                bool stops = grid.WallAt(cell) != null || grid.Blocks(cell);
                if (stops) stopped = cell;
                return stops;
            });
            double length = Math.Sqrt(ux * ux + uy * uy) * weapon.Range;
            double reach = stopped != null ? walked[^1].Enter : length;
            // Along what the grid lets it walk, every wreck whose pixels it crosses, touched where it first meets each.
            (double X, double Y)? met = null;
            if (wrecks.Count > 0)
                for (double s = 0; s <= reach; s += ToolEdges.Step)
                {
                    double px = from.X + ux * s, py = from.Y + uy * s;
                    foreach (var w in wrecks)
                        if (!struck.Contains(w.Piece) && (px - w.X) * (px - w.X) + (py - w.Y) * (py - w.Y) <= w.Reach2 && solid(w.Piece, px, py))
                        {
                            struck.Add(w.Piece);
                            touches.Add(new((px, py), null, w.Piece));
                            met ??= (px, py);
                        }
                }
            var (endX, endY) = (from.X + ux * reach, from.Y + uy * reach);
            foreach (var (cell, enter) in walked)
            {
                if (!cells.Contains(cell)) cells.Add(cell);
                if (!grounded && (cell == stopped || m.Building.PlantAt(cell) != null))
                {
                    double at = Math.Max(enter, 0);
                    touches.Add(new((from.X + ux * at, from.Y + uy * at), cell, null));
                    grounded = true;
                }
            }
            foreach (var c in m.Creatures.Creatures)
            {
                var (left, top, right, bottom) = Aiming.BoxOf(c);
                if (!touched.Contains(c) && Aiming.Entered((from.X, from.Y, endX, endY), left, top, right, bottom) != null) touched.Add(c);
            }
            end ??= met ?? (endX, endY);
        }
        return new EdgeHit(cells, struck, touched, touches, end ?? (from.X, from.Y));
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
