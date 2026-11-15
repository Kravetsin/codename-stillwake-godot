using System;
using System.Collections.Generic;
using Godot;
using Stillwake.Game.Shared;

namespace Stillwake.Game.Features.Items;

/// <summary>
/// The trash's burst (trash-effect A) over its slot as a stack it held is destroyed: the ring flashes, steel sparks fly out
/// (<see cref="TrashSparks"/>). The trash's, not the item's: alike for every item. Drawn over the slots, taking no clicks.
/// </summary>
public partial class TrashBurst : Control
{
    // MarkLooks.Trash's steel: its core, rim and glow.
    private static readonly Color Core = new("f4f7fb"), Rim = new("b4c0ce"), Glow = new("6b7a8f");

    private readonly Random _random = new();
    private Control? _slot;
    private IReadOnlyList<Spark> _sparks = [];
    private double _ms = -1;

    public TrashBurst()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        SetAnchorsPreset(LayoutPreset.FullRect);
        SetProcess(false);
    }

    /// <summary>Bursts over <paramref name="slot"/>, as it lies when drawn; a burst still running starts again.</summary>
    public void Play(Control slot)
    {
        (_slot, _sparks, _ms) = (slot, TrashSparks.Make(_random), 0);
        SetProcess(true);
    }

    public override void _Process(double delta)
    {
        _ms += delta * 1000;
        if (_ms > TrashSparks.Ms || _slot == null || !IsInstanceValid(_slot) || !_slot.IsVisibleInTree())
        {
            (_slot, _ms) = (null, -1);
            SetProcess(false);
        }
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (_slot == null || _ms < 0) return;
        var box = _slot.GetGlobalRect();
        box.Position -= GetGlobalRect().Position;
        float side = box.Size.X, k = side / 40, ms = (float)_ms;
        float ring = TrashSparks.RingShare * (side - 2 * Slots.Inset);
        var centre = box.GetCenter();
        if (TrashSparks.Flash(ms) is > 0 and var flash)
        {
            DrawArc(centre, ring, 0, Mathf.Tau, 32, Glow with { A = 0.5f * flash }, 4 * k, true);
            DrawArc(centre, ring, 0, Mathf.Tau, 32, Core with { A = 0.9f * flash }, 1.5f * k, true);
        }
        foreach (var spark in _sparks)
        {
            if (TrashSparks.Lived(spark, ms) is not float share) continue;
            float a = 1 - share;
            var (hx, hy) = TrashSparks.At(spark, share, side, ring);
            var (tx, ty) = TrashSparks.At(spark, Math.Max(0, share - TrashSparks.Trail), side, ring);
            DrawLine(centre + new Vector2(tx, ty), centre + new Vector2(hx, hy), Rim with { A = 0.75f * a }, 1.2f * k, true);
            DrawCircle(centre + new Vector2(hx, hy), 0.9f * k, Core with { A = a });
        }
    }
}
