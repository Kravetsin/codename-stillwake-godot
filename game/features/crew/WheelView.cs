using System;
using System.Collections.Generic;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// A key's wheel (<see cref="Wheel{T}"/>; gestures-for-words, light-drone) in the thread icons' look, the pick «Одна нить»: round where
/// the key was pressed, a dark disc a side with its icon, one ember thread, the one the pointer has moved toward lit with a spark running
/// it, a shut one dashed and crossed; a ring for the middle, which picks nothing. Let go, the side picked flares where it was as the
/// rest goes (<see cref="WheelFade"/>). No words: the icons say it.
/// </summary>
public partial class WheelView : CanvasLayer
{
    private static readonly Color Disc = new(5 / 255f, 8 / 255f, 14 / 255f, 0.62f), Lit = new(5 / 255f, 8 / 255f, 14 / 255f, 0.9f);
    /// <summary>A disc's radius, a lit or picked one's growth, and its icon's half height: a drone side's, a gesture's, the middle's; at the normal slot size.</summary>
    private const float DiscRadius = 21, Grow = 1.15f, ModeHalf = 12.5f, GestureHalf = 13, MiddleHalf = 9;

    /// <summary>The wheel but the side picked, fading as it goes; and the side picked, flaring.</summary>
    private readonly Part _rest = new(), _picked = new();
    private readonly WheelFade _fade = new();
    private readonly List<bool> _shut = [];
    private readonly List<IconLines> _icons = [];
    private IconLines? _middle;
    private Vector2 _from;
    private int? _toward;
    private bool _open, _drone;
    private float _scale = 1;
    private double _time;

    public WheelView()
    {
        Layer = ScreenLayers.Tips;
        AddChild(_rest);
        AddChild(_picked);
    }

    /// <summary>
    /// Shows <paramref name="wheel"/>, at <paramref name="scale"/> times the normal slot size, each side's icon by <paramref name="icon"/>;
    /// <paramref name="drone"/> for the drone's, else the gestures; <paramref name="middle"/> drawn in the middle, if any. Its let-go plays on
    /// after it shuts.
    /// </summary>
    public void Show<T>(Wheel<T> wheel, float scale, Func<T, IconLines> icon, bool drone, IconLines? middle = null) where T : struct
    {
        _open = wheel.Open;
        if (!_open) return;
        (_from, _scale, _drone, _middle) = (new Vector2((float)wheel.From.X, (float)wheel.From.Y), scale, drone, middle);
        _shut.Clear();
        _icons.Clear();
        foreach (var side in wheel.Sides)
        {
            _shut.Add(!wheel.Opens(side));
            _icons.Add(icon(side));
        }
        _toward = wheel.TowardAt is int at && !_shut[at] ? at : null;
    }

    public override void _Process(double delta)
    {
        bool was = _fade.Drawn;
        _fade.Step(delta, _open, _toward);
        if (!_fade.Drawn)
        {
            if (was) _rest.Visible = _picked.Visible = false;
            return;
        }
        _time += delta;
        _rest.Visible = _picked.Visible = true;
        _rest.Modulate = Colors.White with { A = _fade.Shown };
        _rest.Clear();
        _picked.Clear();
        if (_icons.Count == 0) return;
        float t = (float)_time, s = _scale;
        if (_fade.Shown > 0)
        {
            ThreadIcon.Draw(_rest.Marks, IconInk.Circle, new IconLook(0.32f, Rim: true, Time: t, Seed: 1), _from.X, _from.Y, (float)Wheel.Middle * s, 0.8f * s);
            if (_middle != null) ThreadIcon.Draw(_rest.Marks, _middle, new IconLook(0.5f, Time: t, Seed: 3), _from.X, _from.Y, MiddleHalf * s, 1 * s);
        }
        for (int i = 0; i < _shut.Count; i++)
        {
            float flare = _fade.Picked == i ? _fade.Flare : 0;
            bool lit = _open && i == _toward, shut = _shut[i], bright = lit || flare > 0;
            var layer = flare > 0 ? _picked : _rest;
            if (layer == _rest && _fade.Shown <= 0) continue;
            // From the top, clockwise, shared out evenly as Wheel.SideOf reads them.
            float angle = i * Mathf.Tau / _shut.Count, grow = bright ? Grow : 1, r = DiscRadius * s * grow;
            var at = _from + new Vector2(Mathf.Sin(angle), -Mathf.Cos(angle)) * (float)Wheel.Radius * s;
            layer.Discs.Add((at, r, bright));
            float seed = i * 1.7f + (_drone ? 0 : 5), hover = lit ? 1 : 0;
            ThreadIcon.Draw(layer.Marks, IconInk.Circle, new IconLook(bright ? 0.9f : 0.38f, hover, Shut: shut, Flare: flare, Rim: true, Time: t, Seed: seed),
                at.X, at.Y, r, 0.9f * s);
            ThreadIcon.Draw(layer.Marks, _icons[i], new IconLook(bright ? 1 : 0.6f, hover, Shut: shut, Flare: flare, Time: t, Seed: seed),
                at.X, at.Y, (_drone ? ModeHalf : GestureHalf) * grow * s, 1.25f * grow * s);
            if (shut) ThreadIcon.Draw(layer.Marks, IconInk.Cross, new IconLook(0.5f, Rim: true, Time: t, Seed: seed), at.X, at.Y, r, 0.9f * s);
        }
        _rest.Redraw();
        _picked.Redraw();
    }

    /// <summary>A part of the wheel: its discs' dark glass, under the threads' light added over it.</summary>
    private sealed partial class Part : Control
    {
        private readonly Node2D _light = new() { Material = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Add } };
        private readonly EmberArt _art = new();

        public readonly List<(Vector2 At, float Radius, bool Lit)> Discs = [];
        public readonly EmberMarks Marks = new();

        public Part()
        {
            MouseFilter = MouseFilterEnum.Ignore;
            Visible = false;
            AddChild(_light);
            _light.Draw += () => _art.Light(Marks, _light);
        }

        public void Clear()
        {
            Discs.Clear();
            Marks.Clear();
        }

        public void Redraw()
        {
            QueueRedraw();
            _light.QueueRedraw();
        }

        public override void _Draw()
        {
            foreach (var (at, radius, lit) in Discs) DrawCircle(at, radius, lit ? Lit : Disc);
        }
    }
}
