using System;
using System.Collections.Generic;
using Godot;
using Stillwake.Game.Render;
using Look = Stillwake.Game.WristLook;

namespace Stillwake.Game.Shared;

/// <summary>
/// A gauge in its tab under the wrist computer's cuff (0074, 0081): the screen, the game's picture of what it measures,
/// and a row of hand-cut lamps (the ink-signs ADR) lit as far as <see cref="Motion"/> shows, the lamp at the edge lit pixel
/// by pixel, so the gauge flows rather than ticks, a bright line leading it while it moves. Low, the lamps turn red and
/// pulse, the last one blinking faster; after a hit the lost part flashes pale and drains; refilling, light runs along the
/// lit lamps and motes rise off them; a canister breathed surges (<see cref="Surge"/>), air breathed again sends a wave
/// across (<see cref="Wave"/>). The crew's suit gauge; it reads the sim on its own timer and hands the share here.
/// </summary>
public partial class WristGauge : Control
{
    private static readonly Color Mote = new("bae6fd");
    /// <summary>The lamp pictures' shapes, and the widest cell a normal lamp is drawn into: wider takes the large slots' lamps.</summary>
    private const int Shapes = 5, NormalLampMost = 21;

    private readonly NinePatchRect _tab = WristArt.Frame(WristPieces.Gauge, MouseFilterEnum.Ignore);
    private readonly Texture2D _icon;
    private readonly Rect2 _iconAt;
    private readonly int _meterLeft, _count;
    private readonly Color _colour;
    private IReadOnlyList<(int X, int Width)> _cells = [];
    private string _lamps = "gauge_lamp";
    private double _clock, _surge, _wave = -1, _shownFill, _moving;

    /// <param name="icon">What it measures: the canister, the bolt.</param>
    /// <param name="iconAt">Where the picture is drawn in the tab.</param>
    /// <param name="meterLeft">Where the cells start in the tab.</param>
    /// <param name="count">How many cells.</param>
    /// <param name="colour">The colour a surge washes the tab with.</param>
    public WristGauge(Texture2D icon, Rect2 iconAt, int meterLeft, int count, Color colour)
    {
        (_icon, _iconAt, _meterLeft, _count, _colour) = (icon, iconAt, meterLeft, count, colour);
        MouseFilter = MouseFilterEnum.Ignore;
        AddChild(_tab);
    }

    /// <summary>What the cells show, moving on each frame.</summary>
    public GaugeMotion Motion { get; } = new();

    /// <summary>Whether it runs low: the red lamps, pulsing.</summary>
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
        _lamps = _cells.Count > 0 && _cells[0].Width > NormalLampMost ? "gauge_lampWide" : "gauge_lamp";
    }

    /// <summary>A canister breathed: the tab washes sky, pale light sweeps across every lamp and motes rise thick and high, a little stronger than a slow refill.</summary>
    public void Surge() => _surge = Look.SurgeS;

    /// <summary>Room air breathed again: one wave of pale light across the lamps, lit or not.</summary>
    public void Wave() => _wave = 0;

    public override void _Process(double delta)
    {
        _clock += delta;
        _surge = Math.Max(0, _surge - delta);
        if (_wave >= 0) _wave = _wave + delta < Look.WaveS ? _wave + delta : -1;
        Motion.Step(delta);
        // The leading edge shows while the fill moves, either way, and fades once it rests.
        _moving = Math.Abs(Motion.Fill - _shownFill) > 1e-6 ? 1 : Math.Max(0, _moving - delta / Look.EdgeFadeS);
        _shownFill = Motion.Fill;
        QueueRedraw();
    }

    /// <summary>animate-pulse: down to half and back every two seconds, as the gauge pulsed before.</summary>
    public static float Pulse(double clock) => 0.75f + 0.25f * Mathf.Cos((float)(clock * Math.PI));

    /// <summary>Lamp <paramref name="shape"/> in <paramref name="tone"/>: dark, sky, red or pale.</summary>
    private Texture2D Lamp(int shape, string tone) => PixelArt.Picture($"{_lamps}_{shape}_{tone}");

    /// <summary>
    /// <paramref name="lamp"/> laid over <paramref name="cell"/>, its line's margin round it, only the screen's columns from
    /// <paramref name="from"/> to <paramref name="to"/> of it: what a lamp lit part way shows.
    /// </summary>
    private void DrawLamp(Texture2D lamp, Rect2 cell, float from, float to, float alpha)
    {
        if (alpha <= 0.004f) return;
        var size = lamp.GetSize();
        // The picture's margin is a pixel of its own each side: scaled as the lamp is into the cell.
        float kx = cell.Size.X / (size.X - 2), ky = cell.Size.Y / (size.Y - 2), left = cell.Position.X - kx;
        from = Math.Max(from, left);
        to = Math.Min(to, left + size.X * kx);
        if (to <= from) return;
        DrawTextureRectRegion(lamp, new Rect2(from, cell.Position.Y - ky, to - from, size.Y * ky),
            new Rect2((from - left) / kx, 0, (to - from) / kx, size.Y), Colors.White with { A = alpha });
    }

    /// <summary>A lamp lit to <paramref name="to"/>: whole pixels at <paramref name="alpha"/>, the next as bright as the share of it lit.</summary>
    private void DrawLit(Texture2D lamp, Rect2 cell, float from, float to, float alpha)
    {
        float whole = Mathf.Floor(to), part = to - whole;
        DrawLamp(lamp, cell, from, whole, alpha);
        if (part > 0.02f) DrawLamp(lamp, cell, whole, whole + 1, alpha * part);
    }

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
        float pulse = Low ? Pulse(_clock) : 1, t = (float)_clock;
        int lastLit = (int)Math.Ceiling(m.Fill * _count - 1e-6) - 1;
        int sweep = m.Gain > 0 ? (int)(t * 14 % (_count + 6)) : -9;
        // The surge's sweep runs from the first lamp along the lit ones, over and over as it fades.
        double surged = Look.SurgeS - _surge;
        int surgeAt = surge > 0 ? (int)(surged * Look.SurgeSweep % (lastLit + 4)) : -9;
        double wave = _wave >= 0 ? _wave / Look.WaveS * (_count + 2 * Look.WaveBand) - Look.WaveBand : double.NaN;
        string lit = Low ? "red" : "sky";
        for (int i = 0; i < _cells.Count; i++)
        {
            var (cx, cw) = _cells[i];
            var cell = new Rect2(_meterLeft + cx, Look.CellTop, cw, Look.CellHeight);
            int shape = i * 3 % Shapes;
            float x0 = cell.Position.X, all = x0 + cw + 2;
            double from = (double)i / _count;
            float share = (float)Math.Clamp((m.Fill - from) * _count, 0, 1), lost = (float)Math.Clamp((m.Trail - from) * _count, 0, 1);
            float litTo = x0 + cw * share, end = share >= 1 ? all : litTo;
            Texture2D pale = Lamp(shape, "pale");
            DrawLamp(Lamp(shape, "dark"), cell, x0 - 2, all, Bright);
            if (share > 0)
            {
                float a = pulse * Bright;
                if (Low && i == lastLit) a *= 0.3f + 0.7f * (0.5f + 0.5f * Mathf.Cos(t * Mathf.Pi * 3));
                DrawLit(Lamp(shape, lit), cell, x0 - 2, end, a);
                if (i == sweep || i == sweep - 1) DrawLamp(pale, cell, x0 - 2, end, i == sweep ? 0.55f : 0.22f);
                if (i == surgeAt || i == surgeAt - 1 || i == surgeAt - 2)
                    DrawLamp(pale, cell, x0 - 2, end, (i == surgeAt ? 0.8f : 0.4f / (surgeAt - i)) * surge);
            }
            // The leading edge while the fill moves: a bright line of a pixel at the fill.
            if (_moving > 0 && i == Math.Max(0, lastLit) && share is > 0 and < 1)
                DrawRect(new Rect2(Mathf.Round(litTo) - 1, Look.CellTop + 1, 1, Look.CellHeight - 2), Colors.White with { A = 0.85f * (float)_moving * Bright });
            if (!double.IsNaN(wave))
            {
                // In two hard steps, not a soft bell: the lamps it is on, and a dimmer one either side.
                double d = Math.Abs(i - wave);
                float a = d < 0.6 ? (float)Look.WaveBright * 1.4f : d < 1.6 ? (float)Look.WaveBright * 0.6f : 0;
                DrawLamp(pale, cell, x0 - 2, all, a);
            }
            if (lost <= share) continue;
            // The lost part after a hit, from the fill to the trail's edge: pale, brighter while it flashes.
            DrawLamp(pale, cell, litTo, lost >= 1 ? all : x0 + cw * lost, m.Flash > 0 ? 0.5f + 0.5f * (float)m.Flash : 0.5f);
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
