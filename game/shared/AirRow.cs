using System;
using System.Linq;
using Godot;
using Stillwake.Game.Render;
using Look = Stillwake.Game.WristLook;

namespace Stillwake.Game.Shared;

/// <summary>
/// The air row of the vitals (hud-vitals): the oxygen sign and one piece of light as long as the suit holds, in units
/// (<see cref="Units"/>), lit as far as <see cref="Motion"/> shows, a bright line leading it while it moves. Low, it turns red
/// and pulses, its last units blinking faster; what is lost stays lit white a moment, then drains, chips falling off; refilling,
/// light runs along it and pale motes rise; a canister breathed surges (<see cref="Surge"/>), air breathed again sends a wave
/// across (<see cref="Wave"/>). The crew's suit gauge reads the sim on its own timer and hands the share here.
/// </summary>
public partial class AirRow : Control
{
    private static readonly Color Mote = new("bae6fd"), Sky = new("38bdf8");
    /// <summary>The equal stretches of the piece the chips and the canister's wash count in, as the lamps once were.</summary>
    private const int Cells = Look.SuitCells;
    /// <summary>The last units that blink while low and dim with the fog; how long a helmet put on or off takes to change the piece.</summary>
    private const double LastUnits = 5, ResizeS = 0.45;
    private const float Top = Vitals.AirY - Vitals.PieceHeight / 2, Height = Vitals.PieceHeight;
    private static readonly Rect2 SignAt = new(Vitals.SignX - 11 * Vitals.SignScale, Vitals.AirY - 11 * Vitals.SignScale, 22 * Vitals.SignScale, 22 * Vitals.SignScale);

    private readonly Texture2D _icon;
    private readonly Texture2D? _shortIcon;
    private double _clock, _surge, _wave = -1, _shownFill, _moving;
    private readonly LampFlakes _flakes = new(new Random());
    private readonly GaugeWash _wash = new();
    private readonly Over _over, _light;
    private Vector2 _at;
    private Color _hurtTint = Colors.White;
    private double _hurt = -1;
    private int _units;
    private float _length, _lengthFrom, _lengthTo;
    private double _resize = 1;

    /// <param name="icon">The oxygen sign.</param>
    /// <param name="shortIcon">Its picture while <see cref="Short"/>: the red oxygen sign.</param>
    public AirRow(Texture2D icon, Texture2D shortIcon)
    {
        (_icon, _shortIcon) = (icon, shortIcon);
        MouseFilter = MouseFilterEnum.Ignore;
        AddChild(Spot = new Control { MouseFilter = MouseFilterEnum.Ignore, Position = new Vector2(Vitals.RowLeft, Top) });
        // Over the row: the chips falling off it, and the light added over all.
        AddChild(_over = new Over(this, add: false));
        AddChild(_light = new Over(this, add: true));
    }

    /// <summary>What the piece shows lit, moving on each frame.</summary>
    public GaugeMotion Motion { get; } = new();

    /// <summary>The piece's own rectangle, for its tip.</summary>
    public Control Spot { get; }

    /// <summary>Whether it runs low: red, pulsing.</summary>
    public bool Low { get; set; }

    /// <summary>Whether what it measures runs short: its sign turns red, pulsing.</summary>
    public bool Short { get; set; }

    /// <summary>How far its last units dim, 0 to 1: a low suit's breath (<see cref="Features.Crew.VisorFog"/>).</summary>
    public float LastDim { get; set; }

    /// <summary>How bright its sign and piece are.</summary>
    public float Bright { get; set; } = 1;

    /// <summary>Whether its sign shows grey: a canister cooling (canister-mark).</summary>
    public bool Greyed { get; set; }

    /// <summary>Where its sign is drawn: what a mark round it centres on.</summary>
    public Rect2 IconAt => SignAt;

    /// <summary>Drawn over its sign by the feature it measures for: the canister's ring.</summary>
    public Action<CanvasItem>? DrawMark { get; set; }

    /// <summary>Added over all with the row's own light: the ring's glow.</summary>
    public Action<CanvasItem>? LightMark { get; set; }

    /// <summary>
    /// How many units the suit holds, lungs and helmet together: the piece's length, fitted to the row. A change after the first
    /// grows or shrinks the piece over <see cref="ResizeS"/>.
    /// </summary>
    public int Units
    {
        get => _units;
        set
        {
            if (value == _units) return;
            float length = Vitals.Fit([value], Vitals.AirPerUnit)[0].Width;
            (_lengthFrom, _lengthTo, _resize) = _units == 0 ? (length, length, 1) : (_length, length, 0);
            _units = value;
            if (_resize >= 1) _length = length;
        }
    }

    /// <summary>The row's place, the block's origin on the screen, and its size.</summary>
    public void Place(Vector2 origin, float scale)
    {
        Position = _at = origin;
        Scale = new Vector2(scale, scale);
    }

    /// <summary>A canister breathed: a white sweep runs over the piece, the air it gave glows pale, motes rise thick and the row shakes.</summary>
    public void Surge()
    {
        _surge = Look.SurgeS;
        _wash.Breathe((int)Math.Floor(Motion.Fill * Cells + 1e-6));
    }

    /// <summary>Room air breathed again: one wave of pale light across the piece, lit or not.</summary>
    public void Wave() => _wave = 0;

    /// <summary>A hurt (2026-11-08-hurts-are-shown): the part just lost blinks in <paramref name="tint"/>, the kind's colour, lit behind the sign.</summary>
    public void Hurt(Color tint) => (_hurtTint, _hurt) = (tint, 0);

    public override void _Process(double delta)
    {
        _clock += delta;
        _surge = Math.Max(0, _surge - delta);
        if (_wave >= 0) _wave = _wave + delta < Look.WaveS ? _wave + delta : -1;
        if (_hurt >= 0) _hurt = _hurt + delta < HurtLook.BlinkS ? _hurt + delta : -1;
        if (_resize < 1)
        {
            _resize = Math.Min(1, _resize + delta / ResizeS);
            _length = Mathf.Lerp(_lengthFrom, _lengthTo, Mathf.SmoothStep(0, 1, (float)_resize));
        }
        Spot.Size = new Vector2(Math.Max(8, _length), Height);
        Motion.Step(delta);
        // The leading edge shows while the fill moves, either way, and fades once it rests.
        _moving = Math.Abs(Motion.Fill - _shownFill) > 1e-6 ? 1 : Math.Max(0, _moving - delta / Look.EdgeFadeS);
        _shownFill = Motion.Fill;
        _wash.Step(delta);
        var (sx, sy) = _wash.Shake;
        Position = _at + new Vector2(sx, sy);
        _flakes.Step(delta, Motion.Fill, LastLit, [.. Enumerable.Range(0, Cells).Select(i => (Vitals.RowLeft + i * _length / Cells, _length / Cells))], Top, Height);
        QueueRedraw();
        _over.QueueRedraw();
        _light.QueueRedraw();
    }

    private int LastLit => (int)Math.Ceiling(Motion.Fill * Cells - 1e-6) - 1;

    /// <summary>Where a share of the piece lies along it, from its start.</summary>
    private float At(double share) => _length * (float)Math.Clamp(share, 0, 1);

    private void Span(CanvasItem on, string tone, float from, float to, Color modulate) =>
        LightStrip.Span(on, LightStrip.Of(tone, 0), Vitals.RowLeft, Top, _length, Height, from, to, modulate);

    private void Span(CanvasItem on, string tone, float from, float to, float alpha) => Span(on, tone, from, to, Colors.White with { A = alpha });

    public override void _Draw()
    {
        if (Greyed) DrawTextureRect(Render.Greyed.Of(_icon), SignAt, false, Colors.White with { A = 0.8f * Bright });
        else if (Short && _shortIcon != null) DrawTextureRect(_shortIcon, SignAt, false, Colors.White with { A = Bright * Vitals.Pulse(_clock * 2) });
        else DrawTextureRect(_icon, SignAt, false, Colors.White with { A = Bright });
        DrawMark?.Invoke(this);
        if (_units <= 0) return;
        var m = Motion;
        float t = (float)_clock, pulse = Low ? Vitals.Pulse(_clock) : 1, lit = At(m.Fill);
        Span(this, Low ? "darkRed" : "darkSky", 0, _length, 0.9f * Bright);
        string tone = Low ? "red" : "sky";
        // The last units blink while low and dim with the fogged breath.
        float last = At(m.Fill - LastUnits / _units), lastAlpha = pulse * Bright * (1 - LastDim);
        if (Low) lastAlpha *= 0.3f + 0.7f * (0.5f + 0.5f * Mathf.Cos(t * Mathf.Pi * 3));
        Span(this, tone, 0, last, pulse * Bright);
        Span(this, tone, last, lit, lastAlpha);
        // A slow refill's light running along the lit part; a canister's wash has its own.
        if (m.Gain > 0 && _wash.Since is null)
        {
            float run = (t * 50 % (_units + 12) - 6) * _length / _units, unit = _length / _units;
            Span(this, "pale", run, Math.Min(lit, run + 3 * unit), 0.55f);
            Span(this, "pale", run - 4 * unit, Math.Min(lit, run), 0.22f);
        }
        for (int i = 0; i < Cells; i++)
        {
            float a = At((double)i / Cells), b = At((double)(i + 1) / Cells);
            float white = (float)_wash.White(i);
            if (white > 0) Span(this, "white", a, Math.Min(b, lit), white * Bright);
            // A stretch lost whole flashes white as its chip jumps off.
            foreach (var (cell, age) in _flakes.Pops)
                if (cell == i && age < LampFlakes.FlashS) Span(this, "white", a, b, (float)(1 - age / LampFlakes.FlashS) * Bright);
        }
        if (_wave >= 0)
        {
            // In two hard steps, not a soft bell: the stretch it is on, and a dimmer one either side.
            double wave = (_wave / Look.WaveS * (Cells + 2 * Look.WaveBand) - Look.WaveBand) / Cells, cell = 1.0 / Cells;
            Span(this, "pale", At(wave - 1.6 * cell), At(wave + 1.6 * cell), (float)Look.WaveBright * 0.6f);
            Span(this, "pale", At(wave - 0.6 * cell), At(wave + 0.6 * cell), (float)Look.WaveBright * 0.8f);
        }
        // The part just lost, from the fill to the trail's edge: white, whole while a hit flashes; a hurt blinks it in its colour.
        if (m.Trail > m.Fill)
        {
            var tint = _hurt >= 0 ? Colors.White.Lerp(_hurtTint, (float)HurtLook.Blink(_hurt)) : Colors.White;
            Span(this, "white", lit, At(m.Trail), tint with { A = (0.85f + 0.15f * (float)m.Flash) * Bright });
        }
        // The leading edge while the fill moves: a bright line at the fill.
        if (_moving > 0 && m.Fill is > 0 and < 1)
            DrawRect(new Rect2(Vitals.RowLeft + lit - 0.7f, Top + 0.5f, 1.4f, Height - 1), Colors.White with { A = 0.85f * (float)_moving * Bright });
        float surge = (float)(_surge / Look.SurgeS);
        if (surge > 0) Motes(t, 1, Look.SurgeMotes, Look.SurgeRise, Math.Min(1, surge * 2));
        else if (m.Gain > 0) Motes(t, (float)m.Fill, Look.Motes, Look.MoteRise, 0.85f);
    }

    /// <summary>Light rising off oxygen as it fills: <paramref name="count"/> motes over the lit part, <paramref name="rise"/> pixels up.</summary>
    private void Motes(float t, float fill, int count, int rise, float alpha)
    {
        float width = _length * Math.Clamp(fill, 0, 1);
        if (width < 4) return;
        for (int k = 0; k < count; k++)
        {
            float seed = k * 0.27f;
            float phase = (t * 1.2f + seed) % 1, cycle = Mathf.Floor(t * 1.2f + seed);
            float x = Vitals.RowLeft + (k * 0.37f + cycle * 0.618f) % 1 * (width - 2);
            DrawRect(new Rect2(x - 0.5f, Top - 1 - phase * rise, 1, 1.3f), Mote with { A = alpha * (1 - phase) });
        }
    }

    private static readonly Color ChipBody = new("7dd4f6"), ChipTop = new("c8f0ff"), ChipUnder = new("103048");

    /// <summary>Over the row: the chips of lost air tumbling off it.</summary>
    private void DrawOver(CanvasItem on)
    {
        foreach (var c in _flakes.Chips)
        {
            float a = (1 - Mathf.SmoothStep(0, 1, (float)((c.Age - 0.6) / 0.5))) * Bright;
            on.DrawSetTransform(new Vector2(c.X, c.Y), c.Turn);
            on.DrawRect(new Rect2(-c.W / 2, -c.H / 2, c.W, c.H), ChipBody with { A = a });
            on.DrawRect(new Rect2(-c.W / 2, -c.H / 2, c.W, 1), ChipTop with { A = a });
            on.DrawRect(new Rect2(-c.W / 2, c.H / 2 - 0.8f, c.W, 0.8f), ChipUnder with { A = a * 0.9f });
        }
        on.DrawSetTransform(Vector2.Zero);
    }

    /// <summary>
    /// Added over all: the glow under the lit part, a hit's flash over it, the air a canister gave glowing pale, the light of
    /// stretches lost and their sparks, and light behind the sign for a hurt or a canister.
    /// </summary>
    private void DrawLight(CanvasItem on)
    {
        if (_units <= 0) return;
        var m = Motion;
        float lit = At(m.Fill), pulse = Low ? Vitals.Pulse(_clock) : 1;
        LightStrip.Glow(on, LightStrip.Of(Low ? "red" : "sky", 0), Vitals.RowLeft, Top, _length, Height, 0, lit, pulse * Bright);
        if (m.Flash > 0) Span(on, "white", 0, lit, 0.3f * (float)m.Flash * Bright);
        for (int i = 0; i < Cells; i++)
        {
            float glow = (float)_wash.Glow(i);
            if (glow > 0) Span(on, "pale", At((double)i / Cells), Math.Min(lit, At((double)(i + 1) / Cells)), glow * Bright);
        }
        var sign = SignAt.GetCenter();
        if (_hurt >= 0) Glow(on, sign, 24, _hurtTint with { A = 0.45f * (1 - (float)(_hurt / HurtLook.BlinkS)) * Bright });
        float surge = (float)(_surge / Look.SurgeS);
        if (surge > 0) Glow(on, sign, 26, Sky with { A = 0.55f * surge * Bright });
        foreach (var (cell, age) in _flakes.Pops)
        {
            if (cell >= Cells) continue;
            var mid = new Vector2(Vitals.RowLeft + (cell + 0.5f) * _length / Cells, Vitals.AirY);
            Glow(on, mid, 9, Mote with { A = 0.55f * (float)(1 - age / LampFlakes.GlowS) * Bright });
        }
        foreach (var s in _flakes.SparksFlying)
        {
            float q = (float)(s.Age / s.Life);
            on.DrawLine(new Vector2(s.X, s.Y), new Vector2(s.X - s.Vx * 0.03f, s.Y - s.Vy * 0.03f), Colors.White.Lerp(Mote, q) with { A = (1 - q) * Bright }, 0.9f, true);
        }
        LightMark?.Invoke(on);
    }

    private static void Glow(CanvasItem on, Vector2 at, float radius, Color colour) =>
        on.DrawTextureRect(PixelArt.Glow, new Rect2(at - new Vector2(radius, radius), new Vector2(2 * radius, 2 * radius)), false, colour);

    /// <summary>A layer of the row: <see cref="DrawOver"/>, or <see cref="DrawLight"/> added.</summary>
    private sealed partial class Over : Control
    {
        private readonly AirRow _row;
        private readonly bool _add;

        public Over(AirRow row, bool add)
        {
            (_row, _add) = (row, add);
            MouseFilter = MouseFilterEnum.Ignore;
            if (add) Material = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Add };
        }

        public override void _Draw()
        {
            if (_add) _row.DrawLight(this);
            else _row.DrawOver(this);
        }
    }
}
