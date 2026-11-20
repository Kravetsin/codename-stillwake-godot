using System;
using Godot;
using Stillwake.Game.Features.Crew;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;

namespace Stillwake.Game.Menu;

/// <summary>
/// The title's scene (<see cref="TitleScene"/>), drawn here as the pick page draws it, none of it the game's world: two layers
/// of stars going by to the left, a small far sun in its soft light (<see cref="Dither.Sun"/>), and beside it a hero hanging limp
/// on a tether that runs off the frame's foot, turning slowly. The hero is drawn whole in its own buffer, then laid on dark,
/// with a step of warm light and a rim on the sun's side; a glint on its visor, a tiny red lamp blinking on its chest.
/// </summary>
public partial class TitleBackdrop : CanvasLayer
{
    private static readonly Color Space = new("03050a"), Star = Rgb(223, 232, 245), Ink = new("16171b");

    // The light against the sun: the body dimmed into shade, a lit step and a rim cut on the sun's side, a glint, the tether.
    private static readonly Color Shade = Rgb(8, 8, 11), Lit = Rgb(255, 214, 160), Rim = Rgb(255, 236, 204), Glint = Rgb(255, 226, 190) with { A = 0.7f };
    private static readonly Color Rope = Rgb(14, 15, 19), RopeLit = Rgb(255, 226, 184), Red = Rgb(255, 46, 34), RedOff = Rgb(92, 22, 18) with { A = 0.8f };
    private const float Dim = 0.9f, LitA = 0.3f, LitD = 2.6f, RimA = 1, RimD = 1.3f;

    // The basic suit's tones, the middle of each of the game's own pictures: worn ochre, a brass shell.
    private static readonly Color Torso = new("765542"), Plate = new("817256"), Belt = new("473c31"), ArmC = new("7d5b46"), Glove = new("5c5044"),
        LegC = new("6a5e44"), Boot = new("413830"), Pack = new("4f4436"), Helm = new("99825a"), Collar = new("44413d"), Visor = new("14171c");

    private static readonly Shader HeroShader = new()
    {
        Code = """
            shader_type canvas_item;
            render_mode blend_premul_alpha;
            // The hero's buffer (premultiplied) dimmed into shade, then two steps of light over it: the body less itself moved
            // toward the light, a flat colour; all laid over what is under it at once.
            uniform vec2 light;
            uniform float box;
            uniform vec3 shade; uniform float dim;
            uniform vec3 lit; uniform float lit_a; uniform float lit_d;
            uniform vec3 rim; uniform float rim_a; uniform float rim_d;
            void fragment() {
                vec4 body = texture(TEXTURE, UV);
                float a = body.a;
                vec3 c = body.rgb * (1.0 - dim) + shade * dim * a;
                float l = lit_a * a * (1.0 - texture(TEXTURE, UV + light * lit_d / box).a);
                c = lit * l + c * (1.0 - l); a = l + a * (1.0 - l);
                float r = rim_a * body.a * (1.0 - texture(TEXTURE, UV + light * rim_d / box).a);
                c = rim * r + c * (1.0 - r); a = r + a * (1.0 - r);
                COLOR = vec4(c, a);
            }
            """,
    };

    private readonly Node2D _scene = new() { TextureFilter = CanvasItem.TextureFilterEnum.Linear };
    private readonly Sprite2D _far = Stars(), _near = Stars();
    private readonly Node2D _rope = new(), _marks = new(), _body = new();
    private readonly SubViewport _heroView = new()
    {
        TransparentBg = true,
        RenderTargetUpdateMode = SubViewport.UpdateMode.Always,
        Msaa2D = Viewport.Msaa.Msaa4X,
    };
    private readonly Sprite2D _hero = new() { Centered = false };
    private readonly ShaderMaterial _light = new() { Shader = HeroShader };
    private readonly TitleTether _tether = new();
    private readonly Vec[] _smooth = new Vec[TitleTether.Smoothed];
    private readonly Vector2[] _line = new Vector2[TitleTether.Smoothed], _edge = new Vector2[5];
    private double _seconds, _scale;
    private Drift _at = TitleScene.Motion(0);
    private TitleBody _pose = TitleHero.At(0);

    public TitleBackdrop()
    {
        Layer = ScreenLayers.TitleScene;
        var space = new ColorRect { Color = Space, MouseFilter = Control.MouseFilterEnum.Ignore };
        space.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(space);
        AddChild(_heroView);
        _heroView.AddChild(_body);
        AddChild(_scene);
        _scene.AddChild(_far);
        _scene.AddChild(_near);
        _scene.AddChild(Dither.Sun());
        _scene.AddChild(_rope);
        _scene.AddChild(_hero);
        _scene.AddChild(_marks);
        _hero.Material = _light;
        _light.SetShaderParameter("box", (float)TitleScene.HeroBox);
        _light.SetShaderParameter("shade", Rgb3(Shade));
        _light.SetShaderParameter("dim", Dim);
        _light.SetShaderParameter("lit", Rgb3(Lit));
        _light.SetShaderParameter("lit_a", LitA);
        _light.SetShaderParameter("lit_d", LitD);
        _light.SetShaderParameter("rim", Rgb3(Rim));
        _light.SetShaderParameter("rim_a", RimA);
        _light.SetShaderParameter("rim_d", RimD);
        _rope.Draw += DrawRope;
        _marks.Draw += DrawMarks;
        _body.Draw += DrawBody;
    }

    public override void _Ready() => _hero.Texture = _heroView.GetTexture();

    public override void _Process(double delta)
    {
        _seconds += delta;
        var screen = GetViewport().GetVisibleRect().Size;
        double scale = Math.Max(screen.X / TitleScene.Wide, screen.Y / TitleScene.High);
        _scene.Scale = Vector2.One * (float)scale;
        _scene.Position = (screen - new Vector2(TitleScene.Wide, TitleScene.High) * (float)scale) / 2;
        if (Size(TitleScene.Wide * scale) != Size(TitleScene.Wide * _scale) || Size(TitleScene.High * scale) != Size(TitleScene.High * _scale)) Rescale(scale);

        Slide(_far, TitleScene.FarDrift);
        Slide(_near, TitleScene.NearDrift);
        _tether.RunTo(_seconds);
        _at = TitleScene.Motion(_seconds);
        _pose = TitleHero.At(_seconds);
        float box = (float)TitleScene.HeroBox;
        _hero.Position = V(_at.At) - new Vector2(box, box) / 2;
        _light.SetShaderParameter("light", V(TitleScene.LightAt(_at.At)));
        _rope.QueueRedraw();
        _marks.QueueRedraw();
        _body.QueueRedraw();
    }

    /// <summary>Makes the stars and the hero's buffer again for a scene pixel <paramref name="scale"/> screen pixels.</summary>
    private void Rescale(double scale)
    {
        _scale = scale;
        int w = Size(TitleScene.Wide * scale), h = Size(TitleScene.High * scale);
        _far.Texture = StarTexture(TitleStars.Far(w, h, scale), w, h);
        _near.Texture = StarTexture(TitleStars.Near(w, h, scale), w, h);
        _far.Scale = _near.Scale = Vector2.One / (float)scale;
        int box = Size(TitleScene.HeroBox * scale);
        _heroView.Size = new Vector2I(box, box);
        _hero.Scale = Vector2.One * (float)(TitleScene.HeroBox / box);
    }

    /// <summary>A layer of stars moved on to the left, its texture wrapping round.</summary>
    private void Slide(Sprite2D layer, double speed)
    {
        if (layer.Texture == null) return;
        float w = layer.Texture.GetWidth();
        float o = (float)((_seconds * speed * _scale) % w);
        layer.RegionRect = new Rect2(o, 0, w, layer.Texture.GetHeight());
    }

    /// <summary>The tether: a thin dark line, its edge toward the sun lit, dimming away from it.</summary>
    private void DrawRope()
    {
        _tether.Smooth(_smooth);
        for (int i = 0; i < _smooth.Length; i++) _line[i] = V(_smooth[i]);
        _rope.DrawPolyline(_line, Rope, 1.4f, true);
        for (int i = 0; i < _smooth.Length - 1; i += 4)
        {
            int j = Math.Min(_smooth.Length - 1, i + 4);
            var edge = j - i + 1 == _edge.Length ? _edge : new Vector2[j - i + 1];
            for (int k = i; k <= j; k++)
            {
                var light = TitleScene.LightAt(_smooth[k]);
                edge[k - i] = V(new Vec(_smooth[k].X + light.X * 0.6, _smooth[k].Y + light.Y * 0.6));
            }
            _rope.DrawPolyline(edge, RopeLit with { A = (float)TitleScene.EdgeAlpha(_smooth[i]) }, 0.55f, true);
        }
    }

    /// <summary>Over the hero: the glint on its visor toward the sun, and the suit's lamp, a hard dot and one step of light when on.</summary>
    private void DrawMarks()
    {
        var light = TitleScene.LightAt(_at.At);
        float la = (float)Math.Atan2(light.Y, light.X);
        _marks.DrawArc(V(TitleScene.ToScene(_at, _pose.Helm)), (float)(3.1 * TitleScene.HeroScale), la - 0.55f, la + 0.55f, 12, Glint, 0.9f, true);
        var lamp = V(TitleScene.ToScene(_at, TitleScene.Lamp));
        bool on = TitleScene.LampOn(_seconds);
        if (on) _marks.DrawCircle(lamp, 3, Red with { A = 0.2f }, true, -1, true);
        _marks.DrawRect(new Rect2(lamp - new Vector2(0.8f, 0.8f), new Vector2(1.6f, 1.6f)), on ? Red : RedOff);
    }

    /// <summary>The hero in its buffer, turned about the buffer's middle: legs, arms, pack, torso, collar, helmet and visor.</summary>
    private void DrawBody()
    {
        float k = _heroView.Size.X / (float)TitleScene.HeroBox;
        _body.DrawSetTransform(new Vector2(_heroView.Size.X, _heroView.Size.Y) / 2, (float)_at.Turn, Vector2.One * (float)(k * TitleScene.HeroScale));
        foreach (var leg in _pose.Legs)
        {
            Limb([V(leg[0]), V(leg[1]), V(leg[2])], (float)(Rig.Leg.Width * 0.9), LegC);
            Limb([V(leg[2]), V(leg[3])], 5, Boot);
        }
        foreach (var arm in _pose.Arms)
        {
            Limb([V(arm[0]), V(arm[1]), V(arm[2])], (float)(Rig.Arm.Width * 0.9), ArmC);
            _body.DrawCircle(V(arm[2]), 2.1f, Glove);
            _body.DrawArc(V(arm[2]), 2.1f, 0, Mathf.Tau, 16, Ink, 0.6f);
        }
        Rounded(-7.6f, -10.8f, 15.2f, 11.5f, 2.2f, Pack, true);
        Rounded(-6.2f, -10.4f, 12.4f, 16.6f, 3.2f, Torso, true);
        Rounded(-3.4f, -8, 6.8f, 4.4f, 0.9f, Plate, false);
        _body.DrawRect(new Rect2(-5.6f, 3.2f, 11.2f, 1.8f), Belt);
        _body.DrawColoredPolygon(Ellipse(new Vector2(0, (float)TitleHero.NeckY - 0.4f), 3.8f, 1.5f, 0), Collar);
        var helm = V(_pose.Helm);
        _body.DrawCircle(helm, (float)TitleHero.HelmR, Helm);
        _body.DrawArc(helm, (float)TitleHero.HelmR, 0, Mathf.Tau, 32, Ink, 0.65f);
        var visor = Ellipse(helm + new Vector2(0, 0.5f).Rotated((float)_pose.Tilt), 4.5f, 3.7f, (float)_pose.Tilt);
        _body.DrawColoredPolygon(visor, Visor);
        _body.DrawPolyline([.. visor, visor[0]], Collar, 0.7f);
    }

    /// <summary>A limb through its joints: an ink line a little wider, then its colour, both with round ends and bends.</summary>
    private void Limb(Vector2[] points, float width, Color color)
    {
        foreach (var (w, c) in new[] { (width + 1.1f, Ink), (width, color) })
        {
            for (int i = 0; i < points.Length - 1; i++) _body.DrawLine(points[i], points[i + 1], c, w);
            foreach (var p in points) _body.DrawCircle(p, w / 2, c);
        }
    }

    /// <summary>A rounded box filled, and outlined in ink when <paramref name="inked"/>.</summary>
    private void Rounded(float x, float y, float w, float h, float r, Color fill, bool inked)
    {
        const int steps = 6;
        var points = new Vector2[4 * (steps + 1)];
        var corners = new[] { (x + w - r, y + r, -Mathf.Pi / 2), (x + w - r, y + h - r, 0f), (x + r, y + h - r, Mathf.Pi / 2), (x + r, y + r, Mathf.Pi) };
        for (int c = 0; c < 4; c++)
            for (int i = 0; i <= steps; i++)
            {
                float a = corners[c].Item3 + i * Mathf.Pi / 2 / steps;
                points[c * (steps + 1) + i] = new Vector2(corners[c].Item1 + r * Mathf.Cos(a), corners[c].Item2 + r * Mathf.Sin(a));
            }
        _body.DrawColoredPolygon(points, fill);
        if (inked) _body.DrawPolyline([.. points, points[0]], Ink, 0.6f);
    }

    private static Vector2[] Ellipse(Vector2 middle, float rx, float ry, float turn)
    {
        var points = new Vector2[24];
        for (int i = 0; i < points.Length; i++)
        {
            float a = i * Mathf.Tau / points.Length;
            points[i] = middle + new Vector2(rx * Mathf.Cos(a), ry * Mathf.Sin(a)).Rotated(turn);
        }
        return points;
    }

    private static Sprite2D Stars() => new() { Centered = false, RegionEnabled = true, TextureRepeat = CanvasItem.TextureRepeatEnum.Enabled, Modulate = Star };

    private static ImageTexture StarTexture(byte[] alpha, int w, int h)
    {
        var data = new byte[alpha.Length * 2];
        for (int i = 0; i < alpha.Length; i++) (data[2 * i], data[2 * i + 1]) = (255, alpha[i]);
        return ImageTexture.CreateFromImage(Image.CreateFromData(w, h, false, Image.Format.La8, data));
    }

    private static int Size(double pixels) => Math.Max(1, (int)Math.Round(pixels));

    private static Vector2 V(Vec v) => new((float)v.X, (float)v.Y);

    private static Color Rgb(int r, int g, int b) => Color.Color8((byte)r, (byte)g, (byte)b);

    private static Vector3 Rgb3(Color c) => new(c.R, c.G, c.B);
}
