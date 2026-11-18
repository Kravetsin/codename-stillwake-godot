using System.Reflection;
using Stillwake.Content;
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
        Assert.InRange(Content.Crew.CanisterRefill, 1, Scenario.Full - 1);
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
        Assert.True(Stillwake.Sim.Crew.CrewModule.FillOf(Scenario.Full) < Scenario.Full);
        // By the pod a calm breath gains, even in vacuum; running there holds the suit at worst.
        Assert.True(Content.Crew.PodRefill > 1);
        Assert.True(Content.Crew.PodRefill >= Content.Crew.Exerted);
    }

    [Fact]
    public void OnlyHelmetsHoldAirEachMoreThanTheLungs()
    {
        foreach (var item in Content.Items.All)
            Assert.True((item.Equip == Content.Helmet.Slot) == (item.Tank > Content.Crew.Lungs) && item.Tank >= 0, item.Id);
    }

    [Fact]
    public void TheBodyIsWellBetweenItsDeathsSlowsFromItsNormalAndNeverStops()
    {
        // body-temperature: a death either way of normal, so a body slows before it dies; the least speed is some; space colder outward.
        var t = (Content.Temperature.ColdDeath, Content.Temperature.Normal, Content.Temperature.HeatDeath);
        Assert.True(t.ColdDeath < t.Normal && t.Normal < t.HeatDeath, $"{t}");
        Assert.InRange(Content.Temperature.Least, 1, 999);
        Assert.InRange(Content.Temperature.Pull, 1, 1000);
        Assert.True(Content.Temperature.Drift > 0 && Content.Temperature.Sway > 0);
        Assert.True(Content.Temperature.Deep < Content.Temperature.Space && Content.Temperature.Space < Content.Temperature.Hull);
    }

    [Fact]
    public void EveryKindHasATemperatureABandBodiesWithstandMoreThanBareAndTheCapeAddsSome()
    {
        foreach (var kind in Content.Surfaces.All.Append(Content.Surfaces.Ship))
        {
            int bands = kind is Content.PlanetType planet ? planet.Bands.Count + 1 : 1;
            Assert.True(kind.Temperatures.Count == bands, kind.Id);
        }
        foreach (var item in Content.Items.All)
            Assert.True((item.Low != null) == (item.Equip == Content.Suit.Slot) && (item.Low ?? int.MinValue) < Content.Temperature.BareLow, item.Id);
        Assert.True(Content.Cape.Warmth > 0);
        // A suit feels nothing by the hull nor anywhere on the first planet, and space past the field bites it: its reach is its
        // own (body-temperature).
        int suit = Content.Items.Get(Content.Suit.Basic).Low!.Value;
        Assert.True(suit <= Content.Temperature.Hull && suit > Content.Temperature.Space);
        Assert.True(suit <= Content.PlanetTypes.First.Temperatures.Min() - Content.Sky.First.Chill);
    }

    [Fact]
    public void EveryBodyAndNoneWithstandsColdBelowItsHeatAnAiredRoomInsideBothCapedOrNot()
    {
        // So the cold and the heat are never felt at once, and a sealed room costs no one anything (2026-11-14-heat-limits).
        var limits = Content.Items.All.Where(i => i.Equip == Content.Suit.Slot)
            .Select(i => (i.Id, Low: i.Low ?? Content.Temperature.BareLow, High: i.High ?? Content.Temperature.BareHigh))
            .Append(("none", Content.Temperature.BareLow, Content.Temperature.BareHigh));
        foreach (var (id, low, high) in limits)
        {
            Assert.True(low < high, id);
            Assert.True(low <= Content.Temperature.Room && Content.Temperature.Room <= high - Content.Cape.Warmth, id);
        }
        foreach (var item in Content.Items.All)
            Assert.True(item.High == null || item.Equip == Content.Suit.Slot, item.Id);
    }
}
