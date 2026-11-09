using System.Collections.Generic;
using Godot;
using Stillwake.Game.Features.Effects;
using Stillwake.Game.Features.Sound;
using Stillwake.Game.Render;
using Stillwake.Sim;
using S = Stillwake.Game.Sound;

namespace Stillwake.Game.Shared;

/// <summary>
/// Shots in flight drawn (shots-are-the-sims): each a streak behind its point along its way (<see cref="ShotLooks"/>), handed in
/// every frame; the sim's by <c>ShotsView</c>, this player's own by its hero's hands, which fly them ahead of the sim's. One thrown
/// whole (<see cref="ShotLook.Picture"/>) is drawn as itself instead: its picture lit by the world and turning, its spark unlit over
/// it, a trail of sparks behind, its light about it.
/// </summary>
public partial class ShotStreaks : Node2D
{
    private readonly List<(Vector2 At, Vector2 Way, ShotLook Look)> _streaks = [];
    /// <summary>Those thrown whole this frame: where, turned how far, and their spark's frame.</summary>
    private readonly List<(Vector2 At, float Turn, ShotPicture Picture, int Frame)> _thrown = [];
    private readonly Bodies _bodies;
    /// <summary>A key a thrown one's light is held under, one per place in this frame's list: kept, never made anew.</summary>
    private readonly List<object> _lights = [];

    /// <summary>The streaks this frame: each shot's point and way, in pixels of the world, and its look.</summary>
    public void Show(IEnumerable<(Vector2 At, Vector2 Way, ShotLook Look)> streaks)
    {
        _streaks.Clear();
        _streaks.AddRange(streaks);
        _thrown.Clear();
        double seconds = Time.GetTicksMsec() / 1000.0;
        foreach (var (at, way, look) in _streaks)
        {
            if (look.Picture is not { } picture) continue;
            int frame = (int)(seconds * picture.Fps) % picture.Frames;
            // Turned the way it was thrown: over the top, forward.
            float turn = (float)((way.X < 0 ? -1 : 1) * picture.Turns * Mathf.Tau * (seconds % 1000));
            if (_thrown.Count == _lights.Count) _lights.Add(new object());
            var lit = ToGlobal(at);
            Stages.Of(Layers.Effects).Flares.Hold(_lights[_thrown.Count], lit.X, lit.Y, picture.Light,
                picture.Strength * (0.75 + 0.25 * picture.Flicker[frame % picture.Flicker.Length]) * Fx.Glow, picture.Reach, picture.Keep, 0.6);
            _thrown.Add((at, turn, picture, frame));
        }
        QueueRedraw();
        _bodies.QueueRedraw();
    }

    /// <summary>The steps a streak fades over, tail to head.</summary>
    private const int Fade = 6;

    private readonly Vector2[] _points = new Vector2[Fade + 1];
    private readonly Color[] _colours = new Color[Fade + 1], _rim = new Color[Fade + 1];
    private static readonly Color Rim = new(0.07f, 0.08f, 0.11f, 0.7f);
    private static readonly Dictionary<string, Texture2D> Pictures = new();

    public ShotStreaks()
    {
        Material = new CanvasItemMaterial { LightMode = CanvasItemMaterial.LightModeEnum.Unshaded };
        _bodies = new Bodies { Thrown = _thrown, ShowBehindParent = true };
        // Lit as what is held is: with no material of its own it drew black in the dark of the caves.
        Lit.Set(_bodies);
        AddChild(_bodies);
    }

    public override void _Draw()
    {
        foreach (var (at, way, look) in _streaks)
        {
            if (look.Picture != null)
            {
                Sparks(at, way, look);
                continue;
            }
            var tail = at - way.Normalized() * (float)look.Length;
            Color back = new(look.Tail), body = new(look.Body), point = new(look.Point);
            for (int i = 0; i <= Fade; i++)
            {
                float k = (float)i / Fade;
                _points[i] = tail.Lerp(at, k);
                // A quarter of the way along the tail gives way to the body, which runs on to the point.
                _colours[i] = k < 0.25f ? back.Lerp(body, k / 0.25f) : body.Lerp(point, (k - 0.25f) / 0.75f);
            }
            float w = (float)look.Width;
            // A dark rim under it, so it reads on a pale sky as on a cave's dark; one that glows, its glow instead.
            var under = look.Glow is { } glow ? new Color(glow) with { A = 0.35f } : Rim;
            for (int i = 0; i <= Fade; i++) _rim[i] = under with { A = under.A * _colours[i].A };
            DrawPolylineColors(_points, _rim, look.Glow != null ? w * 3 : w + 2);
            DrawPolylineColors(_points, _colours, w);
            DrawRect(new Rect2(at - new Vector2(w, w) / 2, new Vector2(w, w)), point);
        }
        // The spark at the fuse, turning with the picture under it.
        foreach (var (at, turn, picture, frame) in _thrown)
        {
            var spark = PictureOf(picture.Spark);
            var size = new Vector2(spark.GetWidth() / (float)picture.Frames, spark.GetHeight());
            var corner = -new Vector2((float)picture.Middle.X + picture.Margin, (float)picture.Middle.Y + picture.Margin);
            DrawSetTransform(at, turn);
            DrawTextureRectRegion(spark, new Rect2(corner, size), new Rect2(new Vector2(frame * size.X, 0), size));
        }
        DrawSetTransform(Vector2.Zero);
    }

    /// <summary>
    /// A thrown one's trail: specks along its length behind it, each jittered a pixel and a half anew thirty times a second, the
    /// nearest its point's colour, fading to its tail's.
    /// </summary>
    private void Sparks(Vector2 at, Vector2 way, ShotLook look)
    {
        var back = -way.Normalized();
        int roll = (int)(Time.GetTicksMsec() / 33);
        float w = (float)look.Width;
        Color near = new(look.Point), far = new(look.Tail);
        for (int i = 1; i <= Fade; i++)
        {
            var p = at + back * (float)(look.Length * i / Fade) + new Vector2(Jitter(i, roll), Jitter(i + 9, roll)) * 3;
            var colour = (i < 3 ? near : far) with { A = 1 - i / (Fade + 1f) };
            DrawRect(new Rect2(p - new Vector2(w, w) / 2, new Vector2(w, w)), colour);
        }
    }

    /// <summary>A number from -0.5 to 0.5 for <paramref name="a"/> and <paramref name="b"/>, the same each time.</summary>
    private static float Jitter(int a, int b)
    {
        uint h = unchecked((uint)a * 374761393u + (uint)b * 668265263u);
        h = unchecked((h ^ (h >> 13)) * 1274126177u);
        return (h ^ (h >> 16)) / 4294967296f - 0.5f;
    }

    /// <summary>The hi-res picture <paramref name="name"/> at the world's size, loaded once.</summary>
    private static Texture2D PictureOf(string name)
    {
        if (!Pictures.TryGetValue(name, out var texture))
            Pictures[name] = texture = HiRes.Texture(HiRes.Load(name) ?? throw new System.InvalidOperationException($"No picture {name}"));
        return texture;
    }

    /// <summary>The thrown ones' pictures, under their sparks: lit by the world, as what is held is.</summary>
    private sealed partial class Bodies : Node2D
    {
        public List<(Vector2 At, float Turn, ShotPicture Picture, int Frame)> Thrown = [];

        public override void _Draw()
        {
            foreach (var (at, turn, picture, _) in Thrown)
            {
                DrawSetTransform(at, turn);
                DrawTexture(PictureOf(picture.Still), -new Vector2((float)picture.Middle.X, (float)picture.Middle.Y));
            }
            DrawSetTransform(Vector2.Zero);
        }
    }

    /// <summary>A shot's point <paramref name="progress"/> of the way from <paramref name="was"/> to <paramref name="now"/>, and its way, in pixels of the world.</summary>
    public static (Vector2 At, Vector2 Way) Between(ShotMotion was, ShotMotion now, double progress)
    {
        float k = (float)(Camera.Cell / 1000.0);
        var from = new Vector2(was.X, was.Y) * k;
        var to = new Vector2(now.X, now.Y) * k;
        var way = to - from;
        return (from.Lerp(to, (float)progress), way == Vector2.Zero ? new Vector2(now.VX, now.VY) : way);
    }
}
