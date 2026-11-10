using System;
using System.Collections.Generic;
using System.Linq;
using Stillwake.Game.Shared;
using Stillwake.Sim;
using Stillwake.Sim.Building;
using Stillwake.Sim.Creatures;
using Stillwake.Sim.Crew;
using Stillwake.Sim.Debris;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// A running tool's working part as drawn this frame (tool-edge), in thousandths of a cell: from <paramref name="From"/> to
/// <paramref name="To"/>, <paramref name="HalfWidth"/> either side. A <paramref name="Beam"/> (the cutter's) ends at the first
/// thing that stops it; any other edge touches all it overlaps.
/// </summary>
public sealed record Edge((double X, double Y) From, (double X, double Y) To, double HalfWidth, bool Beam);

/// <summary>Where an edge first touches something, in thousandths of a cell: a wall's or plant's <paramref name="Cell"/>, or a <paramref name="Wreck"/>.</summary>
public readonly record struct EdgeContact((double X, double Y) At, int? Cell, Piece? Wreck);

/// <summary>
/// What an <see cref="Edge"/> or a fan of rays (<see cref="ToolRays"/>) touches (tool-edge, tool-reach): the grid's <paramref name="Cells"/> under it and the <paramref name="Wrecks"/> whose
/// pictures and the <paramref name="Creatures"/> whose bodies it overlaps, each once in the order it first meets them; where it first
/// touches the ground, a wall or a plant, and each wreck (<paramref name="Touches"/>), in that order too; and where it ends, a beam
/// where it stops.
/// </summary>
public sealed record EdgeHit(IReadOnlyList<int> Cells, IReadOnlyList<Piece> Wrecks, IReadOnlyList<Creature> Creatures,
    IReadOnlyList<EdgeContact> Touches, (double X, double Y) End)
{
    /// <summary>Where it first touches anything, none touching nothing.</summary>
    public EdgeContact? Contact => Touches.Count > 0 ? Touches[0] : null;

    /// <summary>Where it first touches the ground, a wall or a plant; none on none.</summary>
    public EdgeContact? Ground => Touches.FirstOrDefault(t => t.Cell != null) is { Cell: not null } ground ? ground : null;
}

/// <summary>What a running tool's hit sends from what its edge touches (tool-edge): each only if the sim would take it, the strikes on creatures last.</summary>
public sealed record EdgeCommands(HewCells? Cells, HewPieces? Pieces, IReadOnlyList<Strike> Strikes)
{
    /// <summary>Whether the hit sends anything.</summary>
    public bool Any => Cells != null || Pieces != null || Strikes.Count > 0;
}

/// <summary>A running tool hits with what is drawn (tool-edge): what its edge touches, the client's word to the sim.</summary>
public static class ToolEdges
{
    /// <summary>How far apart the edge is sampled, along and across: half a world pixel, in thousandths of a cell.</summary>
    public const double Step = Hero.Unit / 16.0;

    /// <summary>
    /// What <paramref name="edge"/> touches on <paramref name="m"/>'s grid: every cell under it; while the hero is
    /// <paramref name="outside"/>, every wreck in space whose picture <paramref name="solid"/> says is opaque there (asked only within
    /// the circle round the picture), each where it is drawn <paramref name="progress"/> of the way to its next tick; every creature whose body and margin it overlaps. A beam stops at
    /// the first wall, the first cell that stops a body, or the first wreck's pixel.
    /// </summary>
    public static EdgeHit Hits(Registry m, Edge edge, bool outside, double progress, Func<Piece, double, double, bool> solid)
    {
        var grid = m.Grid;
        int width = grid.Width, height = grid.Height;
        // Each wreck within the circle round its picture, which it never leaves however it is turned; its pixels decide.
        var wrecks = new List<(Piece Piece, double X, double Y, double Reach2)>();
        if (outside)
            foreach (var w in m.Debris.Wrecks())
                if (m.Debris.Pieces.FirstOrDefault(p => p.Id == w.Piece) is { } piece)
                {
                    var (x, y) = PieceDrawn.At(piece, progress);
                    var (halfW, halfH) = Aiming.PictureOf(piece.Item);
                    wrecks.Add((piece, x, y, (double)halfW * halfW + (double)halfH * halfH));
                }
        var creatures = m.Creatures.Creatures;
        var cells = new List<int>();
        var struck = new List<Piece>();
        var touched = new List<Creature>();
        var touches = new List<EdgeContact>();
        bool grounded = false;
        double dx = edge.To.X - edge.From.X, dy = edge.To.Y - edge.From.Y, length = Math.Sqrt(dx * dx + dy * dy);
        var (ux, uy) = length > 0 ? (dx / length, dy / length) : (1.0, 0.0);
        double reach = length;
        int across = (int)Math.Ceiling(edge.HalfWidth / Step);
        for (double s = 0; s <= reach; s += Step)
        {
            bool stops = false;
            for (int k = -across; k <= across; k++)
            {
                double o = across == 0 ? 0 : edge.HalfWidth * k / across;
                double px = edge.From.X + ux * s - uy * o, py = edge.From.Y + uy * s + ux * o;
                int cx = (int)Math.Floor(px / Hero.Unit), cy = (int)Math.Floor(py / Hero.Unit);
                if (cx >= 0 && cy >= 0 && cx < width && cy < height)
                {
                    int cell = cy * width + cx;
                    if (!cells.Contains(cell)) cells.Add(cell);
                    bool solidCell = grid.WallAt(cell) != null || grid.Blocks(cell);
                    if (!grounded && (solidCell || m.Building.PlantAt(cell) != null || m.Building.TargetAt(cell) is { Wreck: true }))
                    {
                        touches.Add(new((px, py), cell, null));
                        grounded = true;
                    }
                    stops |= solidCell;
                }
                foreach (var w in wrecks)
                {
                    if ((px - w.X) * (px - w.X) + (py - w.Y) * (py - w.Y) > w.Reach2 || !solid(w.Piece, px, py)) continue;
                    if (!struck.Contains(w.Piece))
                    {
                        struck.Add(w.Piece);
                        touches.Add(new((px, py), null, w.Piece));
                    }
                    stops = true;
                }
                foreach (var c in creatures)
                {
                    var (left, top, right, bottom) = Aiming.BoxOf(c);
                    if (px >= left && px <= right && py >= top && py <= bottom && !touched.Contains(c)) touched.Add(c);
                }
            }
            if (edge.Beam && stops)
            {
                reach = s;
                break;
            }
        }
        return new EdgeHit(cells, struck, touched, touches, (edge.From.X + ux * reach, edge.From.Y + uy * reach));
    }

    /// <summary>
    /// What peer <paramref name="peer"/>'s hit of <paramref name="tool"/> sends from <paramref name="hit"/>: the cells, unless the tool
    /// breaks no block (the cutter), and the wrecks, each where the sim would take it as the client asks it, and a strike on each
    /// creature it may strike.
    /// </summary>
    public static EdgeCommands CommandsOf(Registry m, int peer, string tool, EdgeHit hit)
    {
        var cells = hit.Cells.ToList();
        var pieces = hit.Wrecks.Select(p => p.Id).ToList();
        bool digs = cells.Count > 0 && m.Items.BlockDamage(peer, tool) != 0 && m.Building.HewCellsProblem(peer, tool, cells) == null;
        bool chops = pieces.Count > 0 && m.Debris.HewPiecesProblem(peer, tool, pieces) == null;
        return new EdgeCommands(digs ? new HewCells(tool, cells) : null, chops ? new HewPieces(tool, pieces) : null,
            Aiming.StrikesOf(m, peer, hit.Creatures, tool));
    }

    /// <summary>The first cell <paramref name="hit"/> touches that holds something a hit strikes, heard and seen as the hit's own; none for none.</summary>
    public static int? LeadOf(Registry m, EdgeHit hit)
    {
        foreach (int cell in hit.Cells)
            if (!m.Building.Listed([cell]).IsEmpty) return cell;
        return null;
    }

    /// <summary>
    /// The edge of <paramref name="tool"/> drawn from <paramref name="start"/> to <paramref name="end"/>, in thousandths of a cell, by
    /// <paramref name="hero"/>: the drawn part itself, or the cutter's beam, from its nozzle along the barrel to the weapon's
    /// <c>Range</c> from the hand, where its jet ends.
    /// </summary>
    public static Edge Of(string tool, Hero hero, (double X, double Y) start, (double X, double Y) end)
    {
        var look = HeldLook.Of(tool);
        double half = (look?.Bite ?? 0) * Hero.Unit / Rig.Cell;
        if (tool != Spec.Weapons.PlasmaCutter.Item) return new Edge(start, end, half, false);
        double dx = end.X - start.X, dy = end.Y - start.Y, length = Math.Sqrt(dx * dx + dy * dy);
        var (ux, uy) = length > 0 ? (dx / length, dy / length) : (hero.Facing, 0.0);
        var hand = Aiming.HandOf(hero);
        // The jet's end at its reach from the hand, along the barrel: past the nozzle, never short of the last coil.
        double along = (hand.X - start.X) * ux + (hand.Y - start.Y) * uy + Spec.Weapons.PlasmaCutter.Range;
        along = Math.Max(along, length);
        return new Edge(start, (start.X + ux * along, start.Y + uy * along), half, true);
    }
}
