using System;
using Godot;
using Stillwake.Game.Render;

namespace Stillwake.Game.Features.Debris;

/// <summary>
/// A wreck breaking apart (0075): its picture cut into <see cref="Wedges"/> pieces that fly out from its middle,
/// tumbling, lit as it was, their torn edges glowing orange and cooling over a second, each trailing a few
/// embers while young, and fading out rather than blinking. Frees itself once the last piece is gone.
/// </summary>
public partial class BreakView : Node2D
{
    private const int Wedges = 6;
    /// <summary>How long the torn edges glow, and a piece's life: fading over its last <see cref="FadeMs"/>.</summary>
    private const double HotMs = 1000, FadeMs = 25 * 1000.0 / 60, ShortestMs = 70 * 1000.0 / 60, LongestMs = 100 * 1000.0 / 60;
    /// <summary>How long a piece trails embers, and how often.</summary>
    private const double TrailMs = 32 * 1000.0 / 60, TrailEveryMs = 3 * 1000.0 / 60;
    private static readonly (int R, int G, int B)[] Cooling = [(255, 240, 200), (255, 150, 60), (200, 60, 30)];

    private sealed class Piece(Polygon2D body, Vector2 velocity, float spin, double lifeMs, float from, float to)
    {
        public Polygon2D Body { get; } = body;
        public Vector2 Velocity { get; } = velocity;
        public float Spin { get; } = spin;
        public double LifeMs { get; } = lifeMs;
        /// <summary>The angles of its two torn edges, from the wreck's middle.</summary>
        public float From { get; } = from;
        public float To { get; } = to;
        public double TrailMs;
    }

    private readonly Piece[] _pieces = new Piece[Wedges];
    private readonly Node2D _edges = new()
    {
        Material = new CanvasItemMaterial
        {
            BlendMode = CanvasItemMaterial.BlendModeEnum.Add,
            LightMode = CanvasItemMaterial.LightModeEnum.Unshaded,
        },
        TextureFilter = TextureFilterEnum.Linear,
    };
    private readonly Vector2 _half;
    private double _ageMs;
    private float _zoom = 1;
    private readonly Random _random = new();
    private readonly Vector2[] _edge = new Vector2[3];
    private readonly Color[] _edgeColours = new Color[3];

    /// <summary>
    /// <paramref name="picture"/>, a wreck's, at <paramref name="at"/> turned <paramref name="turn"/> and scaled
    /// <paramref name="scale"/>, broken: cut into wedges round its middle, clipped to the picture, flying apart.
    /// </summary>
    public BreakView(Texture2D picture, Vector2 at, float turn, float scale)
    {
        var size = picture.GetSize();
        _half = size / 2;
        float reach = size.Length();
        var rect = new[] { -_half, new Vector2(_half.X, -_half.Y), _half, new Vector2(-_half.X, _half.Y) };
        for (int i = 0; i < Wedges; i++)
        {
            float a0 = i * Mathf.Tau / Wedges + 0.3f, a1 = (i + 1) * Mathf.Tau / Wedges + 0.3f, am = (a0 + a1) / 2;
            var wedge = new[] { Vector2.Zero, Vector2.FromAngle(a0) * reach, Vector2.FromAngle(a1) * reach };
            var cut = Geometry2D.IntersectPolygons(wedge, rect);
            var polygon = cut.Count > 0 ? cut[0] : wedge;
            var uv = new Vector2[polygon.Length];
            for (int k = 0; k < polygon.Length; k++) uv[k] = polygon[k] + _half;
            var body = new Polygon2D { Texture = picture, Polygon = polygon, UV = uv, Position = at, Rotation = turn, Scale = Vector2.One * scale };
            Lit.Set(body);
            AddChild(body);
            double speed = Between(22, 42);
            var v = Vector2.FromAngle(am).Rotated(turn) * (float)speed + new Vector2(0, -4);
            _pieces[i] = new Piece(body, v, (float)Between(-3.5, 3.5), Between(ShortestMs, LongestMs), a0, a1);
        }
        AddChild(_edges);
        _edges.Draw += DrawEdges;
    }

    private double Between(double from, double to) => from + (to - from) * _random.NextDouble();

    public override void _Process(double delta)
    {
        double ms = delta * 1000, dt = delta;
        _zoom = Math.Max(0.01f, GetViewport().GetCanvasTransform().X.Length());
        _ageMs += ms;
        bool any = false;
        foreach (var p in _pieces)
        {
            var body = p.Body;
            body.Position += p.Velocity * (float)dt;
            body.Rotation += p.Spin * (float)dt;
            body.Modulate = Colors.White with { A = (float)Math.Clamp((p.LifeMs - _ageMs) / FadeMs, 0, 1) };
            body.Visible = _ageMs < p.LifeMs;
            any |= body.Visible;
            if (_ageMs >= TrailMs || !body.Visible) continue;
            p.TrailMs += ms;
            for (; p.TrailMs >= TrailEveryMs; p.TrailMs -= TrailEveryMs)
                Glims.Shared.Add(Luminous.ShardEmber, body.Position.X + Between(-1, 1), body.Position.Y + Between(-1, 1),
                    -p.Velocity.X * 0.3 + Between(-6, 6), -p.Velocity.Y * 0.3 + Between(-6, 6));
        }
        if (!any)
        {
            QueueFree();
            return;
        }
        _edges.QueueRedraw();
    }

    /// <summary>Each piece's two torn edges, glowing from its point out, cooling from white-orange to red, and a glow on its point.</summary>
    private void DrawEdges()
    {
        double heat = 1 - _ageMs / HotMs;
        if (heat <= 0) return;
        var rgb = LuminousMath.RampAt(Cooling, 1 - heat);
        var colour = new Color((float)rgb.R / 255, (float)rgb.G / 255, (float)rgb.B / 255);
        foreach (var p in _pieces)
        {
            var body = p.Body;
            if (!body.Visible) continue;
            float a = (float)heat * body.Modulate.A;
            var xf = body.Transform;
            foreach (float angle in (ReadOnlySpan<float>)[p.From, p.To])
            {
                var end = new Vector2(Mathf.Cos(angle) * _half.X * 0.85f, Mathf.Sin(angle) * _half.Y * 0.85f);
                (_edge[0], _edge[1], _edge[2]) = (xf * Vector2.Zero, xf * (end * 0.7f), xf * end);
                (_edgeColours[0], _edgeColours[1], _edgeColours[2]) = (colour with { A = 0.95f * a }, colour with { A = 0.6f * a }, colour with { A = 0 });
                _edges.DrawPolylineColors(_edge, _edgeColours, Math.Max(0.4f, 1.1f / _zoom), true);
            }
            const float glow = 5;
            var at = xf * Vector2.Zero;
            _edges.DrawTextureRect(PixelArt.Glow, new Rect2(at - Vector2.One * glow / 2, Vector2.One * glow), false,
                new Color(1, 140 / 255f, 60 / 255f, 0.35f * a * (float)Fx.Glow));
        }
    }
}
