using Godot;

namespace Stillwake.Game.Features.Research;

/// <summary>
/// The hack's mark over the tablet's screen, placed by hand as picked ([market-decrypt](../../../docs/decisions/2026-11-02-market-decrypt.md)): a dim column the poison bled into.
/// At another window's size its place scales, its width does not.
/// </summary>
public partial class MarketFaults : Control
{
    /// <summary>The screen it was placed on.</summary>
    private static readonly Vector2 Placed = new(1204, 644);

    private const float ColumnX = 957, ColumnFrom = 40, ColumnTo = 636;

    public MarketFaults()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        Visible = false;
    }

    public override void _Draw()
    {
        var scale = Size / Placed;
        DrawRect(new Rect2(ColumnX * scale.X, ColumnFrom * scale.Y, 1, (ColumnTo - ColumnFrom) * scale.Y), MarketPage.Poison with { A = 0.16f });
    }
}
