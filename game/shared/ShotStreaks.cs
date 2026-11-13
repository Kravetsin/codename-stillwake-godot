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
/// it, a trail of sparks behind, its light about it. A bomb (<see cref="ShotLook.Bomb"/>) is drawn as its shapes, lit, point first, a
/// short trail of sparks behind.
/// </summary>
public partial class ShotStreaks : Node2D
{
    private readonly List<(Vector2 At, Vector2 Way, ShotLook Look)> _streaks = [];
    /// <summary>Those thrown whole this frame: where, turned how far, and their spark's frame.</summary>
    private readonly List<(Vector2 At, float Turn, ShotPicture Picture, int Frame)> _thrown = [];
    /// <summary>The bombs this frame: where, and turned along their flight.</summary>
    private readonly List<(Vector2 At, float Turn, ShotBomb Bomb)> _bombs = [];
    /// <summary>The rockets this frame: where, turned along their flight, flipped flying left, and their flame's flicker.</summary>
    private readonly List<(Vector2 At, float Turn, bool Flip, ShotRocket Rocket, float Flicker)> _rockets = [];
    private readonly Flames _flames;
    /// <summary>A key a rocket's light is held under, one per place in this frame's list.</summary>
    private readonly List<object> _rocketLights = [];
    private double _lastMs;
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
        _bombs.Clear();
        _rockets.Clear();
        double nowMs = Time.GetTicksMsec(), elapsedMs = _lastMs > 0 ? System.Math.Min(nowMs - _lastMs, 100) : 0;
        _lastMs = nowMs;
        foreach (var (at, way, look) in _streaks)
        {
            if (look.Bomb is { } bomb) _bombs.Add((at, way.Angle(), bomb));
            if (look.Rocket is { } rocket) Rocket(at, way, rocket, nowMs, elapsedMs);
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
        _flames.QueueRedraw();
    }

    /// <summary>
    /// A rocket at <paramref name="at"/> flying along <paramref name="way"/>: kept for its picture and its flame, its light held at its
    /// nozzle, flickering with the flame, and its smoke left behind for the <paramref name="elapsedMs"/> since the last frame.
    /// </summary>
    private void Rocket(Vector2 at, Vector2 way, ShotRocket rocket, double nowMs, double elapsedMs)
    {
        var ahead = way.Normalized();
        float flicker = 0.75f + 0.25f * (Jitter((int)(nowMs / rocket.FlickerMs), 7) + 0.5f);
        var nozzle = ToGlobal(at - ahead * (float)rocket.Nozzle);
        var stage = Stages.Of(Layers.Effects);
        if (_rockets.Count == _rocketLights.Count) _rocketLights.Add(new object());
        stage.Flares.Hold(_rocketLights[_rockets.Count], nozzle.X, nozzle.Y, rocket.Light, rocket.Strength * flicker * Fx.Glow, rocket.Reach, 0.5f, 0.6);
        stage.Flares.Bloom(nozzle.X, nozzle.Y, 18 * flicker, rocket.Light, 0.36 * flicker * Fx.Glow);
        var glims = stage.Glims;
        double due = elapsedMs / rocket.SmokeMs;
        int puffs = (int)due + (glims.Between(0, 1) < due - System.Math.Floor(due) ? 1 : 0);
        for (int i = 0; i < puffs; i++)
        {
            double push = rocket.Push * glims.Between(0.3, 1), turn = glims.Between(0, Mathf.Tau), spread = rocket.Spread * glims.Between(0, 1);
            glims.Add(rocket.Smoke, nozzle.X + glims.Between(-1, 1), nozzle.Y + glims.Between(-1, 1),
                -ahead.X * push + System.Math.Cos(turn) * spread, -ahead.Y * push + System.Math.Sin(turn) * spread, size: rocket.Smoke.Size * glims.Between(0.8, 1.2));
        }
        _rockets.Add((at, way.Angle(), ahead.X < 0, rocket, flicker));
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
        _bodies = new Bodies { Thrown = _thrown, Bombs = _bombs, Rockets = _rockets, ShowBehindParent = true };
        _flames = new Flames { Rockets = _rockets };
        AddChild(_flames);
        // Lit as what is held is: with no material of its own it drew black in the dark of the caves.
        Lit.Set(_bodies);
        AddChild(_bodies);
    }

    public override void _Draw()
    {
        foreach (var (at, way, look) in _streaks)
        {
            if (look.Rocket != null) continue;
            if (look.Picture != null)
            {
                Sparks(at, way, look);
                continue;
            }
            if (look.Bomb is { } bomb)
            {
                Trail(at, way, look, bomb);
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

    /// <summary>A bomb's trail: a few specks behind its fins, each jittered across its flight anew thirty times a second, fading back.</summary>
    private void Trail(Vector2 at, Vector2 way, ShotLook look, ShotBomb bomb)
    {
        var back = -way.Normalized();
        int roll = (int)(Time.GetTicksMsec() / 33);
        float r = (float)look.Width / 2;
        var point = new Color(look.Point);
        for (int i = 1; i <= bomb.Sparks; i++)
        {
            var p = at + back * (float)(bomb.First + (i - 1) * bomb.Gap) + new Vector2(Jitter(i, roll) * 0.6f, 0);
            DrawCircle(p, r, point with { A = 0.7f * (1 - i / (bomb.Sparks + 1f)) });
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
        public List<(Vector2 At, float Turn, ShotBomb Bomb)> Bombs = [];
        public List<(Vector2 At, float Turn, bool Flip, ShotRocket Rocket, float Flicker)> Rockets = [];

        /// <summary>A round body's outline, a unit across, turned along x: scaled to a bomb's.</summary>
        private static readonly Vector2[] Round = MakeRound(12);

        private readonly Vector2[] _body = new Vector2[Round.Length], _rim = new Vector2[Round.Length + 1];

        public override void _Draw()
        {
            foreach (var (at, turn, picture, _) in Thrown)
            {
                DrawSetTransform(at, turn);
                DrawTexture(PictureOf(picture.Still), -new Vector2((float)picture.Middle.X, (float)picture.Middle.Y));
            }
            // A bomb point first along its flight: its body, its rim, two fins splayed behind.
            foreach (var (at, turn, bomb) in Bombs)
            {
                DrawSetTransform(at, turn);
                var size = new Vector2((float)bomb.Length / 2, (float)bomb.Width / 2);
                for (int i = 0; i < Round.Length; i++) _body[i] = _rim[i] = Round[i] * size;
                _rim[^1] = _rim[0];
                DrawColoredPolygon(_body, new Color(bomb.Fill));
                DrawPolyline(_rim, new Color(bomb.Rim), 0.3f, true);
                var fins = new Color(bomb.Fins);
                for (int s = -1; s <= 1; s += 2)
                    DrawLine(new Vector2(-1.2f, 0), new Vector2(-2.2f, 0.9f * s), fins, 0.35f, true);
            }
            // A rocket nose first, flipped flying left so its top stays up.
            foreach (var (at, turn, flip, rocket, _) in Rockets)
            {
                DrawSetTransform(at, turn, new Vector2(1, flip ? -1 : 1));
                DrawTexture(PictureOf(rocket.Still), -new Vector2((float)rocket.Middle.X, (float)rocket.Middle.Y));
            }
            DrawSetTransform(Vector2.Zero);
        }

        private static Vector2[] MakeRound(int n)
        {
            var points = new Vector2[n];
            for (int i = 0; i < n; i++) points[i] = Vector2.FromAngle(Mathf.Tau * i / n);
            return points;
        }
    }

    /// <summary>
    /// The rockets' flames, added over the world: out of each nozzle a torn orange tongue with a pale heart, as long and as wide as its
    /// flicker says, as the pick drew it.
    /// </summary>
    private sealed partial class Flames : Node2D
    {
        public List<(Vector2 At, float Turn, bool Flip, ShotRocket Rocket, float Flicker)> Rockets = [];

        private static readonly Color Orange = new(1, 120 / 255f, 40 / 255f, 0.75f), Pale = new(1, 236 / 255f, 190 / 255f, 0.95f);
        /// <summary>The points along each side of a flame, nozzle to tip.</summary>
        private const int Steps = 6;
        private readonly Vector2[] _outline = new Vector2[2 * Steps];

        public Flames()
        {
            Material = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Add, LightMode = CanvasItemMaterial.LightModeEnum.Unshaded };
        }

        public override void _Draw()
        {
            foreach (var (at, turn, _, rocket, flicker) in Rockets)
            {
                DrawSetTransform(at, turn);
                float nozzle = -(float)rocket.Nozzle, length = (float)rocket.Flame * (0.6f + 0.4f * flicker), width = (float)rocket.FlameWidth * flicker;
                Tongue(nozzle, length, width, Orange);
                Tongue(nozzle, length * 0.5f, width * 0.53f, Pale);
            }
            DrawSetTransform(Vector2.Zero);
        }

        /// <summary>
        /// A flame from the nozzle at x <paramref name="nozzle"/> back <paramref name="length"/>: each side a curve bent toward a point
        /// <paramref name="width"/> off its middle, two fifths of its way back.
        /// </summary>
        private void Tongue(float nozzle, float length, float width, Color colour)
        {
            var n = new Vector2(nozzle, 0);
            var tip = new Vector2(nozzle - length, 0);
            for (int side = 0; side < 2; side++)
            {
                var control = new Vector2(nozzle - length * 0.4f, side == 0 ? width : -width);
                for (int i = 0; i < Steps; i++)
                {
                    float t = side == 0 ? i / (float)Steps : 1 - i / (float)Steps;
                    _outline[side * Steps + i] = n.Lerp(control, t).Lerp(control.Lerp(tip, t), t);
                }
            }
            DrawColoredPolygon(_outline, colour);
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
