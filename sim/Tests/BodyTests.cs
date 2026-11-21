using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Stillwake.Sim.Crafting;
using Stillwake.Sim.Crew;
using Stillwake.Sim.Items;
using Stillwake.Sim.Sky;
using Xunit;

namespace Stillwake.Sim.Tests;

/// <summary>
/// The sim's side of the body's temperature (body-temperature): the client keeps the body from the degrees the sim says a hero
/// feels past its limits; the cold and the heat cost no air; at either death the client sends <see cref="Succumb"/>. The body's
/// own rules are in the game's <c>BodyHeatTests</c>; the field's degrees in <see cref="FieldTests"/>.
/// </summary>
public class BodyTests
{
    /// <summary>A pod in a vacuum room, never aired: the hull's −100° all through.</summary>
    private const string PodRoom = """
        ###########
        #.........#
        #..OOO...S#
        #..OOO....#
        #..OOO....#
        ###########
        """;

    private static int AtPod(int x, int y) => y * 22 + x;
    /// <summary>The first cell right of the pod past its air's reach: vacuum, and no pod.</summary>
    private static readonly int Beyond = AtPod(14, 6);
    private static readonly int Lungs = Content.Crew.Lungs;

    private static Hero HeroOf(Scenario w, int id = 0) => w.M.Crew.HeroOf(id)!;

    /// <summary>Runs whole breaths: from just after one to just after the <paramref name="count"/>-th next.</summary>
    private static Scenario Breaths(Scenario w, int count) =>
        w.Run(count * Content.Crew.BreathEvery - w.Crew().Clock % Content.Crew.BreathEvery);

    /// <summary>The pod room with hero 0 brought in at <paramref name="cell"/>, bare unless <paramref name="suited"/>.</summary>
    private static Scenario Aboard(int cell, bool suited = false)
    {
        var w = new Scenario(PodRoom) { Bare = !suited }.GiveItems(0, ("scrap", 3));
        return w.Do(new Enter(cell) { From = 0 }).Run();
    }

    /// <summary>The first planet with every band at <paramref name="degrees"/>, its air too: open ground reads them by day.</summary>
    private static Content.PlanetType Planet(int degrees) => Content.PlanetTypes.First with
    {
        Air = null,
        Temperatures = [.. Content.PlanetTypes.First.Temperatures.Select(_ => degrees)],
    };

    /// <summary>Hero 0 on open ground at <paramref name="degrees"/> by day in a basic helmet, <paramref name="suit"/> on its body and the cape if <paramref name="caped"/>.</summary>
    private static Scenario Ground(int degrees, string? suit, bool caped = false)
    {
        var w = TemperatureTests.OpenGround(Planet(degrees), night: false, bare: true).Wear(0, Content.Helmet.Basic);
        if (suit != null) w.Wear(0, suit);
        if (caped) w.Wear(0, Content.Cape.Item);
        return w;
    }

    [Fact]
    public void ABareHeroByTheHullFeelsAHundredDegreesAndPaysOnlyItsBreath()
    {
        var w = Breaths(Aboard(Beyond), 5);
        Assert.Equal((100, 0), (w.M.Crew.Felt(0, Beyond), w.M.Crew.Heat(0, Beyond)));
        Assert.Equal((Lungs - 5, 0, 0), (HeroOf(w).Air, HeroOf(w).Out, HeroOf(w).Gasp));
    }

    [Fact]
    public void ASuccumbPassesTheHeroOutWhereItStandsItsCarryLeftThereAndItWakesAtThePod()
    {
        var w = Aboard(Beyond);
        // Out on the tick it lands, counted down from that tick on.
        w.Do(new Succumb { From = 0 }).Run();
        Assert.Equal((Content.Crew.PassOut - 1, 0, 0), (HeroOf(w).Out, HeroOf(w).Air, HeroOf(w).Gasp));
        Assert.Equal(("scrap", 3), (Assert.Single(w.M.Items.Floor).Item, w.M.Items.Floor[0].Count));
        w.Run(Content.Crew.PassOut - 1);
        Assert.Equal((w.M.Crew.Spawn, Lungs, 0), (HeroOf(w).Cell, HeroOf(w).Air, HeroOf(w).Out));
    }

    [Fact]
    public void ASuccumbIsRefusedWithNoHeroOneAlreadyOutOrACreativeOne()
    {
        var none = new Scenario(PodRoom);
        none.Do(new Succumb { From = 0 }).Run();
        var outAlready = Aboard(Beyond);
        outAlready.Do(new Succumb { From = 0 }).Run().Do(new Succumb { From = 0 }).Run();
        var creative = Aboard(Beyond);
        creative.Do(new SetCreative(true) { From = 0 }).Run().Do(new Succumb { From = 0 }).Run();
        Assert.Equal(["crew.noHero"], none.Refusals.Select(r => r.Reason));
        Assert.Equal(["crew.noHero"], outAlready.Refusals.Select(r => r.Reason));
        Assert.Equal(["crew.noHero"], creative.Refusals.Select(r => r.Reason));
        Assert.Equal((Content.Crew.PassOut - 2, 0), (HeroOf(outAlready).Out, HeroOf(creative).Out));
    }

    /// <summary>
    /// Any degrees, in any body or none, caped or not, from any tick, over a few breaths: the air lost is what a twin at a
    /// comfortable +15° loses, and nothing passes out (body-temperature: the cold and the heat cost no air).
    /// </summary>
    [Property(MaxTest = 40)]
    public Property TheColdAndTheHeatCostNoAir()
    {
        var cases = from suit in Gen.Elements<string?>(null, Content.Suit.Basic, Content.Suit.Heat)
                    from caped in Gen.Elements(false, true)
                    from degrees in Gen.Choose(-220, 120)
                    from start in Gen.Choose(0, 60)
                    from k in Gen.Choose(1, 10)
                    select (suit, caped, degrees, start, k);
        return Prop.ForAll(cases.ToArbitrary(), c =>
        {
            var (felt, calm) = (Ground(c.degrees, c.suit, c.caped).Run(c.start), Ground(15, c.suit, c.caped).Run(c.start));
            Breaths(felt, c.k);
            Breaths(calm, c.k);
            return (HeroOf(felt).Air == HeroOf(calm).Air && HeroOf(felt).Out == 0 && HeroOf(felt).Gasp == HeroOf(calm).Gasp)
                .Label($"{c}: {HeroOf(felt).Air} at {c.degrees}°, {HeroOf(calm).Air} at +15°, out {HeroOf(felt).Out}");
        });
    }

    // The strip (as SkyTests): its night dust storm.

    private const string Ship = """
        ............
        .##########.
        .#..D..S..#.
        .##########.
        ............
        """;

    private static readonly Content.Surfaces.Kind Strip = Content.Surfaces.TestStrip;
    private static readonly int Width = 2 * Strip.Rows[0].Length;
    /// <summary>Out on the strip's flat under the open sky, standing on the ground.</summary>
    private static readonly int Open = 2 * (30 - 1) * Width + 2 * 20 + Width;
    private static int NightAt => Strip.Cycle!.Length - Strip.Cycle.Night;

    /// <summary>The ship with the strip opened and hero 0 beamed down to it in a helmet and no suit.</summary>
    private static Scenario Down()
    {
        var w = new Scenario(Ship) { Bare = true };
        w.Do(new Enter(w.M.Crew.Spawn)).Do(new OpenSurface(Strip.Id)).Run();
        w.Wear(0, Content.Helmet.Basic);
        return w.Do(new Transfer(1)).Run();
    }

    [Fact]
    public void GritCostsAirInTheStormItsColdNone()
    {
        // The night dust storm: the open sky 20° colder still, its grit 2 a breath (surface-storms); the cold felt more, paid in nothing.
        var (stormy, calm) = (Down(), Down());
        stormy.Do(new Enter(Open)).Do(new SetTime(NightAt)).Do(new StretchNow(Content.Stretches.DustStorm.Id)).Run(Content.Stretches.DustStorm.Warning);
        calm.Do(new Enter(Open)).Do(new SetTime(NightAt)).Run(Content.Stretches.DustStorm.Warning);
        Hero a = stormy.On(1).Crew.HeroOf(0)!, b = calm.On(1).Crew.HeroOf(0)!;
        Assert.Equal(2, stormy.On(1).Crew.GritAt(Open));
        (a.Air, b.Air) = (Scenario.Full, Scenario.Full);
        stormy.Run(5 * Content.Crew.BreathEvery);
        calm.Run(5 * Content.Crew.BreathEvery);
        Assert.Equal(2 * 5, b.Air - a.Air);
        // The helmet keeps this night's cold out (gear-roles): the storm's is colder, and still costs no air.
        int cold = stormy.On(1).Crew.TemperatureAt(Open), still = calm.On(1).Crew.TemperatureAt(Open);
        Assert.True(cold < still, $"{cold}° in the storm, {still}° calm");
    }
}
