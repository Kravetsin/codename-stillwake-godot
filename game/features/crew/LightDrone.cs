using System;
using Godot;
using Stillwake.Game.Render;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// The light drone (light-drone), the picked «Диск»: a lens disc floating by the hero's head, its face turned where it shines, in a
/// slowly turning band of marks. Its body is a picture per angle (<c>drone_disc</c>, tools/anime-art drone.js); the band, the lens
/// and its haze are drawn here, as the pick drew them. Drawn unlit, in the world's frame; its light is the head lamp's.
/// </summary>
public partial class LightDrone : Node2D
{
    /// <summary>How many angles its body is drawn at, how wide each, in world px (drone.js's <c>Turns</c>, <c>Box</c>).</summary>
    private const int Turns = 16, Box = 10;

    /// <summary>The band's half-axes and tilt, in world px (the pick's, a quarter of its pixels); its marks' turn a second, shining and scanning.</summary>
    private const float BandX = 5.125f, BandY = 1.55f, Tilt = -0.32f, Turning = 0.45f, ScanTurning = 2.2f;

    /// <summary>How far the lens sits ahead of the disc's middle, its half-axes, in world px.</summary>
    private const float LensAhead = 0.65f, LensX = 0.575f, LensY = 0.925f;

    private static readonly Color Ink = new("16171b"), BackBand = new("4d5157"), FrontBand = new("6c7178"), Edge = new(0.745f, 0.769f, 0.792f, 0.45f);
    private static readonly Color MarkFront = new(0.808f, 0.776f, 0.667f, 0.9f), MarkBack = new(0.588f, 0.565f, 0.486f, 0.7f);
    private static readonly Color Lamp = new(1f, 0.831f, 0.588f), Scan = new(0.463f, 0.925f, 0.808f);

    private static Texture2D? _body;
    private readonly DroneFloat _float = new();
    private Vector2 _toward = Vector2.Right;
    private double _timeMs;

    public LightDrone()
    {
        TopLevel = true;
        // Over the hero and what its near hand holds, as the other drones fly.
        (ZAsRelative, ZIndex) = (false, Render.Layers.Drones);
        Visible = false;
        Material = new CanvasItemMaterial { LightMode = CanvasItemMaterial.LightModeEnum.Unshaded };
    }

    /// <summary>Whether it scans rather than shines: the scanner worn, not the drone.</summary>
    public bool Scanning { get; set; }

    /// <summary>Floats it after the neck at <paramref name="neck"/>, its lens turned <paramref name="toward"/>, a unit vector.</summary>
    public void Follow(Vector2 neck, int facing, Vector2 toward, double elapsedMs, double timeMs)
    {
        var (x, y) = _float.Follow((neck.X, neck.Y), facing, elapsedMs, timeMs);
        GlobalPosition = new Vector2((float)x, (float)y);
        (_toward, _timeMs) = (toward, timeMs);
        QueueRedraw();
    }

    public override void _Draw()
    {
        _body ??= HiRes.Texture(HiRes.Load("drone_disc") ?? throw new InvalidOperationException("No picture drone_disc"));
        float angle = Mathf.Atan2(_toward.Y, _toward.X);
        var colour = Scanning ? Scan : Lamp;
        float marks = (float)(_timeMs / 1000) * (Scanning ? ScanTurning : Turning);
        Band(false, marks, colour);
        int turn = ((int)Math.Round(angle / Mathf.Tau * Turns) % Turns + Turns) % Turns;
        DrawTextureRectRegion(_body, new Rect2(-Box / 2f, -Box / 2f, Box, Box), new Rect2(turn * Box, 0, Box, Box));
        var lens = _toward * LensAhead;
        for (int i = 4; i >= 1; i--) DrawCircle(lens, 0.9f * i, new Color(colour, 0.09f));
        DrawColoredPolygon(Ellipse(lens, LensX, LensY, angle), colour.Lerp(Colors.White, 0.3f));
        DrawColoredPolygon(Ellipse(lens + _toward * 0.15f, 0.25f, 0.45f, angle), colour.Lerp(Colors.White, 0.78f));
        Band(true, marks, colour);
    }

    /// <summary>Half the band, behind the body or before it, its five marks turned <paramref name="marks"/>; scanning, they glow.</summary>
    private void Band(bool front, float marks, Color colour)
    {
        var arc = new Vector2[17];
        for (int i = 0; i < arc.Length; i++) arc[i] = OnBand((front ? 0 : Mathf.Pi) + Mathf.Pi * i / (arc.Length - 1), 0);
        DrawPolyline(arc, Ink, 1.25f, true);
        DrawPolyline(arc, front ? FrontBand : BackBand, 0.75f, true);
        if (front)
        {
            var lit = new Vector2[13];
            for (int i = 0; i < lit.Length; i++) lit[i] = OnBand(0.25f + (Mathf.Pi - 0.5f) * i / (lit.Length - 1), -0.25f);
            DrawPolyline(lit, Edge, 0.2f, true);
        }
        for (int k = 0; k < 5; k++)
        {
            float th = k * Mathf.Tau / 5 + marks;
            if (Mathf.Sin(th) >= 0 != front) continue;
            var at = OnBand(th, 0);
            if (Scanning) DrawCircle(at, 1.1f, new Color(colour, 0.3f));
            DrawRect(new Rect2(at - new Vector2(0.275f, 0.275f), new Vector2(0.55f, 0.55f)), Scanning ? colour.Lerp(Colors.White, 0.4f) : front ? MarkFront : MarkBack);
        }
    }

    private static Vector2 OnBand(float th, float up) => new Vector2(Mathf.Cos(th) * BandX, Mathf.Sin(th) * BandY + up).Rotated(Tilt);

    private static Vector2[] Ellipse(Vector2 at, float rx, float ry, float angle)
    {
        var points = new Vector2[12];
        for (int i = 0; i < points.Length; i++)
        {
            float t = Mathf.Tau * i / points.Length;
            points[i] = at + new Vector2(Mathf.Cos(t) * rx, Mathf.Sin(t) * ry).Rotated(angle);
        }
        return points;
    }
}
