using Stillwake.Sim.Atmosphere;
using Stillwake.Sim.Building;
using Stillwake.Sim.Crew;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Items;
using Xunit;
using Structure = Stillwake.Content.Structure;

namespace Stillwake.Sim.Tests;

/// <summary>A planet's rules from its kind (0076, slice 4), on the test strip: gravity, thin air, no debris, a build list, no cold at the edge.</summary>
public class PlanetRulesTests
{
    /// <summary>A ship with a room heroes wake in at its S, and space round it.</summary>
    private const string Ship = """
        ............
        .##########.
        .#..D..S..#.
        .##########.
        ............
        """;

    private static readonly Content.Surfaces.Kind Strip = Content.Surfaces.TestStrip;
    /// <summary>The strip's width in cells: drawn a character a 16 px cell, it is doubled as it opens (0104).</summary>
    private static readonly int Width = 2 * Strip.Rows[0].Length;
    /// <summary>The top-left cell of the strip's character at (<paramref name="x"/>, <paramref name="y"/>).</summary>
    private static int On(int x, int y) => 2 * y * Width + 2 * x;

    /// <summary>The bottom-left cell of the strip's character at (<paramref name="x"/>, <paramref name="y"/>): what stands on the ground under it is there.</summary>
    private static int Feet(int x, int y) => On(x, y) + Width;

    /// <summary>The cell (<paramref name="x"/>, <paramref name="y"/>) of the strip, a cell of its own.</summary>
    private static int Cell(int x, int y) => y * Width + x;

    /// <summary>The strip's ground: its top row on the flat, where the hero wakes, at column 30.</summary>
    private const int Ground = 30;

    /// <summary>The ship of <see cref="Ship"/> with the strip opened and hero 0 beamed down to it, carrying <paramref name="items"/>.</summary>
    private static Scenario Down(params (string Item, int Count)[] items)
    {
        var w = new Scenario(Ship);
        w.Do(new Enter(w.M.Crew.Spawn)).Do(new OpenSurface(Strip.Id)).Run();
        if (items.Length > 0) w.GiveItems(0, items);
        return w.Do(new Transfer(1)).Run();
    }

    [Fact]
    public void TheStripsKindHasItsOwnRulesAndTheShipsAreAsEver()
    {
        var w = Down();
        Assert.Same(Content.Surfaces.Ship, w.M.Rules);
        Assert.Same(Strip, w.On(1).Rules);
        Assert.True(Content.Surfaces.Ship.Floats);
        Assert.False(Strip.Floats);
        // Two thirds of the ship's, rounded down, never below 1.
        Assert.Equal(Content.Floor.Gravity * 7 / 10, Strip.Fall(Content.Floor.Gravity));
        Assert.Equal(Content.Hero.Gravity * 7 / 10, Strip.Fall(Content.Hero.Gravity));
        Assert.Equal(1, Strip.Fall(1));
        Assert.Equal(Content.Floor.Gravity, Content.Surfaces.Ship.Fall(Content.Floor.Gravity));
    }

    [Fact]
    public void NothingFloatsOnAPlanetItsOpenSkyIncludedAsSpaceDoesAboard()
    {
        var w = Down();
        var m = w.On(1);
        int sky = On(20, 5);
        Assert.Equal(Rooms.RoomsModule.Space, m.Rooms.RoomAt(sky));
        Assert.False(m.Rooms.Afloat(sky));
        Assert.True(w.M.Rooms.Afloat(0));
        Assert.False(w.M.Rooms.Afloat(w.M.Crew.Spawn));
    }

    [Fact]
    public void AStackLetGoInThePlanetsSkyFallsUnderItsGravityAndRestsOnTheGround()
    {
        var w = Down(("scrap", 3));
        var m = w.On(1);
        w.Do(new Drop(new Items.Place(0), 3, On(20, 5), 0)).Run();
        var s = Assert.Single(m.Items.Floor);
        int x = s.X;
        var speeds = new List<int>();
        for (int i = 0; i < 5; i++)
        {
            w.Run();
            speeds.Add(s.Vy);
        }
        // Gaining the planet's fall a tick, not the ship's, and never drifting off as in space.
        int fall = Strip.Fall(Content.Floor.Gravity);
        Assert.All(speeds.Zip(speeds.Skip(1)), p => Assert.Equal(fall, p.Second - p.First));
        Assert.Equal(x, s.X);
        w.Run(200);
        Assert.Equal((Feet(20, Ground - 1), 0, 0), (s.Cell, s.Vx, s.Vy));
    }

    [Fact]
    public void OutsideOnAPlanetTheSuitPaysOneBreathInItsThinAirsEvery()
    {
        int every = Strip.ThinAir!.Value, span = every * Content.Crew.BreathEvery;
        var w = Down();
        var hero = w.On(1).Crew.HeroOf(0)!;
        w.Run(span);
        Assert.Equal(Content.Crew.Suit - 1, hero.Suit);
        w.Run(3 * span);
        Assert.Equal(Content.Crew.Suit - 4, hero.Suit);

        // Out in the ship's space, a breath every breath.
        var ship = new Scenario(Ship);
        ship.Do(new Enter(0)).Run(span);
        Assert.Equal(Content.Crew.Suit - every, ship.M.Crew.HeroOf(0)!.Suit);
    }

    [Fact]
    public void ARunningHeroOutsideOnAPlanetPaysItsWholeExertionOnAPayingBreathOnly()
    {
        int span = Strip.ThinAir!.Value * Content.Crew.BreathEvery;
        var w = Down();
        w.Do(new Exert(true)).Run(span);
        Assert.Equal(Content.Crew.Suit - Content.Crew.Exerted, w.On(1).Crew.HeroOf(0)!.Suit);
    }

    [Fact]
    public void InASealedAiredRoomOnAPlanetTheSuitPaysNothing()
    {
        var w = Down();
        var m = w.On(1);
        // A box of walls a cell thick up in the sky, five by five cells inside, its back walled, and air put in every cell of it.
        var inside = new List<int>();
        for (int y = 20; y <= 26; y++)
            for (int x = 40; x <= 46; x++)
            {
                bool wall = y is 20 or 26 || x is 40 or 46;
                m.Grid.Place(Cell(x, y), wall ? Placement.Wall() : Placement.BackWall());
                if (!wall) inside.Add(Cell(x, y));
            }
        w.Run();
        int room = m.Rooms.RoomAt(Cell(43, 23))!.Value;
        Assert.NotEqual(Rooms.RoomsModule.Space, room);
        // Put in through a save: what the atmosphere keeps of its air is found again on a load.
        ((AtmosphereState)m.Atmosphere.Slice).Aired.AddRange(inside);
        m = w.Reload().On(1);
        // In its middle, where its air lasts longest as it leaks through the walls: a paying breath and four more.
        w.Do(new Enter(Cell(43, 23))).Run(Strip.ThinAir!.Value * Content.Crew.BreathEvery);
        Assert.Equal(Content.Crew.Suit, m.Crew.HeroOf(0)!.Suit);
    }

    [Fact]
    public void APlanetCostsNoColdAtItsEdgeItsWholeGridIsItsField()
    {
        var w = Down();
        var crew = w.On(1).Crew;
        int cells = Width * 2 * Strip.Rows.Count;
        Assert.All(Enumerable.Range(0, cells), cell => Assert.Equal(0, crew.EdgeCost(cell)));
        // At the strip's very corner, outside, the suit still pays its thin air alone.
        w.Do(new Enter(0)).Run(Strip.ThinAir!.Value * Content.Crew.BreathEvery);
        Assert.Equal(Content.Crew.Suit - 1, crew.HeroOf(0)!.Suit);
    }

    [Fact]
    public void TheJetpackDoesNothingOnAPlanetItThrustsNoneAndVentsNothing()
    {
        Scenario Thrusting(bool on)
        {
            var w = new Scenario(Ship);
            w.Do(new Enter(w.M.Crew.Spawn)).Do(new OpenSurface(Strip.Id)).Run();
            w.Wear(0, Content.Jetpack.Item).Do(new Transfer(1)).Run();
            if (on) w.Do(new Thrust(true));
            return w.Run(3 * Content.Jetpack.TicksPerUnit * Content.Crew.BreathEvery);
        }
        var on = Thrusting(true).On(1).Crew.HeroOf(0)!;
        var off = Thrusting(false).On(1).Crew.HeroOf(0)!;
        Assert.Equal((false, 0), (on.Thrusting, on.ThrustTicks));
        Assert.Equal(off.Suit, on.Suit);
    }

    [Fact]
    public void NoDebrisDriftsOnAPlanetNorRoundAHeroThere()
    {
        var w = Down().Run(4 * Content.Debris.Every);
        Assert.Empty(w.On(1).Debris.Pieces);
        // The ship's debris drifts round the ship's heroes only, and the hero is on the planet.
        Assert.Empty(w.M.Crew.Heroes);
    }

    [Fact]
    public void ThePlanetsBuildListRefusesMachinesAndPlansALadderWhichIsBuilt()
    {
        var w = Down(("scrap", 5));
        var m = w.On(1);
        int cell = Feet(16, Ground - 1);
        w.Do(new PlacePlan(cell, new Placement("machine", "furnace")))
            .Do(new PlacePlan(cell, new Placement("machine", "vent")))
            .Run();
        Assert.Equal(["building.notHere", "building.notHere"], w.Refusals.Select(r => r.Reason));
        Assert.Equal("building.notHere", m.Building.Problem(cell, new Placement("machine", "vent")));
        // Aboard, the ship allows a furnace: whatever stops one there, it is not the kind.
        Assert.NotEqual("building.notHere", w.M.Building.Problem(w.At(2, 2), new Placement("machine", "furnace")));

        w.Refusals.Clear();
        // A ladder piece, two by two (0104), over the ground from the block's top-left.
        w.Do(new PlacePlan(On(16, Ground - 1), new Placement("structure", "ladder"))).Run();
        var plan = Assert.Single(m.Building.Plans);
        for (int i = 0; i < Content.Builds.Get("ladder").BuildTicks; i++) w.Do(new Work(plan.Id)).Run();
        Assert.Empty(w.Refusals);
        Assert.Equal(Structure.Ladder, m.Grid.StructureAt(cell));
    }

    [Fact]
    public void EveryPlacementTheStripAllowsIsInItsListAndTheShipAllowsEveryOne()
    {
        Assert.All(["wall", "backWall", "floor", "ladder", "door"], kind => Assert.True(Strip.Allows(kind)));
        Assert.All(["furnace", "vent", "pipe", "valve", "cable", "lamp"], kind => Assert.False(Strip.Allows(kind)));
        Assert.All(Research.Blueprints.All, b => Assert.True(Content.Surfaces.Ship.Allows(b.Placement.Kind)));
    }
}
