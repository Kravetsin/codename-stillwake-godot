using System.Collections.Generic;
using Stillwake.Game.Render;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>Which charge a struck blade carries (skins): a fitted arc module's wins, else the effect skin's round its edge, else none.</summary>
public class ChargeOnTests
{
    private static readonly ChargeLook Skin = new((255, 140, 50), (255, 240, 210), BoltTint.Red, new ArcNumbers { BoltPx = (1, 7) });

    public static IEnumerable<object[]> Struck => [[HeldLook.FireAxe], [HeldLook.Sledgehammer], [HeldLook.Pickaxe], [HeldLook.Spear]];

    [Fact]
    public void AnArcModuleWinsOverTheSkin()
    {
        var box = (51.8, 3.1, 63.4, 7.0);
        var on = ChargeOn.Of(new HeldParts(HeldLook.Spear, [], [box]), Skin);
        Assert.Same(ChargeLook.Arc, on!.Value.Look);
        Assert.Equal(box, on.Value.Box);
    }

    [Fact]
    public void NoModuleAndNoSkinIsNoCharge() => Assert.Null(ChargeOn.Of(new HeldParts(HeldLook.FireAxe, [], []), null));

    [Theory]
    [MemberData(nameof(Struck))]
    public void ASkinChargesEveryStruckBladeRoundItsWholeEdge(HeldLook look)
    {
        var on = ChargeOn.Of(new HeldParts(look, [], []), Skin);
        Assert.Same(Skin, on!.Value.Look);
        var (x0, y0, x1, y1) = on.Value.Box;
        for (int i = 0; i < HeldLook.EdgePoints; i++)
        {
            var (x, y) = look.EdgePoint((double)i / (HeldLook.EdgePoints - 1));
            Assert.InRange(x, x0 + HeldLook.EdgeMargin - 1e-9, x1 - HeldLook.EdgeMargin + 1e-9);
            Assert.InRange(y, y0 + HeldLook.EdgeMargin - 1e-9, y1 - HeldLook.EdgeMargin + 1e-9);
        }
    }
}
