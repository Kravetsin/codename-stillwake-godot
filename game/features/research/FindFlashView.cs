using System;
using Godot;
using Stillwake.Game.Render;

namespace Stillwake.Game.Features.Research;

/// <summary>
/// The flash of a find (<see cref="FindFlash"/>, edge-flash), seen by this player only: amber light runs in along the top of the tablet's edge from both
/// ends to the research key, the whole edge lights as it lands, and the key flares, a ring of light spreading off it. Drawn additively, as the keys are.
/// </summary>
public partial class FindFlashView : CanvasLayer
{
    /// <summary>How long a front's bright head is, how far the ring spreads off the key, and how many pieces a trail is drawn in.</summary>
    private const float Head = 70, Spread = 14;
    private const int Pieces = 24;
    private static readonly Color Amber = new(255 / 255f, 168 / 255f, 60 / 255f), Hot = new(255 / 255f, 228 / 255f, 170 / 255f);

    private readonly FindFlash _flash;
    private readonly Func<bool> _inWorld;
    private readonly Func<Rect2> _art;
    private readonly Func<Rect2?> _key;
    private readonly Part _part;
    /// <summary>Whether anything was drawn last frame: what puts it out may be the research key, between two of its frames.</summary>
    private bool _drawn;

    /// <param name="flash">When it plays, read by the research key.</param>
    /// <param name="inWorld">Whether a world is played.</param>
    /// <param name="art">Where the tablet the keys ride lies on the screen, its top the edge the light runs along.</param>
    /// <param name="key">Where the research key is on the screen; null while it does not show.</param>
    public FindFlashView(FindFlash flash, Func<bool> inWorld, Func<Rect2> art, Func<Rect2?> key)
    {
        (_flash, _inWorld, _art, _key) = (flash, inWorld, art, key);
        Layer = ScreenLayers.TabletEdge;
        AddChild(_part = new Part(Paint) { Material = new ShaderMaterial { Shader = GD.Load<Shader>("res://shared/holo.gdshader") } });
    }

    public override void _Process(double delta)
    {
        _flash.Step(delta);
        Visible = _inWorld();
        bool on = _flash.Time != null;
        if (on || _drawn) _part.QueueRedraw();
        _drawn = on;
    }

    private void Paint(Part c)
    {
        if (_flash.Time == null) return;
        var art = _art();
        float y = art.Position.Y + 1, left = art.Position.X + 6, right = art.End.X - 6;
        var key = _key();
        float to = key is { } k ? k.GetCenter().X : (left + right) / 2;
        if (_flash.Wave is { } wave)
        {
            Front(c, left, to, (float)wave, y);
            Front(c, right, to, (float)wave, y);
            return;
        }
        float glow = (float)_flash.Glow;
        if (glow <= 0) return;
        // As it lands the whole edge lights, then fades.
        c.DrawLine(new Vector2(left, y), new Vector2(right, y), Amber with { A = 0.45f * glow * glow }, 2);
        c.DrawLine(new Vector2(left, y), new Vector2(right, y), Amber with { A = 0.12f * glow * glow }, 8);
        if (key is not { } at) return;
        float spread = Spread * (1 - glow);
        c.DrawRect(at.Grow(2 + spread), Amber with { A = 0.7f * glow }, false, 2);
        c.DrawRect(at.Grow(4 + spread * 1.6f), Amber with { A = 0.25f * glow }, false, 4);
        c.DrawRect(at, Hot with { A = 0.35f * glow * glow });
    }

    /// <summary>One front run <paramref name="wave"/> of the way from <paramref name="from"/> to <paramref name="to"/>: a trail lit behind it, a hot head.</summary>
    private static void Front(Part c, float from, float to, float wave, float y)
    {
        float at = from + (to - from) * wave, run = Math.Abs(at - from);
        if (run < 1) return;
        float dir = Math.Sign(to - from);
        for (int i = 0; i < Pieces; i++)
        {
            float a0 = i / (float)Pieces, a1 = (i + 1) / (float)Pieces;
            float x0 = from + dir * run * a0, x1 = from + dir * run * a1;
            // Brighter toward the head; the last stretch of it hot.
            float near = Mathf.Clamp(1 - (run - run * a1) / Head, 0, 1), alpha = 0.15f + 0.65f * near;
            c.DrawLine(new Vector2(x0, y), new Vector2(x1, y), (near > 0.6f ? Hot : Amber) with { A = alpha }, 2);
            c.DrawLine(new Vector2(x0, y), new Vector2(x1, y), Amber with { A = 0.25f * near }, 8);
        }
        c.DrawCircle(new Vector2(at, y), 5, Hot with { A = 0.6f });
        c.DrawCircle(new Vector2(at, y), 12, Amber with { A = 0.18f });
    }

    /// <summary>What it draws on, through the keys' additive material.</summary>
    private sealed partial class Part(Action<Part> paint) : Node2D
    {
        public override void _Draw() => paint(this);
    }
}
