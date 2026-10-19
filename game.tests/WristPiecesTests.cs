using Stillwake.Game.Shared;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>The wrist computer's metal laid round the HUD's panels at both slot sizes (0074).</summary>
public class WristPiecesTests
{
    /// <summary>The hotbar's and the bag's panels hold their slots 4 px in; the open machine docks 8 px under the bag.</summary>
    private const int Padding = 4, DockGap = 8;

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
    public void TheGaugesTabsHangFromTheCuffSharingItsBottomOutline(SlotScale scale)
    {
        int slot = Slots.SizeOf(scale), hotbarFoot = Slots.Margin + slot + 2 * Padding;
        Assert.Equal(WristPieces.Cuff.OutlineBelow(hotbarFoot), WristPieces.Gauge.OutlineAbove((int)Slots.GaugeTop(slot)));
    }

    [Theory]
    [MemberData(nameof(Scales))]
    public void TheOpenMachinesModuleMeetsTheDisplaySharingItsBottomOutline(SlotScale scale)
    {
        int slot = Slots.SizeOf(scale), bagFoot = (int)Slots.InventoryTop(slot) + 3 * slot + 2 * Slots.Gap + 2 * Padding;
        Assert.Equal(WristPieces.DisplayDocked.OutlineBelow(bagFoot), WristPieces.Dock.OutlineAbove(bagFoot + DockGap));
    }

    [Theory]
    [MemberData(nameof(Scales))]
    public void TheDisplaysBumpersClearTheGaugesTabs(SlotScale scale)
    {
        // The tab's transparent pad may lie under the bumpers; its outline may not.
        int slot = Slots.SizeOf(scale), tabFoot = (int)Slots.GaugeTop(slot) + 30 + WristPiece.Scale * (WristPieces.Gauge.Window.Bottom - WristPieces.Pad);
        Assert.True(tabFoot <= Slots.InventoryTop(slot) - WristPiece.Scale * WristPieces.Display.Window.Top);
    }

    [Fact]
    public void TheDisplayKeepsItsSidesAndLampWhicheverItsBottomAndTheModuleLinesUpUnderIt()
    {
        var (open, docked) = (WristPieces.Display, WristPieces.DisplayDocked);
        Assert.Equal((open.Window.Left, open.Window.Top, open.Window.Right, open.Led), (docked.Window.Left, docked.Window.Top, docked.Window.Right, docked.Led));
        // Both windows start at the margin, so the module's left edge is the display's.
        Assert.Equal(open.Window.Left, WristPieces.Dock.Window.Left);
        Assert.True(docked.Window.Bottom < open.Window.Bottom);
    }
}
