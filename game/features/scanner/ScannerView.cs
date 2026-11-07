using System;
using Godot;
using Stillwake.Game.Render;

namespace Stillwake.Game.Features.Scanner;

/// <summary>
/// The scanner's readout over the wreck it reads (2026-11-07-the-scanner), phosphor: a dark glass, then its digits added with the
/// edge keys' glow (holo.gdshader) and a slow rolling line, then scanlines of the box's own over it; the value flares white as it
/// falls. On a layer that follows the camera, unlit by the world's lights, drawn in whole screen pixels (2026-11-07-steady-scanner).
/// </summary>
public partial class ScannerView : CanvasLayer
{
    private static readonly Color Green = new("7cf096"), Flare = new("fffaeb"), Glass = new("030a05");
    /// <summary>How long it takes to show and to fade, and how long a fall's flare lasts, in ms.</summary>
    private const double InMs = 130, OutMs = 160, FlareMs = 220;
    /// <summary>A rolling line's lap, in ms.</summary>
    private const double RollMs = 1700;
    /// <summary>How much a scanline dims what is under it.</summary>
    private const float Scanline = 0.35f;

    private readonly Node2D _base = new(), _light = new(), _over = new();
    private Readout? _shown;
    private string _left = "", _max = "";
    private double _shows, _timeMs, _hitAt = double.NegativeInfinity;
    private int _scale = 1;

    public ScannerView()
    {
        Layer = ScreenLayers.Hud;
        Visible = false;
        FollowViewportEnabled = true;
        // Its own scanlines, fixed to the box: the shader's run along the screen's rows and shimmer as the box moves over them.
        var material = new ShaderMaterial { Shader = GD.Load<Shader>("res://shared/holo.gdshader") };
        material.SetShaderParameter("scan", 0f);
        _light.Material = material;
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
        Visible = _shows > 0.01 && _shown != null;
        if (!Visible) return;
        // Drawn in screen pixels from the point it reads, each of its pixels a whole number of them.
        float zoom = Math.Max(0.01f, GetViewport().GetCanvasTransform().X.Length());
        _scale = Phosphor.Scale(zoom);
        var r = _shown!.Value;
        var at = new Vector2((float)(r.X * Camera.Cell), (float)(r.Y * Camera.Cell));
        foreach (var layer in (Node2D[])[_base, _light, _over])
        {
            layer.Position = at;
            layer.Scale = Vector2.One / zoom;
            layer.QueueRedraw();
        }
    }

    private (int X, int Y, int W, int H) Box => Phosphor.Box(0, 0, _max);
    private double Heat => Math.Exp(-(_timeMs - _hitAt) / FlareMs);
    private static Color Of(Color c, double a) => new(c, (float)Math.Clamp(a, 0, 1));

    /// <summary>A rectangle in the readout's pixels, each <see cref="_scale"/> screen pixels.</summary>
    private void Rect(Node2D on, int x, int y, int w, int h, Color colour) =>
        on.DrawRect(new Rect2(x * _scale, y * _scale, w * _scale, h * _scale), colour);

    private void DrawBase()
    {
        if (_shown == null) return;
        var (x, y, w, h) = Box;
        Rect(_base, x, y, w, h, Of(Glass, 0.84 * Math.Min(1, _shows * 1.5)));
        // The edge, a readout pixel thick, as four strips: an unfilled rectangle's line would straddle its edge.
        var edge = Of(Green, 0.5 * _shows);
        Rect(_base, x, y, w, 1, edge);
        Rect(_base, x, y + h - 1, w, 1, edge);
        Rect(_base, x, y + 1, 1, h - 2, edge);
        Rect(_base, x + w - 1, y + 1, 1, h - 2, edge);
    }

    private void DrawLight()
    {
        if (_shown == null) return;
        var (x, y, w, h) = Box;
        double heat = Heat, level = _shows * (1 + 0.4 * heat);
        int room = Phosphor.LeftWidth(_max);
        Text(_left, x + Phosphor.Inset + room - Phosphor.Width(_left), y + Phosphor.Inset, Of(Green.Lerp(Flare, (float)(0.6 * heat)), level));
        Text(_max, x + Phosphor.Inset + room + 1, y + Phosphor.Inset, Of(Green, 0.55 * level));
        double roll = _timeMs / RollMs % 1;
        _light.DrawRect(new Rect2((x + 1) * _scale, (y + 1) * _scale + (int)(roll * (h - 2) * _scale), (w - 2) * _scale, _scale), Of(Green, 0.22 * level));
        Rect(_light, 0, y + h, 1, 3, Of(Green, 0.5 * level));
    }

    /// <summary>Every other screen row of the box dimmed, counted from its own top, so they move with it.</summary>
    private void DrawOver()
    {
        if (_shown == null || _scale < 2) return;
        var (x, y, w, h) = Box;
        var dim = Of(Glass, Scanline * _shows);
        for (int row = 1; row < h * _scale; row += 2)
            _over.DrawRect(new Rect2(x * _scale, y * _scale + row, w * _scale, 1), dim);
    }

    private void Text(string text, int x, int y, Color colour)
    {
        for (int i = 0; i < text.Length; i++)
            for (int row = 0; row < 5; row++)
                for (int col = 0; col < 3; col++)
                    if (Phosphor.Lit(text[i], col, row)) Rect(_light, x + i * 4 + col, y + row, 1, 1, colour);
    }
}
