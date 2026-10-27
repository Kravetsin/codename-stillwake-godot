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
        Assert.Equal(Spec.Weapons.Axe.BlockDamage.ToString(), tiles[2].Value);
        Assert.Equal($"{ItemCards.Number(every)}s", tiles[3].Value);
        // As content stands: 2 damage, 4 on blocks, every 10 ticks of 20 a second.
        Assert.Equal([new CardTile("4", "DPS"), new CardTile("2", "DMG"), new CardTile("4", "BLOCK"), new CardTile("0.5s", "HIT")], tiles);
    }

    [Fact]
    public void TileAGunsDamageOffBodiesAndBlocksWithItsAmmoItsReloadAndPierceAndItsAmmosShare()
    {
        var gun = ItemCards.Of(Spec.Shots.Nailer.Item, unknown: false);
        Assert.Equal(Spec.Shots.DamageOf(Spec.Shots.Nailer.Item, Spec.Shots.Nail.Id).ToString(), gun.Tiles![0].Value);
        // As content stands: the nailer's 4 and the nail's 2, 1 off a block, a shot every 60 ticks, half kept through a pierce.
        Assert.Equal([new CardTile("6", "DMG"), new CardTile("1", "BLOCK"), new CardTile("3s", "RELOAD"), new CardTile("50%", "PIERCE")], gun.Tiles);
        Assert.True(gun.Tool && ItemCards.IsTool(Spec.Shots.Nailer.Item));
        var nail = ItemCards.Of(Spec.Shots.Nail.Id, unknown: false);
        Assert.Equal([new CardTile("+2", "DMG"), new CardTile("+0", "BLOCK")], nail.Tiles);
        // A tile card, though no tool: its slot is not tinted as one.
        Assert.False(ItemCards.IsTool(Spec.Shots.Nail.Id));
    }

    [Fact]
    public void TileTheHarpoonsRangeReloadAndReelSpeedFromContentInsteadOfDamage()
    {
        var tiles = ItemCards.Of(Spec.Harpoon.Item, unknown: false).Tiles!;
        Assert.Equal(ItemCards.Number(Spec.Harpoon.Range / 1000.0), tiles[0].Value);
        Assert.Equal($"{ItemCards.Number((double)Spec.Harpoon.CooldownTicks / World.TicksPerSecond)}s", tiles[1].Value);
        Assert.Equal($"{ItemCards.Number(Spec.Harpoon.ReelSpeed * World.TicksPerSecond / 1000.0)}/s", tiles[2].Value);
        // As content stands: 40 cells, 100 ticks, 800 thousandths a tick of 20 a second; a cell half what it was (0104).
        Assert.Equal([new CardTile("40", "RANGE"), new CardTile("5s", "RELOAD"), new CardTile("16/s", "REEL")], tiles);
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
        Assert.Equal(new CardKey("LMB / RMB", "hold on debris or ground to break it; RMB, a back wall"), card.Use);
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
        // As content stands: 18 of the 8 px cells (0104), 30 degrees either side.
        Assert.Equal(new CardTile("18", "CELLS REACH"), lamp.Headline);
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
        Assert.Equal("Not analysed", T("card.notAnalysed"));
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
        // A line for each group of hosts, from its trades (0100): the stations', the drill's, the charger's, whose draw is its charging.
        Assert.Equal(new CardTech("arc", "×2 speed · ×2 power\n×2 damage · ×2 power\n×5 charging · ×5 power"), card.Tech);
        Assert.Empty(card.Effects);
        Assert.All(Spec.Modules.Arc.Hosts, host => Assert.DoesNotContain(T($"tool.{host}"), card.Tech!.About));
        Assert.NotEqual(card, card with { Tech = null });
        Assert.Null(ItemCards.Of("iron", unknown: false).Tech);
    }

    [Fact]
    public void DrawAFittedDrillsTilesFromTheSimsFoldMarkingWhatTheArcChangesAndNamingIt()
    {
        string drill = Spec.Weapons.Drill.Item, arc = Spec.Modules.Arc.Item;
        var plain = With(true).TipOf(new Stack(drill, 1)).Card!;
        var fitted = With(true).TipOf(new Stack(drill, 1, Module: arc)).Card!;
        var stats = ToolStats.Of(new Stack(drill, 1, Module: arc));
        // Every number is the fold's, the one the sim hits and draws by: 20 a second, 2 a hit, every 0.1 s, 6 a tick.
        Assert.Equal(ItemCards.Number(stats.Damage * (double)World.TicksPerSecond / stats.Pace), fitted.Tiles![0].Value);
        Assert.Equal(stats.Damage.ToString(), fitted.Tiles[1].Value);
        Assert.Equal(stats.Draw, fitted.Draw);
        var up = new CardBoost("arc", true, "×2");
        Assert.Equal([new CardTile("20", "DPS", up), new CardTile("2", "DMG", up), new CardTile("8", "BLOCK", up), new CardTile("0.1s", "HIT")], fitted.Tiles);
        Assert.Equal((6, up), (fitted.Draw, fitted.DrawBoost));
        Assert.Equal([arc], fitted.Fitted);
        // The plain drill's, unmarked: no stale draw of 3 on the fitted one, nor a mark on the plain.
        Assert.Equal([new CardTile("10", "DPS"), new CardTile("1", "DMG"), new CardTile("4", "BLOCK"), new CardTile("0.1s", "HIT")], plain.Tiles);
        Assert.Equal((3, (CardBoost?)null), (plain.Draw, plain.DrawBoost));
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
        // Summed, then applied once: damage 1 × (1000 + 1000 + 500) / 1000 = 2.5, down to 2; draw 3 × 1750 / 1000 = 5.25, up to 6.
        Assert.Equal(new ToolStats(2, 2, 6), ToolStats.Of(drill, [Spec.Modules.Arc, Fake]));
        Assert.Equal(new ToolStats(1, 2, 3), ToolStats.Of(drill, [Fake]));
        Assert.Equal(new ToolStats(1, 2, 3), ToolStats.Of(drill, []));
        // On the axe, 2 × 1500 / 1000 = 3, a gain alone: it draws nothing.
        Assert.Equal(new ToolStats(3, 10, 0), ToolStats.Of(Spec.Weapons.Axe.Item, [Fake]));
        var card = ItemCards.Fitted(ItemCards.Of(Spec.Weapons.Axe.Item, unknown: false), [Fake]);
        Assert.Equal([new CardTile("6", "DPS", new CardBoost("arc", true, "×1.5")), new CardTile("3", "DMG", new CardBoost("arc", true, "×1.5")), new CardTile("6", "BLOCK", new CardBoost("arc", true, "×1.5")), new CardTile("0.5s", "HIT")], card.Tiles);
        Assert.Null(card.Draw);
        // A cost that falls is marked down.
        var lighter = ItemCards.Fitted(ItemCards.Of(drill, unknown: false), [new Spec.ModuleSpec("lighter", "arc", [new(["drill"], [new Spec.Trade(Spec.Stat.Draw, -500)])])]);
        Assert.Equal((2, new CardBoost("arc", false, "×0.7")), (lighter.Draw!.Value, lighter.DrawBoost!));
        Assert.Equal(ItemCards.Of(drill, unknown: false).Tiles, lighter.Tiles);
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
            Assert.Equal(m.Items.Damage(0, "drill").ToString(), card.Tiles![1].Value);
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
        foreach (var driver in Spec.Screwdrivers.All) Assert.Same(HeldLook.Screwdriver, HeldLook.Of(driver.Item));
    }
}
