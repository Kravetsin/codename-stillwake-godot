using System;
using System.Collections.Generic;
using Godot;
using Stillwake.Game.Render;
using Look = Stillwake.Game.WristLook;

namespace Stillwake.Game.Shared;

/// <summary>
/// A gauge in its tab under the wrist computer's cuff (0074, 0081): the screen, the game's picture of what it measures,
/// and a row of cells lit as far as <see cref="Motion"/> shows. Low, the cells take the low colour and pulse, the last
/// one blinking faster; after a hit the lost cells flash white and drain; refilling, light runs along the lit cells and
/// motes rise off them; a canister breathed surges (<see cref="Surge"/>), air breathed again sends a wave across (<see cref="Wave"/>).
/// The crew's suit gauge; it reads the sim on its own timer and hands the share here.
/// </summary>
public partial class WristGauge : Control
{
    private static readonly Color Off = AsciiArt.Colour(Look.CellOff), OffTop = AsciiArt.Colour(Look.CellOffTop), Mote = new("bae6fd");

    private readonly NinePatchRect _tab = WristArt.Frame(WristPieces.Gauge, MouseFilterEnum.Ignore);
    private readonly Texture2D _icon;
    private readonly Rect2 _iconAt;
    private readonly int _meterLeft, _count;
    private readonly Color _colour, _lowColour;
    private IReadOnlyList<(int X, int Width)> _cells = [];
    private double _clock, _surge, _wave = -1;

    /// <param name="icon">What it measures: the canister, the bolt.</param>
    /// <param name="iconAt">Where the picture is drawn in the tab.</param>
    /// <param name="meterLeft">Where the cells start in the tab.</param>
    /// <param name="count">How many cells.</param>
    /// <param name="colour">The lit cells' colour, and <paramref name="low"/> while <see cref="Low"/>.</param>
    public WristGauge(Texture2D icon, Rect2 iconAt, int meterLeft, int count, Color colour, Color low)
    {
        (_icon, _iconAt, _meterLeft, _count, _colour, _lowColour) = (icon, iconAt, meterLeft, count, colour, low);
        MouseFilter = MouseFilterEnum.Ignore;
        AddChild(_tab);
    }

    /// <summary>What the cells show, moving on each frame.</summary>
    public GaugeMotion Motion { get; } = new();

    /// <summary>Whether it runs low: the low colour, pulsing.</summary>
    public bool Low { get; set; }

    /// <summary>How bright its picture and cells are: dim with nothing to measure, a belt with no battery.</summary>
    public float Bright { get; set; } = 1;

    /// <summary>Where its screen lies and how wide it is; its tab and cells laid round and across it.</summary>
    public void Place(Vector2 at, int width)
    {
        Position = at;
        Size = new Vector2(width, Look.TabHeight);
        WristArt.Fit(_tab, WristPieces.Gauge, Size);
        _cells = MeterCells.Lay(width - _meterLeft - Look.MeterRight, _count, Look.CellGap);
    }

    /// <summary>A canister breathed: the tab washes sky, a white sweep runs across every cell and motes rise thick and high, a little stronger than a slow refill.</summary>
    public void Surge() => _surge = Look.SurgeS;

    /// <summary>Room air breathed again: one soft wave across the cells, lit or not.</summary>
    public void Wave() => _wave = 0;

    public override void _Process(double delta)
    {
        _clock += delta;
        _surge = Math.Max(0, _surge - delta);
        if (_wave >= 0) _wave = _wave + delta < Look.WaveS ? _wave + delta : -1;
        Motion.Step(delta);
        QueueRedraw();
    }

    /// <summary>animate-pulse: down to half and back every two seconds, as the gauge pulsed before.</summary>
    public static float Pulse(double clock) => 0.75f + 0.25f * Mathf.Cos((float)(clock * Math.PI));

    /// <summary>A colour a shade toward white: a lit cell's top edge, the lost part as it drains.</summary>
    public static Color Lit(Color colour) => colour.Lerp(Colors.White, 0.45f);

    public override void _Draw()
    {
        WristArt.DrawScreen(this, Size, WristArt.Screen, glare: false);
        var m = Motion;
        // A hit washes the whole tab light for its flash.
        if (m.Flash > 0) DrawRect(new Rect2(Vector2.Zero, Size), Colors.White with { A = 0.1f * (float)m.Flash });
        // A canister's surge washes the tab sky, fading as it goes.
        float surge = (float)(_surge / Look.SurgeS);
        if (surge > 0) DrawRect(new Rect2(Vector2.Zero, Size), _colour with { A = (float)Look.SurgeWash * surge * surge });
        DrawTextureRect(_icon, _iconAt, false, Colors.White with { A = Bright });
        var colour = Low ? _lowColour : _colour;
        float pulse = Low ? Pulse(_clock) : 1, t = (float)_clock;
        int lastLit = (int)Math.Ceiling(m.Fill * _count - 1e-6) - 1;
        int sweep = m.Gain > 0 ? (int)(t * 14 % (_count + 6)) : -9;
        // The surge's sweep runs from the first cell along the lit ones, over and over as it fades.
        double surged = Look.SurgeS - _surge;
        int surgeAt = surge > 0 ? (int)(surged * Look.SurgeSweep % (lastLit + 4)) : -9;
        double wave = _wave >= 0 ? _wave / Look.WaveS * (_count + 2 * Look.WaveBand) - Look.WaveBand : double.NaN;
        for (int i = 0; i < _cells.Count; i++)
        {
            var (cx, cw) = _cells[i];
            var cell = new Rect2(_meterLeft + cx, Look.CellTop, cw, Look.CellHeight);
            double from = (double)i / _count;
            float lit = (float)Math.Clamp((m.Fill - from) * _count, 0, 1), lost = (float)Math.Clamp((m.Trail - from) * _count, 0, 1);
            DrawRect(cell, Off with { A = Bright });
            DrawRect(cell with { Size = new Vector2(cw, 1) }, OffTop with { A = Bright });
            if (lit > 0)
            {
                float a = (lit >= 1 ? 1 : 0.2f + 0.45f * lit) * pulse * Bright;
                if (Low && i == lastLit) a *= 0.3f + 0.7f * (0.5f + 0.5f * Mathf.Cos(t * Mathf.Pi * 3));
                if (lit >= 1)
                {
                    DrawRect(cell.Grow(2), colour with { A = 0.08f * pulse * Bright });
                    DrawRect(cell.Grow(1), colour with { A = 0.18f * pulse * Bright });
                }
                DrawRect(cell, colour with { A = a });
                DrawRect(cell with { Size = new Vector2(cw, 1) }, Lit(colour) with { A = 0.6f * a });
                if (i == sweep || i == sweep - 1) DrawRect(cell, Colors.White with { A = i == sweep ? 0.45f : 0.18f });
                if (m.Gain > 0 && i == lastLit && lit < 1) DrawRect(cell, Colors.White with { A = 0.5f * (float)m.Gain });
            }
            if (lit > 0 && (i == surgeAt || i == surgeAt - 1 || i == surgeAt - 2))
                DrawRect(cell, Colors.White.Lerp(_colour, (surgeAt - i) * 0.35f) with { A = (i == surgeAt ? 0.75f : 0.4f / (surgeAt - i)) * surge });
            if (!double.IsNaN(wave))
            {
                double d = (i - wave) / Look.WaveBand;
                float a = (float)(Look.WaveBright * Math.Exp(-d * d * 2));
                if (a > 0.01f)
                {
                    DrawRect(cell.Grow(1), _colour with { A = 0.35f * a });
                    DrawRect(cell, Lit(_colour) with { A = a });
                }
            }
            if (lost <= lit) continue;
            var chip = m.Flash > 0 ? Colors.White with { A = 0.5f + 0.5f * (float)m.Flash } : Lit(colour) with { A = 0.55f };
            DrawRect(cell, chip with { A = chip.A * Math.Min(1, lost - lit) });
        }
        if (_cells.Count == 0) return;
        int meter = _cells[^1].X + _cells[^1].Width;
        if (surge > 0) Motes(t, meter, 1, Look.SurgeMotes, Look.SurgeRise, Math.Min(1, surge * 2));
        else if (m.Gain > 0) Motes(t, meter, (float)m.Fill, Look.Motes, Look.MoteRise, 0.85f);
    }

    /// <summary>Light rising off oxygen as it fills: <paramref name="count"/> motes over the lit cells, <paramref name="rise"/> pixels up.</summary>
    private void Motes(float t, int meter, float fill, int count, int rise, float alpha)
    {
        float width = meter * Math.Clamp(fill, 0, 1);
        if (width < 4) return;
        for (int k = 0; k < count; k++)
        {
            float seed = k * 0.27f;
            float phase = (t * 1.2f + seed) % 1, cycle = Mathf.Floor(t * 1.2f + seed);
            float x = _meterLeft + Mathf.Round((k * 0.37f + cycle * 0.618f) % 1 * (width - 2));
            DrawRect(new Rect2(x, Mathf.Round(Look.CellTop - 1 - phase * rise), 1, 1), Mote with { A = alpha * (1 - phase) });
        }
    }
}
