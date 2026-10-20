using System;
using System.Collections.Generic;
using Godot;
using Stillwake.Game.Render;

namespace Stillwake.Game.Features.Items;

/// <summary>
/// A stack picked up off the floor flying home (0075): its icon, whole and shrinking a little, flies to the hero's chest
/// along an arc in a fifth of a second, trailing a thin ribbon of light, and lands in a glint and a faint ring with a
/// flash of light. Drawn only, this player's own pick-ups. In pixels of the world.
/// </summary>
public partial class PickUpFlights : Node2D
{
    /// <summary>How long a flight takes, in 60 Hz frames, how high its arc rises, pixels, and the icon's size at the start.</summary>
    private const double Frames = 13, Rise = 12, Size = 8;
    /// <summary>The ribbon's points, one each 60th of a second.</summary>
    private const int Kept = 10;
    private static readonly (int, int, int) Ribbon = (150, 215, 255), Landed = (170, 225, 255);

    private sealed class Flight(string item, Vector2 from)
    {
        public string Item { get; } = item;
        public Vector2 From { get; } = from;
        public Vector2 At = from;
        public double Ms, Since;
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

    /// <summary>A stack of <paramref name="item"/> picked up from <paramref name="from"/>: its icon sets off for the chest.</summary>
    public void Fly(string item, Vector2 from) => _flights.Add(new Flight(item, from));

    /// <summary>Every flight <paramref name="elapsedMs"/> on toward <paramref name="chest"/>, where the hero is now; those home land.</summary>
    public void Update(Vector2 chest, double elapsedMs)
    {
        for (int i = _flights.Count - 1; i >= 0; i--)
        {
            var f = _flights[i];
            f.Ms += elapsedMs;
            double q = Math.Min(1, f.Ms / Glim.SampleMs / Frames), z = q * q;
            f.At = f.From.Lerp(chest, (float)z) + new Vector2(0, (float)(-Rise * Math.Sin(Math.PI * z)));
            if ((f.Since += elapsedMs) >= Glim.SampleMs || f.Points == 0)
            {
                f.Since = 0;
                Array.Copy(f.Trail, 0, f.Trail, 1, Kept - 1);
                f.Points = Math.Min(Kept, f.Points + 1);
            }
            f.Trail[0] = (f.At.X, f.At.Y);
            int n = Math.Max(2, (int)Math.Round(f.Points * Math.Min(1, Fx.Length)));
            if (f.Points > 1) Flares.Shared.Trail(f.Trail.AsSpan(0, Math.Min(n, f.Points)), Ribbon, 0.7, 0.85, 0.7);
            Flares.Shared.Bloom(f.At.X, f.At.Y, 14, Ribbon, 0.35);
            if (q < 1) continue;
            Flares.Shared.Glint(chest.X, chest.Y, 6, Landed);
            Flares.Shared.Shock(chest.X, chest.Y, 2, 11, 12 * Glim.SampleMs, Landed, 0.35 * Fx.Glow, 0.26);
            Flares.Shared.Flash(chest.X, chest.Y, Ribbon, 0.3 * Fx.Glow, 2, 0.45f, 0.82);
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
            double q = Math.Min(1, f.Ms / Glim.SampleMs / Frames);
            float size = (float)(Size * (1 - 0.45 * q));
            var colour = alpha < 1 ? new Color(0.81f, 0.92f, 1, alpha) : Colors.White;
            on.DrawTextureRect(ItemIcons.Icon(f.Item), new Rect2(f.At - Vector2.One * size / 2, Vector2.One * size), false, colour);
        }
    }
}
