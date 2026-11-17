using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Stillwake.Sim.Atmosphere;
using Stillwake.Sim.Crafting;
using Stillwake.Sim.Crew;
using Stillwake.Sim.Items;
using Stillwake.Sim.Sky;
using Xunit;
using T = Stillwake.Content.Temperature;

namespace Stillwake.Sim.Tests;

/// <summary>
/// The body's temperature (body-temperature): the degrees felt past a hero's limits draw it off its normal a breath, a quarter of
/// the gap at a time, rounded away from zero; it passes out at 28° or 42°, slows the hero past its ease, and the cold and the heat
/// no longer cost air. The field's degrees are in <see cref="FieldTests"/>.
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
    /// <summary>Where a body passes out, off its normal: −900 and +500.</summary>
    private const int Cold = T.ColdDeath - T.Normal, Hot = T.HeatDeath - T.Normal;

    private static Hero HeroOf(Scenario w, int id = 0) => w.M.Crew.HeroOf(id)!;

    /// <summary>Runs whole breaths: from just after one to just after the <paramref name="count"/>-th next.</summary>
    private static Scenario Breaths(Scenario w, int count) =>
        w.Run(count * Content.Crew.BreathEvery - w.Crew().Clock % Content.Crew.BreathEvery);

    /// <summary>The pod room with hero 0 brought in at <paramref name="cell"/>, bare unless <paramref name="suited"/>.</summary>
    private static Scenario Aboard(int cell, bool suited = false)
    {
        var w = new Scenario(PodRoom) { Bare = !suited };
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

    /// <summary>What the degrees at hero 0's cell draw its body to, off its normal.</summary>
    private static int Target(Scenario w)
    {
        int cell = HeroOf(w).Cell;
        return T.Sway * (w.M.Crew.Heat(0, cell) - w.M.Crew.Felt(0, cell));
    }

    /// <summary>Breathes up to <paramref name="most"/> breaths, stopping on the one hero 0 passes out on; whether it did.</summary>
    private static bool PassesOut(Scenario w, int most)
    {
        for (int i = 0; i < most; i++)
        {
            Breaths(w, 1);
            if (HeroOf(w).Out > 0) return true;
        }
        return false;
    }

    [Fact]
    public void ABareHeroByTheHullIsDeadInThreeBreathsItsAirUntouched()
    {
        // 100 felt: 37 → 32 → 28.25, then 25.43, past the cold's death.
        var w = Aboard(Beyond);
        Assert.Equal(100, w.M.Crew.Felt(0, Beyond));
        Breaths(w, 1);
        Assert.Equal((-500, Lungs - 1), (HeroOf(w).Body, HeroOf(w).Air));
        Breaths(w, 1);
        Assert.Equal((-875, Lungs - 2, 0), (HeroOf(w).Body, HeroOf(w).Air, HeroOf(w).Out));
        Breaths(w, 1);
        Assert.Equal((Content.Crew.PassOut, 0), (HeroOf(w).Out, HeroOf(w).Body));
    }

    [Fact]
    public void ABasicSuitHoldsTheBodyAtItsNormalInTheZone()
    {
        var w = Breaths(Aboard(Beyond, suited: true), 10);
        Assert.Equal((0, T.Normal, 1000, 1000), (HeroOf(w).Body, w.M.Crew.BodyOf(0), w.M.Crew.MoveOf(0), w.M.Crew.WorkOf(0)));
    }

    [Theory]
    // The plan's table: felt, where the body settles off 37.0°, the speed there.
    [InlineData(8, -160, 1000)]
    [InlineData(18, -360, 818)]
    [InlineData(30, -600, 543)]
    [InlineData(40, -800, 315)]
    [InlineData(44, -880, 223)]
    public void ASuitSettlesTheBodyAtSwayADegreeFeltAndSlowsItByTheTable(int felt, int body, int speed)
    {
        var w = Ground(-110 - felt, Content.Suit.Basic);
        Assert.Equal(felt, w.M.Crew.Felt(0, HeroOf(w).Cell));
        Breaths(w, 40);
        Assert.Equal((body, T.Normal + body, speed, speed, 0), (HeroOf(w).Body, w.M.Crew.BodyOf(0), w.M.Crew.MoveOf(0), w.M.Crew.WorkOf(0), HeroOf(w).Out));
    }

    [Fact]
    public void OnTheDeepHotBandABasicSuitSettlesAt39JustShortOfSlowing()
    {
        var w = Breaths(Ground(60, Content.Suit.Basic), 40);
        Assert.Equal((200, 1000), (HeroOf(w).Body, w.M.Crew.MoveOf(0)));
    }

    [Theory]
    // Felt 45 lands on 28.0°, heat 25 on 42.0°: each a death, taken at once; one degree less settles just short of it.
    [InlineData(-155, true)]
    [InlineData(-154, false)]
    [InlineData(75, true)]
    [InlineData(74, false)]
    public void ABodyLandingOnTheColdsOrTheHeatsDeathPassesOut(int degrees, bool dies)
    {
        var w = Ground(degrees, Content.Suit.Basic);
        Assert.Equal(dies, PassesOut(w, 60));
        if (dies) Assert.Equal((Content.Crew.PassOut, 0, 0), (HeroOf(w).Out, HeroOf(w).Body, HeroOf(w).Gasp));
        else Assert.Equal(degrees < 0 ? Cold + 20 : Hot - 20, HeroOf(w).Body);
    }

    [Fact]
    public void ABareHeroOnTheDeepHotBandDies()
    {
        var w = Ground(60, null);
        Assert.Equal(30, w.M.Crew.Heat(0, HeroOf(w).Cell));
        Assert.True(PassesOut(w, 20));
    }

    [Theory]
    // From where it is toward where the degrees draw it: a quarter of the gap, rounded away from zero.
    [InlineData(-100, 0, -500)]
    [InlineData(-8, 0, -40)]
    [InlineData(15, -150, -112)]
    [InlineData(15, 10, 7)]
    [InlineData(31, 1, 6)]
    public void TheBodyClosesAQuarterOfItsGapABreathRoundedAwayFromZero(int degrees, int from, int to)
    {
        var w = Ground(degrees, null);
        HeroOf(w).Body = from;
        Breaths(w, 1);
        Assert.Equal(to, HeroOf(w).Body);
    }

    [Theory]
    [InlineData(-3)]
    [InlineData(-2)]
    [InlineData(-1)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void AGapOfAFewHundredthsLandsExactlyOnTheTarget(int gap)
    {
        // Normal (nothing felt at +15°) and 35.4° (8 felt at −8°): a hundredth a breath, then held.
        foreach (int degrees in new[] { 15, -8 })
        {
            var w = Ground(degrees, null);
            int target = Target(w);
            HeroOf(w).Body = target + gap;
            for (int left = Math.Abs(gap) - 1; left >= 0; left--)
            {
                Breaths(w, 1);
                Assert.Equal(target + Math.Sign(gap) * left, HeroOf(w).Body);
            }
            Breaths(w, 1);
            Assert.Equal(target, HeroOf(w).Body);
        }
    }

    /// <summary>A room aired from the vaporizer its vent is piped to (as <c>SuitsTests</c>), hero 0 brought in at the spawn, suited.</summary>
    private static Scenario Aired()
    {
        const string room = """
            #########
            #V..S...#
            #TT.....#
            #TT.....#
            #########
            """;
        static int Cell(int x, int y) => y * 18 + x;
        var w = new Scenario(HandPiped.Chained(room), doubled: true).Do(new SetCreative(true));
        w.Pipe(Cell(2, 2), Cell(5, 4)).Run();
        int tank = w.M.Grid.Machines.First(m => m.Kind == "tank").Id;
        w.Do(new LoadCanister(tank));
        w.Do(new SetCreative(false)).Run();
        int at = w.M.Rooms.RoomAt(Cell(8, 4))!.Value;
        for (int i = 0; i < 1000 && w.M.Atmosphere.StatusOf(at) != RoomStatus.Aired; i++) w.Run();
        return w.Do(new Enter(w.M.Crew.Spawn) { From = 0 }).Run();
    }

    [Fact]
    public void AnAiredRoomWarmsACold29BackToFullSpeedInFiveBreathsAndToNormal()
    {
        var w = Aired();
        int cell = HeroOf(w).Cell;
        Assert.Equal((T.Room, 0), (w.M.Crew.TemperatureAt(cell), w.M.Crew.Felt(0, cell)));
        HeroOf(w).Body = -800;
        Breaths(w, 4);
        Assert.True(w.M.Crew.MoveOf(0) < 1000);
        Breaths(w, 1);
        Assert.Equal((-189, 1000), (HeroOf(w).Body, w.M.Crew.MoveOf(0)));
        Breaths(w, 20);
        Assert.Equal(0, HeroOf(w).Body);
    }

    [Fact]
    public void PassingOutOfAirMakesTheBodyWellAgainAndItStaysSoWhileOut()
    {
        // Bare at −30°: settling toward 31.0°, out of air, its last breath runs down.
        var w = Breaths(Ground(-30, null), 3);
        Assert.True(HeroOf(w).Body < 0);
        HeroOf(w).Air = 0;
        for (int i = 0; i < 2 * Content.Crew.LastBreath && HeroOf(w).Out == 0; i++) w.Run();
        Assert.Equal((Content.Crew.PassOut, 0), (HeroOf(w).Out, HeroOf(w).Body));
        w.Run(Content.Crew.PassOut - 1);
        Assert.Equal((1, 0), (HeroOf(w).Out, HeroOf(w).Body));
    }

    [Fact]
    public void TheCapeShiftsTheTargetFiveDegreesTimesSwayWarmer()
    {
        var (bare, caped) = (Breaths(Ground(-30, null), 40), Breaths(Ground(-30, null, caped: true), 40));
        Assert.Equal((-600, -600 + Content.Cape.Warmth * T.Sway), (HeroOf(bare).Body, HeroOf(caped).Body));
        // The heat's limit is lower by as much: hotter by the same.
        var (hot, hotCaped) = (Breaths(Ground(45, null), 40), Breaths(Ground(45, null, caped: true), 40));
        Assert.Equal((300, 300 + Content.Cape.Warmth * T.Sway), (HeroOf(hot).Body, HeroOf(hotCaped).Body));
    }

    [Theory]
    // Body off normal, the speed: whole within the ease, a step past it, the least at either death and past it.
    [InlineData(0, 1000)]
    [InlineData(200, 1000)]
    [InlineData(-200, 1000)]
    [InlineData(-201, 999)]
    [InlineData(-600, 543)]
    [InlineData(-900, 200)]
    [InlineData(-5000, 200)]
    [InlineData(201, 998)]
    [InlineData(350, 600)]
    [InlineData(500, 200)]
    [InlineData(5000, 200)]
    public void MoveAndWorkAreWholeWithinTheEaseAndTheLeastAtEitherDeath(int body, int speed)
    {
        var w = Aboard(Beyond, suited: true);
        HeroOf(w).Body = body;
        Assert.Equal((speed, speed), (w.M.Crew.MoveOf(0), w.M.Crew.WorkOf(0)));
    }

    [Fact]
    public void MoveAndWorkAreWholeWithNoHeroOrOnePassedOut()
    {
        var w = Aboard(Beyond, suited: true);
        Assert.Equal((1000, 1000, T.Normal), (w.M.Crew.MoveOf(1), w.M.Crew.WorkOf(1), w.M.Crew.BodyOf(1)));
        HeroOf(w).Body = -800;
        HeroOf(w).Out = 5;
        Assert.Equal((1000, 1000), (w.M.Crew.MoveOf(0), w.M.Crew.WorkOf(0)));
    }

    [Fact]
    public void ACreativeHerosBodyIsHeldWellAndNeverMoves()
    {
        var w = Aboard(Beyond);
        HeroOf(w).Body = -500;
        w.Do(new SetCreative(true)).Run();
        Breaths(w, 10);
        Assert.Equal((0, 0), (HeroOf(w).Body, HeroOf(w).Out));
    }

    /// <summary>
    /// Any degrees no limit dies of, in any body or none, caped or not, from any tick, over a few breaths: the air lost is what a
    /// twin at a comfortable +15° loses (body-temperature: the cold and the heat cost no air).
    /// </summary>
    [Property(MaxTest = 40)]
    public Property TheColdAndTheHeatCostNoAir()
    {
        var cases = from suit in Gen.Elements<string?>(null, Content.Suit.Basic, Content.Suit.Heat)
                    from caped in Gen.Elements(false, true)
                    from degrees in Gen.Choose(-44, 45)
                    from start in Gen.Choose(0, 60)
                    from k in Gen.Choose(1, 10)
                    select (suit, caped, degrees, start, k);
        return Prop.ForAll(cases.ToArbitrary(), c =>
        {
            var (felt, calm) = (Ground(c.degrees, c.suit, c.caped).Run(c.start), Ground(15, c.suit, c.caped).Run(c.start));
            Breaths(felt, c.k);
            Breaths(calm, c.k);
            return (HeroOf(felt).Air == HeroOf(calm).Air && HeroOf(felt).Out == 0)
                .Label($"{c}: {HeroOf(felt).Air} at {c.degrees}°, {HeroOf(calm).Air} at +15°, out {HeroOf(felt).Out}");
        });
    }

    [Fact]
    public void GritStillCostsAirInTheColdWhileTheColdMovesTheBodyAlone()
    {
        // The night dust storm: the open sky 20° colder still, its grit 2 a breath (surface-storms); a bare body cools.
        var (stormy, calm) = (Down(), Down());
        stormy.Do(new Enter(Open)).Do(new SetTime(NightAt)).Do(new StretchNow(Content.Stretches.DustStorm.Id)).Run(Content.Stretches.DustStorm.Warning);
        calm.Do(new Enter(Open)).Do(new SetTime(NightAt)).Run(Content.Stretches.DustStorm.Warning);
        Hero a = stormy.On(1).Crew.HeroOf(0)!, b = calm.On(1).Crew.HeroOf(0)!;
        Assert.Equal(2, stormy.On(1).Crew.GritAt(Open));
        (a.Air, b.Air) = (Scenario.Full, Scenario.Full);
        stormy.Run(5 * Content.Crew.BreathEvery);
        calm.Run(5 * Content.Crew.BreathEvery);
        Assert.Equal(2 * 5, b.Air - a.Air);
        Assert.True(a.Body < b.Body && b.Body < 0, $"{a.Body} in the storm, {b.Body} calm");
    }

    // The strip (as SkyTests): its mild night felt only past a bare body's limit.

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
    public void TheBodyTravelsWithTheHeroToAnotherSurface()
    {
        var w = new Scenario(Ship);
        w.Do(new Enter(w.M.Crew.Spawn)).Do(new OpenSurface(Strip.Id)).Run();
        HeroOf(w).Body = -300;
        w.Do(new Transfer(1)).Run();
        Assert.Null(w.M.Crew.HeroOf(0));
        // Suited by day on the strip, nothing felt: home by a quarter if a breath fell on the tick it arrived.
        bool breathed = ((CrewState)w.On(1).Crew.Slice).Clock % Content.Crew.BreathEvery == 0;
        Assert.Equal(breathed ? -225 : -300, w.On(1).Crew.HeroOf(0)!.Body);
    }

    [Fact]
    public void ACarriedHeroBringsItsBodyOnArrival()
    {
        var (from, to) = (Aboard(Beyond, suited: true), new Scenario(PodRoom));
        HeroOf(from).Body = -300;
        to.M.Crew.Arrive(0, from.M.Crew.Depart(0), Beyond);
        Assert.Equal(-300, HeroOf(to).Body);
    }

    [Fact]
    public void ACoolingBodySurvivesASaveAndALoadAndPlaysOnAsATwinThatNeverSaved()
    {
        // Bare at night on the strip, 8 felt: three breaths into its fall toward 35.4°, saved and loaded, then five more.
        var (saved, twin) = (Down(), Down());
        foreach (var w in new[] { saved, twin }) w.Do(new Enter(Open)).Do(new SetTime(NightAt)).Run(3 * Content.Crew.BreathEvery);
        int body = saved.On(1).Crew.HeroOf(0)!.Body;
        Assert.InRange(body, -159, -1);
        saved.Reload();
        Assert.Equal(body, saved.On(1).Crew.HeroOf(0)!.Body);
        saved.Run(5 * Content.Crew.BreathEvery);
        twin.Run(5 * Content.Crew.BreathEvery);
        Assert.Equal(twin.On(1).Crew.HeroOf(0)!.Body, saved.On(1).Crew.HeroOf(0)!.Body);
        Assert.True(saved.On(1).Crew.HeroOf(0)!.Body < body);
        Assert.Equal(twin.World.Save(), saved.World.Save());
    }

    // Invariants.

    /// <summary>
    /// Any body or none, caped or not, at any degrees, from any body short of death: each breath the body moves toward its target,
    /// never past it, and closer unless it is on it; or it passes out.
    /// </summary>
    [Property(MaxTest = 60)]
    public Property TheBodyNeverPassesItsTarget()
    {
        var cases = from suit in Gen.Elements<string?>(null, Content.Suit.Basic, Content.Suit.Heat)
                    from caped in Gen.Elements(false, true)
                    from degrees in Gen.Choose(-160, 90)
                    from body in Gen.Choose(Cold + 1, Hot - 1)
                    from k in Gen.Choose(1, 8)
                    select (suit, caped, degrees, body, k);
        return Prop.ForAll(cases.ToArbitrary(), c =>
        {
            var w = Ground(c.degrees, c.suit, c.caped);
            HeroOf(w).Body = c.body;
            int target = Target(w);
            for (int i = 0; i < c.k; i++)
            {
                int was = target - HeroOf(w).Body;
                Breaths(w, 1);
                if (HeroOf(w).Out > 0) return (HeroOf(w).Body == 0).Label($"{c}: out with a body of {HeroOf(w).Body}");
                int gap = target - HeroOf(w).Body;
                if (gap != 0 && Math.Sign(gap) != Math.Sign(was)) return false.Label($"{c}: past {target}, from {target - was} to {HeroOf(w).Body}");
                if (was != 0 && Math.Abs(gap) >= Math.Abs(was)) return false.Label($"{c}: no closer to {target}, from {target - was} to {HeroOf(w).Body}");
                if (was == 0 && gap != 0) return false.Label($"{c}: left its target {target}");
            }
            return true.ToProperty();
        });
    }

    private static readonly Lazy<Scenario> Still = new(() => Aboard(Beyond, suited: true));

    /// <summary>
    /// Any body, however far off its normal: its speed is within <c>Least</c> and whole, work's the same, and a body farther off on
    /// the same side is never faster.
    /// </summary>
    [Property(MaxTest = 300)]
    public Property MoveAndWorkStayWithinTheLeastAndWholeAndNeverQuickenFartherOff()
    {
        return Prop.ForAll(Gen.Choose(-6000, 6000).ToArbitrary(), body =>
        {
            var w = Still.Value;
            HeroOf(w).Body = body;
            int move = w.M.Crew.MoveOf(0), work = w.M.Crew.WorkOf(0);
            HeroOf(w).Body = body + Math.Sign(body);
            int farther = w.M.Crew.MoveOf(0);
            return (move >= T.Least && move <= 1000 && work == move && farther <= move).Label($"{body}: move {move}, work {work}, farther {farther}");
        });
    }
}
