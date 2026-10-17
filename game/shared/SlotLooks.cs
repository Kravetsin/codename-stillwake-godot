using Godot;
using Stillwake.Game.Render;

namespace Stillwake.Game.Shared;

/// <summary>
/// How an item slot is painted by what it holds (0058), one definition for the items feature's slots and the
/// Fabricator's recipes: a tool's ground mixed with amber and its edge amber-ish, a worn item's the same in the
/// equipment's green, anything else plain grey. Under the pointer a tinted slot's edge goes full amber or green;
/// the chosen hotbar slot is edged 2 px near-white, which reads on amber too.
/// </summary>
public static class SlotLooks
{
    /// <summary>The equipment's and charge's green.</summary>
    public static readonly Color Green = new("7ee36b");

    private static readonly Color Ground = Style.Slate800 with { A = 0.9f };
    public static readonly Color ToolFill = Ground.Lerp(ItemCardView.Amber, 0.16f), ToolEdge = Style.Slate700.Lerp(ItemCardView.Amber, 0.6f);
    public static readonly Color WornFill = Ground.Lerp(Green, 0.16f), WornEdge = Style.Slate700.Lerp(Green, 0.6f);

    private const int Radius = 3;
    private static readonly StyleBoxFlat[] Boxes = Made();

    /// <summary>The box of a slot tinted <paramref name="tint"/>, under the pointer or not, the chosen hotbar slot or not.</summary>
    public static StyleBoxFlat Of(SlotTint tint, bool hovered, bool chosen) => Boxes[((int)tint * 2 + (hovered ? 1 : 0)) * 2 + (chosen ? 1 : 0)];

    private static StyleBoxFlat[] Made()
    {
        var boxes = new StyleBoxFlat[3 * 2 * 2];
        foreach (var tint in (SlotTint[])[SlotTint.Plain, SlotTint.Tool, SlotTint.Worn])
            foreach (bool hovered in (bool[])[false, true])
                foreach (bool chosen in (bool[])[false, true])
                {
                    var (fill, edge) = (tint, hovered) switch
                    {
                        (SlotTint.Tool, false) => (ToolFill, ToolEdge),
                        (SlotTint.Tool, true) => (ToolFill.Lightened(0.06f), ItemCardView.Amber),
                        (SlotTint.Worn, false) => (WornFill, WornEdge),
                        (SlotTint.Worn, true) => (WornFill.Lightened(0.06f), Green),
                        (_, false) => (Ground, Style.Slate600),
                        _ => (Style.Slate700, Style.Slate400),
                    };
                    var box = Style.Box(fill, chosen ? Style.Slate100 : edge, radius: Radius);
                    if (chosen) box.SetBorderWidthAll(2);
                    boxes[((int)tint * 2 + (hovered ? 1 : 0)) * 2 + (chosen ? 1 : 0)] = box;
                }
        return boxes;
    }
}
