using System.Collections.Generic;
using Stillwake.Game.Features.Crew;
using Stillwake.Game.Shared;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>The Appearance tab's strip and picks (hero-skins).</summary>
public class AppearanceTests
{
    private static readonly IReadOnlyList<Skin> Catalogue =
        [new("free", Skins.Effect), new("paid", Skins.Effect, DlcAppId: 7), new("coat", Skins.Suit, DlcAppId: 7)];

    [Fact]
    public void TheStripIsNoneThenTheKindsSkinsUnownedLocked()
    {
        var strip = Appearance.Strip(Skins.Effect, s => s.DlcAppId == null, Catalogue);
        Assert.Equal([new(null, true), new("free", true), new("paid", false)], strip);
        Assert.Equal([new(null, true)], Appearance.Strip(Skins.Head, _ => true, Catalogue));
    }

    [Fact]
    public void APickReplacesItsKindAndKeepsTheRest()
    {
        IReadOnlyList<string> kept = ["coat", "free"];
        Assert.Equal(["coat", "paid"], Appearance.Pick(kept, Skins.Effect, "paid", Catalogue));
        Assert.Equal(["free"], Appearance.Pick(kept, Skins.Suit, null, Catalogue));
        Assert.Equal(["coat", "free"], Appearance.Pick(["free"], Skins.Suit, "coat", Catalogue));
    }

    [Fact]
    public void EveryKindHasItsSlotOnceAtItsGearsPlaceTheEffectByTheHand()
    {
        var kinds = new List<string>();
        var spots = new List<DollSpot>();
        foreach (var (kind, spot) in Appearance.Spots)
        {
            kinds.Add(kind);
            spots.Add(spot);
        }
        Assert.Equal(Skins.Kinds, kinds);
        Assert.Equal(DollLayout.Looks, spots);
        var layout = DollLayout.Of(DollLayout.BaseSlot);
        Assert.Equal(layout.SlotAt(DollSpot.Belt1), layout.SlotAt(DollSpot.Hand));
    }
}
