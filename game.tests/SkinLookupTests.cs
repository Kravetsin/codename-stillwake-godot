using Stillwake.Game.Features.Crew;
using Stillwake.Game.Platform;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>What a hero is drawn in and what its client sends (hero-skins), over a catalogue of test rows.</summary>
public class SkinLookupTests
{
    private static readonly Skin[] Catalogue =
    [
        new("helmet_a", Skins.Head), new("suit_a", Skins.Suit, 7), new("suit_b", Skins.Suit), new("cape_a", Skins.Cape),
        new("effect_a", Skins.Effect, 7),
    ];

    private static Dressed Dress(string? helmet, string? body, string? cape, params string[] ids) =>
        Skins.Dress(helmet, body, cape, ids, Catalogue);

    [Fact]
    public void DrawsTheItemsOwnWithNoSkin() =>
        Assert.Equal(new Dressed("heavyHelmet", "regenSuit", "cape", null), Dress("heavyHelmet", "regenSuit", "cape"));

    [Fact]
    public void DrawsEachSkinOverTheItemInItsSlot() =>
        Assert.Equal(new Dressed("helmet_a", "suit_a", "cape_a", "effect_a"), Dress("helmet", "suit", "cape", "effect_a", "cape_a", "suit_a", "helmet_a"));

    [Fact]
    public void ABareHeadStaysBareWithAHelmetSkinChosen() =>
        Assert.Equal(new Dressed(null, null, null, "effect_a"), Dress(null, null, null, "helmet_a", "suit_a", "cape_a", "effect_a"));

    [Fact]
    public void AnUnknownIdOrOneOfAnotherKindDrawsTheItem()
    {
        Assert.Equal(new Dressed("helmet", "suit", null, null), Dress("helmet", "suit", null, "nobody's"));
        // A suit's skin is no helmet's: the helmet keeps its own, the suit takes it.
        Assert.Equal(new Dressed("helmet", "suit_a", null, null), Dress("helmet", "suit", null, "suit_a"));
    }

    [Fact]
    public void TheFirstOfAKindWins() =>
        Assert.Equal("suit_b", Dress(null, "suit", null, "suit_b", "suit_a").Body);

    [Fact]
    public void ADebugBuildOwnsEverySkin() =>
        Assert.Equal(["suit_a", "effect_a"], new Owned(true, _ => false).Sendable(["effect_a", "suit_a"], Catalogue));

    [Fact]
    public void WithoutItAPlayerOwnsTheFreeAndTheDlcsItBought()
    {
        Assert.Equal(["suit_b"], new Owned(false, _ => false).Sendable(["suit_b", "effect_a"], Catalogue));
        Assert.Equal(["suit_a", "effect_a"], new Owned(false, app => app == 7).Sendable(["suit_a", "effect_a"], Catalogue));
    }

    [Fact]
    public void SendsOneAKindInTheKindsOrderAndDropsWhatThisBuildDoesNotKnow() =>
        Assert.Equal(["helmet_a", "suit_b", "effect_a"], new Owned(true, _ => false).Sendable(["effect_a", "gone", "suit_b", "suit_a", "helmet_a"], Catalogue));

    [Fact]
    public void SendsTheEmptySetWhenItOwnsNoneOfItsPicks() =>
        Assert.Empty(new Owned(false, _ => false).Sendable(["suit_a", "effect_a"], Catalogue));
}
