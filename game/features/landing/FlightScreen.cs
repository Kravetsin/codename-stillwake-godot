using System;
using Godot;
using Stillwake.Game.Render;
using static Stillwake.Game.Features.Landing.FlightTimeline;

namespace Stillwake.Game.Features.Landing;

/// <summary>
/// What the rider sees of its flight (the descent's slice 2, its storyboard): no planet, only speed. The pod held in the middle at
/// the world's scale, the stars torn past it into lines, faster and longer as it goes; on the way up it leaves a planet's blue sky
/// first, clouds ripped past, the lines rushing down. Every mark is placed from the flight's seconds alone, in one draw.
/// </summary>
public partial class FlightScreen : Control
{
    private const int Lines = 400, Clouds = 10;
    private static readonly Texture2D Pod = PixelArt.Picture("machine_dropPod");
    /// <summary>The vignette: nothing in the middle to black at the rim.</summary>
    private static readonly GradientTexture2D Rim = new()
    {
        Gradient = new Gradient { Colors = [new Color(0, 0, 0, 0), new Color(0, 0, 0, 1)], Offsets = [0, 1] },
        Fill = GradientTexture2D.FillEnum.Radial,
        FillFrom = new Vector2(0.5f, 0.5f),
        FillTo = new Vector2(1, 0.5f),
        Width = 128,
        Height = 128,
    };
    private readonly Vector2[] _quad = new Vector2[4];
    private readonly Color[] _colours = new Color[4];
    private double _s, _length;
    private bool _up;
    private float _zoom = 1;

    public FlightScreen()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        SetAnchorsPreset(LayoutPreset.FullRect);
    }

    /// <summary>Shows the flight <paramref name="s"/> seconds in, of <paramref name="length"/>, up or down, at the world's <paramref name="zoom"/>.</summary>
    public void Show(double s, double length, bool up, float zoom)
    {
        (_s, _length, _up, _zoom) = (s, length, up, zoom);
        QueueRedraw();
    }

    public override void _Draw()
    {
        float z = Math.Max(_zoom, 0.5f), w = Size.X / z, h = Size.Y / z;
        double s = _s, k = Fraction(s, _length);
        float air = (float)Air(k, _up);
        // Pixels a second the stars rush past, quickly at full speed; up, they rush down.
        float speed = (float)(Lerp(900, 2600, Smooth(k / 0.3)) * (1 - air * 0.35));
        DrawSetTransform(Vector2.Zero, 0, new Vector2(z, z));

        // Ground: black space, a planet's blue while still in its air on the way up.
        var top = new Color(0.012f, 0.016f, 0.03f).Lerp(new Color(0.07f, 0.27f, 0.59f), air);
        var low = new Color(0.024f, 0.03f, 0.055f).Lerp(new Color(0.27f, 0.55f, 0.84f), air);
        Quad(0, 0, w, h, top, top, low, low);

        float shake = 0.6f + air * 0.8f;
        var jolt = new Vector2((float)(Hash(Math.Floor(s * 60)) - 0.5) * shake, (float)(Hash(Math.Floor(s * 60) + 7) - 0.5) * shake);
        DrawSetTransform(jolt * z, 0, new Vector2(z, z));

        // Speed lines: as many as fit the screen at the storyboard's density; the near ones longer, brighter, faster.
        int count = Math.Min(Lines, (int)(220 * w * h / (320 * 180)));
        var tint = new Color(0.86f, 0.91f, 1);
        for (int i = 0; i < count; i++)
        {
            bool near = Hash(i + 2200) > 0.82;
            float x = (float)Hash(i + 1000) * w, len = (near ? 70 : 34) * (0.4f + (float)Hash(i + 1600) * 0.6f) * (0.6f + (float)k);
            float span = h + 120, v = speed * (near ? 1.8f : 1);
            float y = h + 60 - (float)((s * v / span + Hash(i + 1300)) % 1) * span;
            if (_up) y = h - y - len;
            float a = (0.25f + (float)Hash(i + 1900) * 0.75f) * (near ? 0.9f : 0.5f) * (air > 0.5f ? 0.55f : 1);
            // Bright at the end the stars rush toward: the trailing end fades.
            var faint = tint with { A = 0 };
            var bright = tint with { A = a };
            if (_up) Quad(x, y, near ? 2 : 1, len, bright, bright, faint, faint);
            else Quad(x, y, near ? 2 : 1, len, faint, faint, bright, bright);
        }

        if (air > 0) for (int i = 0; i < Clouds - 3; i++) Cloud(i, false, s, speed, w, h, air);

        // The pod, held still against it all but for a little buffeting in the air.
        float cx = w / 2, py = h / 2 - 40, tilt = (float)(Hash(Math.Floor(s * 12)) - 0.5) * 0.03f * air;
        DrawSetTransform((jolt + new Vector2(cx, py + 28)) * z, tilt, new Vector2(z, z));
        DrawTexture(Pod, new Vector2(-16, -28));
        DrawSetTransform(jolt * z, 0, new Vector2(z, z));

        if (air > 0) for (int i = Clouds - 3; i < Clouds; i++) Cloud(i, true, s, speed, w, h, air);

        // The vignette, pulling the eye to the pod.
        DrawSetTransform(Vector2.Zero, 0, Vector2.One);
        DrawTextureRect(Rim, new Rect2(-Size.X * 0.25f, -Size.Y * 0.25f, Size.X * 1.5f, Size.Y * 1.5f), false, new Color(1, 1, 1, 0.35f));
    }

    /// <summary>A cloud sheet ripped through the frame: some behind the pod, the fastest in front of it.</summary>
    private void Cloud(int i, bool front, double s, float speed, float w, float h, float air)
    {
        float v = speed * (front ? 1.6f : 0.9f), span = h + 160;
        float y = h + 80 - (float)((s * v / span + Hash(i + 2500)) % 1) * span;
        if (_up) y = h - y;
        float width = 80 + (float)Hash(i + 2600) * 160 * w / 320, x = (float)Hash(i + 2700) * (w + 120) - 100, tall = 26 + (float)Hash(i + 2800) * 40;
        float a = air * (front ? 0.55f : 0.4f) * (0.6f + (float)Hash(i + 2900) * 0.4f);
        var clear = new Color(1, 1, 1, 0);
        var white = new Color(0.94f, 0.96f, 1, a);
        Quad(x, y, width, tall * 0.6f, clear, clear, white, white);
        Quad(x, y + tall * 0.6f, width, tall * 0.4f, white, white, clear, clear);
    }

    /// <summary>A rectangle with a colour at each corner: top-left, top-right, bottom-left, bottom-right.</summary>
    private void Quad(float x, float y, float w, float h, Color tl, Color tr, Color bl, Color br)
    {
        _quad[0] = new Vector2(x, y);
        _quad[1] = new Vector2(x + w, y);
        _quad[2] = new Vector2(x + w, y + h);
        _quad[3] = new Vector2(x, y + h);
        (_colours[0], _colours[1], _colours[2], _colours[3]) = (tl, tr, br, bl);
        DrawPolygon(_quad, _colours);
    }

    private static double Lerp(double a, double b, double t) => a + (b - a) * t;

    private static double Smooth(double x)
    {
        x = Math.Clamp(x, 0, 1);
        return x * x * (3 - 2 * x);
    }

    /// <summary>The storyboard's seeded number in 0..1 for <paramref name="n"/>: the same mark in the same place every frame.</summary>
    private static double Hash(double n)
    {
        double x = Math.Sin(n * 127.1 + 311.7) * 43758.5453;
        return x - Math.Floor(x);
    }
}
