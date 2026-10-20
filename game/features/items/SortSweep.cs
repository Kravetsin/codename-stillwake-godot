using System.Collections.Generic;
using Godot;
using Look = Stillwake.Game.WristLook;

namespace Stillwake.Game.Features.Items;

/// <summary>
/// A sort's light (0081): a bright line crossing the sorted grid left to right in <see cref="Look.SweepS"/>, a band of
/// the screen's blue behind it, fading once across. Drawn over the slots, taking no clicks.
/// </summary>
public partial class SortSweep : Control
{
    private static readonly Color Sky = new("38bdf8"), Line = new("e0f2fe");

    private readonly List<(Control Grid, double Age)> _running = [];

    public SortSweep()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        SetAnchorsPreset(LayoutPreset.FullRect);
    }

    /// <summary>Sends the light across <paramref name="grid"/>, as it lies when drawn.</summary>
    public void Run(Control grid) => _running.Add((grid, 0));

    public override void _Process(double delta)
    {
        if (_running.Count == 0) return;
        for (int i = _running.Count - 1; i >= 0; i--)
        {
            var (grid, age) = _running[i];
            age += delta;
            if (age > Look.SweepS + Look.SweepFadeS || !GodotObject.IsInstanceValid(grid) || !grid.IsVisibleInTree()) _running.RemoveAt(i);
            else _running[i] = (grid, age);
        }
        QueueRedraw();
    }

    public override void _Draw()
    {
        foreach (var (grid, age) in _running)
        {
            var box = grid.GetGlobalRect().Grow(2);
            box.Position -= GetGlobalRect().Position;
            double p = age / Look.SweepS;
            float x = box.Position.X + (float)System.Math.Min(1, p) * box.Size.X;
            float a = p < 1 ? 1 : (float)((Look.SweepS + Look.SweepFadeS - age) / Look.SweepFadeS);
            float from = System.Math.Max(box.Position.X, x - Look.SweepBand);
            if (x > from)
            {
                // The band brightens toward the line: a quad with its colours set per corner.
                float lead = (x - from) / Look.SweepBand * 0.16f * a, top = box.Position.Y, foot = box.End.Y;
                Vector2[] band = [new(from, top), new(x, top), new(x, foot), new(from, foot)];
                Color clear = Sky with { A = 0.16f * a - lead }, lit = Sky with { A = 0.16f * a };
                DrawPolygon(band, [clear, lit, lit, clear]);
            }
            float lineX = Mathf.Min(x, box.End.X - 1) - 1;
            DrawRect(new Rect2(lineX - 2, box.Position.Y, 6, box.Size.Y), Sky with { A = 0.25f * a });
            DrawRect(new Rect2(lineX, box.Position.Y, 2, box.Size.Y), Line with { A = 0.85f * a });
        }
    }
}
