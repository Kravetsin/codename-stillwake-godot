using System;
using System.Collections.Generic;
using Godot;
using Stillwake.Game.Render;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// A weapon effect skin drawn (skins, <see cref="BladeFx"/>): in the arms' frame, as the haze is (<see cref="ReachAir"/>), so a
/// figure facing left mirrors it, its points kept in the world. Three layers, unshaded: light added under, dark painted over
/// it, light added over that.
/// </summary>
public partial class BladeFxView : Node2D
{
    private readonly Layer _under = new() { Blend = CanvasItemMaterial.BlendModeEnum.Add }, _dark = new() { Blend = CanvasItemMaterial.BlendModeEnum.Mix },
        _over = new() { Blend = CanvasItemMaterial.BlendModeEnum.Add };
    private readonly Layer[] _layers;
    private readonly Func<(double X, double Y), (double X, double Y)> _local;
    private Transform2D _here;
    private BladeFx? _fx;

    public BladeFxView()
    {
        _local = Local;
        Visible = false;
        _layers = [_under, _dark, _over];
        foreach (var layer in _layers) AddChild(layer);
    }

    /// <summary>
    /// <paramref name="elapsedMs"/> on with the effect of <paramref name="look"/>, none for none (a new look starts a new effect);
    /// the blade as <paramref name="held"/>, none while none is; <paramref name="edge"/> what its trail follows, none while it leaves none.
    /// </summary>
    public void Follow(EffectLook? look, double elapsedMs, BladeFx.Held? held, IReadOnlyList<(double X, double Y)>? edge)
    {
        if (look != null && !ReferenceEquals(look, _fx?.Look)) _fx = BladeFx.Of(look);
        if (_fx == null) return;
        // With no look the effect left winds down: its trail and loose bits die out, nothing new.
        _fx.Step(elapsedMs, look != null ? held : null, look != null ? edge : null);
        Visible = _fx.Live;
        if (!Visible) return;
        _here = GetGlobalTransform().AffineInverse();
        float pixel = InkCanvas.PixelOf(this);
        foreach (var layer in _layers)
        {
            layer.Ink.Clear();
            layer.Ink.Feather = pixel;
        }
        _fx.Draw(_under.Ink, _dark.Ink, _over.Ink, _local);
        foreach (var layer in _layers) layer.QueueRedraw();
    }

    private (double X, double Y) Local((double X, double Y) p)
    {
        var l = _here * new Vector2((float)p.X, (float)p.Y);
        return (l.X, l.Y);
    }

    private sealed partial class Layer : Node2D
    {
        public readonly Ink Ink = new();
        private readonly InkCanvas _canvas = new();

        public CanvasItemMaterial.BlendModeEnum Blend
        {
            init => Material = new CanvasItemMaterial { BlendMode = value, LightMode = CanvasItemMaterial.LightModeEnum.Unshaded };
        }

        public override void _Draw() => _canvas.Draw(this, Ink);
    }
}
