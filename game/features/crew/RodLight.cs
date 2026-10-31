using System;
using Godot;
using Stillwake.Game.Render;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// The harpoon's light (0044, 0075), one a gun, this hero's or another's: a glint at the muzzle and a jet of cold gas
/// as it fires; a thin trail of light behind the flying rod, motes left in its wake; where it bites metal a glint, a
/// short spray of sparks and a faint ring; a bead of light running down the line to the gun as it goes taut; and once a
/// reel is over, a streak zipping home along the line to the hand, ending in a glint and a ring. In pixels of the world.
/// </summary>
public sealed class RodLight
{
    /// <summary>Motes the flying rod leaves a second, times <see cref="Fx.Count"/>.</summary>
    private const double WakePerSecond = 20;
    private static readonly (int, int, int) Muzzle = (230, 238, 255), Warm = (255, 228, 180), Bead = (255, 236, 200),
        BiteGlint = (255, 220, 160), BiteRing = (255, 236, 210), Home = (170, 225, 255), HomeGlint = (190, 230, 255);

    private readonly RodFlourish _flourish = new();
    private double _wake;
    private Vector2 _zipFrom;

    /// <summary>The shot leaving <paramref name="muzzle"/> along <paramref name="barrel"/>, a unit vector: a glint and the gas, on <paramref name="stage"/>, its figure's held one.</summary>
    public static void Fired(Stage stage, Vector2 muzzle, Vector2 barrel)
    {
        stage.Flares.Glint(muzzle.X + barrel.X, muzzle.Y + barrel.Y, 5, Muzzle);
        var glims = stage.Glims;
        foreach (var (look, count, heading, spread, speed, ahead) in HarpoonShot.Puff((barrel.X, barrel.Y)))
            for (int i = 0; i < Math.Round(count * Fx.Count); i++)
            {
                double a = heading + glims.Between(-spread, spread), s = glims.Between(speed.From, speed.To);
                glims.Add(look, muzzle.X + barrel.X * ahead, muzzle.Y + barrel.Y * ahead, Math.Cos(a) * s, Math.Sin(a) * s);
            }
    }

    /// <summary>
    /// The rod biting at <paramref name="tip"/>, flying along <paramref name="heading"/>: a glint and a faint ring, and off
    /// <paramref name="metal"/> a short spray of sparks thrown back.
    /// </summary>
    public static void Bit(Vector2 tip, Vector2 heading, bool metal)
    {
        Stages.Of(Layers.Effects).Flares.Shock(tip.X, tip.Y, 1.5, 9.5, 9 * Glim.SampleMs, BiteRing, 0.3 * Fx.Glow, 0.26);
        if (!metal) return;
        Stages.Of(Layers.Effects).Flares.Glint(tip.X, tip.Y, 7, BiteGlint);
        Stages.Of(Layers.Effects).Glims.Spray(Luminous.BiteSpark, tip.X, tip.Y, (int)Math.Round(8 * Fx.Count), Math.Atan2(-heading.Y, -heading.X), (-1.3, 1.3), (40, 110));
    }

    /// <summary>
    /// The rod where <paramref name="rod"/> says, its point at <paramref name="tip"/> pointing <paramref name="pointing"/> (a unit
    /// vector), its eye at <paramref name="eye"/>, from the gun at <paramref name="muzzle"/>; none while it is in the gun.
    /// </summary>
    public void Step(Rod rod, Vector2 tip, Vector2 pointing, Vector2 eye, Vector2? muzzle, double elapsedMs)
    {
        var shown = _flourish.Step(rod, muzzle != null, elapsedMs);
        if (shown.ZipBegan) _zipFrom = tip;
        if (muzzle is not { } gun) return;
        if (rod == Rod.Flying) Fly(tip, pointing, eye, gun, elapsedMs);
        if (shown.Bead is { } bead) BeadDown(eye, gun, bead);
        if (shown.Zip is { } zip) ZipHome(gun, zip);
        if (shown.ZipHome) Arrive(gun);
    }

    private void Fly(Vector2 tip, Vector2 pointing, Vector2 eye, Vector2 gun, double elapsedMs)
    {
        // The eye still in the gun leaves nothing, and its trail never reaches back into the gun.
        float clear = (eye - gun).Dot(pointing);
        if (clear <= 0) return;
        var back = eye - pointing * Math.Min((float)(26 * Fx.Length), clear);
        Stages.Of(Layers.Tethers).Flares.Trail([(tip.X - pointing.X * 4, tip.Y - pointing.Y * 4), (eye.X, eye.Y), (back.X, back.Y)], Warm, 0.6, 0.75, 0.45);
        var glims = Stages.Of(Layers.Tethers).Glims;
        _wake += WakePerSecond * Fx.Count * elapsedMs / 1000;
        for (; _wake >= 1; _wake--)
            glims.Add(Luminous.Wake, eye.X, eye.Y, glims.Between(-4, 4) - pointing.X * 6, glims.Between(-4, 4) - pointing.Y * 6);
    }

    /// <summary>A bead of light running from the rod's eye down the taut line to the gun, quickening.</summary>
    private static void BeadDown(Vector2 eye, Vector2 gun, double q)
    {
        var at = eye.Lerp(gun, (float)(q * q));
        var behind = at + (eye - gun).Normalized() * 7;
        Stages.Of(Layers.Tethers).Flares.Bloom(at.X, at.Y, 6, Warm, 0.8);
        Stages.Of(Layers.Tethers).Flares.Trail(at.X, at.Y, behind.X, behind.Y, Bead, 0.7, 0.9, 0.35);
    }

    /// <summary>The streak home in: a glint, a ring and a flash at the hand.</summary>
    private static void Arrive(Vector2 gun)
    {
        Stages.Of(Layers.Tethers).Flares.Glint(gun.X, gun.Y, 6, HomeGlint);
        Stages.Of(Layers.Tethers).Flares.Shock(gun.X, gun.Y, 2, 11, 12 * Glim.SampleMs, HomeGlint, 0.35 * Fx.Glow, 0.28);
        Stages.Of(Layers.Tethers).Flares.Flash(gun.X, gun.Y, (160, 220, 255), 0.3 * Fx.Glow, 2, 0.45f, 0.85);
    }

    /// <summary>A reel over: a streak zipping from where the rod was home to the gun.</summary>
    private void ZipHome(Vector2 gun, double q)
    {
        double e = q * q;
        var head = _zipFrom.Lerp(gun, (float)e);
        var way = (gun - _zipFrom).Normalized();
        float length = (float)((10 + 30 * Math.Sin(Math.PI * e)) * Fx.Length);
        var tail = head - way * Math.Min(length, head.DistanceTo(_zipFrom));
        Stages.Of(Layers.Tethers).Flares.Trail(head.X, head.Y, tail.X, tail.Y, Home, 0.75, 1, 0.5);
        Stages.Of(Layers.Tethers).Flares.Bloom(head.X, head.Y, 7, HomeGlint, 0.9);
    }
}
