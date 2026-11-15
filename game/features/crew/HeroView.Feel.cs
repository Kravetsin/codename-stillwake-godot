using System;
using Stillwake.Game.Features.Effects;
using Stillwake.Game.Render;
using Stillwake.Sim;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// What this hero's actions stir: the air pushed round it (air-gusts), a stroke's from its blade, a railgun shot's round the feet and the
/// muzzle; and the room its body's answer may step into (body-answers).
/// </summary>
public partial class HeroView
{
    /// <summary>What <paramref name="gust"/> stirs, from the ground <paramref name="m"/> holds; no dust where a cell floats in no air.</summary>
    private static void Blow(Registry m, Gust gust) =>
        Gusts.Throw(Stages.Of(Layers.Effects), m.Grid, c => m.Rooms.Afloat(c) && !m.Atmosphere.AiredAt(c), m.Rooms.Afloat, gust);

    /// <summary>
    /// A stroke of <paramref name="tool"/> landing as <paramref name="hit"/>: the air it drives on, from where it first touches, else
    /// from the end of its reach, away from the shoulder.
    /// </summary>
    private void StrokeGust(Registry m, string tool, EdgeHit hit)
    {
        if (GustStrengths.StrokeOf(tool) is not var (strength, radius)) return;
        var at = hit.Contact?.At ?? hit.End;
        var hand = Aiming.HandOf(_current);
        double tx = at.X / Hero.Unit, ty = at.Y / Hero.Unit, dx = tx - (double)hand.X / Hero.Unit, dy = ty - (double)hand.Y / Hero.Unit;
        double d = Math.Sqrt(dx * dx + dy * dy);
        Blow(m, new Gust(tx, ty, d > 0 ? dx / d : 0, d > 0 ? dy / d : 0, strength, radius));
    }

    /// <summary>A railgun's shot: the floor round the feet blown out every way, the air ahead of the muzzle driven along the barrel.</summary>
    private void ShotGust(Registry m)
    {
        double fx = (double)_current.X / Hero.Unit, fy = (double)_current.Y / Hero.Unit;
        var (feet, reach) = GustStrengths.RailFeet;
        Blow(m, new Gust(fx, fy - 0.2, 0, 0, feet, reach));
        if (_figure.Muzzle is { } at)
        {
            var (muzzle, ahead) = GustStrengths.RailMuzzle;
            Blow(m, new Gust(at.X / Camera.Cell, at.Y / Camera.Cell, _figure.Barrel.X, _figure.Barrel.Y, muzzle, ahead));
        }
    }

    /// <summary>How far, in pixels, the floor ahead of <paramref name="hero"/> and behind it is free for an answer's step.</summary>
    private static (double Ahead, double Behind) RoomOf(Ship ship, Hero hero)
    {
        int most = (int)Math.Ceiling(BodyAnswers.MostAheadPx / Camera.Cell * Hero.Unit);
        return ((double)Hero.Room(ship, hero, 1, most) * Camera.Cell / Hero.Unit, (double)Hero.Room(ship, hero, -1, most) * Camera.Cell / Hero.Unit);
    }
}
