using System;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using I = Stillwake.Game.Shared.DroneInk;

namespace Stillwake.Game.Features.Scanner;

/// <summary>
/// The scanner's gauge over the wreck it reads (drone-threads, the pick «Нити»): a row of ember dots, the lit ones its hit points
/// left, flown in from the drone's spyglass and home to its eye, every few on a thread (<see cref="ScanDots"/>). On a layer that
/// follows the camera, unlit by the world's lights, each dot whole screen pixels (2026-11-07-steady-scanner).
/// </summary>
public partial class ScannerView : CanvasLayer
{
    private static readonly Color Ember = Rgb(I.Ember), Red = Rgb(I.Red), Dark = Rgb(I.Dark);

    private readonly ScanDots _dots = new();
    private readonly Node2D _dark = new(), _light = new();
    private readonly Triangles _darks = new(), _lights = new();
    private float _zoom = 1, _tint;

    public ScannerView()
    {
        Layer = ScreenLayers.Hud;
        Visible = false;
        FollowViewportEnabled = true;
        _light.Material = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Add };
        _dark.Draw += DrawDark;
        _light.Draw += DrawLight;
        AddChild(_dark);
        AddChild(_light);
    }

    /// <summary>
    /// Shows <paramref name="now"/>, what the scanner reads this frame (null: nothing), <paramref name="elapsedMs"/> after the last;
    /// the dots fly from the drone's spyglass and home to its eye, red as its alarm (<paramref name="drone"/>; none: in place).
    /// </summary>
    public void Frame(Readout? now, (Vector2 Mouth, Vector2 Eye, float Tint)? drone, double elapsedMs)
    {
        _dots.Step(now, drone is { } d ? (d.Mouth.X, d.Mouth.Y) : null, drone is { } e ? (e.Eye.X, e.Eye.Y) : null, elapsedMs / 1000);
        _tint = drone?.Tint ?? 0;
        Visible = _dots.Visible;
        if (!Visible) return;
        // Drawn in screen pixels from the point it reads, so each dot keeps whole ones as the camera eases.
        _zoom = Math.Max(0.01f, GetViewport().GetCanvasTransform().X.Length());
        var r = _dots.Shown!.Value;
        var at = new Vector2((float)(r.X * Camera.Cell), (float)(r.Y * Camera.Cell));
        foreach (var layer in (ReadOnlySpan<Node2D>)[_dark, _light])
        {
            layer.Position = at;
            layer.Scale = Vector2.One / _zoom;
            layer.QueueRedraw();
        }
    }

    /// <summary>A point of the world in the layers' screen pixels from the point read, whole.</summary>
    private Vector2 Screen(float x, float y) => new(MathF.Round((x - _dark.Position.X) * _zoom), MathF.Round((y - _dark.Position.Y) * _zoom));

    /// <summary>A side of <paramref name="world"/> px in whole screen pixels, at least one.</summary>
    private float Side(float world) => Math.Max(1, MathF.Round(world * _zoom));

    private Color Hot => Ember.Lerp(Red, Math.Min(1, _tint * I.AlarmRed));

    private void DrawDark()
    {
        _darks.Clear();
        float side = Side(I.Dot * I.WorldPx);
        for (int j = 0; j < ScanDots.Count; j++)
        {
            var dot = _dots.Dot(j);
            if (dot.Alpha <= 0) continue;
            Square(_darks, Screen(dot.X, dot.Y), side, new Color(Dark, dot.Alpha));
            if (_dots.Ember(j) is { } ember) Square(_darks, Screen(ember.X, ember.Y), Side(I.Dot * I.WorldPx * ember.Side), Dark);
        }
        _darks.Flush(_dark.GetCanvasItem());
    }

    /// <summary>Each dot's glow, a tail behind it as it flies; every few's thread; the embers a hit burst off.</summary>
    private void DrawLight()
    {
        _lights.Clear();
        var hot = Hot;
        float side = Side(I.Dot * I.WorldPx), step = I.TailStep * I.Px * _zoom;
        for (int j = 0; j < ScanDots.Count; j++)
        {
            var dot = _dots.Dot(j);
            var at = Screen(dot.X, dot.Y);
            if (dot.Flying && j % I.ThreadEvery == 0)
                _lights.Quad(Screen(dot.FromX, dot.FromY), at, I.ThreadLine * I.Px * _zoom, new Color(hot, I.ThreadAlpha * dot.Alpha));
            Glow(at, side, new Vector2(dot.Dx, dot.Dy) * step, Math.Min(1, dot.Heat + _tint * I.AlarmHeat * dot.Alpha), hot);
            if (_dots.Ember(j) is { } ember)
                Glow(Screen(ember.X, ember.Y), Side(I.Dot * I.WorldPx * ember.Side), Vector2.Zero, Math.Min(1, ember.Heat + _tint * I.AlarmHeat), hot);
        }
        _lights.Flush(_light.GetCanvasItem());
    }

    /// <summary>A glow <paramref name="heat"/> hot at <paramref name="at"/>: its square and two behind it by <paramref name="back"/>, paler and fainter.</summary>
    private void Glow(Vector2 at, float side, Vector2 back, float heat, Color hot)
    {
        if (heat <= 0.02f) return;
        for (int m = 0; m < I.Tail; m++)
        {
            var colour = hot.Lerp(Colors.White, I.TailPale * (1 - m / (float)I.Tail));
            Square(_lights, new Vector2(MathF.Round(at.X - back.X * m), MathF.Round(at.Y - back.Y * m)), side, new Color(colour, (I.TailAlpha - m * I.TailFade) * heat));
        }
    }

    /// <summary>A square <paramref name="side"/> whole pixels round <paramref name="at"/>, on whole pixels.</summary>
    private static void Square(Triangles into, Vector2 at, float side, Color colour)
    {
        float half = MathF.Floor(side / 2);
        into.Rect(at.X - half, at.Y - half, side, side, colour);
    }

    private static Color Rgb((int R, int G, int B) c) => new(c.R / 255f, c.G / 255f, c.B / 255f);
}
