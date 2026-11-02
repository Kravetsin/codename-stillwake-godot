using System;
using Godot;
using Stillwake.Game.Render;

namespace Stillwake.Game.Features.Research;

/// <summary>Where a hero stands as drawn this frame, for what hangs over its head: its helmet, its feet, and whether it is afloat.</summary>
public readonly record struct HeadSpot(Vector2 Head, Vector2 Feet, bool Afloat);

/// <summary>
/// The bulb over this player's hero, seen by no one else (<see cref="BulbSignal"/>): the glass and the cap lit as the world is, the filament, the flash and what flies shining
/// by themselves. The bulb goes with the hero; what it threw stays where it burst. Pulled far back it keeps a size that reads, for its whole use is to be seen.
/// </summary>
public partial class HeroBulb : Node2D
{
    private const float PictureW = 5, PictureH = 6, Flash = 3.75f, FlashUp = 1.5f, ShardsUp = 1.5f;
    /// <summary>How far above the helmet's joint its cap hangs, and how far its middle is above the cap at its own size.</summary>
    private const float OverHelmet = 17, CapBelow = 5;
    /// <summary>The camera's zoom below which it stops shrinking.</summary>
    private const float ReadsAt = 1.4f;
    /// <summary>How fast its height follows the head's: it takes only the walk, or the bob jitters at this size.</summary>
    private const float Follows = 14;
    private const float LightOf = 1.2f, FlashLight = 2.4f;
    private static readonly Color AmberLight = new(0.95f, 0.72f, 0.29f), Glass = new("aeb8c8"), GlassBright = new("e4ebf5"), SmokeInk = new("8f96a3"), SparkHot = new("ffe6a8"), SparkWarm = new("f2b84b"), SparkCold = new("8a3c17");

    private readonly BulbSignal _signal;
    private readonly Func<HeadSpot?> _spot;
    private readonly Part _lit = new() { ZIndex = Layers.Glows }, _shines = new() { ZIndex = Layers.Glows };
    private readonly Glow _glow = new() { Visible = false };
    private Vector2 _at;
    private HeadSpot _last;
    private bool _hanging;
    private Vector2? _burst;
    private float _size = 1;

    public HeroBulb(BulbSignal signal, Func<HeadSpot?> spot)
    {
        (_signal, _spot) = (signal, spot);
        Lit.Set(_lit);
        _shines.Material = new CanvasItemMaterial { LightMode = CanvasItemMaterial.LightModeEnum.Unshaded };
        _lit.Paint = PaintLit;
        _shines.Paint = PaintShining;
        AddChild(_lit);
        AddChild(_shines);
        AddChild(_glow);
    }

    public override void _Process(double delta)
    {
        bool played = _signal.Time != null;
        _signal.Step(delta);
        if (_signal.Time is not { } t)
        {
            _glow.Visible = _hanging = false;
            _burst = null;
            if (played) Redraw();
            return;
        }
        // A hero out of sight, in a pod, shows nothing of it; the time still runs, for the key's lamp says the rest.
        var spot = _spot();
        _lit.Visible = _shines.Visible = spot != null;
        if (spot is not { } s)
        {
            _glow.Visible = false;
            return;
        }
        _last = s;
        _size = Mathf.Max(1, ReadsAt / GetViewport().CanvasTransform.Scale.X);
        float over = s.Head.Y - OverHelmet - CapBelow * _size;
        _at = new Vector2(s.Head.X, _hanging ? Mathf.Lerp(_at.Y, over, 1 - Mathf.Exp(-Follows * (float)delta)) : over);
        _hanging = true;
        double age = t - _signal.BurstsAt;
        if (age >= 0) _burst ??= _at;
        _glow.Position = _at;
        float strength = age >= 0 && age < BulbSignal.Flash ? FlashLight : (float)_signal.Heat(t) * LightOf;
        _glow.Visible = strength > 0.01f;
        _glow.Colour = AmberLight * strength;
        Redraw();
    }

    private void Redraw()
    {
        _lit.QueueRedraw();
        _shines.QueueRedraw();
    }

    private Vector2 Sized => Vector2.One * _size;

    private static Vector2 PictureCorner => new(-PictureW, -PictureH);

    private void PaintLit(Part part)
    {
        if (_signal.Time is not { } t || !_hanging) return;
        double age = t - _signal.BurstsAt;
        part.DrawSetTransform(_at, 0, Sized);
        if (age < 0)
        {
            string name = BulbSignal.DrawingOf(t) is { } drawing ? $"bulb_drawing_{drawing}" : "bulb_glass";
            part.DrawTexture(PixelArt.Picture(name), PictureCorner);
            return;
        }
        part.DrawTexture(PixelArt.Picture("bulb_stump"), PictureCorner, new Color(1, 1, 1, (float)BulbSignal.CapAlpha(age)));
        // Smoke needs air: afloat, the glass only flies.
        if (_last.Afloat || _burst is not { } burst || BulbSignal.SmokeAlpha(age) is var alpha && alpha <= 0) return;
        part.DrawSetTransform(burst, 0, Sized);
        var points = new Vector2[15];
        for (int i = 0; i < points.Length; i++)
        {
            var (x, y) = BulbSignal.Smoke(i / (double)(points.Length - 1), age);
            points[i] = new Vector2((float)x, (float)y);
        }
        part.DrawPolyline(points, SmokeInk with { A = (float)alpha }, 0.55f);
    }

    private void PaintShining(Part part)
    {
        if (_signal.Time is not { } t || !_hanging) return;
        double age = t - _signal.BurstsAt;
        part.DrawSetTransform(_at, 0, Sized);
        if (age < 0)
        {
            if (BulbSignal.FireOf(_signal.Heat(t)) is { } fire) part.DrawTexture(PixelArt.Picture($"bulb_fire_{fire}"), PictureCorner);
            return;
        }
        if (age < BulbSignal.Flash) part.DrawCircle(new Vector2(0, -FlashUp), Flash, Colors.White);
        if (_burst is not { } burst) return;
        var from = burst + new Vector2(0, -ShardsUp * _size);
        double floor = (_last.Feet.Y - from.Y) / _size;
        for (int i = 0; i < BulbSignal.Shards; i++)
        {
            if (BulbSignal.Shard(i, age, _last.Afloat, floor) is not { } shard) continue;
            var (x, y, turn, size, alpha, bright) = shard;
            part.DrawSetTransform(from + new Vector2((float)x, (float)y) * _size, (float)turn, Sized);
            float s = (float)size;
            part.DrawColoredPolygon([new Vector2(-s, -s * 0.4f), new Vector2(s, -s * 0.7f), new Vector2(s * 0.2f, s)], (bright ? GlassBright : Glass) with { A = (float)alpha });
        }
        part.DrawSetTransform(burst, 0, Sized);
        for (int i = 0; i < BulbSignal.Sparks; i++)
        {
            if (BulbSignal.Spark(i, age, _last.Afloat) is not { } spark) continue;
            var (x, y, dx, dy, cooled) = spark;
            var at = new Vector2((float)x, (float)y);
            part.DrawLine(at, at - new Vector2((float)dx, (float)dy) * 0.028f, cooled < 0.35 ? SparkHot : cooled < 0.7 ? SparkWarm : SparkCold, 0.7f);
        }
    }

    /// <summary>A layer of it, drawn through its own material.</summary>
    private sealed partial class Part : Node2D
    {
        public Action<Part>? Paint;

        public override void _Draw() => Paint?.Invoke(this);
    }
}
