using Stillwake.Game.Features.Crafting;
using Stillwake.Game.Features.Items;
using Stillwake.Game.Shared;
using Stillwake.Sim;
using Xunit;
using static Stillwake.Game.I18n.En;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Tests;

/// <summary>An item's card in the one tooltip: a tool's numbers from content, anything else's kind and what it goes into, a station's craft block, the band of an unknown item.</summary>
public class ItemCardTests
{
    private static Reading With(bool creative, params string[] analysed) => new([], null, creative, null, null, analysed);

    [Fact]
    public void TakeTheAxesNumbersFromContentAndItsDamageASecondFromThem()
    {
        var tiles = ItemCards.Of("fireAxe", unknown: false).Tiles!;
        double every = (double)Spec.Weapons.Axe.Pace / World.TicksPerSecond;
        Assert.Equal(ItemCards.Number(Spec.Weapons.Axe.Damage / every), tiles[0].Value);
        Assert.Equal(Spec.Weapons.Axe.Damage.ToString(), tiles[1].Value);
        Assert.Equal($"{ItemCards.Number(every)}s", tiles[2].Value);
        // As content stands: 2 damage every 10 ticks of 20 a second.
        Assert.Equal([new CardTile("4", "DPS"), new CardTile("2", "DMG"), new CardTile("0.5s", "HIT")], tiles);
    }

    [Fact]
    public void TileTheHarpoonsRangeReloadAndReelSpeedFromContentInsteadOfDamage()
    {
        var tiles = ItemCards.Of(Spec.Harpoon.Item, unknown: false).Tiles!;
        Assert.Equal(ItemCards.Number(Spec.Harpoon.Range / 1000.0), tiles[0].Value);
        Assert.Equal($"{ItemCards.Number((double)Spec.Harpoon.CooldownTicks / World.TicksPerSecond)}s", tiles[1].Value);
        Assert.Equal($"{ItemCards.Number(Spec.Harpoon.ReelSpeed * World.TicksPerSecond / 1000.0)}/s", tiles[2].Value);
        // As content stands: 12 cells, 100 ticks, 250 thousandths a tick of 20 a second.
        Assert.Equal([new CardTile("12", "RANGE"), new CardTile("5s", "RELOAD"), new CardTile("5/s", "REEL")], tiles);
    }

    [Fact]
    public void NameTheHarpoonAToolForDebrisAndWreckageShotWithTheLeftButton()
    {
        var card = ItemCards.Of(Spec.Harpoon.Item, unknown: false);
        Assert.True(card.Tool);
        Assert.True(ItemCards.IsTool(Spec.Harpoon.Item));
        Assert.Equal(("Harpoon", "Tool · Harpoon"), (card.Name, card.Kind));
        Assert.Equal(["Debris", "Wreckage"], card.Uses);
        Assert.Equal("LMB", card.Use!.Key);
        Assert.Empty(card.Effects);
        Assert.NotEqual(card, card with { Tiles = [.. card.Tiles!.Take(2)] });
        Assert.Equal(card, card with { Tiles = [.. card.Tiles!] });
    }

    [Fact]
    public void WriteNumbersWithOneDecimalAtMostHalfRoundedUp()
    {
        Assert.Equal("4", ItemCards.Number(4));
        Assert.Equal("5.3", ItemCards.Number(16.0 / 3));
        Assert.Equal("0.5", ItemCards.Number(0.5));
        Assert.Equal("0.3", ItemCards.Number(0.25));
        Assert.Equal("6.7", ItemCards.Number(20.0 / 3));
    }

    [Fact]
    public void NameTheAxeItsKindWhatItBreaksAndHowItIsUsed()
    {
        var card = ItemCards.Of("fireAxe", unknown: false);
        Assert.True(card.Tool);
        Assert.Equal(("Fire axe", "Tool · Axe"), (card.Name, card.Kind));
        Assert.Equal(["Hull section", "Cargo container", "Machine block", "Electronics rack", "Oxygen tanks", "Shield slab", "Asteroid",
            "Small asteroid", "Large asteroid", "Ore-rich asteroid", "Engine block", "Cargo hold", "Pod cluster"], card.Uses);
        Assert.Equal(new CardKey("LMB", "hold on wreckage in space"), card.Use);
        Assert.Empty(card.Effects);
        Assert.Equal(Spec.Wreckage.All.Count, card.Uses.Count);
    }

    [Fact]
    public void GiveAnItemThatIsNoToolItsKindAndWhatItGoesIntoButNoTiles()
    {
        var gear = ItemCards.Of("gear", unknown: false);
        Assert.False(gear.Tool);
        Assert.Null(gear.Tiles);
        Assert.Null(gear.Use);
        Assert.Equal(("Gear", "Part"), (gear.Name, gear.Kind));
        // The crafts first, then the blueprints in content's order: the rotor, then the door and the valve.
        Assert.Equal(["Rotor", "Drill", "Door", "Valve"], gear.Uses);
        Assert.Equal(["Rotor", "Coil", "Harpoon", "Oxygen jetpack", "+6"], ItemCards.Of("wire", false).Uses);
        Assert.Equal(("Iron", "Material"), (ItemCards.Of("iron", false).Name, ItemCards.Of("iron", false).Kind));
        Assert.Equal("Salvage", ItemCards.Of("scrapChunk", false).Kind);
    }

    [Fact]
    public void NameEveryCraftBlueprintAndCableWhoseCostHasTheItemUpToAFewAndCountTheRest()
    {
        var scrap = ItemCards.Of("scrap", false).Uses;
        Assert.Equal(ItemCards.MostInto + 1, scrap.Count);
        Assert.Equal(["Wire", "Wall: scrap", "Back wall: scrap", "Floor"], scrap.Take(ItemCards.MostInto));
        int into = Spec.Crafts.All.Count(c => c.Cost.Any(i => i.Item == "scrap"))
            + Sim.Research.Blueprints.All.Count(b => b.Cost.Any(i => i.Item == "scrap"));
        Assert.Equal($"+{into - ItemCards.MostInto}", scrap[^1]);
        // Electronics need circuits: the crafts first, then the builds.
        Assert.Equal(["Battery", "Drill", "Solar panel", "Charging station"], ItemCards.Of("circuit", false).Uses);
    }

    [Fact]
    public void SayHowACanisterIsBreathedAndAJetpackWornThoughNeitherIsATool()
    {
        var canister = ItemCards.Of("canister", false);
        Assert.False(canister.Tool);
        Assert.Equal(new CardKey("RMB", "breathe"), canister.Use);
        Assert.Equal(["Oxygen jetpack"], canister.Uses);
        var jetpack = ItemCards.Of("jetpack", false);
        Assert.False(jetpack.Tool);
        Assert.Equal("Shift", jetpack.Use?.Key);
        Assert.Null(ItemCards.Of("scrap", false).Use);
    }

    [Fact]
    public void SayNothingOfWhatAnItemGoesIntoWhenItGoesIntoNothing()
    {
        Assert.Empty(ItemCards.Of("supplyPod", false).Uses);
        Assert.Empty(ItemCards.Of("jetpack", false).Uses);
    }

    [Fact]
    public void HaveAKindForEveryItemThereIs()
    {
        Assert.All(Spec.Items.All, item => Assert.True(Table.ContainsKey($"item.{item.Id}.kind"), item.Id));
    }

    [Fact]
    public void GiveEveryStackACardWithTheBandOnlyWhileNotAnalysedAndNeverInCreativeMode()
    {
        Assert.True(With(false).TipOf("fireAxe").Card!.Unknown);
        Assert.False(With(false, "fireAxe").TipOf("fireAxe").Card!.Unknown);
        Assert.False(With(true).TipOf("fireAxe").Card!.Unknown);
        Assert.True(With(false).TipOf("iron").Card!.Unknown);
        Assert.False(With(false, "iron").TipOf("iron").Card!.Unknown);
        Assert.Equal("Iron", With(false).TipOf("iron").Name);
        Assert.Equal("? Not analysed", T("card.notAnalysed"));
    }

    [Fact]
    public void LayARecipesCostAgainstWhatIsCarriedWithItsTimeAtThePod()
    {
        var block = Crafts.Block("rotor", [("gear", 3), ("wire", 1)], creative: false);
        Assert.Equal(T("time.seconds", ("n", Crafts.Seconds("rotor"))), block.Seconds);
        // 60 ticks at full speed, the pod at a fifth: fifteen seconds.
        Assert.Equal("15 s", block.Seconds);
        Assert.Equal([new CraftRow("gear", "Gear", 3, 1, false), new CraftRow("wire", "Wire", 1, 2, true)], block.Rows);
        Assert.Equal([new CardKey("LMB", "go to what is missing")], block.Keys);
    }

    [Fact]
    public void QueueARecipeCarriedByAClickOrFiveWithShiftAndNothingShortInCreativeMode()
    {
        var ready = Crafts.Block("wire", [("scrap", 2)], creative: false);
        Assert.Equal([new CraftRow("scrap", "Scrap", 2, 2, false)], ready.Rows);
        Assert.Equal([new CardKey("LMB", "craft"), new CardKey("Shift", "×5")], ready.Keys);
        Assert.Equal(ready.Keys, Crafts.Block("rotor", [], creative: true).Keys);
        Assert.All(Crafts.Block("rotor", [], creative: true).Rows, r => Assert.False(r.Short));
    }

    [Fact]
    public void SayWhereARawItemShortComesFromAndOfferNoClick()
    {
        var plate = Crafts.Block("plate", [("iron", 1)], creative: false);
        Assert.Equal([new CraftRow("iron", "Iron", 1, 2, true, "smelt hull plates")], plate.Rows);
        Assert.Empty(plate.Keys);
    }

    [Fact]
    public void TellTipsWithCardsApartByWhatTheCardsSay()
    {
        Tip Of(bool unknown) => With(!unknown).TipOf("fireAxe");
        Assert.Equal(Of(true), Of(true));
        Assert.Equal(Of(true).GetHashCode(), Of(true).GetHashCode());
        Assert.NotEqual(Of(true), Of(false));
        Assert.NotEqual(Of(false), new Tip("Fire axe"));
        var effect = new CardEffect("Fire", "sets it burning", "ff7a2e", "3a1a0c");
        var card = ItemCards.Of("fireAxe", false);
        Assert.Equal(card with { Effects = [effect] }, card with { Effects = [effect with { }] });
        Assert.NotEqual(card with { Effects = [effect] }, card);
        var short1 = Crafts.Block("rotor", [], false);
        Assert.Equal(card with { Station = short1 }, card with { Station = Crafts.Block("rotor", [], false) });
        Assert.Equal((card with { Station = short1 }).GetHashCode(), (card with { Station = Crafts.Block("rotor", [], false) }).GetHashCode());
        Assert.NotEqual(card with { Station = short1 }, card with { Station = Crafts.Block("rotor", [("gear", 1)], false) });
        Assert.NotEqual(card with { Station = short1 }, card);
    }

    [Fact]
    public void HaveWordsForEveryEffectOfTheAxe()
    {
        Assert.All(Spec.Weapons.Axe.Effects, e =>
        {
            Assert.True(Table.ContainsKey($"effect.{e.Id}"), e.Id);
            Assert.True(Table.ContainsKey($"effect.{e.Id}.about"), e.Id);
        });
    }
}
