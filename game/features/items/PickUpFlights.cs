using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Stillwake.Game.Render;

namespace Stillwake.Game.Features.Items;

/// <summary>
/// A stack picked up flying home (0075): off the floor, or out of a piece of debris this hero caught (slice 3). Its icon,
/// whole and shrinking a little, flies to the hero's chest along an arc in a fifth of a second, trailing a thin ribbon of
/// light, and lands in a glint and a faint ring with a flash of light. What one catch gave sets off one after another, each
/// arc a little higher, and only the first lands with a ring and a flash; at most <see cref="Fx.MostRibbons"/> fly at once,
/// the rest shown by their counts alone. A gain dug, caught or picked up flies as a spark instead, no icon (<see cref="Spark"/>). Drawn only, this player's own pick-ups. In pixels of the world.
/// </summary>
public partial class PickUpFlights : Node2D
{
    /// <summary>How long a flight takes, in 60 Hz frames, and how high its arc rises, pixels; the picture starts as large as it lay.</summary>
    private const double Frames = 13, Rise = 12;
    /// <summary>How many 60 Hz frames after the one before it each of a catch's icons sets off; how much higher it arcs, a share of <see cref="Rise"/>.</summary>
    private const double Stagger = 3, Higher = 0.35;
    /// <summary>The ribbon's points, one each 60th of a second.</summary>
    private const int Kept = 10;
    /// <summary>
    /// A spark's flight on the screen: a fifth of a second and a second more for every <see cref="SparkPxPerS"/> px it goes; its arc
    /// rising <see cref="SparkRisePx"/> and a share of the way over the higher end. On the screen, so it reads alike at every zoom.
    /// </summary>
    private const double SparkS = 0.22, SparkPxPerS = 800, SparkRisePx = 30, SparkRiseShare = 0.12;
    private static readonly (int, int, int) Ribbon = (150, 215, 255), Landed = (170, 225, 255);

    private sealed class Flight(string item, Vector2 from, int nth, Vector2? to)
    {
        /// <summary>Where it lands: a crate stashed into; none for the chest, wherever the hero is by then.</summary>
        public Vector2? To { get; } = to;
        /// <summary>A spark: its light alone, no icon, landing in a small glint.</summary>
        public bool Spark { get; init; }
        /// <summary>A spark's flight, ms, set as it sets off.</summary>
        public double Takes;
        public string Item { get; } = item;
        public Vector2 From { get; } = from;
        /// <summary>Which of its catch's icons it is, 0 the first.</summary>
        public int Nth { get; } = nth;
        public Vector2 At = from;
        /// <summary>How long it has flown, in ms; below 0 while it waits its turn.</summary>
        public double Ms = -nth * Stagger * Glim.SampleMs, Since;
        public readonly (double X, double Y)[] Trail = new (double, double)[Kept];
        public int Points;
    }

    private readonly List<Flight> _flights = [];
    private readonly Node2D _icons = new(), _tints = new();

    public PickUpFlights()
    {
        ZIndex = Layers.Effects;
        ZAsRelative = false;
        _icons.Material = new CanvasItemMaterial { LightMode = CanvasItemMaterial.LightModeEnum.Unshaded };
        _tints.Material = new CanvasItemMaterial
        {
            BlendMode = CanvasItemMaterial.BlendModeEnum.Add,
            LightMode = CanvasItemMaterial.LightModeEnum.Unshaded,
        };
        AddChild(_icons);
        AddChild(_tints);
        _icons.Draw += () => DrawIcons(_icons, 1);
        _tints.Draw += () => DrawIcons(_tints, 0.45f);
    }

    /// <summary>
    /// A stack of <paramref name="item"/> picked up from <paramref name="from"/>, the <paramref name="nth"/> of what one catch
    /// gave (0 the first, and any stack off the floor): its icon sets off for the chest in its turn, or for <paramref name="to"/>, a
    /// crate stashed into (2026-11-06-quick-stash); none past the cap.
    /// </summary>
    public void Fly(string item, Vector2 from, int nth = 0, Vector2? to = null)
    {
        if (_flights.Count(f => !f.Spark) < Fx.MostRibbons) _flights.Add(new Flight(item, from, nth, to));
    }

    /// <summary>
    /// A gain (caught, picked up, dug, felled or refunded) from <paramref name="from"/>, the <paramref name="nth"/> of its catch: a
    /// spark, no icon, so it hides nothing, arcing into the chest; none past the cap, its count beside its place saying it all.
    /// </summary>
    public void Spark(string item, Vector2 from, int nth)
    {
        if (_flights.Count(f => f.Spark) < Fx.MostSparks) _flights.Add(new Flight(item, from, nth, null) { Spark = true });
    }

    /// <summary>Every flight <paramref name="elapsedMs"/> on toward <paramref name="chest"/>, where the hero is now, or its crate; those there land.</summary>
    public void Update(Vector2 chest, double elapsedMs)
    {
        float zoom = GetViewport().GetCanvasTransform().Scale.Y;
        for (int i = _flights.Count - 1; i >= 0; i--)
        {
            var f = _flights[i];
            f.Ms += elapsedMs;
            if (f.Ms < 0) continue;
            var home = f.To ?? chest;
            double q;
            if (f.Spark)
            {
                // Its time is set by the way on the screen as it sets off.
                double screen = (home - f.From).Length() * zoom;
                if (f.Takes == 0) f.Takes = (SparkS + screen / SparkPxPerS) * 1000;
                q = Math.Min(1, f.Ms / f.Takes);
                double z = 0.35 * q + 0.65 * q * q, u = 1 - z, rise = (SparkRisePx + SparkRiseShare * screen) * (1 + Higher * f.Nth) / zoom;
                var top = new Vector2((f.From.X + home.X) / 2, Math.Min(f.From.Y, home.Y) - (float)rise);
                f.At = (float)(u * u) * f.From + (float)(2 * u * z) * top + (float)(z * z) * home;
            }
            else
            {
                q = Math.Min(1, f.Ms / Glim.SampleMs / Frames);
                double z = q * q;
                f.At = f.From.Lerp(home, (float)z) + new Vector2(0, (float)(-Rise * (1 + Higher * f.Nth) * Math.Sin(Math.PI * z)));
            }
            if ((f.Since += elapsedMs) >= Glim.SampleMs || f.Points == 0)
            {
                f.Since = 0;
                Array.Copy(f.Trail, 0, f.Trail, 1, Kept - 1);
                f.Points = Math.Min(Kept, f.Points + 1);
            }
            f.Trail[0] = (f.At.X, f.At.Y);
            int n = Math.Max(2, (int)Math.Round(f.Points * Math.Min(1, Fx.Length)));
            if (f.Points > 1) Stages.Of(Layers.Effects).Flares.Trail(f.Trail.AsSpan(0, Math.Min(n, f.Points)), Ribbon, 0.7, 0.85, 0.7);
            Stages.Of(Layers.Effects).Flares.Bloom(f.At.X, f.At.Y, f.Spark ? 7 / zoom : 14, Ribbon, f.Spark ? 0.5 : 0.35);
            if (q < 1) continue;
            if (f.Spark) Stages.Of(Layers.Effects).Flares.Glint(home.X, home.Y, 5 / zoom, Landed);
            else if (f.Nth == 0)
            {
                Stages.Of(Layers.Effects).Flares.Glint(home.X, home.Y, 6, Landed);
                Stages.Of(Layers.Effects).Flares.Shock(home.X, home.Y, 2, 11, 12 * Glim.SampleMs, Landed, 0.35 * Fx.Glow, 0.26);
                Stages.Of(Layers.Effects).Flares.Flash(home.X, home.Y, Ribbon, 0.3 * Fx.Glow, 4, 0.671f, 0.82);
            }
            else Stages.Of(Layers.Effects).Flares.Glint(home.X, home.Y, 4, Landed);
            _flights.RemoveAt(i);
        }
        _icons.QueueRedraw();
        _tints.QueueRedraw();
    }

    /// <summary>Each flying icon, shrinking toward the chest; drawn again added over itself, pale, so it reads as light.</summary>
    private void DrawIcons(Node2D on, float alpha)
    {
        foreach (var f in _flights)
        {
            if (f.Spark || f.Ms < 0) continue;
            double q = Math.Min(1, f.Ms / Glim.SampleMs / Frames);
            var picture = ItemIcons.Icon(f.Item);
            var size = picture.GetSize() * (float)(1 - 0.45 * q);
            var colour = alpha < 1 ? new Color(0.81f, 0.92f, 1, alpha) : Colors.White;
            on.DrawTextureRect(picture, new Rect2(f.At - size / 2, size), false, colour);
        }
    }
}
