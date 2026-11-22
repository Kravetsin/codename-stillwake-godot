using System;
using Godot;
using Stillwake.Game.Render;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// A jetpack's plume (0047, 0075), one a hero: while it thrusts, a short tapered core of light from the nozzle, white
/// there and blue toward its tip, flickering in length; hot streaks and blue motes blown the other way; a blue pool of
/// light moving with the hero; heat shimmer in the plume; a faint ring as it lights. Stopped, the core dies back. Drawn
/// only, each peer its own from what it sees.
/// </summary>
public partial class JetPlume : Node2D
{
    /// <summary>Streaks and motes a second while it thrusts, times <see cref="Fx.Count"/>: restrained, a jet and not a fountain.</summary>
    public const double StreaksPerSecond = 90, MotesPerSecond = 24;
    /// <summary>The core's length, in pixels, round which it flickers, and how far either way.</summary>
    private const double CoreLength = 8, CoreFlicker = 3;
    /// <summary>How hard it thrusts, 0 to 1, a hero's always whole: the core <see cref="ThrustLeast"/> as long at none (catcher-motion).</summary>
    public double Thrust { get; set; } = 1;

    public const double ThrustLeast = 0.3;

    private static readonly (int, int, int) Blue = (110, 170, 255), Ring = (150, 200, 255), Bloomed = (120, 180, 255);
    /// <summary>The colours along the core, nozzle to tip, and how opaque each is.</summary>
    private static readonly Color[] Along = [new(1, 1, 1, 0.95f), new(170 / 255f, 215 / 255f, 1, 0.8f), new(70 / 255f, 120 / 255f, 1, 0.35f), new(40 / 255f, 70 / 255f, 1, 0)];
    private static readonly float[] AlongAt = [0, 0.25f, 0.7f, 1];

    private readonly Shimmer _shimmer = new() { Rise = 4 };
    private double _streaks, _motes, _length, _clock;
    private bool _on;
    private Vector2 _nozzle, _way = Vector2.Down;
    private readonly (float X, float Y)[] _outline = new (float X, float Y)[PlumeWay.Corners];
    private readonly Vector2[] _shape = new Vector2[PlumeWay.Corners];
    private readonly Color[] _colours = new Color[PlumeWay.Corners];

    public JetPlume()
    {
        // Behind the heroes: it leaves a pack on the back, and a core of light over the legs would read as in front of them.
        ZIndex = Layers.Tethers;
        ZAsRelative = false;
        TopLevel = true;
        Material = new CanvasItemMaterial
        {
            BlendMode = CanvasItemMaterial.BlendModeEnum.Add,
            LightMode = CanvasItemMaterial.LightModeEnum.Unshaded,
        };
        AddChild(_shimmer);
    }

    /// <summary>
    /// The plume <paramref name="elapsedMs"/> on, from <paramref name="nozzle"/>, pixels of the world, while the hero
    /// thrusts toward <paramref name="toward"/>: blown the other way, down with no way; dying back with none.
    /// </summary>
    public void Step(Vector2? nozzle, (double X, double Y)? toward, double elapsedMs)
    {
        _clock += elapsedMs;
        double frames = elapsedMs / Glim.SampleMs;
        bool on = nozzle != null && toward != null;
        if (on)
        {
            var (tx, ty) = toward!.Value;
            var (wx, wy) = PlumeWay.Of(tx, ty);
            _way = new Vector2(wx, wy);
            _nozzle = nozzle!.Value;
            if (!_on) Stages.Of(Layers.Tethers).Flares.Shock(_nozzle.X + _way.X * 3, _nozzle.Y + _way.Y * 3, 1.5, 10.5, 12 * Glim.SampleMs, Ring, 0.35 * Fx.Glow, 0.3);
            Emit(elapsedMs);
            double want = (CoreLength + CoreFlicker * (LuminousMath.Noise(_clock / 1000 * 21, 3.1) * 2 - 1)) * Fx.Length * (ThrustLeast + (1 - ThrustLeast) * Thrust);
            _length += (want - _length) * (1 - Math.Pow(0.5, frames));
            var light = _nozzle + _way * 4;
            Stages.Of(Layers.Tethers).Flares.Hold(this, light.X, light.Y, Blue, 0.3 * (1 + 0.2 * Math.Sin(_clock / 1000 * 54)) * Fx.Glow, 3, 0.5f, 0.6);
            Stages.Of(Layers.Tethers).Flares.Bloom(_nozzle.X + _way.X * 2, _nozzle.Y + _way.Y * 2, 12, Bloomed, 0.5);
        }
        else _length *= Math.Pow(0.6, frames);
        _on = on;
        var middle = _nozzle + _way * 8;
        _shimmer.Box = new Rect2(middle - new Vector2(9, 9), new Vector2(18, 18));
        _shimmer.Amplitude = on ? 0.45f : 0;
        Visible = _length > 0.5;
        if (Visible) QueueRedraw();
    }

    private void Emit(double elapsedMs)
    {
        var glims = Stages.Of(Layers.Tethers).Glims;
        double heading = Math.Atan2(_way.Y, _way.X);
        _streaks += StreaksPerSecond * Fx.Count * elapsedMs / 1000;
        for (; _streaks >= 1; _streaks--)
        {
            double a = heading + glims.Between(-0.22, 0.22), s = glims.Between(70, 140);
            glims.Add(Luminous.Thrust, _nozzle.X + glims.Between(-0.6, 0.6), _nozzle.Y + glims.Between(-0.6, 0.6), Math.Cos(a) * s, Math.Sin(a) * s);
        }
        _motes += MotesPerSecond * Fx.Count * elapsedMs / 1000;
        for (; _motes >= 1; _motes--)
        {
            double a = heading + glims.Between(-0.5, 0.5), s = glims.Between(20, 45);
            glims.Add(Luminous.ThrustMote, _nozzle.X, _nozzle.Y, Math.Cos(a) * s, Math.Sin(a) * s);
        }
    }

    /// <summary>The core: two curved flanks from the nozzle meeting at its tip, its colour running along it.</summary>
    public override void _Draw()
    {
        var (x, y) = PlumeWay.Core(_outline, (_nozzle.X, _nozzle.Y), (_way.X, _way.Y), (float)_length);
        for (int i = 0; i < _outline.Length; i++) _shape[i] = new Vector2(_outline[i].X, _outline[i].Y);
        var at = PlumeWay.Stations;
        for (int i = 0; i < at.Length; i++)
        {
            var colour = ColourAt(at[i]);
            (_colours[i], _colours[^(i + 1)]) = (colour, colour);
        }
        _colours[at.Length] = Along[^1];
        DrawSetTransform(new Vector2(x, y));
        DrawPolygon(_shape, _colours);
    }

    private static Color ColourAt(float t)
    {
        for (int i = 1; i < AlongAt.Length; i++)
            if (t <= AlongAt[i]) return Along[i - 1].Lerp(Along[i], (t - AlongAt[i - 1]) / (AlongAt[i] - AlongAt[i - 1]));
        return Along[^1];
    }
}
