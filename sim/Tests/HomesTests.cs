using Stillwake.Content;
using Stillwake.Sim.Building;
using Stillwake.Sim.Crew;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Items;
using Xunit;

namespace Stillwake.Sim.Tests;

/// <summary>
/// The arc makers' homes (0091): rooms of alloy and plaster stamped into the first planet's ground, furnished with pieces that are
/// growths over several cells, on floors, ceilings, back walls and each other, each felled by its hits for the page's yields.
/// </summary>
public class HomesTests
{
    private const int W = 12;
    private static int At(int x, int y) => y * W + x;
    private static readonly string Axe = Weapons.Axe.Item;

    /// <summary>A home in miniature: an alloy room, its back wall plaster over an alloy wainscot, its floor row 4.</summary>
    private static Scenario Room(params (int Cell, string Kind)[] pieces) => new(Layout.Parse("""
        ############
        #..........#
        #..........#
        #..........#
        ############
        """).WithMaterials("""
        aaaaaaaaaaaa
        appppppppppa
        appppppppppa
        aaaaaaaaaaaa
        aaaaaaaaaaaa
        """) with { Flora = [.. pieces.OrderBy(p => p.Cell)] });

    private static int Count(Scenario w, string item, int peer = 0) => w.M.Items.Totals(peer).Where(t => t.Item == item).Sum(t => t.Count);
    private static string[] Kinds(Scenario w) => w.M.Building.Plants.Select(p => p.Kind).ToArray();

    /// <summary>Hits <paramref name="cell"/> with <paramref name="tool"/> <paramref name="times"/> times, a tick apart.</summary>
    private static Scenario Hit(Scenario w, int cell, int times = 1, string? tool = null)
    {
        for (int i = 0; i < times; i++) w.Do(new Dig(cell, tool ?? Axe)).Run();
        return w;
    }

    [Fact]
    public void TheirBackWallsArePlasterOverAnAlloyWainscotAndPlasterIsNeverBuilt()
    {
        var w = Room();
        Assert.Equal("plaster", w.M.Grid.BackAt(At(3, 2))?.Id);
        Assert.Equal("alloy", w.M.Grid.BackAt(At(3, 3))?.Id);
        var plaster = Materials.Get("plaster");
        Assert.True(plaster.Natural && !plaster.Built);
    }

    [Fact]
    public void AHitOnAnyCellOfTheBedHitsItAndTheThirdSwingFellsItForItsPlanksAndCloth()
    {
        // The bed stands three cells across and two up from its own, the bottom-left at (2, 3).
        var w = Room((At(2, 3), "bed"));
        var bed = w.M.Building.Plants.Single();
        Assert.All((int[])[At(2, 3), At(3, 3), At(4, 3), At(2, 2), At(4, 2)], cell => Assert.Equal(bed, w.M.Building.PlantAt(cell)));
        Assert.Null(w.M.Building.PlantAt(At(5, 3)));
        Assert.Null(w.M.Building.PlantAt(At(2, 1)));
        // Its crack is kept by its own cell, whichever is hit: six hit points, three swings.
        Hit(w, At(4, 2), 2);
        Assert.Equal((At(2, 3), 4), (w.M.Building.Cracks.Single().Cell, w.M.Building.Cracks.Single().Damage));
        Hit(w, At(3, 3));
        Assert.Empty(w.Refusals);
        Assert.Empty(w.M.Building.Plants);
        Assert.Equal((3, 2), (Count(w, "planks"), Count(w, "cloth")));
    }

    [Fact]
    public void LampsFallToASwingAndAWholeOneGivesAnArcLampTheChestToAnyHitWithItsSmallCache()
    {
        var w = Room((At(5, 1), "ceilLamp"), (At(7, 3), "chest"), (At(9, 1), "sconce"));
        Hit(w, At(5, 1));
        Hit(w, At(9, 1));
        Assert.Equal(2, Count(w, "arcLamp"));
        Assert.Equal(0, Flora.Get("chest").Strength);
        Hit(w, At(7, 3));
        Assert.Empty(w.M.Building.Plants);
        Assert.Equal((2, 1, 1, 1), (Count(w, "planks"), Count(w, "alloyScrap"), Count(w, "wire"), Count(w, "glass")));
    }

    [Fact]
    public void FellingTheTableFellsWhatStandsOnItItsYieldsToTheSameHero()
    {
        // The table two cells across on the floor, the dishes and the candle on it, a row up.
        var w = Room((At(3, 3), "table"), (At(3, 2), "dishes"), (At(4, 2), "arcCandle"));
        // The dishes first: crockery falls to any hit, the table stands.
        Hit(w, At(3, 2));
        Assert.Equal(["arcCandle", "table"], Kinds(w).Order(StringComparer.Ordinal));
        Assert.Equal(1, Count(w, "rock"));
        Hit(w, At(4, 3), 3);
        Assert.Empty(w.M.Building.Plants);
        Assert.Equal((3, 1, 1), (Count(w, "planks"), Count(w, "alloyScrap"), Count(w, "wire")));
        Assert.Empty(w.M.Items.Floor);
    }

    [Fact]
    public void AHitTakesWhatStandsInFrontFirstThenWhatHangsBehindItAndNothingTakesAMural()
    {
        // A stool in front of the child's drawings hung low on the back wall, a mural painted beside them.
        var w = Room((At(2, 3), "stool"), (At(1, 3), "drawings"), (At(5, 2), "mural"));
        Assert.Equal("stool", w.M.Building.PlantAt(At(2, 3))!.Kind);
        Assert.Equal("drawings", w.M.Building.PlantAt(At(1, 3))!.Kind);
        Hit(w, At(2, 3));
        Assert.DoesNotContain("stool", Kinds(w));
        Assert.Equal("drawings", w.M.Building.PlantAt(At(2, 3))!.Kind);
        Hit(w, At(2, 3));
        Assert.DoesNotContain("drawings", Kinds(w));
        // The mural is painted on the plaster: no hit takes it, a hit there finds nothing.
        Assert.Null(w.M.Building.PlantAt(At(6, 2)));
        Hit(w, At(6, 2));
        Assert.Equal("building.nothingToDig", w.Refusals.Single().Reason);
        Assert.Contains("mural", Kinds(w));
    }

    [Fact]
    public void DiggingTheFloorUnderAnyColumnOfAWidePieceFellsItForTheDigger()
    {
        var w = Room((At(5, 3), "bench"));
        // The bench's right column stands on the alloy at (6, 4): thirty swings of the axe.
        Hit(w, At(6, 4), Materials.Get("alloy").Strength / Weapons.Axe.Damage);
        Assert.Empty(w.Refusals);
        Assert.Empty(w.M.Building.Plants);
        Assert.Equal((2, 1, 1), (Count(w, "planks"), Count(w, "cloth"), Count(w, "alloyScrap")));
    }

    [Fact]
    public void AWallBuiltInAPiecesOwnCellFellsItForAnyoneAndOneOnABackWallToo()
    {
        var w = Room((At(3, 3), "basket"), (At(6, 1), "holoPortrait"));
        w.Do(new Place(At(3, 3), Placement.Wall("scrap"))).Do(new Place(At(6, 1), Placement.Wall("scrap"))).Run();
        Assert.Empty(w.M.Building.Plants);
        Assert.Equal(0, Count(w, "planks"));
        var dropped = w.M.Items.Floor.Select(s => (s.Item, s.Count, s.By)).Order().ToArray();
        Assert.Equal([("circuit", 1, FloorStack.Nobody), ("cloth", 1, FloorStack.Nobody), ("glass", 1, FloorStack.Nobody), ("planks", 1, FloorStack.Nobody)], dropped);
    }

    [Fact]
    public void TheFallenBeamAndTheRubbleLieInWallsThatBlockTheWayAndGoWithAnyOfThem()
    {
        // The beam in two alloy walls on the floor, the rubble in two of stone.
        var w = new Scenario(Layout.Parse("""
            ############
            #..........#
            #..........#
            #..##.##...#
            ############
            """).WithMaterials("""
            aaaaaaaaaaaa
            appppppppppa
            appppppppppa
            aaaaaattaaaa
            aaaaaaaaaaaa
            """) with { Flora = [(At(3, 3), "fallenBeam"), (At(6, 3), "rubble")] });
        Assert.All((int[])[At(3, 3), At(4, 3), At(6, 3), At(7, 3)], cell => Assert.Equal(Structure.Wall, w.M.Grid.StructureAt(cell)));
        Assert.Equal("fallenBeam", w.M.Building.PlantAt(At(4, 3))!.Kind);
        // Its right wall dug out, sixty damage of alloy, and the beam goes, its scrap with the wall's.
        Hit(w, At(4, 3), Materials.Get("alloy").Strength / Weapons.Axe.Damage);
        Assert.DoesNotContain("fallenBeam", Kinds(w));
        Assert.Equal(Structure.Wall, w.M.Grid.StructureAt(At(3, 3)));
        Assert.Equal(1 + 3, Count(w, "alloyScrap"));
        // The rubble's stone, six swings.
        Hit(w, At(6, 3), Materials.Get("stone").Strength / Weapons.Axe.Damage);
        Assert.DoesNotContain("rubble", Kinds(w));
        Assert.Equal((1 + 2, 1 + 3 + 1), (Count(w, "rock"), Count(w, "alloyScrap")));
    }

    [Fact]
    public void TheMusicBoxStandsTheAxesFirstSwingAndFallsToItsSecondTheArcDrillsFirst()
    {
        var w = Room((At(3, 3), "musicBox"));
        Hit(w, At(3, 3));
        Assert.Contains("musicBox", Kinds(w));
        Assert.Equal(Weapons.Axe.Damage, w.M.Building.Cracks.Single().Damage);
        Hit(w, At(3, 3));
        Assert.Empty(w.M.Building.Plants);
        Assert.Equal((1, 1), (Count(w, "gear"), Count(w, "copper")));
        // The drill's hit is a third of it; the arc drill's, the whole.
        Assert.Equal((3, 1), ((Flora.Get("musicBox").Strength + Weapons.Drill.Damage - 1) / Weapons.Drill.Damage, Flora.Get("musicBox").Strength / Weapons.ArcDrill.Damage));
    }

    [Fact]
    public void FurnitureTakesTheHitsItsStrengthSaysTheHearthAndTheResonatorAsATree()
    {
        int Swings(string kind) => (Flora.Get(kind).Strength + Weapons.Axe.Damage - 1) / Weapons.Axe.Damage;
        Assert.Equal((1, 2, 3, 6), (Swings("ceilLamp"), Swings("chair"), Swings("table"), Swings("hearth")));
        Assert.Equal(Flora.Get("acacia").Strength, Flora.Get(Ruins.Resonator).Strength);
        var w = Room((At(2, 3), "chair"), (At(5, 3), "hearth"));
        Hit(w, At(2, 2), Swings("chair") - 1);
        Assert.Contains("chair", Kinds(w));
        Hit(w, At(2, 3));
        Assert.DoesNotContain("chair", Kinds(w));
        // The hearth stands floor to ceiling, its flue too: a hit up there is a hit on it.
        Assert.Equal("hearth", w.M.Building.PlantAt(At(6, 1))!.Kind);
        Hit(w, At(6, 1), Swings("hearth"));
        Assert.Empty(w.M.Building.Plants);
        Assert.Equal((3, 2, 1), (Count(w, "alloyScrap"), Count(w, "copper"), Count(w, "coil")));
    }

    /// <summary>A room with a resonator at (5, 3), three cells up, and heroes 0 and 1 brought in on a breath's tick.</summary>
    private static Scenario Resonating(int near, int far)
    {
        var w = Room((At(5, 3), Ruins.Resonator));
        while (w.Crew().Clock % Content.Crew.BreathEvery != Content.Crew.BreathEvery - 1) w.Run();
        w.Do(new Enter(near) { From = 0 }).Do(new Enter(far) { From = 1 }).Run();
        return w;
    }

    [Fact]
    public void TheResonatorZapsAHeroRightByItEveryBreathAndNotOneACellFurther()
    {
        Assert.True(Room((At(5, 3), Ruins.Resonator)).M.Building.Zaps(At(6, 2)));
        var w = Resonating(near: At(6, 2), far: At(8, 2));
        int Suit(int peer) => w.M.Crew.HeroOf(peer)!.Suit;
        // Both breathe the room's thin air alike; the one beside it pays its zap besides, each breath.
        Assert.Equal(Suit(1) - Ruins.Zap, Suit(0));
        w.Run(Content.Crew.BreathEvery);
        Assert.Equal(Suit(1) - 2 * Ruins.Zap, Suit(0));
        // A cell either way of its three: up to its sphere's row and over it, beside it, never two columns off.
        Assert.True(w.M.Building.Zaps(At(4, 1)));
        Assert.True(w.M.Building.Zaps(At(6, 3)));
        Assert.False(w.M.Building.Zaps(At(7, 2)));
        Assert.False(w.M.Building.Zaps(At(3, 2)));
    }

    [Fact]
    public void AFelledResonatorZapsNoMoreAndInCreativeModeItNeverDid()
    {
        var w = Resonating(near: At(6, 2), far: At(8, 2));
        Hit(w, At(5, 1), (Flora.Get(Ruins.Resonator).Strength + 1) / Weapons.Axe.Damage);
        Assert.Empty(w.M.Building.Plants);
        Assert.False(w.M.Building.Zaps(At(6, 2)));
        Assert.Equal((3, 2, 1, 2), (Count(w, "copper"), Count(w, "wire"), Count(w, "crystal"), Count(w, "alloyScrap")));
        int gap = w.M.Crew.HeroOf(1)!.Suit - w.M.Crew.HeroOf(0)!.Suit;
        w.Run(3 * Content.Crew.BreathEvery);
        Assert.Equal(gap, w.M.Crew.HeroOf(1)!.Suit - w.M.Crew.HeroOf(0)!.Suit);
        var creative = Room((At(5, 3), Ruins.Resonator)).Do(new SetCreative(true)).Do(new Enter(At(6, 2))).Run(3 * Content.Crew.BreathEvery);
        Assert.Equal(Content.Crew.Suit, creative.M.Crew.HeroOf(0)!.Suit);
    }

    [Fact]
    public void ZappedDryAHeroPassesOutAsTheColdLeavesIt()
    {
        var w = Resonating(near: At(6, 2), far: At(8, 2));
        for (int i = 0; i < 40 && w.M.Crew.HeroOf(0)!.Out == 0; i++) w.Run(Content.Crew.BreathEvery);
        Assert.True(w.M.Crew.HeroOf(0)!.Out > 0);
        Assert.Equal(0, w.M.Crew.HeroOf(1)!.Out);
    }

    [Fact]
    public void AGrownHomeFellsNothingAsItOpensAndAFelledPieceStaysFelledAcrossALoad()
    {
        foreach (uint seed in new uint[] { 1, 2, 3, 4 })
        {
            // Hero 0 beamed down to a small planet, its one home grown with it.
            var w = new Scenario(".S..\n####", seed);
            w.Do(new Enter(w.M.Crew.Spawn)).Run();
            w.Do(new OpenSurface(Content.Surfaces.SmallPlanet.Id)).Run();
            w.Do(new Transfer(1)).Run(2);
            var m = w.On(1);
            var l = Terrain.Generate(Ground.Small, w.World.Surfaces[1].Seed, out _, out _, out _, out var homes);
            var (hx, hy, home) = Assert.Single(homes);
            Assert.Empty(m.Items.Floor);
            // Every piece of its scheme stands where its prefab sets it.
            Assert.All(home.Pieces, q => Assert.Contains(m.Building.Plants, p => p.Kind == q.Kind && p.Cell == (hy + q.Y) * l.Width + hx + q.X));
            // A cobweb, in every scheme, to any hit; and a load grows the ground again but for it.
            int webCell = (hy + home.Pieces.Single(p => p.Kind == "cobweb").Y) * l.Width + hx + home.Pieces.Single(p => p.Kind == "cobweb").X;
            w.Do(new Dig(webCell, Axe)).Run();
            Assert.DoesNotContain(PiecesOf(w.On(1), "cobweb"), p => p.Cell == webCell);
            int standing = w.On(1).Building.Plants.Count;
            w.Reload().Run(2);
            Assert.DoesNotContain(PiecesOf(w.On(1), "cobweb"), p => p.Cell == webCell);
            Assert.Equal(standing, w.On(1).Building.Plants.Count);
        }
    }

    private static List<Plant> PiecesOf(Registry m, string kind) => m.Building.Plants.Where(p => p.Kind == kind).ToList();

    /// <summary>
    /// Why the homes stamped on <paramref name="l"/> of ground <paramref name="g"/> break a rule, or null: each at least
    /// <c>HomeClear</c> columns from the middle, clear of the others and two columns clear of every spire, inside the planet over
    /// its lava, its ceiling <c>HomeDeepLeast</c> to <c>HomeDeepMost</c> under the lowest surface across it (or moved up to fit),
    /// every cell of it as its prefab draws it, nothing grown in its box but its pieces, each where its anchor holds it.
    /// </summary>
    private static string? Wrong(Ground g, Layout l, IReadOnlyList<(int X, int Y)> spires, IReadOnlyList<(int X, int Y, Ruins.Home Home)> homes, int[] surface)
    {
        int w = l.Width, middle = w / 2, floor = l.Height - g.CoreRows;
        if (homes.Count != g.Homes) return $"{homes.Count} homes";
        var flora = l.Flora!.ToDictionary(p => p.Cell, p => p.Kind);
        int Code(string id) => Materials.Get(id).Code;
        char At(int x, int y) => l.Rows[y][x];
        for (int i = 0; i < homes.Count; i++)
        {
            var (x0, y0, home) = homes[i];
            string at = $"the {home.Id} at ({x0}, {y0})";
            if (g.HomeClear > 0 && x0 + home.Wide - 1 >= middle - g.HomeClear && x0 < middle + g.HomeClear) return $"{at}: within {g.HomeClear} of the middle";
            if (homes.Where((_, j) => j != i).Any(o => o.X < x0 + home.Wide && x0 < o.X + o.Home.Wide)) return $"{at}: overlaps another";
            if (spires.Any(s => s.X < x0 + home.Wide + 2 && x0 - 2 < s.X + Ruins.SpireWide)) return $"{at}: by a spire";
            if (x0 <= g.BedrockSide || x0 + home.Wide >= w - g.BedrockSide || y0 < 1 || y0 + home.Tall > floor - 1) return $"{at}: not inside the planet over its lava";
            int deep = y0 - Enumerable.Range(x0, home.Wide).Max(x => surface[x]);
            if (deep > g.HomeDeepMost || deep < g.HomeDeepLeast && y0 + home.Tall != floor - 1) return $"{at}: {deep} under its ground";
            for (int dy = 0; dy < home.Tall; dy++)
                for (int dx = 0; dx < home.Wide; dx++)
                {
                    char c = home.Rows[dy][dx];
                    int cell = (y0 + dy) * w + x0 + dx;
                    var want = c switch
                    {
                        Ruins.Alloy => ('#', Code("alloy")),
                        Ruins.Open => ('.', Code("alloy")),
                        Ruins.Plaster => ('.', Code("plaster")),
                        Ruins.Plug => ('#', Code("soil")),
                        Ruins.Stone => ('#', Code("stone")),
                        Ruins.OpenStone => ('.', Code("stone")),
                        _ => (At(x0 + dx, y0 + dy), l.Materials![cell]),
                    };
                    if ((At(x0 + dx, y0 + dy), l.Materials![cell]) != want) return $"{at}: ({dx}, {dy}) not as drawn";
                    string? piece = home.Pieces.Where(p => (p.X, p.Y) == (dx, dy)).Select(p => p.Kind).FirstOrDefault();
                    if (flora.GetValueOrDefault(cell) != piece) return $"{at}: ({dx}, {dy}) grows {flora.GetValueOrDefault(cell) ?? "nothing"}, not {piece ?? "nothing"}";
                }
            if (Held(home) is { } loose) return $"{at}: {loose}";
        }
        return null;
    }

    /// <summary>
    /// Why a piece of <paramref name="home"/> is not where its anchor holds it, or null: every cell of one in the open open, of
    /// one in the rock a wall; a floor's standing on a whole wall or a piece under each column, a ceiling's hanging from walls, a
    /// back wall's on a back wall; no two pieces sharing their own cell, nor a cell in front or behind; all inside the home.
    /// </summary>
    private static string? Held(Ruins.Home home)
    {
        var own = new HashSet<(int, int)>();
        var front = new Dictionary<(int, int), string>();
        var behind = new HashSet<(int, int)>();
        char At(int x, int y) => x < 0 || y < 0 || x >= home.Wide || y >= home.Tall ? '?' : home.Rows[y][x];
        bool Wall(int x, int y) => At(x, y) is Ruins.Alloy or Ruins.Stone or Ruins.Plug;
        foreach (var (kind, x, y) in home.Pieces)
        {
            var k = Flora.Get(kind);
            if (!own.Add((x, y))) return $"{kind} at ({x}, {y}) shares its own cell";
            int step = k.On == Flora.Anchor.Floor ? -1 : 1;
            for (int dx = 0; dx < k.Across; dx++)
                for (int i = 0; i < k.Tall; i++)
                {
                    var cell = (x + dx, y + i * step);
                    if (k.On == Flora.Anchor.Rock ? !Wall(cell.Item1, cell.Item2) : At(cell.Item1, cell.Item2) is not (Ruins.Open or Ruins.Plaster or Ruins.OpenStone))
                        return $"{kind} at ({x}, {y}): its cell ({cell.Item1}, {cell.Item2}) is {At(cell.Item1, cell.Item2)}";
                    if (k.Behind ? !behind.Add(cell) : !front.TryAdd(cell, kind)) return $"{kind} at ({x}, {y}): shares ({cell.Item1}, {cell.Item2})";
                }
        }
        foreach (var (kind, x, y) in home.Pieces)
        {
            var k = Flora.Get(kind);
            for (int dx = 0; dx < k.Across; dx++)
            {
                bool held = k.On switch
                {
                    Flora.Anchor.Floor => Wall(x + dx, y + 1) || front.TryGetValue((x + dx, y + 1), out var under) && Flora.Get(under).On == Flora.Anchor.Floor,
                    Flora.Anchor.Ceiling => Wall(x + dx, y - 1),
                    _ => true,
                };
                if (!held) return $"{kind} at ({x}, {y}): nothing holds its column {dx}";
            }
        }
        return null;
    }

    [Fact]
    public void TheFirstPlanetHoldsOneHomeOfEachSchemeClearOfTheMiddleAndTheSpiresWholeAndHeldOnEverySeed()
    {
        foreach (uint seed in Enumerable.Range(1, 50).Select(s => (uint)s))
        {
            var l = Terrain.Generate(Ground.Planet, seed, out _, out var surface, out var spires, out var homes);
            Assert.Null(Wrong(Ground.Planet, l, spires, homes, surface));
            Assert.Equal(Ruins.Homes.Select(h => h.Id).Order(StringComparer.Ordinal), homes.Select(h => h.Home.Id).Order(StringComparer.Ordinal));
        }
    }

    [Fact]
    public void TheSmallPlanetHoldsOneHomeBesideItsSpireEveryScheme()
    {
        var schemes = new HashSet<string>(StringComparer.Ordinal);
        foreach (uint seed in Enumerable.Range(1, 50).Select(s => (uint)s))
        {
            var l = Terrain.Generate(Ground.Small, seed, out _, out var surface, out var spires, out var homes);
            Assert.Null(Wrong(Ground.Small, l, spires, homes, surface));
            schemes.Add(homes.Single().Home.Id);
        }
        // The fuzz digs every scheme's pieces across its seeds.
        Assert.Equal(Ruins.Homes.Count, schemes.Count);
    }

    [Fact]
    public void EveryPieceIsAKindOfItsOwnSizedHeldAndGivingRealItemsAndEveryHomesPiecesHeld()
    {
        var ids = Flora.Every.Select(k => k.Id).ToList();
        Assert.Equal(ids.Count, ids.Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(49, Ruins.Pieces.Count);
        Assert.All(Ruins.Pieces, p =>
        {
            Assert.True(p.Across >= 1 && p.Tall >= 1 && p.Strength >= 0, p.Id);
            // A back wall's pieces are behind what stands in front; a fixed one is painted there and gives nothing.
            if (p.On == Flora.Anchor.Back) Assert.True(p.Behind, p.Id);
            if (p.Fixed) Assert.True(p.On == Flora.Anchor.Back && p.Yields.Count == 0, p.Id);
            Assert.All(p.Yields, y => Assert.True(Content.Items.TryGet(y.Item, out _) && y.Count > 0, $"{p.Id}: {y.Item}"));
        });
        // The new items pay off (the page): cloth from about ten pieces, the arc lamp from the three whole lamps.
        Assert.InRange(Ruins.Pieces.Count(p => p.Yields.Any(y => y.Item == "cloth")), 10, 14);
        Assert.Equal(["ceilLamp", "floorLamp", "sconce"], Ruins.Pieces.Where(p => p.Yields.Any(y => y.Item == "arcLamp")).Select(p => p.Id).Order(StringComparer.Ordinal));
        foreach (var home in Ruins.Homes)
        {
            Assert.All(home.Rows, r => Assert.Equal(home.Wide, r.Length));
            Assert.All(home.Rows, r => Assert.True(r.All(c => c is Ruins.Alloy or Ruins.Open or Ruins.Plaster or Ruins.Plug or Ruins.Stone or Ruins.OpenStone), home.Id));
            Assert.Null(Held(home));
        }
        // Every piece is in some home.
        Assert.All(Ruins.Pieces, p => Assert.Contains(Ruins.Homes, h => h.Pieces.Any(q => q.Kind == p.Id)));
    }

    [Fact]
    public void TheSpiresChamberIsFurnishedEachPieceHeldAndClearOfItsCacheAndShrine()
    {
        Assert.Null(Held(new Ruins.Home("spire", Ruins.Spire, Ruins.SpireFurnishing)));
        // Nothing stands in the cache's cell nor in the shrine's.
        Assert.DoesNotContain(Ruins.SpireFurnishing, p => Ruins.Spire[p.Y][p.X] is Ruins.Cache or Ruins.Shrine);
        foreach (uint seed in Enumerable.Range(1, 20).Select(s => (uint)s))
        {
            var l = Terrain.Generate(Ground.Small, seed, out _, out _, out var spires, out _);
            var (x0, y0) = spires.Single();
            Assert.All(Ruins.SpireFurnishing, q => Assert.Contains(l.Flora!, p => p.Kind == q.Kind && p.Cell == (y0 + q.Y) * l.Width + x0 + q.X));
        }
    }
}
