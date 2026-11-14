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
    private static readonly Content.Station Workbench = Content.Stations.Get("workbench");

    private static Reading With(bool creative) => new([], null, creative, null);

    /// <summary>What a card's rows say, value and label, in order: the signs and groups are checked on their own.</summary>
    private static (string, string)[] Said(IReadOnlyList<CardTile>? tiles) => [.. tiles!.Select(t => (t.Value, t.Label))];

    [Fact]
    public void TakeTheAxesNumbersFromContentWithItsSwingAndReachButNoDamageASecond()
    {
        var tiles = ItemCards.Of("fireAxe").Tiles!;
        double every = (double)Spec.Weapons.Axe.Pace / World.TicksPerSecond;
        Assert.Equal(Spec.Weapons.Axe.Damage.ToString(), tiles[0].Value);
        Assert.Equal(ItemCards.Number(Spec.Weapons.Axe.BlockDamage / 10.0), tiles[1].Value);
        Assert.Equal($"{ItemCards.Number(every)}s", tiles[2].Value);
        // As content stands: 2 damage, 4 on blocks, every 10 ticks of 20 a second; 64 of a turn's 256 is 90°, 4500 thousandths 4.5 cells.
        Assert.Equal([("2", "Damage"), ("4", "Block"), ("0.5s", "Hits every"), ("90°", "Swing"), ("4.5 cells", "Range")], Said(tiles));
        Assert.Equal([CardSign.Damage, CardSign.Block, CardSign.Pace, CardSign.Arc, CardSign.Reach], tiles.Select(t => t.Sign));
        Assert.Equal([CardGroup.Hit, CardGroup.Hit, CardGroup.Hit, CardGroup.Reach, CardGroup.Reach], tiles.Select(t => t.Group));
        Assert.Equal(["wrecks", "rock", null, null, null], tiles.Select(t => t.Note));
        Assert.DoesNotContain(tiles, t => t.Off);
    }

    [Fact]
    public void SayTheCutterCutsNoRockAndGivesHalfAgainOfAWrecksLootUnderWhatIsSpecial()
    {
        var card = ItemCards.Of(Spec.Weapons.PlasmaCutter.Item);
        // As content stands: 1 a hit, none on rock, every 4 ticks, a beam 8 of a turn's 256 (11°) and 8 cells out; the cut bonus of 1500 thousandths.
        Assert.Equal([("1", "Damage"), ("cuts none", "Block"), ("0.2s", "Hits every"), ("11°", "Swing"), ("8 cells", "Range"), ("+50%", "Loot from wrecks")], Said(card.Tiles));
        Assert.True(card.Tiles![1].Off);
        Assert.Equal((CardSign.Loot, CardGroup.Special), (card.Tiles[5].Sign, card.Tiles[5].Group));
        // Its draw is the card's own, the battery's row the view puts first under what is special.
        Assert.Equal(Spec.Items.Get(Spec.Weapons.PlasmaCutter.Item).Draw, card.Draw);
        // Only a weapon that salvages has the row.
        Assert.DoesNotContain(ItemCards.Of(Spec.Weapons.Drill.Item).Tiles!, t => t.Sign == CardSign.Loot);
    }

    [Fact]
    public void GiveEverySignADrawingAndAHueAndEveryGroupItsWordsInBothLanguages()
    {
        foreach (var sign in System.Enum.GetValues<CardSign>())
        {
            if (sign == CardSign.None) continue;
            Assert.NotEmpty(CardSigns.Of(sign).Shape);
            Assert.Matches("^[0-9a-f]{6}$", CardSigns.Hue(sign));
            Assert.Contains("stroke=\"#" + CardSigns.Hue(sign), CardSigns.Svg(sign, 18));
        }
        Assert.Contains("opacity", CardSigns.Svg(CardSign.Block, 18, off: true));
        Assert.DoesNotContain("opacity", CardSigns.Svg(CardSign.Block, 18));
        foreach (var group in System.Enum.GetValues<CardGroup>()) Assert.True(Table.ContainsKey(CardSigns.TitleKey(group)), group.ToString());
    }

    [Fact]
    public void SignEveryToolsEveryRowAndListItsGroupsInOrder()
    {
        foreach (var item in Spec.Items.All.Select(i => i.Id).Where(id => ItemCards.IsTool(id) || ItemCards.IsAmmo(id)))
        {
            var tiles = ItemCards.Of(item).Tiles!;
            Assert.All(tiles, t => Assert.NotEqual(CardSign.None, t.Sign));
            // The groups as the card lists them: a hit or a shot, then the reach, then what is special, never back.
            Assert.Equal(tiles.Select(t => t.Group).OrderBy(g => g), tiles.Select(t => t.Group));
        }
    }

    [Fact]
    public void TileAGunsDamageOffBodiesAndBlocksWithItsAmmoItsReloadAndPierceAndItsAmmosShare()
    {
        var gun = ItemCards.Of(Spec.Shots.Nailer.Item);
        Assert.Equal(Spec.Shots.DamageOf(Spec.Shots.Nailer.Item, Spec.Shots.Nail.Id).ToString(), gun.Tiles![0].Value);
        // As content stands: the nailer's 4 and the nail's 2, 1 off a block, a shot every 60 ticks, half kept through a pierce.
        Assert.Equal([("6", "Damage"), ("1", "Block"), ("3s", "Reload time"), ("50%", "Kept through")], Said(gun.Tiles));
        Assert.Equal([CardSign.Damage, CardSign.Block, CardSign.Pace, CardSign.Pierce], gun.Tiles.Select(t => t.Sign));
        Assert.All(gun.Tiles, t => Assert.Equal(CardGroup.Shot, t.Group));
        Assert.True(gun.Tool && ItemCards.IsTool(Spec.Shots.Nailer.Item));
        var nail = ItemCards.Of(Spec.Shots.Nail.Id);
        Assert.Equal([("+2", "Damage"), ("+0", "Block")], Said(nail.Tiles));
        // A tile card, though no tool: its slot is not tinted as one.
        Assert.False(ItemCards.IsTool(Spec.Shots.Nail.Id));
        // The railgun's: 22 and the nail's 2, 4 off a block, 2 s between shots, 65% kept, 120 of the belt's charge a shot.
        var rail = ItemCards.Of(Spec.Shots.Railgun.Item).Tiles;
        Assert.Equal([("24", "Damage"), ("14", "Block"), ("2s", "Reload time"), ("65%", "Kept through"), ("120", "Charge a shot")], Said(rail));
        Assert.Equal((CardSign.Battery, CardGroup.Special), (rail![4].Sign, rail[4].Group));
        // A tool that carries on past what it breaks (unified-damage) says so as a gun's pierce: the pickaxe's 70%; the axe none.
        var pick = ItemCards.Of(Spec.Weapons.Pickaxe.Item).Tiles!;
        Assert.Equal((("70%", "Kept through"), CardSign.Pierce, CardGroup.Hit), ((pick[3].Value, pick[3].Label), pick[3].Sign, pick[3].Group));
        Assert.DoesNotContain(ItemCards.Of("fireAxe").Tiles!, t => t.Sign == CardSign.Pierce);
    }

    [Fact]
    public void TileAThrownChargesBlastNotWhatItWouldAddToAGun()
    {
        var charge = ItemCards.Of(Spec.Shots.BlastCharge.Id);
        // As content stands: 120 off each wreck within 8 cells, power 500 on rock in tenths, a throw a second.
        Assert.Equal([("120", "Damage"), ("50", "Block"), ("8 cells", "Radius"), ("1s", "Reload time")], Said(charge.Tiles));
        Assert.Equal([CardSign.Damage, CardSign.Block, CardSign.Reach, CardSign.Pace], charge.Tiles!.Select(t => t.Sign));
        Assert.All(charge.Tiles!, t => Assert.Equal(CardGroup.Blast, t.Group));
        Assert.True(charge.Tool);
    }

    [Fact]
    public void TileTheHarpoonsRangeReloadAndReelSpeedFromContentInsteadOfDamage()
    {
        var tiles = ItemCards.Of(Spec.Harpoon.Item).Tiles!;
        Assert.Equal($"{ItemCards.Number((double)Spec.Harpoon.CooldownTicks / World.TicksPerSecond)}s", tiles[0].Value);
        Assert.Equal($"{ItemCards.Number(Spec.Harpoon.Range / 1000.0)} cells", tiles[1].Value);
        Assert.Equal($"{ItemCards.Number(Spec.Harpoon.ReelSpeed * World.TicksPerSecond / 1000.0)}/s", tiles[2].Value);
        // As content stands: 100 ticks, 40 cells, 800 thousandths a tick of 20 a second; a cell half what it was (0104).
        Assert.Equal([("5s", "Reload time"), ("40 cells", "Range"), ("16/s", "Reel")], Said(tiles));
        Assert.Equal([CardSign.Pace, CardSign.Reach, CardSign.Reel], tiles.Select(t => t.Sign));
    }

    [Fact]
    public void NameTheHarpoonAToolShotWithTheLeftButton()
    {
        var card = ItemCards.Of(Spec.Harpoon.Item);
        Assert.True(card.Tool);
        Assert.True(ItemCards.IsTool(Spec.Harpoon.Item));
        Assert.Equal(("Harpoon", "Tool · Harpoon"), (card.Name, card.Kind));
        Assert.Equal("LMB", card.Use!.Key);
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
    public void NameTheAxeItsKindAndItsKeyButNotWhatItBreaks()
    {
        var card = ItemCards.Of("fireAxe");
        Assert.True(card.Tool);
        Assert.Equal(("Fire axe", "Tool · Axe"), (card.Name, card.Kind));
        // Its key alone, no words on what it does: the player finds out.
        Assert.Equal(new CardKey("LMB"), card.Use);
    }

    [Fact]
    public void GiveAnItemThatIsNoToolItsNameAndKindButNoTiles()
    {
        var gear = ItemCards.Of("gear");
        Assert.False(gear.Tool);
        Assert.Null(gear.Tiles);
        Assert.Null(gear.Use);
        Assert.False(gear.Worn);
        Assert.Equal(("Gear", "Part"), (gear.Name, gear.Kind));
        Assert.Equal(("Iron", "Material"), (ItemCards.Of("iron").Name, ItemCards.Of("iron").Kind));
        Assert.Equal("Salvage", ItemCards.Of("scrapChunk").Kind);
    }

    [Fact]
    public void TheCapesCardShowsItsShareOffBlowsAndTheDegreesItAdds()
    {
        // From content: a fifth off blows (2026-10-27-the-cape-is-worn), 5° against the cold (2026-11-14-temperature-in-degrees).
        var cape = ItemCards.Of(Spec.Cape.Item);
        Assert.True(cape.Worn);
        Assert.Equal(new CardTile("20", "% OFF BLOWS"), cape.Headline);
        Assert.Equal("WARMTH +5°", cape.Detail);
        Assert.Null(cape.Use);
    }

    [Fact]
    public void ABodysCardShowsTheColdestItWithstandsAHelmetsNone()
    {
        Assert.Equal("WITHSTANDS DOWN TO −40°", ItemCards.Of(Spec.Suit.Basic).Detail);
        Assert.Equal("WITHSTANDS DOWN TO −40°", ItemCards.Of(Spec.Suit.Regen).Detail);
        Assert.True(ItemCards.Of(Spec.Suit.Basic).Worn);
        Assert.NotEqual("WITHSTANDS DOWN TO −40°", ItemCards.Of(Spec.Helmet.Basic).Detail);
    }

    [Fact]
    public void WearEquipmentOnTheMediumCardTheHelmetLampWithItsReachFromItsLight()
    {
        var lamp = ItemCards.Of(Spec.HelmetLamp.Item);
        Assert.True(lamp.Worn);
        Assert.False(lamp.Tool);
        Assert.Equal(new CardTile(Light.HeadLampReach.ToString(), "CELLS REACH"), lamp.Headline);
        // As content stands: 18 of the 8 px cells (0104), 30 degrees either side.
        Assert.Equal(new CardTile("18", "CELLS REACH"), lamp.Headline);
        Assert.Equal("BEAM 60°", lamp.Detail);
        // It draws nothing: the card shows a bolt and 0 rather than saying so.
        Assert.Null(lamp.Draw);
        // Worn, it takes no key.
        Assert.Null(lamp.Use);
        // A battery's number is its charge, set per stack; the jetpack has none.
        Assert.True(ItemCards.Of("battery").Worn);
        Assert.Null(ItemCards.Of("battery").Headline);
        Assert.True(ItemCards.Of("jetpack").Worn);
        Assert.Null(ItemCards.Of("jetpack").Headline);
        Assert.False(ItemCards.Of("drill").Worn);
        Assert.False(ItemCards.Of("scrap").Worn);
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
        Assert.Equal(new CardOutput(Yields("quartz"), "3s"), ItemCards.OutputAt("quartz", Taking("smelter", "quartz", 3)));
        Assert.Equal(new CardOutput(Yields("seed"), "8 min"), ItemCards.OutputAt("seed", Taking("growCapsule", "seed", 8 * 60)));
        Assert.Null(ItemCards.OutputAt("planks", Spec.Stations.Get("furnace")));
    }

    [Fact]
    public void SayHowManyASmeltTakesAndWhatItMayMakeBesidesByChance()
    {
        // No content has either yet: an item made up for it, as the crusher's gravel and ore will be.
        var gravel = new Spec.ItemSpec("gravel", 50, Yields: [("glass", 1)], SmeltTicks: 20, SmeltTakes: 4);
        var ore = new Spec.ItemSpec("ore", 50, Yields: [("iron", 1)], SmeltTicks: 20, Byproducts: [new("crystal", 1, 2, OneIn: 3), new("scrap", 1, 1)]);
        var furnace = Spec.Stations.Get("furnace");
        Assert.Equal(new CardOutput([("glass", 1)], "1s", 4), ItemCards.OutputAt(gravel, furnace));
        Assert.Equal(new CardOutput([("iron", 1)], "1s", 1, [("crystal", 2, 3), ("scrap", 1, 1)]), ItemCards.OutputAt(ore, furnace));
        Assert.NotEqual(new CardOutput([("iron", 1)], "1s"), ItemCards.OutputAt(ore, furnace));
        Assert.NotEqual(new CardOutput([("glass", 1)], "1s"), ItemCards.OutputAt(gravel, furnace));
    }

    [Fact]
    public void PutWhatTheOpenFurnaceMakesOnTheCardOfWhatItSmeltsButNothingOnFuel()
    {
        var furnace = new Reading([], null, false, "furnace");
        var chunk = furnace.HereTipOf(new Stack("scrapChunk", 12)).Card!;
        Assert.Equal(SlotMark.Smelts, chunk.Here);
        Assert.Equal(new CardOutput([("scrap", 4)], "3s"), chunk.Output);
        var planks = furnace.HereTipOf(new Stack("planks", 30)).Card!;
        Assert.Equal(SlotMark.Burns, planks.Here);
        Assert.Null(planks.Output);
        Assert.Null(new Reading([], null, false, null).HereTipOf(new Stack("scrapChunk", 12)).Card!.Output);
        Assert.NotEqual(chunk, chunk with { Output = null });
    }

    [Fact]
    public void ShowTheKeyACanisterAndAJetpackAreUsedByThoughNeitherIsATool()
    {
        var canister = ItemCards.Of("canister");
        Assert.False(canister.Tool);
        Assert.Equal(new CardKey("RMB"), canister.Use);
        Assert.False(canister.Worn);
        var jetpack = ItemCards.Of("jetpack");
        Assert.False(jetpack.Tool);
        Assert.Equal("Shift", jetpack.Use?.Key);
        Assert.Null(ItemCards.Of("scrap").Use);
    }

    [Fact]
    public void TheSalvageDronesCardSaysNoCrateNoFlight()
    {
        var card = ItemCards.Of(Spec.Logistics.SalvageDrone);
        Assert.False(card.Tool);
        Assert.EndsWith("No crate with room in reach: it does not fly.", card.About);
        // A plain item without a line of its own shows none.
        Assert.Null(ItemCards.Of("scrap").About);
        Assert.Null(ItemCards.Of(Spec.Logistics.Drone).About);
    }

    [Fact]
    public void HaveAKindForEveryItemThereIs()
    {
        Assert.All(Spec.Items.All, item => Assert.True(Table.ContainsKey($"item.{item.Id}.kind"), item.Id));
    }

    [Fact]
    public void GiveEveryStackACardWithItsPriceAndNoneForWhatNoOneBuys()
    {
        Assert.Equal(Spec.Market.PriceOf("iron"), With(false).TipOf("iron").Card!.Price);
        Assert.Null(With(false).TipOf("fireAxe").Card!.Price);
        Assert.Equal("Iron", With(false).TipOf("iron").Name);
    }

    [Fact]
    public void LayARecipesCostAgainstWhatIsCarriedWithItsTimeAtTheWorkbench()
    {
        var block = Crafts.Block("rotor", Workbench, [("gear", 3), ("wire", 1)], creative: false);
        Assert.Equal(T("time.seconds", ("n", Crafts.Seconds("rotor", Workbench))), block.Seconds);
        Assert.Equal($"{Crafts.Seconds("rotor", Workbench)} s", block.Seconds);
        Assert.Equal([new CraftRow("gear", "Gear", 3, 1, false), new CraftRow("wire", "Wire", 1, 2, true)], block.Rows);
        Assert.Equal([new CardKey("LMB", "go to what is missing")], block.Keys);
    }

    [Fact]
    public void QueueARecipeCarriedByAClickOrUpToFiveWithShiftAndNothingShortInCreativeMode()
    {
        var ready = Crafts.Block("wire", Workbench, [("scrap", 10)], creative: false);
        Assert.Equal([new CraftRow("scrap", "Scrap", 10, 2, false)], ready.Rows);
        Assert.Equal([new CardKey("LMB", "craft"), new CardKey("Shift", "×5")], ready.Keys);
        Assert.Equal(ready.Keys, Crafts.Block("rotor", Workbench, [], creative: true).Keys);
        Assert.Equal([new CardKey("LMB", "craft"), new CardKey("Shift", "×3")], Crafts.Block("wire", Workbench, [("scrap", 7)], creative: false).Keys);
        Assert.All(Crafts.Block("rotor", Workbench, [], creative: true).Rows, r => Assert.False(r.Short));
    }

    [Fact]
    public void SayWhereARawItemShortComesFromAndOfferNoClick()
    {
        var plate = Crafts.Block("plate", Workbench, [("iron", 1)], creative: false);
        Assert.Equal([new CraftRow("iron", "Iron", 1, 2, true, "smelt hull plates or iron ore")], plate.Rows);
        Assert.Empty(plate.Keys);
    }

    [Fact]
    public void TellTipsWithCardsApartByWhatTheCardsSay()
    {
        Tip Of(string item) => new("Fire axe") { Card = ItemCards.Of(item) };
        Assert.Equal(Of("fireAxe"), Of("fireAxe"));
        Assert.Equal(Of("fireAxe").GetHashCode(), Of("fireAxe").GetHashCode());
        Assert.NotEqual(Of("fireAxe"), Of("iron"));
        Assert.NotEqual(Of("fireAxe"), new Tip("Fire axe"));
        var card = ItemCards.Of("fireAxe");
        Assert.NotEqual(card, card with { Tiles = [.. card.Tiles!.Select(t => t with { Sign = CardSign.Loot })] });
        Assert.Equal(card, card with { Tiles = [.. card.Tiles!.Select(t => t with { })] });
        var short1 = Crafts.Block("rotor", Workbench, [], false);
        Assert.Equal(card with { Station = short1 }, card with { Station = Crafts.Block("rotor", Workbench, [], false) });
        Assert.Equal((card with { Station = short1 }).GetHashCode(), (card with { Station = Crafts.Block("rotor", Workbench, [], false) }).GetHashCode());
        Assert.NotEqual(card with { Station = short1 }, card with { Station = Crafts.Block("rotor", Workbench, [("gear", 1)], false) });
        Assert.NotEqual(card with { Station = short1 }, card);
    }

    [Fact]
    public void CardAModuleWithItsTechAndWhatItDoesNeverWhereItGoes()
    {
        var card = ItemCards.Of(Spec.Modules.Arc.Item);
        // A line for each group of hosts, from its trades (0100): the stations', the drill's and grinder's, the spear's, the charger's, whose draw is its charging.
        Assert.Equal(new CardTech("arc", "×2 speed · ×2 power\n×2 damage · ×2 power\n×2 damage\n×5 charging · ×5 power"), card.Tech);
        Assert.All(Spec.Modules.Arc.Hosts, host => Assert.DoesNotContain(T($"tool.{host}"), card.Tech!.About));
        Assert.NotEqual(card, card with { Tech = null });
        Assert.Null(ItemCards.Of("iron").Tech);
    }

    [Fact]
    public void DrawAFittedDrillsTilesFromTheSimsFoldMarkingWhatTheArcChangesAndNamingIt()
    {
        string drill = Spec.Weapons.Drill.Item, arc = Spec.Modules.Arc.Item;
        var plain = With(true).TipOf(new Stack(drill, 1)).Card!;
        var fitted = With(true).TipOf(new Stack(drill, 1, Module: arc)).Card!;
        var stats = ToolStats.Of(new Stack(drill, 1, Module: arc));
        // Every number is the fold's, the one the sim hits and draws by: 2 a hit, every 0.1 s, 2 a tick.
        Assert.Equal(stats.Damage.ToString(), fitted.Tiles![0].Value);
        Assert.Equal(stats.Draw, fitted.Draw);
        var up = new CardBoost("arc", true, "×2");
        Assert.Equal([("2", "Damage"), ("8", "Block"), ("0.1s", "Hits every"), ("45°", "Swing"), ("5.5 cells", "Range")], Said(fitted.Tiles));
        Assert.Equal([up, up, null, null, null], fitted.Tiles.Select(t => t.Boost));
        Assert.Equal((2, up), (fitted.Draw, fitted.DrawBoost));
        Assert.Equal([arc], fitted.Fitted);
        // The plain drill's, unmarked: no stale draw of 1 on the fitted one, nor a mark on the plain.
        Assert.Equal([("1", "Damage"), ("4", "Block"), ("0.1s", "Hits every"), ("45°", "Swing"), ("5.5 cells", "Range")], Said(plain.Tiles));
        Assert.All(plain.Tiles!, t => Assert.Null(t.Boost));
        Assert.Equal((1, (CardBoost?)null), (plain.Draw, plain.DrawBoost));
        Assert.Empty(plain.Fitted);
        Assert.Null(fitted.Tech);
        Assert.NotEqual(plain, fitted);
    }

    /// <summary>A second module that is not in content: half as much again of damage for a quarter less draw.</summary>
    private static readonly Spec.ModuleSpec Fake = new("fakeModule", "arc", [new(["drill", Spec.Weapons.Axe.Item], [new Spec.Trade(Spec.Stat.Draw, -250), new Spec.Trade(Spec.Stat.Damage, 500)])]);

    [Fact]
    public void FoldTwoModulesStatsSummedOnceAndMarkEachNumberByHowItMoved()
    {
        string drill = Spec.Weapons.Drill.Item;
        // Summed, then applied once: damage 1 × (1000 + 1000 + 500) / 1000 = 2.5, down to 2; draw 1 × 1750 / 1000 = 1.75, up to 2.
        Assert.Equal(new ToolStats(2, 2, 2), ToolStats.Of(drill, [Spec.Modules.Arc, Fake]));
        Assert.Equal(new ToolStats(1, 2, 1), ToolStats.Of(drill, [Fake]));
        Assert.Equal(new ToolStats(1, 2, 1), ToolStats.Of(drill, []));
        // On the axe, 2 × 1500 / 1000 = 3, a gain alone: it draws nothing.
        Assert.Equal(new ToolStats(3, 10, 0), ToolStats.Of(Spec.Weapons.Axe.Item, [Fake]));
        var card = ItemCards.Fitted(ItemCards.Of(Spec.Weapons.Axe.Item), [Fake]);
        var more = new CardBoost("arc", true, "×1.5");
        Assert.Equal([("3", "Damage"), ("6", "Block"), ("0.5s", "Hits every"), ("90°", "Swing"), ("4.5 cells", "Range")], Said(card.Tiles));
        Assert.Equal([more, more, null, null, null], card.Tiles!.Select(t => t.Boost));
        Assert.Null(card.Draw);
        // A cost that falls is marked down: the grinder's, as the drill's 1 cannot fall.
        var lighter = ItemCards.Fitted(ItemCards.Of(Spec.Weapons.Grinder.Item), [new Spec.ModuleSpec("lighter", "arc", [new([Spec.Weapons.Grinder.Item], [new Spec.Trade(Spec.Stat.Draw, -500)])])]);
        Assert.Equal((1, new CardBoost("arc", false, "×0.5")), (lighter.Draw!.Value, lighter.DrawBoost!));
        Assert.Equal(ItemCards.Of(Spec.Weapons.Grinder.Item).Tiles, lighter.Tiles);
    }

    [Fact]
    public void ACardsNumbersAreTheSimsForEveryStackAToolCanBe()
    {
        // Whatever is fitted, the card never works out a number of its own: the sim's damage and draw for the same stack.
        foreach (var stack in new[] { new Stack("drill", 1), new Stack("drill", 1, Module: Spec.Modules.Arc.Item) })
        {
            var slot = new System.Text.Json.Nodes.JsonObject { ["item"] = stack.Item, ["count"] = 1 };
            if (stack.Module is { } module) slot["module"] = module;
            var (m, _) = ItemsWorlds.With(Layout.Parse("..."), items => items["inventories"] = new System.Text.Json.Nodes.JsonArray(
                new System.Text.Json.Nodes.JsonObject { ["id"] = 0, ["slots"] = new System.Text.Json.Nodes.JsonArray(slot) }));
            var card = With(true).TipOf(stack).Card!;
            Assert.Equal(stack, m.Items.ToolStack(0, "drill"));
            Assert.Equal(m.Items.Damage(0, "drill").ToString(), card.Tiles![0].Value);
            Assert.Equal(ToolStats.Of(stack).Draw, card.Draw);
        }
    }

    [Fact]
    public void LayEachModulesOverlayOnTheToolsMountsInTheToolsOrderThenTheModulesWithItsCrackle()
    {
        var arc = ModuleLooks.Arc;
        // The arc's coil over the drill's coil mount, crackling there; a plain drill has nothing over it.
        var fitted = HeldParts.Of(HeldLook.Drill, [Spec.Modules.Arc]);
        Assert.Equal([new HeldLayer("res://art/module_arc_coil.png", 4, 4)], fitted.Layers);
        Assert.Equal([(3.0, 3.5, 11.0, 6.5)], fitted.Crackles);
        Assert.Empty(HeldParts.Of(HeldLook.Drill, []).Layers);
        Assert.Empty(HeldParts.Of(HeldLook.FireAxe, [Spec.Modules.Arc]).Layers);
        // The railgun crackles along its own barrel with no module, and nothing is laid over it.
        var rail = HeldParts.Of(HeldLook.Railgun, []);
        Assert.Equal(HeldLook.Railgun.Arcs, rail.Crackles);
        Assert.Equal(3, rail.Crackles.Count);
        Assert.Empty(rail.Layers);
        Assert.Empty(HeldParts.Of(HeldLook.Drill, []).Crackles);
        // Two mounts and two modules: by mount as the tool declares them, then by module as fitted; a mount it lacks shows nothing.
        var tool = HeldLook.Drill with { Mounts = [new ToolMount("grip", 1, 9, (0, 8, 4, 12)), new ToolMount("coil", 4, 4, (3, 3.5, 11, 6.5))] };
        IReadOnlyList<ModuleOverlay> second = [new("coil", "second_coil.png"), new("grip", "second_grip.png"), new("barrel", "second_barrel.png")];
        var both = HeldParts.Overlaid(tool, [arc, second]);
        Assert.Equal(
            [new HeldLayer("second_grip.png", 1, 9), new HeldLayer("res://art/module_arc_coil.png", 4, 4), new HeldLayer("second_coil.png", 4, 4)],
            both.Layers);
        Assert.Equal([(3.0, 3.5, 11.0, 6.5)], both.Crackles);
        // Fitted the other way round, the second's coil goes under the arc's.
        Assert.Equal(
            [new HeldLayer("second_grip.png", 1, 9), new HeldLayer("second_coil.png", 4, 4), new HeldLayer("res://art/module_arc_coil.png", 4, 4)],
            HeldParts.Overlaid(tool, [second, arc]).Layers);
        Assert.Same(HeldLook.Drill, HeldLook.Of(Spec.Weapons.Drill.Item));
        Assert.True(HeldLook.IsDrill(Spec.Weapons.Drill.Item));
    }

    [Fact]
    public void EveryScrewdriverIsHeldInTheHandSoTheToolInUseShowsWithoutTheHotbar()
    {
        // Each in its own look: the screwdriver, and the power driver as the user picked it (power-driver).
        Assert.Same(HeldLook.Screwdriver, HeldLook.Of(Spec.Screwdrivers.Basic.Item));
        Assert.Same(HeldLook.PowerDriver, HeldLook.Of(Spec.Screwdrivers.PowerDriver.Item));
        foreach (var driver in Spec.Screwdrivers.All) Assert.NotNull(HeldLook.Of(driver.Item)?.Twist);
    }
}
