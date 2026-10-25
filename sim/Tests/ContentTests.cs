using System.Reflection;
using Stillwake.Content;
using Stillwake.Game;
using Xunit;
using DebrisSpec = Stillwake.Content.Debris;

namespace Stillwake.Sim.Tests;

/// <summary>packages/content/src/content.test.ts, ported.</summary>
public partial class ContentTests
{
    private const int Cell = 1000;

    /// <summary>Every constant of a table, by name: all of them must be whole numbers.</summary>
    private static IReadOnlyList<(string Name, int Value)> Constants(Type table)
    {
        var fields = table.GetFields(BindingFlags.Public | BindingFlags.Static).Where(f => f.IsLiteral).ToList();
        Assert.All(fields, f => Assert.True(f.FieldType == typeof(int), $"{table.Name}.{f.Name} is no whole number"));
        return fields.Select(f => (f.Name, (int)f.GetRawConstantValue()!)).ToList();
    }

    private static void AllPositive(Type table) =>
        Assert.All(Constants(table), c => Assert.True(c.Value > 0, $"{table.Name}.{c.Name} is {c.Value}"));

    /// <summary>Slots <paramref name="amounts"/> fill in an empty inventory, every item in stacks as full as they go.</summary>
    private static int SlotsFor(IEnumerable<(string Item, int Count)> amounts) =>
        amounts.Sum(a => (a.Count + Content.Items.Get(a.Item).Stack - 1) / Content.Items.Get(a.Item).Stack);

    private static IEnumerable<(IReadOnlyList<(string Item, int Count)> Cost, int BuildTicks)> AllBuilds() =>
        Builds.All.Select(b => (b.Cost, b.BuildTicks)).Concat(Machines.All.Where(m => !m.Fixed).Select(m => (m.Cost, m.BuildTicks)))
            .Concat(Materials.All.Where(m => m.Built).Select(m => (m.Cost, m.BuildTicks)));

    // Materials

    [Fact]
    public void MaterialsHaveUniquePositiveCodesSinceSavesStoreTheCode()
    {
        var codes = Materials.All.Select(m => m.Code).ToList();
        Assert.All(codes, c => Assert.True(c > 0));
        Assert.Equal(codes.Count, codes.Distinct().Count());
    }

    [Fact]
    public void MaterialsHavePositiveWholeNumberStats()
    {
        foreach (var m in Materials.All)
        {
            Assert.True(m.Leak > 0, m.Id);
            // Bedrock and lava alone break never and are never built (0077).
            Assert.True(m.Strength > 0 || m.Id is "bedrock" or "lava", m.Id);
            // What is built costs something; a vein costs nothing to build, for it never is, but yields its ore.
            Assert.True(!m.Built || m.Cost.Count > 0, m.Id);
        }
    }

    [Fact]
    public void NaturalGroundBreaksInMoreThanOneHitOfAnyWeaponAndYieldsWhatBuildsIt()
    {
        // A wall breaking in one hit would show no crack before its dust; what a dig gives is what a plan of it costs.
        foreach (var m in Materials.All.Where(m => m.Breaks))
        {
            Assert.All(Weapons.All, w => Assert.True(w.Damage < m.Strength, $"{w.Item} on {m.Id}"));
            Assert.All(m.Cost, c => Assert.True(Content.Items.TryGet(c.Item, out _), c.Item));
        }
        Assert.Equal(["soil", "stone", "bedrock", "lava", "alloy", "plaster", "copperVein", "iceVein"], Materials.All.Where(m => m.Natural).Select(m => m.Id));
        // The ruins' alloy (0085): three seconds of the drill, one and a half of the drill with the arc module (0098), fifteen of
        // the axe; in whole seconds.
        int Seconds(Weapon w) => Materials.Get("alloy").Strength / (w.Damage * World.TicksPerSecond / w.Pace);
        var fitted = Weapons.Drill with { Damage = Sim.Items.Boost.Gain(Weapons.Drill.Damage, Modules.Arc.Adds(Weapons.Drill.Item, Stat.Damage)) };
        Assert.Equal((15, 3, 1), (Seconds(Weapons.Axe), Seconds(Weapons.Drill), Seconds(fitted)));
        // The fitted drill alone takes soil in one hit (the fan of rays: the drill hits 2, the arc doubles it, 0098); stone and harder
        // still crack first.
        Assert.Equal(Materials.Get("soil").Strength, fitted.Damage);
        Assert.True(fitted.Damage < Materials.All.Where(m => m.Breaks && m.Id != "soil").Min(m => m.Strength));
    }

    // Machines

    [Fact]
    public void PlantsHaveUniqueIdsATallnessAndChancesThatFitAThousand()
    {
        // The generator draws under 1000 a column and walks the chances in order (0079): past 1000, the last never grows.
        Assert.Equal(Flora.All.Count, Flora.All.Select(k => k.Id).Distinct().Count());
        Assert.All(Flora.All, k => Assert.True(k.Tall >= 1 && k.Level >= 0 && k.Room >= 0 && k.Clear >= 1 && k.Chance > 0 && k.Apart >= 1, k.Id));
        Assert.InRange(Flora.All.Sum(k => k.Chance), 1, 1000);
    }

    [Fact]
    public void CaveKindsHaveIdsOfTheirOwnAChanceInEveryBandButTheRuinsAndRoomInTheirDraw()
    {
        // One id among the surface's and the caves' (0080): a felled cell names no kind, but the client draws by id.
        var ids = Flora.All.Select(k => k.Id).Concat(Flora.Cave.Select(k => k.Id)).ToList();
        Assert.Equal(ids.Count, ids.Distinct().Count());
        Assert.Equal(PlanetTypes.First.Bands.Order(), PlanetTypes.First.Bands);
        Assert.All(Flora.Cave, k =>
        {
            Assert.Equal(PlanetTypes.First.Bands.Count + 1, k.Chances.Count);
            Assert.True(k.Tall >= 1 && k.Wide >= 0 && k.Apart >= 1 && k.Chances.All(c => c >= 0), k.Id);
            // Grown by some chance in some band, or set by a ruin (0085) and never grown.
            Assert.Equal(!Ruins.Growths.Contains(k.Id), k.Chances.Any(c => c > 0));
            // What lies in the rock lies in its own block of two by two cells, a 16 px cell (0104).
            if (k.On == Flora.Anchor.Rock) Assert.True(k is { Tall: 2, Across: 2, Wide: 0 }, k.Id);
            else Assert.False(k.Floors, k.Id);
        });
        // A slot draws under 10000 and walks its anchor's chances; at their most, in a hall's dense patch, they may pass it,
        // which only crowds out the last of them: the commonest, listed last.
        foreach (var on in Enum.GetValues<Flora.Anchor>())
            for (int band = 0; band <= PlanetTypes.First.Bands.Count; band++)
                Assert.InRange(Flora.Cave.Where(k => k.On == on).Sum(k => k.Chances[band]), 0, 10000);
    }

    [Fact]
    public void SalvageSpawnsOnTheWrecksGridItsLeadRightOfTheHull()
    {
        int right = Wreck.Rows.Max(row => row.TrimEnd().Length - 1);
        Assert.True(right + DebrisSpec.Lead < Wreck.Rows.Max(row => row.Length));
    }

    [Fact]
    public void CentresTheBuildZoneInTheWorldExactlyWithRoomForTheWreckInIt()
    {
        var (world, zone) = ((Content.World.Width, Content.World.Height), (Content.World.ZoneWidth, Content.World.ZoneHeight));
        Assert.InRange(zone.ZoneWidth, 1, world.Width);
        Assert.InRange(zone.ZoneHeight, 1, world.Height);
        // An even difference, so the zone and the wreck sit in the very middle, not a cell off it.
        int wreckWidth = Wreck.Rows.Max(row => row.Length);
        Assert.All(new[] { world.Width - zone.ZoneWidth, world.Height - zone.ZoneHeight, world.Width - wreckWidth, world.Height - Wreck.Rows.Count }, d => Assert.Equal(0, d % 2));
        Assert.True(wreckWidth <= zone.ZoneWidth && Wreck.Rows.Count <= zone.ZoneHeight);
    }

    [Fact]
    public void LeavesBandsOfColdBetweenTheFieldAndTheWorldsEdgeThatEmptyAFullSuitThere()
    {
        AllPositive(typeof(Field));
        // Room past the field every way, so its line is crossed before the grid's wall.
        Assert.True(Content.World.ZoneWidth + 2 * Field.Margin < Content.World.Width);
        Assert.True(Content.World.ZoneHeight + 2 * Field.Margin < Content.World.Height);
        Assert.True(Field.EdgeCost <= Content.Crew.Suit);
        Assert.InRange(Content.Crew.ReportEvery, 1, Content.Crew.PassOut);
    }

    /// <summary>
    /// A surface plant with strength takes more than one hit of any weapon and fewer than a stone wall's (0077): a tree some
    /// seconds of the axe, more than a rock, more than a bush. A tree gives more planks than any cargo crate holds.
    /// </summary>
    [Fact]
    public void TreesRocksAndBushesTakeSeveralHitsOfAnyWeaponATreeTheMostAndGiveMorePlanksThanACrate()
    {
        int Seconds(string kind) => Flora.Get(kind).Strength / (Weapons.Axe.Damage * World.TicksPerSecond / Weapons.Axe.Pace);
        Assert.Equal((3, 2, 1), (Seconds("acacia"), Seconds("rock"), Seconds("bush")));
        foreach (var kind in Flora.All.Where(k => k.Strength > 0))
            Assert.All(Weapons.All, w => Assert.True(w.Damage < kind.Strength && kind.Strength <= Materials.Get("stone").Strength, $"{w.Item} on {kind.Id}"));
        Assert.All(Flora.Cave, k => Assert.Equal(0, k.Strength));
        Assert.True(Flora.Get("acacia").Yields.Single(y => y.Item == "planks").Count > Loot.Of("cargoCrate")!.Single(e => e.Item == "planks").Max);
    }

    /// <summary>
    /// Growths give fixed counts of real items, none of themselves (0080): every cave kind a player would break for
    /// something gives it, and what is only to look at gives nothing.
    /// </summary>
    [Fact]
    public void FloraYieldsWholeCountsOfRealItemsAndEveryKindWorthBreakingGivesSome()
    {
        foreach (var kind in Flora.Every)
            Assert.All(kind.Yields, y => Assert.True(Content.Items.TryGet(y.Item, out _) && y.Count > 0, $"{kind.Id}: {y.Item}"));
        var giving = Flora.All.Concat<Flora.Growth>(Flora.Cave).Where(k => k.Yields.Count > 0).Select(k => k.Id).Order(StringComparer.Ordinal);
        Assert.Equal(
            ["acacia", "bush", "crystalAmber", "crystalAmberHang", "crystalEmber", "crystalIce", "crystalIceHang", "crystalRose", "geode", "glowcapTeal", "glowcapViolet", "hangcap", "heatCrack", "mica", "rock", "spireCache"],
            giving);
        // A geode is a hollow of crystals: more than any one crystal gives.
        Assert.True(Flora.Get("geode").Yields.Single().Count > Flora.Cave.Where(k => k.Id.StartsWith("crystal", StringComparison.Ordinal)).Max(k => k.Yields.Single().Count));
    }

    [Fact]
    public void ResearchAnalysesInWholeTicks() => Assert.True(Content.Research.Ticks > 0);

    // What TypeScript's types guaranteed, which C#'s strings do not.

    [Fact]
    public void GivesEveryKindOfDebrisThatSpawnsItsLooksOnceAndAtLeastOne()
    {
        var spawned = DebrisSpec.Zones.SelectMany(z => z.Tiers).SelectMany(t => t.Kinds).Select(k => k.Item).Distinct().ToList();
        Assert.Equal(Looks.All.Count, Looks.All.Select(l => l.Kind).Distinct().Count());
        Assert.All(Looks.All, l => Assert.True(l.Count > 0, l.Kind));
        Assert.All(spawned, kind => Assert.Contains(kind, Looks.All.Select(l => l.Kind)));
        // Every peer picks a piece's look alike, round its kind's looks.
        Assert.Equal((1, 0, 7), (Looks.Of("cargoCrate", 1), Looks.Of("cargoCrate", 8), Looks.Of("cargoCrate", 15)));
        Assert.Equal(0, Looks.Of("planks", 5));
    }

    [Fact]
    public void EveryMachineALegendCharacterDrawsIsAMachine()
    {
        foreach (var (c, _) in Legend.Map)
            if (Legend.MachineOf(c) is { } kind) Assert.True(Machines.TryGet(kind, out _), kind);
    }

    [Fact]
    public void EveryMachineWithACharacterHasItsOwnAndSaysWhatItMeans()
    {
        var drawn = Machines.All.Where(m => m.Char != null).ToList();
        Assert.All(Machines.All, m => Assert.Equal(m.Char != null, !string.IsNullOrWhiteSpace(m.Means)));
        Assert.Equal(drawn.Count, drawn.Select(m => m.Char).Distinct().Count());
        Assert.Equal(Legend.MapChars.Length, Legend.MapChars.Distinct().Count());
        Assert.All(drawn, m =>
        {
            Assert.Equal(m.Kind, Legend.MachineOf(m.Char!.Value));
            Assert.Contains((m.Char!.Value, m.Means!), Legend.Map);
            Assert.Null(Structures.OfChar(m.Char!.Value));
        });
    }
}
