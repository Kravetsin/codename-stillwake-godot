using Stillwake.Game.Shared;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>The wrist computer's metal laid round the HUD's panels at both slot sizes (0074).</summary>
public class WristPiecesTests
{
    /// <summary>The hotbar's and the bag's panels hold their slots 4 px in.</summary>
    private const int Padding = 4;

    public static TheoryData<SlotScale> Scales => new() { SlotScale.Normal, SlotScale.Large };

    [Fact]
    public void LaysTheArtRoundTheWindowAtTwoScreenPixelsAPixelRoundingAnOddSideUp()
    {
        var piece = WristPieces.Cuff;
        Assert.Equal((-8, -8, 200 + 8, 24 + 8), piece.Around(400, 48));
        // An odd side is rounded up: the bezel starts past the window, never over its last pixel.
        Assert.Equal((-8, -8, 201 + 8, 25 + 8), piece.Around(401, 49));
    }

    [Theory]
    [MemberData(nameof(Scales))]
    public void TheDisplaysBumpersClearTheCuff(SlotScale scale)
    {
        // The bag opens under the hotbar now the gauges have left it (hud-vitals): its bumpers stand clear of the cuff's outline.
        int slot = Slots.SizeOf(scale), hotbarFoot = Slots.Margin + slot + 2 * Padding;
        Assert.True(WristPieces.Cuff.OutlineBelow(hotbarFoot) <= Slots.InventoryTop(slot) - WristPiece.Scale * WristPieces.Display.Window.Top);
    }
}
