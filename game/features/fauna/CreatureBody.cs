using System;
using Godot;
using Stillwake.Game.Render;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Features.Fauna;

/// <summary>
/// One creature's placeholder body (0102), drawn in its own pixels, its origin the middle of its feet, facing right: a soft
/// round body of its species' size on two stubby legs, a belly, ears, a tail tuft and one big eye that watches the hero near it.
/// Lit as the world is; its white flash an unlit copy over it. The art is the user's to pick later: mechanics first.
/// </summary>
public partial class CreatureBody : Node2D
{
    private static readonly Color Fur = new(0.80f, 0.63f, 0.45f), Belly = new(0.93f, 0.83f, 0.68f), Dark = new(0.30f, 0.21f, 0.16f);
    private static readonly Color White = new(0.97f, 0.97f, 0.93f), Pupil = new(0.10f, 0.08f, 0.08f);

    // Unshaded, so the world's darkness does not grey it: a flash is light.
    private readonly Flash _flash = new() { Material = new CanvasItemMaterial { LightMode = CanvasItemMaterial.LightModeEnum.Unshaded } };
    private float _w = 32, _h = 32;
    private Color _fur = Fur;
    private double _phase, _ears, _eye = 1, _white;
    private bool _grounded = true, _fleeing;
    private Vector2 _look;

    /// <summary>How far through its death it is, 0 to 1, swelling white and fading; null alive.</summary>
    public double? Dying { get; set; }

    public override void _Ready() => AddChild(_flash);

    /// <summary>Dressed as a creature of <paramref name="species"/>, its fur a shade its own by <paramref name="id"/>.</summary>
    public void Dress(string species, int id)
    {
        var spec = Spec.Fauna.Get(species);
        (_w, _h) = (spec.Width * ShipCamera.Cell, spec.Height * ShipCamera.Cell);
        float shade = (id * 37 % 11 - 5) * 0.012f;
        _fur = new Color(Fur.R + shade, Fur.G + shade * 0.6f, Fur.B - shade * 0.4f);
        (Dying, Modulate) = (null, Colors.White);
    }

    /// <summary>Posed by its motion this frame, its eye on <paramref name="toHero"/> (pixels from the eye, in the world's way) or ahead.</summary>
    public void Pose(CreatureMotion motion, bool fleeing, bool grounded, Vector2? toHero)
    {
        (_phase, _ears, _eye, _white, _grounded, _fleeing) = (motion.Phase, motion.Ears(fleeing), motion.Eye, motion.Flash, grounded, fleeing);
        // In the body's own way: mirrored with it.
        _look = toHero is { } h ? new Vector2(h.X * motion.Facing, h.Y).Normalized() : new Vector2(1, 0.15f);
        QueueRedraw();
        _flash.QueueRedraw();
    }

    public override void _Draw()
    {
        float alpha = Dying is double d ? (float)(1 - d) : 1;
        Shape(this, _fur with { A = alpha }, Belly with { A = alpha }, Dark with { A = alpha });
        Eye(alpha);
    }

    /// <summary>The body's silhouette in <paramref name="fur"/>, its belly and its dark tuft and leg ends, on <paramref name="on"/>.</summary>
    private void Shape(CanvasItem on, Color fur, Color belly, Color dark)
    {
        float rx = _w * 0.44f, ry = _h * 0.34f, cy = -_h * 0.5f;
        // Legs: swinging opposite each other by the stride walking, tucked under in the air.
        float swing = _grounded ? (float)Math.Sin(_phase * Math.Tau) * _w * 0.08f : 0;
        float lift = _grounded ? 0 : _h * 0.08f;
        foreach (var (x, s) in new[] { (-_w * 0.2f, swing), (_w * 0.18f, -swing) })
        {
            float up = _grounded ? Math.Max(0, (float)-Math.Cos(_phase * Math.Tau + (s > 0 ? 0 : Math.PI))) * _h * 0.05f : lift;
            on.DrawRect(new Rect2(x + s - _w * 0.05f, -_h * 0.22f - up, _w * 0.1f, _h * 0.22f), fur);
            on.DrawRect(new Rect2(x + s - _w * 0.06f, -_h * 0.06f - up, _w * 0.12f, _h * 0.06f), dark);
        }
        // The tail's tuft behind, the ears on top: up, laid back when startled or fleeing.
        on.DrawCircle(new Vector2(-rx * 0.98f, cy - ry * 0.2f), _w * 0.09f, dark);
        float back = (float)_ears;
        foreach (var x in new[] { rx * 0.15f, rx * 0.5f })
        {
            var root = new Vector2(x, cy - ry * 0.8f);
            float angle = (float)(-Math.PI / 2 - back * 1.15 - (x > rx * 0.3f ? 0.15 : -0.1));
            var tip = root + new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * _h * 0.3f;
            var across = new Vector2((float)-Math.Sin(angle), (float)Math.Cos(angle)) * _w * 0.06f;
            on.DrawColoredPolygon([root - across, tip, root + across], fur);
            on.DrawCircle(tip, _w * 0.035f, dark);
        }
        on.DrawColoredPolygon(Ellipse(new Vector2(0, cy), rx, ry), fur);
        on.DrawColoredPolygon(Ellipse(new Vector2(rx * 0.1f, cy + ry * 0.4f), rx * 0.7f, ry * 0.5f), belly);
    }

    /// <summary>Its eye, front and high: wide when startled, shut blinking, its pupil on the hero it watches.</summary>
    private void Eye(float alpha)
    {
        var at = new Vector2(_w * 0.2f, -_h * 0.6f);
        float r = _w * 0.11f * (float)Math.Max(0.35, _eye);
        if (_eye < 0.3)
        {
            DrawLine(at - new Vector2(r, 0), at + new Vector2(r, 0), Pupil with { A = alpha }, 1.2f);
            return;
        }
        DrawColoredPolygon(Ellipse(at, r, r * (float)Math.Min(1.15, _eye)), White with { A = alpha });
        float p = r * (_eye > 1.2 ? 0.38f : 0.55f);
        DrawCircle(at + _look * (r - p) * 0.8f, p, Pupil with { A = alpha });
        DrawCircle(at + _look * (r - p) * 0.8f + new Vector2(-p * 0.35f, -p * 0.4f), p * 0.3f, White with { A = alpha });
    }

    private static Vector2[] Ellipse(Vector2 at, float rx, float ry, int points = 18)
    {
        var all = new Vector2[points];
        for (int i = 0; i < points; i++)
        {
            double a = i * Math.Tau / points;
            all[i] = at + new Vector2((float)Math.Cos(a) * rx, (float)Math.Sin(a) * ry);
        }
        return all;
    }

    /// <summary>The white flash over the body: the same silhouette unlit, as bright as the blow's.</summary>
    private sealed partial class Flash : Node2D
    {
        public override void _Draw()
        {
            var body = (CreatureBody)GetParent();
            float white = (float)Math.Max(body._white, body.Dying is double d ? Math.Min(1, 1.6 * (1 - d)) * (1 - d) : 0);
            if (white <= 0.01f) return;
            var c = new Color(1, 1, 1, white);
            body.Shape(this, c, c, c);
        }
    }
}
