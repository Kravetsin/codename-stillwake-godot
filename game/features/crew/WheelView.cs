using System;
using System.Collections.Generic;
using Godot;
using Stillwake.Game.Render;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// A key's wheel (<see cref="Wheel{T}"/>; gestures-for-words, light-drone): round where the key was pressed, a dark disc a side with its picture,
/// the one the pointer has moved toward lit, a shut one dimmed and crossed; a ring for the middle, which picks nothing. No words: the pictures
/// say it, a gesture's hand (<see cref="Hand"/>) or a drone mode's lens (<see cref="Drone"/>).
/// </summary>
public partial class WheelView : CanvasLayer
{
    private static readonly Color Disc = new(5 / 255f, 8 / 255f, 14 / 255f, 0.62f), Lit = new(5 / 255f, 8 / 255f, 14 / 255f, 0.9f);
    private static readonly Color Ring = new(1, 1, 1, 0.35f), LitRing = new("e8d9a8"), Shut = new(5 / 255f, 8 / 255f, 14 / 255f, 0.6f);
    /// <summary>A disc's radius and its picture's height in it, in pixels at the normal slot size; a lit one grows this much.</summary>
    private const float DiscRadius = 21, PictureHeight = 30, Grow = 1.15f;
    /// <summary>A hand frame of the strip, in its pictures' pixels.</summary>
    private const int FrameW = 48, FrameH = 52;
    /// <summary>The drone's lamp and scan colours, as <see cref="LightDrone"/> draws them.</summary>

    private static Texture2D? _hand, _palm;
    private readonly Control _plate = new() { MouseFilter = Control.MouseFilterEnum.Ignore, Visible = false };
    private readonly List<bool> _shut = [];
    private Action<CanvasItem, int, Vector2, float>? _picture;
    private Vector2 _from;
    private int? _toward;
    private float _scale = 1;

    public WheelView()
    {
        Layer = ScreenLayers.Tips;
        _plate.Draw += Paint;
        AddChild(_plate);
    }

    /// <summary>
    /// Shows <paramref name="wheel"/> while it is open, at <paramref name="scale"/> times the normal slot size, each side drawn by
    /// <paramref name="picture"/> on a canvas, centred on a point, so tall.
    /// </summary>
    public void Show<T>(Wheel<T> wheel, float scale, Action<CanvasItem, Vector2, float, T> picture) where T : struct
    {
        bool open = wheel.Open;
        if (!open && !_plate.Visible) return;
        _plate.Visible = open;
        if (!open) return;
        (_from, _scale) = (new Vector2((float)wheel.From.X, (float)wheel.From.Y), scale);
        _shut.Clear();
        foreach (var side in wheel.Sides) _shut.Add(!wheel.Opens(side));
        _toward = wheel.TowardAt is int at && !_shut[at] ? at : null;
        _picture = (on, i, centre, height) => picture(on, centre, height, wheel.Sides[i]);
        _plate.QueueRedraw();
    }

    private void Paint()
    {
        if (_picture == null) return;
        _plate.DrawArc(_from, (float)Wheel.Middle * _scale, 0, Mathf.Tau, 32, Ring, 1.5f, true);
        for (int i = 0; i < _shut.Count; i++)
        {
            bool lit = i == _toward;
            // From the top, clockwise, shared out evenly as Wheel.SideOf reads them.
            float angle = i * Mathf.Tau / _shut.Count;
            var at = _from + new Vector2(Mathf.Sin(angle), -Mathf.Cos(angle)) * (float)Wheel.Radius * _scale;
            float grow = lit ? Grow : 1, radius = DiscRadius * _scale * grow;
            _plate.DrawCircle(at, radius, lit ? Lit : Disc);
            _plate.DrawArc(at, radius, 0, Mathf.Tau, 48, lit ? LitRing : Ring, lit ? 2 : 1, true);
            _picture(_plate, i, at, PictureHeight * _scale * grow);
            if (!_shut[i]) continue;
            // Shut: dimmed under a dark glass and crossed out.
            _plate.DrawCircle(at, radius, Shut);
            var arm = new Vector2(radius, radius) * 0.5f;
            _plate.DrawLine(at - arm, at + arm, Ring, 1.5f, true);
            _plate.DrawLine(at + new Vector2(arm.X, -arm.Y), at - new Vector2(arm.X, -arm.Y), Ring, 1.5f, true);
        }
    }

    /// <summary>A gesture's side: the hand's own frame enlarged, turned as the gesture shows it.</summary>
    public static void Hand(CanvasItem on, Vector2 at, float height, Gesture gesture)
    {
        _hand ??= GD.Load<Texture2D>("res://art/hd/hero_hand_glove.png");
        _palm ??= GD.Load<Texture2D>("res://art/hd/hero_hand_far_glove.png");
        var (frame, turn, mirrored) = HandOf(gesture);
        float w = height * FrameW / FrameH;
        on.DrawSetTransform(at, turn, new Vector2(mirrored ? -1 : 1, 1));
        on.DrawTextureRectRegion(GestureMotions.Of(gesture).Side == HandSide.Palm ? _palm : _hand, new Rect2(-w / 2, -height / 2, w, height), new Rect2(frame * FrameW, 0, FrameW, FrameH));
        on.DrawSetTransform(Vector2.Zero, 0, Vector2.One);
    }

    /// <summary>
    /// How a gesture's hand is shown: its frame, turned clockwise from hanging, and mirrored. The thumb sticks out to the right of the
    /// hanging hand, so a quarter turn back stands it up; mirrored first, the same turn points it down.
    /// </summary>
    private static (int Frame, float Turn, bool Mirrored) HandOf(Gesture gesture) => gesture switch
    {
        Gesture.Yes => (GestureMotions.ThumbFrame, -Mathf.Pi / 2, false),
        Gesture.No => (GestureMotions.ThumbFrame, -Mathf.Pi / 2, true),
        Gesture.Wave => (GestureMotions.OpenFrame, Mathf.Pi, false),
        _ => (GestureMotions.PointFrame, 0, false),
    };

    /// <summary>A drone mode's side, its lens in the mode's colour: the lamp's ringed with rays; the scanner's in a ring a line sweeps round.</summary>
    public static void Drone(CanvasItem on, Vector2 at, float height, DroneMode mode)
    {
        float lens = height * 0.17f, outer = height * 0.42f;
        var colour = mode == DroneMode.Scan ? LightDrone.Scan : LightDrone.Lamp;
        if (mode == DroneMode.Scan)
        {
            // The sweep up and to the right, a fading wedge behind it.
            const float sweep = -Mathf.Pi / 4, trail = 1.1f;
            var wedge = new Vector2[10];
            wedge[0] = at;
            for (int i = 1; i < wedge.Length; i++) wedge[i] = at + Vector2.FromAngle(sweep - trail * (i - 1) / (wedge.Length - 2)) * outer;
            on.DrawColoredPolygon(wedge, new Color(colour, 0.28f));
            on.DrawArc(at, outer, 0, Mathf.Tau, 40, new Color(colour, 0.7f), 1.5f, true);
            on.DrawLine(at, at + Vector2.FromAngle(sweep) * outer, colour.Lerp(Colors.White, 0.4f), 2, true);
        }
        else
            for (int i = 0; i < 8; i++)
            {
                var ray = Vector2.FromAngle(i * Mathf.Tau / 8);
                on.DrawLine(at + ray * lens * 1.6f, at + ray * outer, colour, 2, true);
            }
        on.DrawCircle(at, lens * 1.25f, new Color(colour, 0.25f));
        on.DrawCircle(at, lens, colour.Lerp(Colors.White, 0.3f));
        on.DrawCircle(at - new Vector2(lens, lens) * 0.3f, lens * 0.35f, colour.Lerp(Colors.White, 0.78f));
    }
}
