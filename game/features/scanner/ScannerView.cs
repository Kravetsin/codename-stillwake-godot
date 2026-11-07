using System;
using Godot;
using Stillwake.Game.Render;

namespace Stillwake.Game.Features.Scanner;

/// <summary>
/// The scanner's readout over the wreck it reads (2026-11-07-the-scanner), phosphor: a dark glass, then its digits added with the
/// edge keys' scanlines (holo.gdshader), grain and a rolling line, then dark grain over it; the value flares white as it falls.
/// On a layer that follows the camera: in world pixels, unlit by the world's lights.
/// </summary>
public partial class ScannerView : CanvasLayer
{
    private static readonly Color Green = new("7cf096"), Flare = new("fffaeb"), Glass = new("030a05"), Grain = new("030e06");
    /// <summary>How long it takes to show and to fade, and how long a fall's flare lasts, in ms.</summary>
    private const double InMs = 130, OutMs = 160, FlareMs = 220;
    /// <summary>A rolling line's lap, the grain's and the flicker's beats, the flicker while it comes on, in ms.</summary>
    private const double RollMs = 1700, GrainMs = 42, FlickMs = 33, ComingMs = 30;

    private readonly Node2D _base = new(), _light = new(), _over = new();
    private Readout? _shown;
    private string _left = "", _max = "";
    private double _shows, _level, _timeMs, _hitAt = double.NegativeInfinity;

    public ScannerView()
    {
        Layer = ScreenLayers.Hud;
        Visible = false;
        FollowViewportEnabled = true;
        _light.Material = new ShaderMaterial { Shader = GD.Load<Shader>("res://shared/holo.gdshader") };
        _base.Draw += DrawBase;
        _light.Draw += DrawLight;
        _over.Draw += DrawOver;
        AddChild(_base);
        AddChild(_light);
        AddChild(_over);
    }

    /// <summary>Shows <paramref name="now"/>, what the scanner reads this frame (null: nothing), <paramref name="elapsedMs"/> after the last.</summary>
    public void Frame(Readout? now, double elapsedMs)
    {
        _timeMs += elapsedMs;
        if (now is { } read)
        {
            if (_shown is { } was && was.Id == read.Id && read.Left < was.Left) _hitAt = _timeMs;
            else if (_shown?.Id != read.Id) _hitAt = double.NegativeInfinity;
            if (_shown?.Left != read.Left) _left = read.Left.ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (_shown?.Max != read.Max) _max = "/" + read.Max.ToString(System.Globalization.CultureInfo.InvariantCulture);
            _shown = read;
        }
        _shows = Math.Clamp(_shows + (now != null ? elapsedMs / InMs : -elapsedMs / OutMs), 0, 1);
        bool coming = _shows < 1 && now != null && Phosphor.Noise(Math.Floor(_timeMs / ComingMs)) < 0.35;
        _level = _shows * (coming ? 0.35 : 1);
        Visible = _level > 0.01 && _shown != null;
        if (!Visible) return;
        _base.QueueRedraw();
        _light.QueueRedraw();
        _over.QueueRedraw();
    }

    private (int X, int Y, int W, int H) Box(out int pointX)
    {
        var r = _shown!.Value;
        pointX = (int)Math.Round(r.X * Camera.Cell, MidpointRounding.AwayFromZero);
        return Phosphor.Box(pointX, (int)Math.Round(r.Y * Camera.Cell, MidpointRounding.AwayFromZero), _left, _max);
    }

    private double Flicker => 0.9 + 0.1 * Phosphor.Noise(Math.Floor(_timeMs / FlickMs) * 1.3 + 3);
    private double Heat => Math.Exp(-(_timeMs - _hitAt) / FlareMs);
    private static Color Of(Color c, double a) => new(c, (float)Math.Clamp(a, 0, 1));

    private void DrawBase()
    {
        if (_shown == null) return;
        var (x, y, w, h) = Box(out _);
        double level = _level * Flicker;
        _base.DrawRect(new Rect2(x, y, w, h), Of(Glass, 0.84 * Math.Min(1, level * 1.5)));
        _base.DrawRect(new Rect2(x + 0.5f, y + 0.5f, w - 1, h - 1), Of(Green, 0.5 * level), false, 1);
    }

    private void DrawLight()
    {
        if (_shown == null) return;
        var (x, y, w, h) = Box(out int pointX);
        double heat = Heat, level = _level * Flicker * (1 + 0.4 * heat);
        Text(_left, x + Phosphor.Inset, y + Phosphor.Inset, Of(Green.Lerp(Flare, (float)(0.6 * heat)), level));
        Text(_max, x + Phosphor.Inset + Phosphor.Width(_left) + 1, y + Phosphor.Inset, Of(Green, 0.55 * level));
        double roll = _timeMs / RollMs % 1;
        _light.DrawRect(new Rect2(x + 1, y + 1 + (int)(roll * (h - 2)), w - 2, 1), Of(Green, 0.22 * level));
        Specks(_light, x, y, w, h, 1, Of(Green, 0.35 * level));
        _light.DrawRect(new Rect2(pointX, y + h, 1, 3), Of(Green, 0.5 * level));
    }

    private void DrawOver()
    {
        if (_shown == null) return;
        var (x, y, w, h) = Box(out _);
        Specks(_over, x, y, w, h, 0, Of(Grain, 0.55 * Math.Min(1, _level * 1.5)));
    }

    /// <summary>Six pixels of grain inside the box, moving every beat: the odd ones lit, the even ones dark.</summary>
    private void Specks(Node2D on, int x, int y, int w, int h, int first, Color colour)
    {
        double beat = Math.Floor(_timeMs / GrainMs);
        for (int i = first; i < 12; i += 2)
            on.DrawRect(new Rect2(x + 1 + (int)(Phosphor.Noise(beat * 17.3 + i * 3.1) * (w - 2)), y + 1 + (int)(Phosphor.Noise(beat * 11.9 + i * 5.7) * (h - 2)), 1, 1), colour);
    }

    private void Text(string text, int x, int y, Color colour)
    {
        for (int i = 0; i < text.Length; i++)
            for (int row = 0; row < 5; row++)
                for (int col = 0; col < 3; col++)
                    if (Phosphor.Lit(text[i], col, row)) _light.DrawRect(new Rect2(x + i * 4 + col, y + row, 1, 1), colour);
    }
}
