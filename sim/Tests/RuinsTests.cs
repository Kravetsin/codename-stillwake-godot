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
    private static int At(int x, int y) => y * W + x;
    private static readonly string Axe = Weapons.Axe.Item;

    /// <summary>
    /// A spire's chamber in miniature: alloy all round, a cache on its floor at (1, 3), the relic's cradle three across from (3, 3),
    /// a chip's niche on the back wall at (7, 1), and a bush on soil out at (6, 3) beside the cradle.
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
        """) with { Flora = [(At(7, 1), Ruins.Niche), (At(1, 3), Ruins.SpireCache), (At(3, 3), Ruins.Cradle), (At(6, 3), "bush")] });

    private static int Count(Scenario w, string item, int peer = 0) => w.M.Items.Totals(peer).Where(t => t.Item == item).Sum(t => t.Count);
    private static string[] Kinds(Scenario w) => w.M.Building.Plants.Select(p => p.Kind).ToArray();

    [Fact]
    public void AlloyStandsUnderFiftyNineDamageAndBreaksAtSixtyGivingItsScrap()
    {
        var w = Chamber();
        int hits = Materials.Get("alloy").Strength / Weapons.Axe.Damage;
        Assert.Equal(30, hits);
        for (int i = 0; i < hits - 1; i++) w.Do(new Dig(At(7, 4), Axe)).Run();
        Assert.Equal("alloy", w.M.Grid.WallAt(At(7, 4))?.Id);
        Assert.Equal(58, w.M.Building.Cracks.Single().Damage);
        w.Do(new Dig(At(7, 4), Axe)).Run();
        Assert.Empty(w.Refusals);
        Assert.Null(w.M.Grid.WallAt(At(7, 4)));
        Assert.Equal(1, Count(w, "alloyScrap"));
        // Its back wall stays, alloy too.
        Assert.Equal("alloy", w.M.Grid.BackAt(At(7, 4))?.Id);
    }

    [Fact]
    public void ASpireCacheOpensToAnyHitGivingItsScrapWireAndSilicon()
    {
        var w = Chamber();
        w.Do(new Dig(At(1, 3), Axe)).Run();
        Assert.Empty(w.Refusals);
        Assert.Equal((4, 6, 2), (Count(w, "alloyScrap"), Count(w, "wire"), Count(w, "silicon")));
        Assert.DoesNotContain(Ruins.SpireCache, Kinds(w));
    }

    /// <summary>The chamber with hero 0 by the cradle and hero 1 across it, both in on the same tick, breathing alike.</summary>
    private static Scenario WithHeroes() => Chamber().Do(new Enter(At(2, 3)) { From = 0 }).Do(new Enter(At(6, 2)) { From = 1 }).Run();

    private static int Suit(Scenario w, int peer) => w.M.Crew.HeroOf(peer)!.Suit;
    private static bool Taken(Scenario w, string kind) => w.M.Building.Relics.Single(r => r.Kind == kind).Taken;

    [Fact]
    public void TheCradleSplitsIntoFourShardsAtItsFourthSwingEachHitDischargingFiveIntoItsStriker()
    {
        var w = WithHeroes();
        var cradle = w.M.Building.Plants.Single(p => p.Kind == Ruins.Cradle);
        // Three across and three up from its own cell: a hit on its top right is a hit on it.
        Assert.Equal(cradle, w.M.Building.PlantAt(At(5, 1)));
        // By cell: the niche up on the wall first.
        Assert.Equal([new Relic(At(7, 1), Ruins.Niche, false), new Relic(At(3, 3), Ruins.Cradle, false)], w.M.Building.Relics);
        int swings = Flora.Get(Ruins.Cradle).Strength / Weapons.Axe.Damage;
        Assert.Equal(4, swings);
        for (int i = 1; i < swings; i++)
        {
            w.Do(new Dig(At(5, 1), Axe)).Run();
            // Untouched it only glows: the shock is the striker's alone, as the two breathe alike.
            Assert.Equal(i * Ruins.CradleShock, Suit(w, 1) - Suit(w, 0));
        }
        Assert.Contains(Ruins.Cradle, Kinds(w));
        Assert.Equal(0, Count(w, Ruins.Shard));
        w.Do(new Dig(At(4, 2), Axe)).Run();
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
        w.Do(new Dig(At(1, 3), Axe)).Do(new Dig(At(6, 3), Axe)).Run();
        Assert.Equal(Suit(w, 1), Suit(w, 0));
    }

    [Fact]
    public void TheHomesNicheGivesItsOneShardToASwingAndShocksNobody()
    {
        var w = WithHeroes();
        w.Do(new Dig(At(7, 1), Axe)).Run();
        Assert.Empty(w.Refusals);
        Assert.Equal(1, Count(w, Ruins.Shard));
        Assert.Equal(Suit(w, 1), Suit(w, 0));
        Assert.True(Taken(w, Ruins.Niche));
        Assert.False(Taken(w, Ruins.Cradle));
    }

    [Fact]
    public void ATakenRelicStaysTakenAcrossALoadAndForAFriendJoiningLater()
    {
        // Hero 0 beamed down to a small planet, its one spire and its one home grown with it.
        var w = new Scenario(".S..\n####", 3);
        w.Do(new Enter(w.M.Crew.Spawn)).Run();
        w.Do(new OpenSurface(Content.PlanetTypes.Small.Id)).Run();
        w.Do(new Transfer(1)).Run(2);
        var cradle = w.On(1).Building.Relics.Single(r => r.Kind == Ruins.Cradle);
        Assert.False(cradle.Taken);
        for (int i = 0; i < 4; i++) w.Do(new Dig(cradle.Cell, Axe)).Run();
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
        for (int i = 0; i < Materials.Get("alloy").Strength / Weapons.Axe.Damage; i++) w.Do(new Dig(At(4, 4), Axe)).Run();
        Assert.DoesNotContain(Ruins.Cradle, Kinds(w));
        Assert.True(Taken(w, Ruins.Cradle));
        Assert.Equal(Suit(w, 1), Suit(w, 0));
        // Its digger gets the shards with the alloy: what a wall held goes to who broke it out.
        Assert.Equal((4, 1), (Count(w, Ruins.Shard), Count(w, "alloyScrap")));
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
        var w = BuildFloor(Chamber(), At(1, 3));
        Assert.DoesNotContain(Ruins.SpireCache, Kinds(w));
        // Nobody felled it: nothing in the builder's hands, its yields let fall at its cell, anyone's at once, lying on the
        // floor built there, a cell up.
        Assert.Equal(0, Count(w, "alloyScrap"));
        var floor = w.M.Items.Floor.OrderBy(s => s.Item, StringComparer.Ordinal).Select(s => (s.Item, s.Count, s.Cell, s.By));
        Assert.Equal([("alloyScrap", 4, At(1, 2), FloorStack.Nobody), ("silicon", 2, At(1, 2), FloorStack.Nobody), ("wire", 6, At(1, 2), FloorStack.Nobody)], floor);
    }

    [Fact]
    public void AnyGrowthFelledByNobodyDropsItsYieldsOnceEvenAcrossALoad()
    {
        var w = BuildFloor(Chamber(), At(6, 3));
        Assert.Equal([("seed", 1)], w.M.Items.Floor.Select(s => (s.Item, s.Count)));
        // Felled is saved: a load grows the ground's growth again but for it, and drops nothing more.
        w.Reload().Run(5);
        Assert.DoesNotContain("bush", Kinds(w));
        Assert.Equal([("seed", 1)], w.M.Items.Floor.Select(s => (s.Item, s.Count)));
    }

    /// <summary>
    /// Why the spires stamped on <paramref name="l"/> of ground <paramref name="g"/> break a rule, or null: each at least
    /// <c>RuinClear</c> columns from the middle, none overlapping another, every cell of it as its prefab draws it — alloy walls
    /// whole, never cut to a slope or taken off as a spike, its plugs soil, its open cells backed with alloy — its growths in
    /// place and nothing else grown in its box, anchored at the highest surface across it.
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
            int highest = Enumerable.Range(x0, wide).Min(x => surface[x]);
            if (y0 + g.SpireRise != highest) return $"{at}: anchored at {y0 + g.SpireRise}, the highest ground across it at {highest}";
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
                    string? grows = c switch { Ruins.Cache => Ruins.SpireCache, Ruins.Lamp => Ruins.SpireLamp, _ => null }
                        ?? Ruins.SpireFurnishing.Where(p => (p.X, p.Y) == (dx, dy)).Select(p => p.Kind).FirstOrDefault();
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

    [Fact]
    public void TheSmallPlanetHoldsOneSpireForTheFuzzToDigAndItsSpawnOutsideIt()
    {
        foreach (uint seed in Enumerable.Range(1, 50).Select(s => (uint)s))
        {
            var l = Terrain.Generate(PlanetTypes.Small, seed, out _, out var surface, out var spires);
            var (x0, y0) = Assert.Single(spires);
            int highest = Enumerable.Range(x0, Ruins.SpireWide).Min(x => surface[x]);
            // Anchored at its highest ground, but where the small grid has no room for its tip or its body.
            var g = PlanetTypes.Small.Ground with { SpireRise = highest - y0 };
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
            var w = new Scenario(Terrain.Generate(PlanetTypes.Small, seed)).Run(2);
            Assert.Empty(w.M.Items.Floor);
            Assert.All(Ruins.Growths.Append(Ruins.Cradle), kind => Assert.Contains(kind, Kinds(w)));
            Assert.All(w.M.Building.Relics, r => Assert.False(r.Taken));
        }
    }
}
