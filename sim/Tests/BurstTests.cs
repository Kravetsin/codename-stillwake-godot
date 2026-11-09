using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Stillwake.Content;
using Stillwake.Sim.Building;
using Stillwake.Sim.Crew;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Items;
using Stillwake.Sim.Places;
using Xunit;
using static Stillwake.Sim.Tests.SpireHall;
using Tune = Stillwake.Content.Creatures;

namespace Stillwake.Sim.Tests;

/// <summary>
/// A burst (unified-damage, damage-scale): the hit of a tool or a shot in every direction through the shared wave. A cell costs what is
/// left of its strength, a player's own ship ten times that to their burst (<see cref="Damage.OwnShipTakes"/>); what it pays for breaks
/// and falls for anyone, what it reaches but cannot pay for cracks by what was left, a tenth of it there; bedrock, lava, the fog and
/// machines stop it.
/// </summary>
public class BurstTests
{
    private static readonly int Charge = Blasting.Charge.Power;

    private static Scenario Ground(string map, string materials, Box? zone = null)
    {
        var layout = Layout.Parse(map).WithMaterials(materials).OnPlanet();
        return new(zone is { } z ? layout with { Zone = z } : layout);
    }

    /// <summary>The same aboard the ship in the starting space, where tools break nothing.</summary>
    private static Scenario Aboard(string map, string materials) => new(Layout.Parse(map).WithMaterials(materials));

    /// <summary>A hero on the small spire planet, at its spawn far from the hall; its suit endless if <paramref name="breathing"/>.</summary>
    private static Scenario Down(bool breathing = true)
    {
        var w = new Scenario("""
            ......
            .S....
            ######
            """, 1);
        w.Do(new Enter(w.M.Crew.Spawn)).Run();
        w.Do(new OpenSurface(PlanetTypes.SmallSpire.Id)).Run();
        w.Do(new Transfer(1)).Run();
        if (breathing) w.Do(new SetCreative(true)).Run();
        return w;
    }

    private static int At(Scenario w, int x, int y) => y * w.M.Grid.Width + x;

    private static bool Burst(Scenario w, int cell, int power, int reach = 8) => w.M.Building.Burst(0, cell, power, reach, Spoils.Drop);

    private static (int Cell, int Damage)[] Cracks(Scenario w) => [.. w.M.Building.Cracks.Select(c => (c.Cell, c.Damage))];

    private static int Lying(Scenario w, string item) => w.M.Items.Floor.Where(s => s.Item == item).Sum(s => s.Count);

    /// <summary>A cell of a player's own ship cracked by their burst's <paramref name="left"/>: a tenth, rounded down.</summary>
    private static int Tenth(int left) => left * Damage.OwnShipTakes / 1000;

    private static int Scrap => Materials.Get("scrap").Strength;

    [Fact]
    public void WhatItBreaksFallsThereForAnyoneStoneAsGravelOreAsOreBuiltAsItsCost()
    {
        // Soil, stone, a copper vein and a scrap wall in a row right of the burst, a power to break them all.
        var w = Ground("......####", "......stcw");
        Assert.True(Burst(w, At(w, 5, 0), 4000));
        Assert.All(Enumerable.Range(6, 4), x => Assert.Null(w.M.Grid.WallAt(At(w, x, 0))));
        Assert.Equal((1, 1, 1, 0, 1), (Lying(w, "soil"), Lying(w, "gravel"), Lying(w, "copperOre"), Lying(w, "rock"), Lying(w, "scrapChunk")));
        Assert.All(w.M.Items.Floor, s => Assert.Equal(FloorStack.Nobody, s.By));
        Assert.Equal("gravel", Materials.Get("stone").Crushed);
    }

    [Fact]
    public void ACellItReachesButCannotPayForCracksByWhatWasLeftBuiltOrNatural()
    {
        // From the open cell beside them: 500 less a step's 2 left at each.
        var stone = Ground(".####", ".aaaa");
        Burst(stone, 0, Charge);
        Assert.Equal([(1, Charge - Damage.Step)], Cracks(stone));
        // Scrap on a planet in full: the first paid for, the next cracked by what was left after it.
        var scrap = Ground(".####", ".wwww");
        Burst(scrap, 0, Charge);
        Assert.Null(scrap.M.Grid.WallAt(1));
        Assert.Equal([(2, Charge - Damage.Step - Scrap - Damage.Step)], Cracks(scrap));
    }

    [Fact]
    public void AboardAPlayersOwnBurstPaysTenTimesAndCracksByATenthNineChargesToAScrapWall()
    {
        var w = Aboard(".####", ".wwww");
        Burst(w, 0, Charge);
        Assert.NotNull(w.M.Grid.WallAt(1));
        Assert.Equal([(1, Tenth(Charge - Damage.Step))], Cracks(w));
        Assert.Equal(49, Tenth(Charge - Damage.Step));
        int charges = 1;
        for (; charges < 20 && w.M.Grid.WallAt(1) != null; charges++) Burst(w, 0, Charge);
        Assert.Equal(9, charges);
        Assert.NotNull(w.M.Grid.WallAt(2));
        Assert.Equal(1, Lying(w, "scrapChunk"));
        // A crack under the tenth's 1 is none.
        var faint = Aboard(".#", ".w");
        Burst(faint, 0, Damage.Step + 9);
        Assert.Empty(Cracks(faint));
    }

    [Fact]
    public void AboardABurstOfNoOnesAMeteorsIsInFull()
    {
        var w = Aboard(".####", ".wwww");
        Assert.True(w.M.Building.Burst(FloorStack.Nobody, 0, Charge, 8, Spoils.Lost));
        Assert.Null(w.M.Grid.WallAt(1));
        Assert.Equal([(2, Charge - Damage.Step - Scrap - Damage.Step)], Cracks(w));
    }

    [Fact]
    public void InAVesselsBoxAPlayersBurstIsInFullTheShipBesideItATenth()
    {
        var w = new Scenario(Registry.Wreck);
        w.Do(new Find("shuttle")).Run();
        w.Do(new Jump(w.M.Places.Contacts[^1].Id)).Run();
        var box = w.M.Grid.Reserved!.Value;
        int width = w.M.Grid.Width;
        // The vessel's hull on its box's left edge, iron put just outside it, of the ship.
        int hull = Enumerable.Range(box.Top, box.Bottom - box.Top + 1).Select(y => y * width + box.Left).First(c => w.M.Grid.WallAt(c) != null);
        w.Do(new Place(hull - 1, Placement.Wall("iron"))).Run();
        Assert.Empty(w.Refusals);
        Assert.True(w.M.Building.Burst(0, hull, w.M.Grid.WallAt(hull)!.Strength, 0, Spoils.Drop));
        Assert.Null(w.M.Grid.WallAt(hull));
        int iron = Materials.Get("iron").Strength;
        Assert.True(w.M.Building.Burst(0, hull - 1, iron, 0, Spoils.Drop));
        Assert.NotNull(w.M.Grid.WallAt(hull - 1));
        Assert.Equal([(hull - 1, Tenth(iron))], Cracks(w));
    }

    [Fact]
    public void ABuiltPieceCracksAtItsHeadOnceByTheMostLeft()
    {
        var w = Ground("""
            ....
            .DD.
            .DD.
            ....
            """, """
            ....
            ....
            ....
            ....
            """);
        Burst(w, At(w, 0, 1), Charge);
        Assert.True(Builds.Get("door").Strength > Charge - Damage.Step);
        Assert.Equal([(At(w, 1, 1), Charge - Damage.Step)], Cracks(w));
    }

    [Fact]
    public void ADoorItPaysForOnceAcrossBothCells()
    {
        // Shut in by bedrock: the way on runs through the door's two columns to the soil behind.
        var w = Ground("""
            #####
            .DD##
            .DD##
            #####
            """, """
            bbbbb
            ...sb
            ...sb
            bbbbb
            """);
        int door = Builds.Get("door").Strength;
        // The door once, a step into its second column, the soil: enough for those and no second door.
        Assert.True(Burst(w, At(w, 0, 1), door + 2 + 2 + Materials.Get("soil").Strength + 2));
        Assert.Equal(Structure.Empty, w.M.Grid.StructureAt(At(w, 2, 2)));
        Assert.Null(w.M.Grid.WallAt(At(w, 3, 1)));
        Assert.Equal(Builds.Get("door").Cost.Sum(c => c.Count), Builds.Get("door").Cost.Sum(c => Lying(w, c.Item)));
    }

    [Fact]
    public void BedrockLavaAndTheFogStopIt()
    {
        foreach (var (w, from) in new[] { (Ground(".##", ".bs"), 0), (Ground(".##", ".ls"), 0), (Ground("..#", "..s", new Box(0, 0, 1, 0)), 1) })
        {
            Assert.True(Burst(w, from, 4000));
            Assert.NotNull(w.M.Grid.WallAt(2));
            Assert.Empty(Cracks(w));
        }
        // From inside what stops it, it never goes off.
        var rock = Ground(".#", ".b");
        Assert.False(Burst(rock, 1, 4000));
        Assert.False(Burst(Ground("..#", "..s", new Box(0, 0, 1, 0)), 2, 4000));
    }

    [Fact]
    public void ARelicItTakesShocksItsThrowerWhereverTheyStandOnce()
    {
        var w = Down(breathing: false);
        var m = M(w);
        // Under the cradle's floor, from the rock beneath the hall, charge after charge as a digger would set them.
        int under = Cell(w, 24, 58);
        int air = m.Crew.HeroOf(0)!.Air;
        for (int i = 0; i < 10 && !m.Building.Relics.Single(r => r.Kind == Ruins.Cradle).Taken; i++)
            m.Building.Burst(0, under, Charge, Blasting.Charge.Reach, Spoils.Drop);
        Assert.True(m.Building.Relics.Single(r => r.Kind == Ruins.Cradle).Taken);
        Assert.Equal(Ruins.CradleShock, air - m.Crew.HeroOf(0)!.Air);
        Assert.Equal(Flora.Get(Ruins.Cradle).Yields.Single().Count, m.Items.Floor.Where(s => s.Item == Ruins.Shard).Sum(s => s.Count));
    }

    [Fact]
    public void AWardenWhosePostItBreaksStandsDown()
    {
        var w = Down().Run(Tune.Beat + 1);
        var m = M(w);
        var wardens = m.Creatures.Creatures.Where(c => c.Species == Tune.Warden.Id).ToList();
        Assert.Equal(2, wardens.Count);
        int post = Post(w, Ruins.PostLeft);
        for (int i = 0; i < 6 && m.Building.PostAt(post) != null; i++)
            m.Building.Burst(0, post, Charge, Blasting.Charge.Reach, Spoils.Drop);
        Assert.Null(m.Building.PostAt(post));
        w.Run();
        Assert.Single(m.Creatures.Creatures, c => c.Species == Tune.Warden.Id);
    }

    [Property(MaxTest = 200)]
    public Property NothingItCouldNotPayForBreaksNorCracksWhole()
    {
        const int Width = 9, Height = 7;
        const string Kinds = ".sstawbn";
        var gen =
            from cells in Gen.Choose(0, Kinds.Length - 1).ArrayOf(Width * Height)
            from start in Gen.Choose(0, Width * Height - 1)
            from aboard in Gen.Elements(false, true)
            from power in Gen.Choose(0, aboard ? 15000 : 1500)
            select (cells, start, aboard, power);
        return Prop.ForAll(gen.ToArbitrary(), t =>
        {
            string Row(int y, Func<char, char> look) => new([.. Enumerable.Range(0, Width).Select(x => look(Kinds[t.cells[y * Width + x]]))]);
            string map = string.Join('\n', Enumerable.Range(0, Height).Select(y => Row(y, k => k == '.' ? '.' : '#')));
            string materials = string.Join('\n', Enumerable.Range(0, Height).Select(y => Row(y, k => k)));
            var w = t.aboard ? Aboard(map, materials) : Ground(map, materials);
            var before = Enumerable.Range(0, Width * Height).Select(c => w.M.Grid.WallAt(c)).ToArray();
            Burst(w, t.start, t.power, reach: 4);
            for (int c = 0; c < Width * Height; c++)
            {
                if (before[c] is not { } wall) continue;
                int dx = c % Width - t.start % Width, dy = c / Width - t.start / Width;
                bool far = dx * dx + dy * dy > 16;
                // What it costs alone, from the burst's own cell: no way there is cheaper. A player's own ship, ten times.
                int cost = wall.Strength == Material.Unbreakable ? int.MaxValue : t.aboard ? wall.Strength * 1000 / Damage.OwnShipTakes : wall.Strength;
                bool broken = w.M.Grid.WallAt(c) == null;
                if (broken && (far || (long)cost + (c == t.start ? 0 : Damage.Step) > t.power)) return false;
                int crack = w.M.Building.Cracks.SingleOrDefault(k => k.Cell == c)?.Damage ?? 0;
                bool unbreakable = wall.Strength == Material.Unbreakable;
                if (crack < 0 || (unbreakable ? crack > 0 || broken : crack >= wall.Strength) || far && crack > 0) return false;
            }
            return true;
        });
    }
}
