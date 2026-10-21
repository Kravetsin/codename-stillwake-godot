using Stillwake.Game.Features.Crafting;
using Stillwake.Game.Features.Items;
using Stillwake.Game.Shared;
using Stillwake.Sim;
using Stillwake.Sim.Items;
using Xunit;
using static Stillwake.Game.I18n.En;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Tests;

/// <summary>An item's card in the one tooltip: a tool's numbers from content, a worn item's one number, anything else's name and kind, what a fed station makes of it, a station's craft block, the band of an unknown item.</summary>
public class ItemCardTests
{
    private static readonly Content.Station Fabricator = Content.Stations.Get("lifepod");

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
        // As content stands: 20 cells, 100 ticks, 400 thousandths a tick of 20 a second.
        Assert.Equal([new CardTile("20", "RANGE"), new CardTile("5s", "RELOAD"), new CardTile("8/s", "REEL")], tiles);
    }

    [Fact]
    public void NameTheHarpoonAToolShotWithTheLeftButton()
    {
        var card = ItemCards.Of(Spec.Harpoon.Item, unknown: false);
        Assert.True(card.Tool);
        Assert.True(ItemCards.IsTool(Spec.Harpoon.Item));
        Assert.Equal(("Harpoon", "Tool · Harpoon"), (card.Name, card.Kind));
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
    public void NameTheAxeItsKindAndHowItIsUsedButNotWhatItBreaks()
    {
        var card = ItemCards.Of("fireAxe", unknown: false);
        Assert.True(card.Tool);
        Assert.Equal(("Fire axe", "Tool · Axe"), (card.Name, card.Kind));
        Assert.Equal(new CardKey("LMB", "hold on debris or ground to break it"), card.Use);
        Assert.Empty(card.Effects);
    }

    [Fact]
    public void GiveAnItemThatIsNoToolItsNameAndKindButNoTiles()
    {
        var gear = ItemCards.Of("gear", unknown: false);
        Assert.False(gear.Tool);
        Assert.Null(gear.Tiles);
        Assert.Null(gear.Use);
        Assert.False(gear.Worn);
        Assert.Equal(("Gear", "Part"), (gear.Name, gear.Kind));
        Assert.Equal(("Iron", "Material"), (ItemCards.Of("iron", false).Name, ItemCards.Of("iron", false).Kind));
        Assert.Equal("Salvage", ItemCards.Of("scrapChunk", false).Kind);
    }

    [Fact]
    public void WearEquipmentOnTheMediumCardTheHelmetLampWithItsReachFromItsLight()
    {
        var lamp = ItemCards.Of(Spec.HelmetLamp.Item, false);
        Assert.True(lamp.Worn);
        Assert.False(lamp.Tool);
        Assert.Equal(new CardTile(Light.HeadLampReach.ToString(), "CELLS REACH"), lamp.Headline);
        // As content stands: 9 cells, 30 degrees either side.
        Assert.Equal(new CardTile("9", "CELLS REACH"), lamp.Headline);
        Assert.Equal("BEAM 60°", lamp.Detail);
        // It draws nothing: the card shows a bolt and 0 rather than saying so.
        Assert.Null(lamp.Draw);
        Assert.Equal("a soft light round your head, for caves", lamp.Use!.Words);
        // A battery's number is its charge, set per stack; the jetpack has none.
        Assert.True(ItemCards.Of("battery", false).Worn);
        Assert.Null(ItemCards.Of("battery", false).Headline);
        Assert.True(ItemCards.Of("jetpack", false).Worn);
        Assert.Null(ItemCards.Of("jetpack", false).Headline);
        Assert.False(ItemCards.Of("drill", false).Worn);
        Assert.False(ItemCards.Of("scrap", false).Worn);
    }

    [Fact]
    public void SayWhatAFedStationMakesOfAnItemAndHowLongOneTakesAtItsSpeed()
    {
        // What it yields, and its time at a station as fast as takes it so many seconds: under a minute in seconds, past it in minutes.
        static Spec.Station Taking(string station, string item, int seconds) => Spec.Stations.Get(station) with
        {
            Speed = Spec.Items.Get(item).SmeltTicks!.Value * 1000 / (World.TicksPerSecond * seconds),
        };
        static IReadOnlyList<(string, int)> Yields(string item) => Spec.Items.Get(item).Yields!;
        Assert.Equal(new CardOutput(Yields("scrapChunk"), "3s"), ItemCards.OutputAt("scrapChunk", Taking("furnace", "scrapChunk", 3)));
        Assert.Equal(new CardOutput(Yields("hullPlate"), "5s"), ItemCards.OutputAt("hullPlate", Taking("furnace", "hullPlate", 5)));
        Assert.Equal(new CardOutput(Yields("silicon"), "3s"), ItemCards.OutputAt("silicon", Taking("smelter", "silicon", 3)));
        Assert.Equal(new CardOutput(Yields("seed"), "8 min"), ItemCards.OutputAt("seed", Taking("growCapsule", "seed", 8 * 60)));
        Assert.Null(ItemCards.OutputAt("planks", Spec.Stations.Get("furnace")));
    }

    [Fact]
    public void PutWhatTheOpenFurnaceMakesOnTheCardOfWhatItSmeltsButNothingOnFuel()
    {
        var furnace = new Reading([], null, false, "furnace", null, []);
        var chunk = furnace.HereTipOf(new Stack("scrapChunk", 12)).Card!;
        Assert.Equal(SlotMark.Smelts, chunk.Here);
        Assert.Equal(new CardOutput([("scrap", 4)], "3s"), chunk.Output);
        var planks = furnace.HereTipOf(new Stack("planks", 30)).Card!;
        Assert.Equal(SlotMark.Burns, planks.Here);
        Assert.Null(planks.Output);
        Assert.Null(new Reading([], null, false, null, null, []).HereTipOf(new Stack("scrapChunk", 12)).Card!.Output);
        Assert.NotEqual(chunk, chunk with { Output = null });
    }

    [Fact]
    public void SayHowACanisterIsBreathedAndAJetpackWornThoughNeitherIsATool()
    {
        var canister = ItemCards.Of("canister", false);
        Assert.False(canister.Tool);
        Assert.Equal(new CardKey("RMB", "breathe"), canister.Use);
        Assert.False(canister.Worn);
        var jetpack = ItemCards.Of("jetpack", false);
        Assert.False(jetpack.Tool);
        Assert.Equal("Shift", jetpack.Use?.Key);
        Assert.Null(ItemCards.Of("scrap", false).Use);
    }

    [Fact]
    public void HaveAKindForEveryItemThereIs()
    {
        Assert.All(Spec.Items.All, item => Assert.True(Table.ContainsKey($"item.{item.Id}.kind"), item.Id));
    }

    [Fact]
    public void GiveEveryStackACardWithTheBandOnlyWhileNotAnalysedAndNeverInCreativeMode()
    {
        Assert.True(With(false).TipOf("iron").Card!.Unknown);
        Assert.False(With(false, "iron").TipOf("iron").Card!.Unknown);
        Assert.False(With(true).TipOf("iron").Card!.Unknown);
        // The fire axe goes into nothing: known from the start, never banded.
        Assert.False(With(false).TipOf("fireAxe").Card!.Unknown);
        Assert.Equal("Iron", With(false).TipOf("iron").Name);
        Assert.Equal("? Not analysed", T("card.notAnalysed"));
    }

    [Fact]
    public void LayARecipesCostAgainstWhatIsCarriedWithItsTimeAtThePod()
    {
        var block = Crafts.Block("rotor", Fabricator, [("gear", 3), ("wire", 1)], creative: false);
        Assert.Equal(T("time.seconds", ("n", Crafts.Seconds("rotor", Fabricator))), block.Seconds);
        Assert.Equal($"{Crafts.Seconds("rotor", Fabricator)} s", block.Seconds);
        Assert.Equal([new CraftRow("gear", "Gear", 3, 1, false), new CraftRow("wire", "Wire", 1, 2, true)], block.Rows);
        Assert.Equal([new CardKey("LMB", "go to what is missing")], block.Keys);
    }

    [Fact]
    public void QueueARecipeCarriedByAClickOrFiveWithShiftAndNothingShortInCreativeMode()
    {
        var ready = Crafts.Block("wire", Fabricator, [("scrap", 2)], creative: false);
        Assert.Equal([new CraftRow("scrap", "Scrap", 2, 2, false)], ready.Rows);
        Assert.Equal([new CardKey("LMB", "craft"), new CardKey("Shift", "×5")], ready.Keys);
        Assert.Equal(ready.Keys, Crafts.Block("rotor", Fabricator, [], creative: true).Keys);
        Assert.All(Crafts.Block("rotor", Fabricator, [], creative: true).Rows, r => Assert.False(r.Short));
    }

    [Fact]
    public void SayWhereARawItemShortComesFromAndOfferNoClick()
    {
        var plate = Crafts.Block("plate", Fabricator, [("iron", 1)], creative: false);
        Assert.Equal([new CraftRow("iron", "Iron", 1, 2, true, "smelt hull plates")], plate.Rows);
        Assert.Empty(plate.Keys);
    }

    [Fact]
    public void TellTipsWithCardsApartByWhatTheCardsSay()
    {
        Tip Of(bool unknown) => new("Fire axe") { Card = ItemCards.Of("fireAxe", unknown) };
        Assert.Equal(Of(true), Of(true));
        Assert.Equal(Of(true).GetHashCode(), Of(true).GetHashCode());
        Assert.NotEqual(Of(true), Of(false));
        Assert.NotEqual(Of(false), new Tip("Fire axe"));
        var effect = new CardEffect("Fire", "sets it burning", "ff7a2e", "3a1a0c");
        var card = ItemCards.Of("fireAxe", false);
        Assert.Equal(card with { Effects = [effect] }, card with { Effects = [effect with { }] });
        Assert.NotEqual(card with { Effects = [effect] }, card);
        var short1 = Crafts.Block("rotor", Fabricator, [], false);
        Assert.Equal(card with { Station = short1 }, card with { Station = Crafts.Block("rotor", Fabricator, [], false) });
        Assert.Equal((card with { Station = short1 }).GetHashCode(), (card with { Station = Crafts.Block("rotor", Fabricator, [], false) }).GetHashCode());
        Assert.NotEqual(card with { Station = short1 }, card with { Station = Crafts.Block("rotor", Fabricator, [("gear", 1)], false) });
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

    [Fact]
    public void CardAModuleWithItsTechAndWhatItDoesNeverWhereItGoes()
    {
        var card = ItemCards.Of(Spec.Modules.Arc.Item, unknown: false);
        Assert.Equal(new CardTech("arc", "×2 speed · ×2 power"), card.Tech);
        Assert.Empty(card.Effects);
        Assert.All(Spec.Modules.Arc.Hosts, host => Assert.DoesNotContain(T($"tool.{host}"), card.Tech!.About));
        Assert.NotEqual(card, card with { Tech = null });
        Assert.Null(ItemCards.Of("iron", unknown: false).Tech);
    }
}
