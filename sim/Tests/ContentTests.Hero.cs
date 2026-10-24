using System.Reflection;
using Stillwake.Content;
using Stillwake.Game;
using Xunit;
using DebrisSpec = Stillwake.Content.Debris;

namespace Stillwake.Sim.Tests;

/// <summary>The hero's stats, the suit, the jetpack and what is kept in death.</summary>
public partial class ContentTests
{
    // Hero

    [Fact]
    public void HeroHasPositiveWholeNumberStats() => AllPositive(typeof(Hero));

    [Fact]
    public void HeroFitsThroughADoorwayThreeCellsTallAndAShaftOneWideAtSixteenPixelsAndCentresOnALadder()
    {
        // Six and two of the 8 px cells (0104).
        Assert.True(Hero.Height > 5 * Cell);
        Assert.True(Hero.Height < 6 * Cell);
        Assert.True(Hero.Width < 2 * Cell);
        Assert.Equal(0, Hero.Width % 2);
    }

    [Fact]
    public void HeroWalksClimbsAndSwimsLessThanACellPerStep()
    {
        foreach (int speed in (int[])[Hero.Walk, Hero.Run, Hero.ClimbUp, Hero.ClimbDown, Hero.Slide, Hero.Swim, Jetpack.Top]) Assert.True(speed < Cell);
    }

    [Fact]
    public void HeroJumpsOntoALedgeSevenCellsHighNotEightAndElevenOnAPlanet()
    {
        // A jump held to its top, as the controller steps it: eased gravity below Hero.Apex (0089).
        static int Rise(int gravity)
        {
            int apex = gravity * Hero.ApexGravity / 1000, rise = 0;
            for (int v = Hero.Jump; ;)
            {
                v -= v < Hero.Apex ? apex : gravity;
                if (v <= 0) return rise;
                rise += v;
            }
        }
        int aboard = Rise(Hero.Gravity), planet = Rise(PlanetTypes.First.Fall(Hero.Gravity));
        Assert.True(aboard > 7 * Cell && aboard < 8 * Cell, $"rise {aboard}");
        // Eleven and a half, not twelve: the planet's gravity on the hero's rounds down to 29, not twice the 14 it was (0104).
        Assert.True(planet > 11 * Cell && planet < 12 * Cell, $"rise {planet}");
    }

    [Fact]
    public void TheJetpackOutfliesTheSwimIsWornOnTheBackAndACanisterRefillsLessThanAWholeSuit()
    {
        AllPositive(typeof(Jetpack));
        Assert.True(Jetpack.Top > Hero.Swim * 4);
        Assert.True(Jetpack.Accel < Jetpack.Top && Jetpack.Drag < Jetpack.Accel);
        Assert.Equal("back", Content.Items.Get(Jetpack.Item).Equip);
        Assert.Equal(1, Content.Items.Get(Jetpack.Item).Stack);
        Assert.Contains(Inventory.Equipment, kind => kind == "back");
        // Everything worn names a kind of slot there is.
        Assert.All(Content.Items.All.Where(i => i.Equip != null), i => Assert.Contains(i.Equip, Inventory.Equipment));
        Assert.InRange(Content.Crew.CanisterRefill, 1, Content.Crew.Suit - 1);
        Assert.Contains(Crafts.Get(Jetpack.Item).Cost, c => c.Item == Content.Crew.Canister);
    }

    [Fact]
    public void EverythingWornElectricOrAWeaponIsGearKeptInDeathAndBodiesLastAWhileButNotForever()
    {
        // The harpoon is named by hand: it is no weapon, worn nor electric, and still a tool kept (0061).
        Assert.True(Content.Items.Get("harpoon").Gear);
        Assert.All(
            Content.Items.All.Where(i => i.Equip != null || i.Draw != null || Weapons.Of(i.Id) != null),
            i => Assert.True(i.Gear, $"{i.Id} is not gear"));
        // Resources are not: dying costs what was gathered.
        Assert.All(new[] { "scrap", "iron", "canister", "planks", "rock", "circuit" }, id => Assert.False(Content.Items.Get(id).Gear));
        Assert.InRange(Content.Floor.BodyLasts, 60 * World.TicksPerSecond, 60 * 60 * World.TicksPerSecond);
    }

    // Crew

    [Fact]
    public void CrewBreathesInWholeUnitsAndTicksWithASuitThatLastsSomeBreaths()
    {
        AllPositive(typeof(Content.Crew));
        Assert.True(Content.Crew.Refill < Content.Crew.Suit);
        // By the pod a calm breath gains, even in vacuum; running there holds the suit at worst.
        Assert.True(Content.Crew.PodRefill > 1);
        Assert.True(Content.Crew.PodRefill >= Content.Crew.Exerted);
    }
}
