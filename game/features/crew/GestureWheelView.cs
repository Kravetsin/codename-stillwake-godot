using System;
using Godot;
using Stillwake.Game.Render;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// The gesture wheel (emotes): round where the key was pressed, a dark disc a side with the hand's own frame enlarged, turned as the
/// gesture shows it, the one the pointer has moved toward lit; a ring for the middle, which picks nothing. No words: the hands say it.
/// </summary>
public partial class GestureWheelView : CanvasLayer
{
    private static readonly Color Disc = new(5 / 255f, 8 / 255f, 14 / 255f, 0.62f), Lit = new(5 / 255f, 8 / 255f, 14 / 255f, 0.9f);
    private static readonly Color Ring = new(1, 1, 1, 0.35f), LitRing = new("e8d9a8");
    /// <summary>A disc's radius and the hand's height in it, in pixels at the normal slot size; a lit one grows this much.</summary>
    private const float DiscRadius = 21, HandHeight = 30, Grow = 1.15f;
    /// <summary>A hand frame of the strip, in its pictures' pixels.</summary>
    private const int FrameW = 48, FrameH = 52;

    private readonly Control _plate = new() { MouseFilter = Control.MouseFilterEnum.Ignore, Visible = false };
    private Texture2D? _hand;
    private Vector2 _from;
    private Gesture? _toward;
    private float _scale = 1;

    public GestureWheelView()
    {
        Layer = ScreenLayers.Tips;
        _plate.Draw += Paint;
        AddChild(_plate);
    }

    /// <summary>Shows <paramref name="wheel"/> while it is open, at <paramref name="scale"/> times the normal slot size.</summary>
    public void Show(GestureWheel wheel, float scale)
    {
        bool open = wheel.Open;
        if (!open && !_plate.Visible) return;
        _plate.Visible = open;
        (_from, _toward, _scale) = (new Vector2((float)wheel.From.X, (float)wheel.From.Y), wheel.Toward, scale);
        _plate.QueueRedraw();
    }

    private void Paint()
    {
        _hand ??= GD.Load<Texture2D>("res://art/hd/hero_hand_glove.png");
        _plate.DrawArc(_from, (float)GestureWheel.Middle * _scale, 0, Mathf.Tau, 32, Ring, 1.5f, true);
        for (int i = 0; i < GestureWheel.Sides.Length; i++)
        {
            var gesture = GestureWheel.Sides[i];
            bool lit = gesture == _toward;
            // From the top, clockwise: up, right, down, left.
            float angle = i * Mathf.Pi / 2;
            var at = _from + new Vector2(Mathf.Sin(angle), -Mathf.Cos(angle)) * (float)GestureWheel.Radius * _scale;
            float grow = lit ? Grow : 1;
            _plate.DrawCircle(at, DiscRadius * _scale * grow, lit ? Lit : Disc);
            _plate.DrawArc(at, DiscRadius * _scale * grow, 0, Mathf.Tau, 48, lit ? LitRing : Ring, lit ? 2 : 1, true);
            var (frame, turn, mirrored) = PictureOf(gesture);
            float h = HandHeight * _scale * grow, w = h * FrameW / FrameH;
            _plate.DrawSetTransform(at, turn, new Vector2(mirrored ? -1 : 1, 1));
            _plate.DrawTextureRectRegion(_hand, new Rect2(-w / 2, -h / 2, w, h), new Rect2(frame * FrameW, 0, FrameW, FrameH));
            _plate.DrawSetTransform(Vector2.Zero, 0, Vector2.One);
        }
    }

    /// <summary>
    /// How a gesture's hand is shown: its frame, turned clockwise from hanging, and mirrored. The thumb sticks out to the right of the
    /// hanging hand, so a quarter turn back stands it up; mirrored first, the same turn points it down.
    /// </summary>
    private static (int Frame, float Turn, bool Mirrored) PictureOf(Gesture gesture) => gesture switch
    {
        Gesture.Yes => (Gestures.ThumbFrame, -Mathf.Pi / 2, false),
        Gesture.No => (Gestures.ThumbFrame, -Mathf.Pi / 2, true),
        Gesture.Wave => (Gestures.OpenFrame, Mathf.Pi, false),
        // Come: the finger pointing in at the middle, "here".
        Gesture.Come => (Gestures.PointFrame, Mathf.Pi / 2, false),
        _ => (Gestures.PointFrame, 0, false),
    };
}
