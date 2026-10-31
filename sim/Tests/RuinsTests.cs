using Stillwake.Content;
using Stillwake.Sim.Building;
using Stillwake.Sim.Crew;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Items;
using Xunit;

namespace Stillwake.Sim.Tests;

/// <summary>
/// Ruins on the first planet (0085): spires of alloy stamped into the ground at places the seed chooses, a cache and a relic's
/// cradle in each (0096); and a growth felled by nobody letting its yields fall where it stood.
/// </summary>
public class RuinsTests
{
    private const int W = 9;
    /// <summary>The drawn chamber's cell at (<paramref name="x"/>, <paramref name="y"/>): where its growths are listed, before it doubles.</summary>
    private static int At(int x, int y) => y * W + x;

    /// <summary>The lower row of the drawn character at (<paramref name="x"/>, <paramref name="y"/>): a floor growth's own cell.</summary>
    private static int Bottom(Scenario w, int x, int y) => w.At(x, y) + w.Layout.Width;
    private static readonly string Axe = Weapons.Axe.Item;

    /// <summary>
    /// A spire's chamber in miniature: alloy all round, a cache on its floor at (1, 3), the relic's cradle three across from (3, 3),
    /// a chip's niche on the back wall at (7, 1), and a bush on soil out at (6, 3) beside the cradle; doubled, as a spire is.
    /// </summary>
    private static Scenario Chamber() => new(Layout.Parse("""
        #########
        #.......#
        #.......#
        #.......#
        #########
        """).WithMaterials("""
        aaaaaaaaa
        a.......a
        a.......a
        a.......a
        aaaaaasaa
        """) with { Flora = [(At(7, 1), Ruins.Niche), (At(1, 3), Ruins.SpireCache), (At(3, 3), Ruins.Cradle), (At(6, 3), "bush")], Kind = Content.Surfaces.TestStrip.Id }, doubled: true);

    private static int Count(Scenario w, string item, int peer = 0) => w.M.Items.Totals(peer).Where(t => t.Item == item).Sum(t => t.Count);
    private static string[] Kinds(Scenario w) => w.M.Building.Plants.Select(p => p.Kind).ToArray();

    [Fact]
    public void AlloyStandsUnderFiftyNineDamageAndBreaksAtSixtyGivingItsScrap()
    {
        var w = Chamber();
        int hits = Materials.Get("alloy").Strength / Weapons.Axe.BlockDamage;
        Assert.Equal(15, hits);
        for (int i = 0; i < hits - 1; i++) w.Do(HewTesting.Dig(w, w.At(7, 4), Axe)).Run();
        Assert.Equal("alloy", w.M.Grid.WallAt(w.At(7, 4))?.Id);
        Assert.Equal(56, w.M.Building.Cracks.Single().Damage);
        w.Do(HewTesting.Dig(w, w.At(7, 4), Axe)).Run();
        Assert.Empty(w.Refusals);
        Assert.Null(w.M.Grid.WallAt(w.At(7, 4)));
        Assert.Equal(3, Count(w, "alloyScrap"));
        // Its back wall stays, alloy too.
        Assert.Equal("alloy", w.M.Grid.BackAt(w.At(7, 4))?.Id);
    }

    [Fact]
    public void ASpireCacheOpensToAnyHitGivingItsScrapWireAndSilicon()
    {
        var w = Chamber();
        w.Do(HewTesting.Dig(w, w.At(1, 3), Axe)).Run();
        Assert.Empty(w.Refusals);
        Assert.Equal((4, 6, 2), (Count(w, "alloyScrap"), Count(w, "wire"), Count(w, "silicon")));
        Assert.DoesNotContain(Ruins.SpireCache, Kinds(w));
    }

    /// <summary>The chamber with hero 0 by the cradle and hero 1 across it, both in on the same tick, breathing alike.</summary>
    private static Scenario WithHeroes()
    {
        var w = Chamber();
        return w.Do(new Enter(w.At(2, 3)) { From = 0 }).Do(new Enter(w.At(6, 2)) { From = 1 }).Run();
    }

    private static int Suit(Scenario w, int peer) => w.M.Crew.HeroOf(peer)!.Air;
    private static bool Taken(Scenario w, string kind) => w.M.Building.Relics.Single(r => r.Kind == kind).Taken;

    [Fact]
    public void TheCradleSplitsIntoFourShardsAtItsFourthSwingEachHitDischargingFiveIntoItsStriker()
    {
        var w = WithHeroes();
        var cradle = w.M.Building.Plants.Single(p => p.Kind == Ruins.Cradle);
        // Three characters across and three up from its own cell: a hit on its top right is a hit on it.
        Assert.Equal(cradle, w.M.Building.PlantAt(w.At(5, 1)));
        // By cell: the niche up on the wall first.
        Assert.Equal([new Relic(w.At(7, 1), Ruins.Niche, false), new Relic(Bottom(w, 3, 3), Ruins.Cradle, false)], w.M.Building.Relics);
        int swings = Flora.Get(Ruins.Cradle).Strength / Weapons.Axe.BlockDamage;
        Assert.Equal(4, swings);
        for (int i = 1; i < swings; i++)
        {
            w.Do(HewTesting.Dig(w, w.At(5, 1), Axe)).Run();
            // Untouched it only glows: the shock is the striker's alone, as the two breathe alike.
            Assert.Equal(i * Ruins.CradleShock, Suit(w, 1) - Suit(w, 0));
        }
        Assert.Contains(Ruins.Cradle, Kinds(w));
        Assert.Equal(0, Count(w, Ruins.Shard));
        w.Do(HewTesting.Dig(w, w.At(4, 2), Axe)).Run();
        Assert.Empty(w.Refusals);
        Assert.Equal(swings * Ruins.CradleShock, Suit(w, 1) - Suit(w, 0));
        Assert.Equal(4, Count(w, Ruins.Shard));
        Assert.DoesNotContain(Ruins.Cradle, Kinds(w));
        Assert.True(Taken(w, Ruins.Cradle));
        Assert.False(Taken(w, Ruins.Niche));
        // A shard is gear: kept when its hero dies.
        Assert.True(Content.Items.Get(Ruins.Shard).Gear);
    }

    [Fact]
    public void AFasterToolPaysItsShareOfTheAxesShockSoASecondOfHittingCostsAlike()
    {
        Assert.Equal((5, 1), (BuildingModule.ShockOf(Axe), BuildingModule.ShockOf(Weapons.Drill.Item)));
        // Twenty ticks a second: the axe's 2 hits, the drill's 10, each 10 units a second, its module or none.
        Assert.All(Weapons.All, weapon => Assert.Equal(10, BuildingModule.ShockOf(weapon.Item) * 20 / weapon.Pace));
    }

    [Fact]
    public void AHitOnAnythingElseInTheChamberShocksNobody()
    {
        var w = WithHeroes();
        // The cache aimed at leftward and the bush rightward, away from the cradle between them: their fans miss it.
        var (cache, bush) = (HewTesting.Dig(w, w.At(1, 3), Axe, heading: 128), HewTesting.Dig(w, w.At(6, 3), Axe, heading: 0));
        Assert.All((Hew[])[cache, bush], h => Assert.DoesNotContain(w.M.Building.Struck(Axe, h.X, h.Y, h.Heading).Plants, p => p.Kind == Ruins.Cradle));
        w.Do(cache).Do(bush).Run();
        Assert.Empty(w.Refusals);
        Assert.Equal(Suit(w, 1), Suit(w, 0));
    }

    [Fact]
    public void TheHomesNicheGivesItsOneShardToASwingAndShocksNobody()
    {
        var w = WithHeroes();
        w.Do(HewTesting.Dig(w, w.At(7, 1), Axe)).Run();
        Assert.Empty(w.Refusals);
        Assert.Equal(1, Count(w, Ruins.Shard));
        Assert.Equal(Suit(w, 1), Suit(w, 0));
        Assert.True(Taken(w, Ruins.Niche));
        Assert.False(Taken(w, Ruins.Cradle));
    }

    [Fact]
    public void ATakenRelicStaysTakenAcrossALoadAndForAFriendJoiningLater()
    {
        // Hero 0 beamed down to a small planet, its one spire grown with it.
        var w = new Scenario(".S..\n####", 3);
        w.Do(new Enter(w.M.Crew.Spawn)).Run();
        w.Do(new OpenSurface(Content.PlanetTypes.SmallSpire.Id)).Run();
        w.Do(new Transfer(1)).Run(2);
        var cradle = w.On(1).Building.Relics.Single(r => r.Kind == Ruins.Cradle);
        Assert.False(cradle.Taken);
        for (int i = 0; i < 4; i++) w.Do(HewTesting.Dig(w, cradle.Cell, Axe, surface: 1)).Run();
        var taken = w.On(1).Building.Relics.ToList();
        Assert.Contains(cradle with { Taken = true }, taken);
        // What a friend joining later gets is the host's save: a world loaded from it reads its relics from the felled cells alone.
        var joiner = new Scenario(".S..\n####", 3).Reload(w.World.Save());
        Assert.Equal(taken, joiner.On(1).Building.Relics);
        w.Reload().Run(5);
        Assert.Equal(taken, w.On(1).Building.Relics);
        Assert.DoesNotContain(Ruins.Cradle, w.On(1).Building.Plants.Select(p => p.Kind));
    }

    [Fact]
    public void ACradleWhoseFloorIsDugOutFallsGivingItsShardsToTheDiggerUnshocked()
    {
        var w = WithHeroes();
        for (int i = 0; i < Materials.Get("alloy").Strength / Weapons.Axe.BlockDamage; i++) w.Do(HewTesting.Dig(w, w.At(4, 4), Axe)).Run();
        Assert.DoesNotContain(Ruins.Cradle, Kinds(w));
        Assert.True(Taken(w, Ruins.Cradle));
        Assert.Equal(Suit(w, 1), Suit(w, 0));
        // Its digger gets the shards with the alloy: what a wall held goes to who broke it out.
        Assert.Equal((4, 3), (Count(w, Ruins.Shard), Count(w, "alloyScrap")));
    }

    /// <summary>Builds a floor in creative mode at <paramref name="cell"/>, working its plan to the end.</summary>
    private static Scenario BuildFloor(Scenario w, int cell)
    {
        w.Do(new SetCreative(true)).Run();
        w.Do(new PlacePlan(cell, new Placement("structure", "floor"))).Run();
        int plan = w.M.Building.Plans.Single().Id;
        for (int i = 0; i < 100 && w.M.Building.Plans.Count > 0; i++) w.Do(new Work(plan)).Run();
        Assert.Equal(Structure.Floor, w.M.Grid.StructureAt(cell));
        return w;
    }

    [Fact]
    public void AFloorBuiltOverACacheFellsItAndItsYieldsFallThereForAnyone()
    {
        var w = Chamber();
        BuildFloor(w, Bottom(w, 1, 3));
        Assert.DoesNotContain(Ruins.SpireCache, Kinds(w));
        // Nobody felled it: nothing in the builder's hands, its yields let fall at its cell, anyone's at once, lying on the
        // floor built there, a cell up.
        Assert.Equal(0, Count(w, "alloyScrap"));
        var floor = w.M.Items.Floor.OrderBy(s => s.Item, StringComparer.Ordinal).Select(s => (s.Item, s.Count, s.Cell, s.By));
        int up = w.At(1, 3);
        Assert.Equal([("alloyScrap", 4, up, FloorStack.Nobody), ("silicon", 2, up, FloorStack.Nobody), ("wire", 6, up, FloorStack.Nobody)], floor);
    }

    [Fact]
    public void AnyGrowthFelledByNobodyDropsItsYieldsOnceEvenAcrossALoad()
    {
        var w = Chamber();
        BuildFloor(w, Bottom(w, 6, 3));
        Assert.Equal([("seed", 1)], w.M.Items.Floor.Select(s => (s.Item, s.Count)));
        // Felled is saved: a load grows the ground's growth again but for it, and drops nothing more.
        w.Reload().Run(5);
        Assert.DoesNotContain("bush", Kinds(w));
        Assert.Equal([("seed", 1)], w.M.Items.Floor.Select(s => (s.Item, s.Count)));
    }

    /// <summary>
    /// Why the spires stamped on <paramref name="l"/> of ground <paramref name="g"/> break a rule, or null: each at least
    /// <c>RuinClear</c> columns from the middle, none overlapping another, every cell of it as its prefab draws it — alloy walls
    /// whole, never taken off as a spike, its plugs soil, its open cells backed with alloy — its growths in
    /// place and nothing else grown in its box, anchored at the highest surface across its tip.
    /// </summary>
    private static string? Wrong(Ground g, Layout l, IReadOnlyList<(int X, int Y)> spires, int[] surface)
    {
        var prefab = Ruins.Spire;
        int w = l.Width, wide = Ruins.SpireWide, tall = prefab.Count, middle = w / 2;
        int alloy = Materials.Get("alloy").Code, soil = Materials.Get("soil").Code;
        if (spires.Count != g.Spires) return $"{spires.Count} spires";
        var flora = l.Flora!.ToDictionary(p => p.Cell, p => p.Kind);
        for (int i = 0; i < spires.Count; i++)
        {
            var (x0, y0) = spires[i];
            string at = $"the spire at ({x0}, {y0})";
            if (x0 + wide - 1 > middle - g.RuinClear && x0 < middle + g.RuinClear) return $"{at}: within {g.RuinClear} of the middle";
            if (spires.Where((_, j) => j != i).Any(o => o.X < x0 + wide && x0 < o.X + wide)) return $"{at}: overlaps another";
            int highest = Enumerable.Range(x0 + Ruins.SpireTip, Ruins.SpireTipWide).Min(x => surface[x]);
            if (y0 + g.SpireRise != highest) return $"{at}: anchored at {y0 + g.SpireRise}, the highest ground across its tip at {highest}";
            if (x0 <= g.Fog || x0 + wide > w - g.Fog - 1) return $"{at}: out of the zone";
            for (int dy = 0; dy < tall; dy++)
                for (int dx = 0; dx < wide; dx++)
                {
                    char c = prefab[dy][dx];
                    int cell = (y0 + dy) * w + x0 + dx;
                    var (row, code) = (l.Rows[y0 + dy][x0 + dx], l.Materials![cell]);
                    var want = c switch
                    {
                        Ruins.Open or Ruins.Cache => ('.', alloy),
                        Ruins.Plug => ('#', soil),
                        Ruins.Keep => (row, code),
                        _ => ('#', alloy),
                    };
                    if ((row, code) != want) return $"{at}: ({dx}, {dy}) is {row} of {code}, not {want.Item1} of {want.Item2}";
                    string? drawn = c switch { Ruins.Cache => Ruins.SpireCache, Ruins.Lamp => Ruins.SpireLamp, _ => null };
                    // A growth the prefab draws grows once in its character's 2×2 (0104): a floor's at its bottom-left, else its top-left.
                    if (drawn != null && (dx % 2 != 0 || dy % 2 != (Flora.Get(drawn).On == Flora.Anchor.Floor ? 1 : 0))) drawn = null;
                    string? grows = drawn ?? Ruins.SpireFurnishing.Where(p => (p.X, p.Y) == (dx, dy)).Select(p => p.Kind).FirstOrDefault();
                    if (flora.GetValueOrDefault(cell) != grows) return $"{at}: ({dx}, {dy}) grows {flora.GetValueOrDefault(cell) ?? "nothing"}, not {grows ?? "nothing"}";
                }
        }
        return null;
    }

    [Fact]
    public void TheFirstPlanetHoldsFourSpiresClearOfTheMiddleApartWholeAndAnchoredAtTheirHighestGroundOnEverySeed()
    {
        foreach (uint seed in Enumerable.Range(1, 50).Select(s => (uint)s))
        {
            var l = Terrain.Generate(PlanetTypes.First, seed, out _, out var surface, out var spires);
            Assert.Null(Wrong(PlanetTypes.First.Ground, l, spires, surface));
            // Spread along the planet: both sides of the middle hold one.
            Assert.Contains(spires, s => s.X < l.Width / 2);
            Assert.Contains(spires, s => s.X > l.Width / 2);
        }
    }

    /// <summary>Why a hall stands open under ground too high for its roof: the place the plan accepts only where its band holds no other.</summary>
    private static string? Bare(PlanetType type, uint seed)
    {
        Terrain.Generate(type, seed, out _, out var surface, out var spires);
        foreach (var (x0, y0) in spires)
        {
            int ground = Enumerable.Range(x0, Ruins.SpireWide).Max(x => surface[x]);
            if (y0 + Ruins.SpireHall < ground + Ruins.HallCover) return $"seed {seed}: the hall at ({x0}, {y0}), its roof at {y0 + Ruins.SpireHall}, under ground at {ground}";
        }
        return null;
    }

    [Fact]
    public void EveryHallOnTheFirstPlanetLiesUnderCoverOnEverySeed()
    {
        foreach (uint seed in Enumerable.Range(1, 50).Select(s => (uint)s)) Assert.Null(Bare(PlanetTypes.First, seed));
    }

    [Fact]
    public void TheSmallSpirePlanetHoldsOneSpireForTheFuzzToDigAndItsSpawnOutsideIt()
    {
        foreach (uint seed in Enumerable.Range(1, 50).Select(s => (uint)s))
        {
            var l = Terrain.Generate(PlanetTypes.SmallSpire, seed, out _, out var surface, out var spires);
            var (x0, y0) = Assert.Single(spires);
            int highest = Enumerable.Range(x0 + Ruins.SpireTip, Ruins.SpireTipWide).Min(x => surface[x]);
            // Anchored at its highest ground, but where the small grid has no room for its tip or its body.
            var g = PlanetTypes.SmallSpire.Ground with { SpireRise = highest - y0 };
            Assert.Null(Wrong(g, l, spires, surface));
            int spawn = Assert.Single(l.CellsOf('S')) % l.Width;
            Assert.True(spawn < x0 || spawn >= x0 + Ruins.SpireWide, $"seed {seed}: spawn in the spire");
        }
    }

    [Fact]
    public void AFreshPlanetFellsNothingAsItOpensItsRuinsGrowthHeldWhereItStands()
    {
        foreach (uint seed in new uint[] { 1, 2, 3, 4, 5 })
        {
            var w = new Scenario(Terrain.Generate(PlanetTypes.SmallSpire, seed)).Run(2);
            Assert.Empty(w.M.Items.Floor);
            Assert.All(Ruins.Growths.Append(Ruins.Cradle), kind => Assert.Contains(kind, Kinds(w)));
            Assert.All(w.M.Building.Relics, r => Assert.False(r.Taken));
        }
    }
}
