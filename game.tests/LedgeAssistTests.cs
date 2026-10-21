using Stillwake.Game.Features.Crew;
using Stillwake.Sim;
using Xunit;
using static Stillwake.Game.Features.Crew.Poses;
using Spec = Stillwake.Content.Hero;
using Surfaces = Stillwake.Content.Surfaces;

namespace Stillwake.Game.Tests;

/// <summary>The ledge assist (0092): a jump whose drawn foot clears a ledge's top it is pressed against steps up onto it.</summary>
public class LedgeAssistTests
{
    private const int Unit = Hero.Unit;
    private const int Half = Spec.Width / 2;

    /// <summary>A hall nine rows tall, floor on row 9's top, a ledge <paramref name="high"/> cells high from column 3 on.</summary>
    private static Ship Ledge(int high, Surfaces.Kind? rules = null)
    {
        var rows = new[] { "#######" }.Concat(Enumerable.Repeat("#.....#", 8)).Append("#######").ToArray();
        for (int y = 9 - high; y <= 8; y++) rows[y] = "#..####";
        var world = new TestWorld(Layout.Parse(rows));
        return Ship.Of(world.M.Grid, world.M.Rooms, rules: rules);
    }

    /// <summary>A hero in a jump at (<paramref name="x"/>, <paramref name="y"/>), rising or falling at <paramref name="vy"/>.</summary>
    private static Hero Leaping(int x, int y, int vy) => new(x, y, 0, vy, false, 1, Leaping: true);

    private const int Flush = 3 * Unit - Half;
    private static readonly HeroInput Toward = new(1, 0, false, JumpHeld: true);

    [Fact]
    public void ReachesAsHighAsTheFootIsDrawnAboutAThirdOfACellAtAJumpsTopLittleFallingFast()
    {
        Assert.Equal(297, Hero.MantleReach(0));
        Assert.Equal(278, Hero.MantleReach(-Spec.Jump));
        Assert.Equal(144, Hero.MantleReach(60));
        Assert.Equal(36, Hero.MantleReach(Spec.MaxFall));
        foreach (int vy in new[] { -300, -60, 0, 100, 600 })
            Assert.Equal((int)(FootLift(vy) * Unit / Rig.Cell), Hero.MantleReach(vy));
    }

    [Fact]
    public void AJumpJustShortOfALedgeStepsUpOntoItNoFasterThanAClimbAndStandsThere()
    {
        var ship = Ledge(3);
        int top = 6 * Unit;
        var h = Leaping(Flush, top + 290, 0);
        var seen = new List<Hero>();
        for (int i = 0; i < 6 && h.Y > top; i++)
        {
            var was = h;
            h = h.Step(Toward, ship);
            seen.Add(h);
            Assert.InRange(was.Y - h.Y, 0, Spec.MantleRise);
        }
        Assert.Equal((top, 0), (h.Y, h.Vy));
        Assert.Equal(Flush + Spec.MantleAcross, h.X);
        Assert.Equal(3, seen.Count);
        // Standing on it, the keys go on walking.
        var on = HeroTests.Settle(ship, h, Toward with { JumpHeld = false });
        Assert.Equal(top, on.Y);
        Assert.True(on.X > 4 * Unit);
    }

    [Fact]
    public void AJumpClearlyShortOrFallingFastFallsAsAnyAndNotWithoutTheKeyTowardIt()
    {
        var ship = Ledge(3);
        int top = 6 * Unit;
        Assert.True(Leaping(Flush, top + 350, 0).Step(Toward, ship).Y > top + 350);
        Assert.True(Leaping(Flush, top + 100, 400).Step(Toward, ship).Y > top + 100);
        // Let go of the key, or held away, it falls: the assist never holds the hero.
        Assert.True(Leaping(Flush, top + 200, 0).Step(Toward with { X = 0 }, ship).Y > top + 200);
        Assert.True(Leaping(Flush, top + 200, 0).Step(Toward with { X = -1 }, ship).Y > top + 200);
        // Walked off a ledge, not a jump: falling at its foot pressed against it, it does not climb back.
        Assert.True(new Hero(Flush, top + 200, 0, 0, false, 1).Step(Toward, ship).Y > top + 200);
        // Rising faster than the assist lifts, the jump goes on by itself.
        var rising = Leaping(Flush, top + 200, -200).Step(Toward, ship);
        Assert.Equal(top + 200 - 200 + ship.Gravity, rising.Y);
    }

    [Fact]
    public void FourCellsAboardBecomeReachableFiveNotAndOnThePlanetSixStillNotSeven()
    {
        // A held jump's feet rise 3954 aboard: 46 short of a fourth cell, inside the reach; 1046 short of a fifth.
        Assert.InRange(4 * Unit - 3954, 1, Hero.MantleReach(0));
        Assert.True(5 * Unit - 3954 > Hero.MantleReach(0));
        // On the planet 6014: six cleared outright, seven 986 short, out of reach.
        Assert.True(7 * Unit - 6014 > Hero.MantleReach(0));
    }

    [Fact]
    public void StepsUpOnThePlanetAsAboard()
    {
        var planet = Ledge(3, Surfaces.TestStrip);
        var h = Leaping(Flush, 6 * Unit + 250, 0);
        for (int i = 0; i < 6; i++) h = h.Step(Toward, planet);
        Assert.Equal(6 * Unit, h.Y);
        Assert.True(h.X > Flush);
    }

    [Fact]
    public void AFriendStepsUpInTheFiguresPoseFromWhereItIs()
    {
        var ship = Ledge(3);
        int top = 6 * Unit;
        var lifting = Leaping(Flush, top + 290, 0).Step(Toward, ship);
        Assert.True(lifting.Vy < 0);
        // Its peer sends no Leaping: the ledge is read from the position and the rise alone.
        var heard = SentHero.Decode(new SentHero(new Seen(lifting), 10).Encode())!.Value.Seen.Hero;
        Assert.False(heard.Leaping);
        Assert.Equal(Hero.MantleLedge(ship, lifting), Hero.MantleLedge(ship, heard));
        Assert.Equal(lifting.Y - top, Hero.MantleLedge(ship, heard));
        // Drawn stepping up: the front foot raised onto the ledge, the body leaning into it.
        double ledge = (double)(heard.Y - top) / Unit * Rig.Cell;
        var stepping = LimbsOf(Pose.Air, heard, new Moment(0, 0, Ledge: ledge));
        var air = LimbsOf(Pose.Air, heard, new Moment(0, 0));
        Assert.NotEqual(air, stepping);
        PoseTests.Close(Rig.Hip - ledge, Rig.EndOf(stepping.FrontLeg, Rig.Leg).Y, 0);
        // Falling, or away from any wall: none.
        Assert.Null(Hero.MantleLedge(ship, heard with { Vy = 50 }));
        Assert.Null(Hero.MantleLedge(ship, heard with { X = Flush - 300 }));
    }
}
