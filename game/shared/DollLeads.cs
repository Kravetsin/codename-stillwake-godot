using System.Collections.Generic;
using Godot;

namespace Stillwake.Game.Shared;

/// <summary>
/// The doll's leader lines (gear-tabs, pick B): from each slot's edge to the part of the body it dresses, a dot at the body's end,
/// brighter while the slot holds something; the gear's in the screen's blue, the Appearance tab's in amber. Drawn under the slots.
/// </summary>
public partial class DollLeads : Control
{
    private static readonly Color Line = new(140 / 255f, 190 / 255f, 235 / 255f, 0.3f), LineFull = new(140 / 255f, 205 / 255f, 245 / 255f, 0.55f),
        Dot = new(160 / 255f, 205 / 255f, 240 / 255f, 0.65f);
    private static readonly Color AmberLine = new(233 / 255f, 168 / 255f, 76 / 255f, 0.32f), AmberFull = AmberLine with { A = 0.6f },
        AmberDot = AmberLine with { A = 0.75f };

    private readonly DollLayout _layout;
    private readonly IReadOnlyList<DollSpot> _spots;
    private readonly bool _amber;
    private readonly bool[] _full = new bool[System.Enum.GetValues<DollSpot>().Length];

    /// <param name="spots">The spots whose lines it draws.</param>
    /// <param name="amber">The Appearance tab's: amber.</param>
    public DollLeads(DollLayout layout, IReadOnlyList<DollSpot> spots, bool amber)
    {
        (_layout, _spots, _amber) = (layout, spots, amber);
        MouseFilter = MouseFilterEnum.Ignore;
        Size = new Vector2(layout.Width, layout.Height);
    }

    /// <summary>Whether <paramref name="spot"/>'s slot holds something: its line and dot brighter.</summary>
    public void Fill(DollSpot spot, bool full)
    {
        if (_full[(int)spot] == full) return;
        _full[(int)spot] = full;
        QueueRedraw();
    }

    public override void _Draw()
    {
        float side = _layout.Slot, dot = 1.8f * side / DollLayout.BaseSlot;
        foreach (var spot in _spots)
        {
            if (_layout.LeadTo(spot) is not var (tx, ty)) continue;
            var (x, y) = _layout.SlotAt(spot);
            var middle = new Vector2(x + side / 2, y + side / 2);
            var to = new Vector2(tx, ty);
            // From where the line leaves the slot, so none of it shows through the slot's own glass.
            var way = to - middle;
            float exit = Mathf.Min(way.X == 0 ? float.MaxValue : side / 2 / Mathf.Abs(way.X), way.Y == 0 ? float.MaxValue : side / 2 / Mathf.Abs(way.Y));
            bool full = _full[(int)spot];
            DrawLine(middle + way * exit, to, _amber ? (full ? AmberFull : AmberLine) : (full ? LineFull : Line), 1, true);
            DrawCircle(to, dot, _amber ? AmberDot : Dot, true, -1, true);
            if (full) DrawCircle(to, dot * 0.55f, Colors.White with { A = 0.5f }, true, -1, true);
        }
    }
}
