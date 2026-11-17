using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Stillwake.Sim.Crew;
using Xunit;
using FieldSpec = Stillwake.Content.Field;
using JetpackSpec = Stillwake.Content.Jetpack;

namespace Stillwake.Sim.Tests;

/// <summary>
/// The field round the build zone (0048) and its degrees (body-temperature): the hull's by the ship, colder across the field to
/// space's at its line and past it; the cold there moves the body, never the air.
/// </summary>
public class FieldTests
{
    /// <summary>A lifepod in a room with no air, centred in a world of <c>Content.World</c>'s size, as a new world's wreck is.</summary>
    private const string PodRoom = """
        ###########
        #.........#
        #..OOO...S#
        #..OOO....#
        #..OOO....#
        ###########
        """;

    private const int Width = Content.World.Width, Height = Content.World.Height;
    private const int Hull = Content.Temperature.Hull, Space = Content.Temperature.Space;
    /// <summary>The field's edges, the zone grown by <c>Field.Margin</c>: 384 cells from the grid's left and right, 128 from its top and bottom.</summary>
    private const int Left = (Width - Content.World.ZoneWidth) / 2 - FieldSpec.Margin, Right = Width - 1 - Left;
    private const int Top = (Height - Content.World.ZoneHeight) / 2 - FieldSpec.Margin, Bottom = Height - 1 - Top;
    /// <summary>A row and a column through the middle of the world, inside the field.</summary>
    private const int MidY = Height / 2, MidX = Width / 2;

    private static int At(int x, int y) => y * Width + x;

    private static Scenario World() => new(Layout.Parse(PodRoom).Doubled().InWorld());

    /// <summary>One world, never stepped, for the queries alone.</summary>
    private static readonly Lazy<Scenario> Still = new(World);
    private static int Degrees(int cell) => Still.Value.M.Crew.TemperatureAt(cell);
    private static int Degrees(int x, int y) => Degrees(At(x, y));

    private static Hero HeroOf(Scenario w, int id = 0) => w.M.Crew.HeroOf(id)!;

    /// <summary>A world with hero 0, suited, brought in at <paramref name="cell"/> on the tick after a breath: <see cref="Breaths"/> counts from there.</summary>
    private static Scenario BroughtIn(int cell)
    {
        var w = ToBreath(World()).Run();
        return w.Do(new Enter(cell) { From = 0 }).Run();
    }

    /// <summary>Runs to the tick before the next breath: a command done now lands on the breath's tick.</summary>
    private static Scenario ToBreath(Scenario w)
    {
        while (w.Crew().Clock % Content.Crew.BreathEvery != Content.Crew.BreathEvery - 1) w.Run();
        return w;
    }

    /// <summary>Runs whole breaths from just after one.</summary>
    private static Scenario Breaths(Scenario w, int count) =>
        w.Run(count * Content.Crew.BreathEvery - w.Crew().Clock % Content.Crew.BreathEvery);

    [Fact]
    public void GrowsTheWorldsZoneByTheMarginIntoTheField()
    {
        var zone = Still.Value.M.Grid.Zone;
        Assert.Equal((Left, Top, Right, Bottom), (zone.Left - FieldSpec.Margin, zone.Top - FieldSpec.Margin, zone.Right + FieldSpec.Margin, zone.Bottom + FieldSpec.Margin));
    }

    [Fact]
    public void SpaceIsTheHullsByTheShipColderAcrossTheFieldByTheFartherAxisAndSpacesFromItsLineOn()
    {
        // The user's ask: the farther from the ship, the colder (2026-11-14-temperature-in-degrees), −100° to −200° (body-temperature).
        var zone = Still.Value.M.Grid.Zone;
        Assert.Equal((Hull, Hull), (Degrees(zone.Left, MidY), Degrees(MidX, zone.Top)));
        // One cell out, 100° × 1/128 rounded toward the warmer; half way, 64 of 128 cells by the farther axis, half of it.
        Assert.Equal(-100, Degrees(zone.Left - 1, MidY));
        Assert.Equal(-101, Degrees(zone.Left - 2, MidY));
        Assert.Equal((-150, -150), (Degrees(zone.Left - FieldSpec.Margin / 2, MidY), Degrees(zone.Left - FieldSpec.Margin / 2, zone.Top - 10)));
        Assert.Equal((-150, -150), (Degrees(MidX, zone.Bottom + FieldSpec.Margin / 2), Degrees(zone.Right + FieldSpec.Margin / 2, MidY)));
        Assert.Equal((Space, Space, Space), (Degrees(Left, MidY), Degrees(Right + 1, Bottom + 1), Degrees(0, 0)));
        for (int x = zone.Left; x > 0; x--)
            Assert.InRange(Degrees(x, MidY) - Degrees(x - 1, MidY), 0, 1);
    }

    [Fact]
    public void HoldsAtSpacesEveryWayOnTheFieldsLinePastItHalfWayToTheEdgeAndAtTheWorldsEdge()
    {
        foreach (int cell in new[] { At(Left, MidY), At(Right, MidY), At(MidX, Top), At(MidX, Bottom), At(Left, Top), At(Right, Bottom) })
            Assert.Equal(Space, Degrees(cell));
        foreach (int cell in new[] { At(Left - 1, MidY), At(Right + 1, MidY), At(MidX, Top - 1), At(MidX, Bottom + 1), At(Left - 1, Top - 1) })
            Assert.Equal(Space, Degrees(cell));
        foreach (int cell in new[] { At(Left / 2, MidY), At(Right + Left / 2, MidY), At(MidX, Top / 2), At(MidX, Bottom + Top / 2), At(Left / 2, Top - 10) })
            Assert.Equal(Space, Degrees(cell));
        foreach (int cell in new[] { At(0, MidY), At(Width - 1, MidY), At(MidX, 0), At(Width - 1, Height - 1) })
            Assert.Equal(Space, Degrees(cell));
        Assert.Equal((Space, Space), (Degrees(-1), Degrees(Width * Height)));
    }

    [Fact]
    public void ABasicSuitFeelsNothingInTheZoneAndBreathesThereAsAnywhere()
    {
        var w = Breaths(BroughtIn(At(Left + FieldSpec.Margin, MidY)), 5);
        Assert.Equal((Scenario.Full - 5, 0, 0), (HeroOf(w).Air, HeroOf(w).Body, w.M.Crew.Felt(0, HeroOf(w).Cell)));
    }

    [Fact]
    public void PastTheFieldTheColdCostsNoAirAndPassesASuitedHeroOutByItsBody()
    {
        // Space's −200° on a basic suit's −110°: 90 felt, a target of 37 − 18 = 19°; 37 → 32.5 → 29.12 → 26.59, out on the third.
        foreach (int cell in new[] { At(Left - 1, MidY), At(0, MidY), At(MidX, 0), -5 })
        {
            var w = Breaths(BroughtIn(cell), 2);
            Assert.Equal((Scenario.Full - 2, -788, 0), (HeroOf(w).Air, HeroOf(w).Body, HeroOf(w).Out));
            Breaths(w, 1);
            Assert.Equal((Content.Crew.PassOut, 0, 0), (HeroOf(w).Out, HeroOf(w).Body, HeroOf(w).Gasp));
            // Out on a breath's tick, it wakes on one: looked at then, before the next breath is paid.
            w.Run(Content.Crew.PassOut - 1);
            Assert.Equal(1, HeroOf(w).Out);
            w.Run();
            Assert.Equal((w.M.Crew.Spawn, Scenario.Full, 0, 0), (HeroOf(w).Cell, HeroOf(w).Air, HeroOf(w).Out, HeroOf(w).Body));
            Assert.True(w.M.Crew.ByPod(HeroOf(w).Cell));
        }
    }

    [Fact]
    public void StopsCoolingOnTheTickTheReportFromInsideLands()
    {
        // Half way across the field, −150° on −110°: 40 felt, a target of −800; a breath closes a quarter, −200.
        var w = Breaths(BroughtIn(At(Left + FieldSpec.Margin / 2, MidY)), 1);
        Assert.Equal((-200, Scenario.Full - 1), (HeroOf(w).Body, HeroOf(w).Air));
        // Back in the zone on the breath's tick: nothing felt, it warms a quarter of its gap home.
        ToBreath(w).Do(new Enter(At(Left + FieldSpec.Margin + 10, MidY)) { From = 0 }).Run();
        Assert.Equal((-150, Scenario.Full - 2), (HeroOf(w).Body, HeroOf(w).Air));
    }

    [Fact]
    public void ThrustVentsOnlyItsOwnPastTheField()
    {
        var w = ToBreath(World().Wear(0, JetpackSpec.Item)).Run();
        int cell = At(Left + FieldSpec.Margin / 2, MidY);
        w.Do(new Enter(cell) { From = 0 }).Do(new Enter(cell) { From = 1 }).Do(new Thrust(true) { From = 0 }).Run();
        Breaths(w, 1);
        // Hero 1 breathes; hero 0 too, and vents 20 ticks of thrust on top; the cold takes neither's air, it cools both bodies alike.
        Assert.Equal(Scenario.Full - 1, HeroOf(w, 1).Air);
        Assert.Equal(Scenario.Full - 1 - Content.Crew.BreathEvery / JetpackSpec.TicksPerUnit, HeroOf(w, 0).Air);
        Assert.Equal((-200, -200), (HeroOf(w, 0).Body, HeroOf(w, 1).Body));
    }

    [Fact]
    public void ACanisterBreathedPastTheFieldPaysOnlyTheBreath()
    {
        var w = BroughtIn(At(Left / 2, MidY)).GiveItems(0, (Content.Crew.Canister, 1));
        HeroOf(w).Air = 2;
        w.Do(new Refill { From = 0 }).Run();
        Assert.Equal(2 + Content.Crew.CanisterRefill, HeroOf(w).Air);
        ToBreath(w).Run();
        Assert.Equal(2 + Content.Crew.CanisterRefill - 1, HeroOf(w).Air);
    }

    [Fact]
    public void IsTheHullsAnywhereOnAMapThatSetsNoZoneAndSpacesOffIt()
    {
        // A test map's zone is the whole map, so its field reaches past the grid every way.
        var w = new Scenario(PodRoom);
        Assert.All(Enumerable.Range(0, w.Layout.Width * w.Layout.Height), cell => Assert.Equal(Hull, w.M.Crew.TemperatureAt(cell)));
        Assert.Equal((Space, Space), (w.M.Crew.TemperatureAt(-1), w.M.Crew.TemperatureAt(w.Layout.Width * w.Layout.Height)));
    }

    // Invariants.

    private static readonly Gen<(int X, int Y)> Cells =
        from x in Gen.Choose(0, Width - 1) from y in Gen.Choose(0, Height - 1) select (x, y);

    [Property(MaxTest = 500)]
    public Property TheDegreesAreTheHullsInTheZoneSpacesPastTheFieldAndNeverWarmerFartherOut()
    {
        var zone = Still.Value.M.Grid.Zone;
        return Prop.ForAll(Cells.ToArbitrary(), c =>
        {
            int degrees = Degrees(c.X, c.Y);
            bool inZone = c.X >= zone.Left && c.X <= zone.Right && c.Y >= zone.Top && c.Y <= zone.Bottom;
            bool inside = c.X > Left && c.X < Right && c.Y > Top && c.Y < Bottom;
            if (inZone && degrees != Hull) return false.Label($"{degrees} in the zone");
            if (!inside && degrees != Space) return false.Label($"{degrees} past the field");
            if (degrees < Space || degrees > Hull) return false.Label($"{degrees}");
            // A cell farther from the middle on either axis is no warmer.
            int outX = c.X < MidX ? c.X - 1 : c.X + 1, outY = c.Y < MidY ? c.Y - 1 : c.Y + 1;
            if (outX >= 0 && outX < Width && Degrees(outX, c.Y) > degrees) return false.Label("warmer farther out sideways");
            if (outY >= 0 && outY < Height && Degrees(c.X, outY) > degrees) return false.Label("warmer farther out up or down");
            return true.ToProperty();
        });
    }

    private sealed record Step(string Type, int Hero = 0, int Cell = 0, int Ticks = 0);

    /// <summary>
    /// Heroes 0–3 entering anywhere on the grid or a little off it, half of them near the field's line;
    /// thrusting (heroes 0 and 1 wear jetpacks), breathing canisters, and waits.
    /// </summary>
    private static readonly Gen<Step> Steps = Gen.OneOf(
        from hero in Gen.Choose(0, 3)
        from cell in Gen.OneOf(
            Gen.Choose(-50, Width * Height + 50),
            from x in Gen.Choose(Left - 30, Left + 3) from y in Gen.Choose(0, Height - 1) select At(x, y))
        select new Step("enter", hero, cell),
        from hero in Gen.Choose(0, 3)
        from thrusting in Gen.Elements(0, 1)
        select new Step("thrust", hero, Ticks: thrusting),
        from hero in Gen.Choose(0, 3) select new Step("refill", hero),
        Gen.Frequency((3, Gen.Choose(1, 40)), (1, Gen.Choose(1, 300))).Select(t => new Step("wait", Ticks: t)));

    [Property(MaxTest = 20)]
    public Property KeepsEverySuitBetweenEmptyAndFullWithHeroesAnywhere()
    {
        var steps = from n in Gen.Choose(0, 25) from s in Steps.ArrayOf(n) select s;
        return Prop.ForAll(steps.ToArbitrary(), all =>
        {
            var w = World().Wear(0, JetpackSpec.Item).Wear(1, JetpackSpec.Item);
            for (int peer = 0; peer < 4; peer++) w.GiveItems(peer, (Content.Crew.Canister, 3));
            foreach (var s in all)
            {
                if (s.Type == "enter") w.Do(new Enter(s.Cell) { From = s.Hero });
                else if (s.Type == "thrust") w.Do(new Thrust(s.Ticks == 1) { From = s.Hero });
                else if (s.Type == "refill") w.Do(new Refill { From = s.Hero });
                w.Run(s.Type == "wait" ? s.Ticks : 1);
                foreach (var hero in w.Crew().Heroes)
                    if (hero.Air < 0 || hero.Air > Scenario.Full)
                        return false.Label($"hero {hero.Id}'s suit holds {hero.Air}");
            }
            return true.ToProperty();
        });
    }
}
