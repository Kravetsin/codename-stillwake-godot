using System;
using Stillwake.Game.Render;
using Stillwake.Sim.Grid;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// A tool's aim shown without a word (the fan of rays): faint cold glints on the faces its hit would strike and on the plants,
/// as the sim's own query lists them (<see cref="Fan"/>), and its reach: a fading arc for a wide fan, the axe's, a dotted line
/// for a narrow one, the drill's. Luminous motes only (<see cref="Luminous.AimGlint"/>), no light.
/// </summary>
public sealed class FanPreview
{
    /// <summary>Glints a second on each face struck, and along the arc of the reach.</summary>
    private const double PerFace = 6, PerArc = 16;

    /// <summary>How often the dotted line is drawn anew, about a glint's life, so it holds steady.</summary>
    private const double DotsMs = 300;

    /// <summary>A fan at least this wide, in steps of the turn, shows its reach as an arc; a narrower one as a dotted line.</summary>
    private const int WideArc = Spec.Headings.Turn / 16;

    private readonly Random _random = new();
    private double _faces, _arc, _dotsMs;

    /// <summary>Glints <paramref name="fan"/> for this frame of <paramref name="elapsedMs"/>; nothing with none.</summary>
    public void Show(Fan? fan, IGridQueries grid, double elapsedMs)
    {
        if (fan == null)
        {
            (_faces, _arc, _dotsMs) = (0, 0, 0);
            return;
        }
        var weapon = Spec.Weapons.Of(fan.Ground.Tool)!;
        var hand = new Point(fan.Ground.X, fan.Ground.Y);
        double seconds = elapsedMs / 1000;
        var glints = FanFaces.Of(grid, fan.Hewn, hand, fan.Ground.Back);
        for (_faces += seconds * PerFace * glints.Count; _faces >= 1 && glints.Count > 0; _faces--)
        {
            var g = glints[_random.Next(glints.Count)];
            double t = _random.NextDouble(), u = _random.NextDouble();
            // A face is a line: along it; a whole cell, anywhere in it.
            Glint(g.X0 + (g.X1 - g.X0) * t, g.Y0 + (g.Y1 - g.Y0) * (g.X0 == g.X1 || g.Y0 == g.Y1 ? t : u));
        }
        if (weapon.Arc >= WideArc)
        {
            for (_arc += seconds * PerArc; _arc >= 1; _arc--)
            {
                // Fainter toward the arc's ends, so it fades out there.
                double along = _random.NextDouble();
                var (x, y) = FanFaces.ArcAt(weapon, hand, fan.Ground.Heading, along);
                Glint(x, y, 1 - Math.Abs(along - 0.5) * 1.6);
            }
            return;
        }
        _dotsMs -= elapsedMs;
        if (_dotsMs > 0) return;
        _dotsMs = DotsMs;
        foreach (var (x, y) in FanFaces.DotsOf(weapon, hand, fan.Ground.Heading)) Glint(x, y, 0.7);
    }

    /// <summary>One glint at (<paramref name="x"/>, <paramref name="y"/>), in cells, still, <paramref name="bright"/> of its look.</summary>
    private static void Glint(double x, double y, double bright = 1) =>
        Glims.Shared.Add(Luminous.AimGlint, x * Rig.Cell, y * Rig.Cell, 0, 0, bright: bright);
}
