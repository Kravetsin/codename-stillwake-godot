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
    /// <summary>The core's width at its widest, in pixels.</summary>
    private const float CoreWidth = 1.5f;
    private static readonly (int, int, int) Blue = (110, 170, 255), Ring = (150, 200, 255), Bloomed = (120, 180, 255);
    /// <summary>The colours along the core, nozzle to tip, and how opaque each is.</summary>
    private static readonly Color[] Along = [new(1, 1, 1, 0.95f), new(170 / 255f, 215 / 255f, 1, 0.8f), new(70 / 255f, 120 / 255f, 1, 0.35f), new(40 / 255f, 70 / 255f, 1, 0)];
    private static readonly float[] AlongAt = [0, 0.25f, 0.7f, 1];

    private readonly Shimmer _shimmer = new() { Rise = 4 };
    private double _streaks, _motes, _length, _clock;
    private bool _on;
    private Vector2 _nozzle, _way = Vector2.Down;
    private readonly Vector2[] _shape = new Vector2[9];
    private readonly Color[] _colours = new Color[9];

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
            _way = tx == 0 && ty == 0 ? Vector2.Down : new Vector2((float)-tx, (float)-ty).Normalized();
            _nozzle = nozzle!.Value;
            if (!_on) Flares.Shared.Shock(_nozzle.X + _way.X * 3, _nozzle.Y + _way.Y * 3, 1.5, 10.5, 12 * Glim.SampleMs, Ring, 0.35 * Fx.Glow, 0.3);
            Emit(elapsedMs);
            double want = (CoreLength + CoreFlicker * (LuminousMath.Noise(_clock / 1000 * 21, 3.1) * 2 - 1)) * Fx.Length;
            _length += (want - _length) * (1 - Math.Pow(0.5, frames));
            var light = _nozzle + _way * 4;
            Flares.Shared.Hold(this, light.X, light.Y, Blue, 0.3 * (1 + 0.2 * Math.Sin(_clock / 1000 * 54)) * Fx.Glow, 3, 0.5f, 0.6);
            Flares.Shared.Bloom(_nozzle.X + _way.X * 2, _nozzle.Y + _way.Y * 2, 12, Bloomed, 0.5);
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
        var glims = Glims.Shared;
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
        float length = (float)_length;
        var across = new Vector2(-_way.Y, _way.X);
        // Stations along it, nozzle to tip, and its half width at each: a quadratic swell from the nozzle's to nothing.
        ReadOnlySpan<float> at = [0, 0.2f, 0.45f, 0.7f];
        for (int i = 0; i < at.Length; i++)
        {
            float t = at[i], u = 1 - t, half = CoreWidth * (0.6f * u * u + 2 * u * t * 1.15f);
            var point = _nozzle + _way * (length * t);
            var colour = ColourAt(t);
            (_shape[i], _shape[8 - i]) = (point + across * half, point - across * half);
            (_colours[i], _colours[8 - i]) = (colour, colour);
        }
        _shape[4] = _nozzle + _way * length;
        _colours[4] = Along[^1];
        DrawPolygon(_shape, _colours);
    }

    private static Color ColourAt(float t)
    {
        for (int i = 1; i < AlongAt.Length; i++)
            if (t <= AlongAt[i]) return Along[i - 1].Lerp(Along[i], (t - AlongAt[i - 1]) / (AlongAt[i] - AlongAt[i - 1]));
        return Along[^1];
    }
}
