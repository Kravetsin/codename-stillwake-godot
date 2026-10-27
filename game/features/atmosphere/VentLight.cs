using System;
using System.Collections.Generic;
using Godot;
using Stillwake.Game.Render;

namespace Stillwake.Game.Features.Atmosphere;

/// <summary>
/// Air escaping into space (0075), luminous: out of each opening a room vents through, a jet of fast pale streaks and a
/// plume of condensation curling and thinning into space, as thick as its stream (<see cref="AirStream.PerSecond"/>);
/// over the nearest breaches the escaping air bends the light (heat shimmer's wobble, <see cref="Shimmer"/>), and frost
/// on their edges twinkles. Only openings on the screen throw anything. In pixels of the world.
/// </summary>
public partial class VentLight : Node2D
{
    private const int Cell = ShipCamera.Cell;
    /// <summary>The most breaches that shimmer at once: each reads the screen again.</summary>
    public const int Shimmers = 6;
    /// <summary>Condensation puffs a stream throws, a share of its streaks; how often a breach's frost twinkles somewhere.</summary>
    private const double CondensationShare = 0.4, FrostEveryMs = 280;
    private static readonly (int, int, int) Frost = (215, 240, 255), FrostGlint = (210, 240, 255);

    private readonly Shimmer[] _shimmers = new Shimmer[Shimmers];
    private double[] _jets = [], _puffs = [];
    private double _frostMs;
    private readonly Random _random = new();
    private readonly List<int> _seen = [];
    private readonly HashSet<(int X, int Y)> _openings = [];

    public VentLight()
    {
        for (int i = 0; i < Shimmers; i++) AddChild(_shimmers[i] = new Shimmer { Rise = -14 });
    }

    private double Between(double from, double to) => from + (to - from) * _random.NextDouble();

    /// <summary>The breaches of <paramref name="streams"/> (cells) <paramref name="elapsedMs"/> on; those off the screen throw nothing.</summary>
    public void Step(IReadOnlyList<AirStream> streams, double elapsedMs)
    {
        if (_jets.Length != streams.Count) (_jets, _puffs) = (new double[streams.Count], new double[streams.Count]);
        var view = GetViewport().GetCanvasTransform().AffineInverse() * GetViewportRect();
        view = view.Grow(4 * Cell);
        _seen.Clear();
        for (int i = 0; i < streams.Count; i++)
        {
            var s = streams[i];
            var at = new Vector2((float)s.X, (float)s.Y) * Cell;
            if (!view.HasPoint(at))
            {
                (_jets[i], _puffs[i]) = (0, 0);
                continue;
            }
            _seen.Add(i);
            Emit(s, at, ref _jets[i], ref _puffs[i], elapsedMs);
        }
        _openings.Clear();
        foreach (var s in streams) _openings.Add(((int)Math.Floor(s.X), (int)Math.Floor(s.Y)));
        int shown = 0;
        foreach (int i in _seen)
        {
            if (streams[i].Heading is not double heading) continue;
            var at = new Vector2((float)streams[i].X, (float)streams[i].Y) * Cell;
            var way = Vector2.FromAngle((float)heading);
            if (shown < Shimmers)
            {
                var middle = at + way * Cell * 1.4f;
                _shimmers[shown].Box = new Rect2(middle - new Vector2(15, 16), new Vector2(30, 32));
                _shimmers[shown++].Amplitude = 0.6f;
            }
            Edges(at, way, ((int)Math.Floor(streams[i].X), (int)Math.Floor(streams[i].Y)));
        }
        for (int i = shown; i < Shimmers; i++) _shimmers[i].Amplitude = 0;
        Twinkle(streams, elapsedMs);
    }

    /// <summary>A stream's streaks and condensation, from anywhere across its opening, its owed share carried over.</summary>
    private void Emit(AirStream s, Vector2 at, ref double jets, ref double puffs, double elapsedMs)
    {
        var glims = Stages.Of(Layers.Effects).Glims;
        double perMs = s.PerSecond * Fx.Count / 1000;
        for (jets += perMs * elapsedMs; jets >= 1; jets--)
        {
            double heading = s.Heading ?? Between(0, Math.Tau), a = heading + Between(-0.5, 0.5), v = Between(90, 170);
            glims.Add(Luminous.Jet, at.X + Between(-0.5, 0.5) * Cell, at.Y + Between(-0.5, 0.5) * Cell, Math.Cos(a) * v, Math.Sin(a) * v);
        }
        for (puffs += perMs * CondensationShare * elapsedMs; puffs >= 1; puffs--)
        {
            double heading = s.Heading ?? Between(0, Math.Tau), a = heading + Between(-0.55, 0.55), v = Between(20, 44);
            var from = at + Vector2.FromAngle((float)heading) * 4;
            glims.Add(Luminous.Condensation, from.X + Between(-0.35, 0.35) * Cell, from.Y + Between(-0.35, 0.35) * Cell, Math.Cos(a) * v, Math.Sin(a) * v);
        }
    }

    /// <summary>
    /// Frost along the edges of an opening, cell <paramref name="cell"/>, the air streams out of along <paramref name="way"/>, pale and
    /// faint: only where it meets the wall, not between two cells of one breach.
    /// </summary>
    private void Edges(Vector2 at, Vector2 way, (int X, int Y) cell)
    {
        var across = new Vector2(-way.Y, way.X) * (Cell * 0.48f);
        foreach (int side in (ReadOnlySpan<int>)[-1, 1])
        {
            if (_openings.Contains((cell.X + (int)Math.Round(-way.Y * side), cell.Y + (int)Math.Round(way.X * side)))) continue;
            var (inner, outer) = (at + across * side - way * Cell * 0.5f, at + across * side + way * Cell * 0.5f);
            Stages.Of(Layers.Effects).Flares.Trail(inner.X, inner.Y, outer.X, outer.Y, Frost, 0.3, 0.3, 0.3);
        }
    }

    /// <summary>Now and then a glint on the frost of a breach on the screen, at a point along one of its edges.</summary>
    private void Twinkle(IReadOnlyList<AirStream> streams, double elapsedMs)
    {
        if ((_frostMs += elapsedMs) < FrostEveryMs || _seen.Count == 0) return;
        _frostMs = 0;
        var s = streams[_seen[_random.Next(_seen.Count)]];
        if (s.Heading is not double heading) return;
        var way = Vector2.FromAngle((float)heading);
        var across = new Vector2(-way.Y, way.X) * (Cell * 0.48f) * (_random.NextDouble() < 0.5 ? -1 : 1);
        var at = new Vector2((float)s.X, (float)s.Y) * Cell + across + way * (float)(Between(-0.4, 0.5) * Cell);
        Stages.Of(Layers.Effects).Flares.Glint(at.X, at.Y, 2.8, FrostGlint);
    }
}
