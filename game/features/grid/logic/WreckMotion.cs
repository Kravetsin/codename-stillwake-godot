using System;
using System.Collections.Generic;
using Stillwake.Content;
using Stillwake.Game.Render;
using Stillwake.Sim.Grid;

namespace Stillwake.Game.Features.Grid;

/// <summary>A wire hanging out of a tear: its cell of the drawing, its ends at rest and how it sways, in pixels of the drawing.</summary>
public sealed record WreckWire((int X, int Y) Cell, (double X, double Y) From, (double X, double Y) To, double Bow, double Swing, double DriftSpeed, int Seed, (int R, int G, int B) Colour);

/// <summary>
/// What still moves on the wreck (2026-10-31-the-scoop), a rule of drawing, never the sim's: wires swaying out of its keel, arcs jumping at two
/// tears, a thread of vapour from a third. Each lasts as long as its tear shows (<see cref="WreckDress.ShowsTear"/>), and throws nothing off the screen.
/// </summary>
public sealed class WreckMotion
{
    private const int Pixels = 8;
    /// <summary>Points of a wire past its root, as a cable's rope has; how far apart in phase they sway, a floating cable's wave.</summary>
    private const int Links = 16;
    private const double Wave = 0.35;
    private const double ThreadEveryMs = 150, ThreadHeading = 0.75, ThreadSpread = 0.07;
    private const double ArcFromMs = 6000, ArcToMs = 14000, ArcShortestMs = 150, ArcLongestMs = 210;
    /// <summary>How far an arc bows, a share of its length.</summary>
    private const double TipBow = 0.37, RoofBow = 0.13;
    /// <summary>Where the wire's arc lands, in links back from its tip; which wire throws it.</summary>
    private const int TipLinks = 4, LongWire = 2;

    /// <summary>The wires, the long one last, drawn over the others.</summary>
    public static readonly IReadOnlyList<WreckWire> Wires =
    [
        new((66, 21), (531, 174), (523, 188), -1.5, 2.5, 0.0078, 9, (86, 111, 140)),
        new((68, 21), (548, 174), (539, 196), 1.5, 3, 0.012, 12, (107, 122, 85)),
        new((67, 21), (540, 174), (532, 228), 2.5, 6, 0.01, 7, (124, 135, 148)),
    ];

    /// <summary>The tear the thread leaves, and where from in pixels of the drawing.</summary>
    public static readonly (int X, int Y) ThreadCell = (69, 20);
    private static readonly (double X, double Y) ThreadAt = (557, 166);

    /// <summary>The cells of the roof tear its arc jumps between, and its ends in pixels of the drawing.</summary>
    public static readonly (int X, int Y)[] RoofCells = [(34, 8), (32, 7)];
    private static readonly (double X, double Y) RoofFrom = (273, 71), RoofTo = (263, 65);

    private readonly IGridQueries _grid;
    private readonly Glims _glims;
    private readonly Bolts _bolts;
    private readonly Func<double> _random;
    private readonly (int X, int Y) _corner;
    private readonly (double X, double Y) _origin;
    private readonly List<WreckWire> _live = [];
    private readonly BoltEnds _tipEnds, _roofEnds;
    private readonly (double X, double Y)[] _tipArc = new (double, double)[Links + 1];
    private int _version = -1;
    private bool _threads, _roofArcs, _tipArcs;
    private double _ms, _owed, _roofDue, _tipDue;

    /// <param name="random">Draws from 0 to 1; tests pass their own.</param>
    public WreckMotion(IGridQueries grid, Glims glims, Bolts bolts, Func<double>? random = null)
    {
        (_grid, _glims, _bolts, _random) = (grid, glims, bolts, random ?? Random.Shared.NextDouble);
        _corner = WreckDress.CornerIn(grid.Width, grid.Height);
        _origin = ((_corner.X + Wreck.Left) * Pixels, (_corner.Y + Wreck.Top) * Pixels);
        _roofDue = Between(ArcFromMs, ArcToMs);
        _tipDue = Between(ArcFromMs, ArcToMs);
        _tipEnds = TipEnds;
        _roofEnds = () => (RoofFrom.X + _origin.X, RoofFrom.Y + _origin.Y, RoofTo.X + _origin.X, RoofTo.Y + _origin.Y, RoofBow);
    }

    /// <summary>The wires whose tears show now.</summary>
    public IReadOnlyList<WreckWire> LiveWires => _live;

    /// <summary>The whole number of points a wire is drawn with.</summary>
    public static int PointCount => Links + 1;

    private double Between(double from, double to) => from + (to - from) * _random();

    /// <summary>
    /// Everything <paramref name="elapsedMs"/> on, <paramref name="seen"/> saying whether a pixel of the world is on the screen: what is off it throws nothing, and
    /// an arc falling due there is simply missed.
    /// </summary>
    public void Step(double elapsedMs, Func<double, double, bool> seen)
    {
        _ms += elapsedMs;
        if (_grid.Version != _version) LookAgain();
        double threadX = ThreadAt.X + _origin.X, threadY = ThreadAt.Y + _origin.Y;
        if (_threads && seen(threadX, threadY))
        {
            for (_owed += elapsedMs / ThreadEveryMs * Fx.Count; _owed >= 1; _owed--)
            {
                double a = ThreadHeading + Between(-ThreadSpread, ThreadSpread), v = Between(6, 9);
                _glims.Add(Luminous.WreckThread, threadX + Between(-0.25, 0.25), threadY + Between(-0.25, 0.25), Math.Cos(a) * v, Math.Sin(a) * v);
            }
        }
        else _owed = 0;
        if ((_tipDue -= elapsedMs) <= 0)
        {
            _tipDue = Between(ArcFromMs, ArcToMs);
            if (_tipArcs && Seen(_tipEnds, seen)) Jump(_tipEnds);
        }
        if ((_roofDue -= elapsedMs) <= 0)
        {
            _roofDue = Between(ArcFromMs, ArcToMs);
            if (_roofArcs && Seen(_roofEnds, seen)) Jump(_roofEnds);
        }
    }

    private static bool Seen(BoltEnds ends, Func<double, double, bool> seen)
    {
        var (x, y, _, _, _) = ends();
        return seen(x, y);
    }

    private void Jump(BoltEnds ends) =>
        _bolts.Add(ends, (int)Math.Round(Between(ArcShortestMs, ArcLongestMs) / Glim.SampleMs), fine: true);

    private void LookAgain()
    {
        _version = _grid.Version;
        _live.Clear();
        foreach (var wire in Wires)
            if (Shows(wire.Cell)) _live.Add(wire);
        _threads = Shows(ThreadCell);
        _roofArcs = Array.TrueForAll(RoofCells, Shows);
        _tipArcs = Shows(Wires[LongWire].Cell);
    }

    private bool Shows((int X, int Y) cell) => WreckDress.ShowsTear(_grid, cell.X, cell.Y, _corner);

    /// <summary>
    /// The points of <paramref name="wire"/> now, in pixels of the world, root first, into <paramref name="into"/>: its curve at rest, plus a floating cable's slow drift
    /// growing toward the free end. Worked out from the time alone: a rope pins both its ends, and this has one free.
    /// </summary>
    public void PointsOf(WreckWire wire, Span<(double X, double Y)> into)
    {
        double steps = _ms / Rope.StepMs;
        for (int i = 0; i <= Links; i++)
        {
            double s = (double)i / Links, a = steps * wire.DriftSpeed + wire.Seed * 1.7 + i * Wave, grow = Math.Pow(s, 1.6);
            double x = wire.From.X + (wire.To.X - wire.From.X) * s + wire.Bow * Math.Sin(s * Math.PI * 1.15);
            double y = wire.From.Y + (wire.To.Y - wire.From.Y) * s;
            into[i] = (x + Math.Cos(a) * wire.Swing * grow + _origin.X, y + Math.Sin(a * 1.3) * wire.Swing * 0.5 * grow + _origin.Y);
        }
    }

    /// <summary>The long wire's arc, on its tip as it moves: an arc lives on ends that move (<see cref="Bolt"/>).</summary>
    private (double, double, double, double, double) TipEnds()
    {
        PointsOf(Wires[LongWire], _tipArc);
        var (tip, up) = (_tipArc[Links], _tipArc[Links - TipLinks]);
        return (tip.X, tip.Y, up.X, up.Y, TipBow);
    }
}
