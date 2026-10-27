using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Stillwake.Sim.Crew;
using Xunit;
using FieldSpec = Stillwake.Content.Field;
using JetpackSpec = Stillwake.Content.Jetpack;

namespace Stillwake.Sim.Tests;

/// <summary>The field round the build zone, and the cold past it that drains the suit (0050), in crew.</summary>
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
    /// <summary>The field's edges, the zone grown by <c>Field.Margin</c>: 384 cells from the grid's left and right, 128 from its top and bottom.</summary>
    private const int Left = (Width - Content.World.ZoneWidth) / 2 - FieldSpec.Margin, Right = Width - 1 - Left;
    private const int Top = (Height - Content.World.ZoneHeight) / 2 - FieldSpec.Margin, Bottom = Height - 1 - Top;
    /// <summary>A row and a column through the middle of the world, inside the field.</summary>
    private const int MidY = Height / 2, MidX = Width / 2;

    private static int At(int x, int y) => y * Width + x;

    private static Scenario World() => new(Layout.Parse(PodRoom).Doubled().InWorld());

    /// <summary>One world, never stepped, for the queries alone.</summary>
    private static readonly Lazy<Scenario> Still = new(World);
    private static int Cost(int cell) => Still.Value.M.Crew.EdgeCost(cell);

    private static Hero HeroOf(Scenario w, int id = 0) => w.M.Crew.HeroOf(id)!;

    /// <summary>A world with hero 0 brought in at <paramref name="cell"/> on the tick after a breath: <see cref="Breaths"/> counts from there.</summary>
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
    private static Scenario Breaths(Scenario w, int count) => w.Run(count * Content.Crew.BreathEvery);

    [Fact]
    public void GrowsTheWorldsZoneByTheMarginIntoTheField()
    {
        var zone = Still.Value.M.Grid.Zone;
        Assert.Equal((Left, Top, Right, Bottom), (zone.Left - FieldSpec.Margin, zone.Top - FieldSpec.Margin, zone.Right + FieldSpec.Margin, zone.Bottom + FieldSpec.Margin));
    }

    [Fact]
    public void BreathesInsideTheFieldAsBeforeEvenOnItsEdgeAndOutsideTheZone()
    {
        foreach (int cell in new[] { At(Left, MidY), At(Right, MidY), At(MidX, Top), At(MidX, Bottom), At(Left, Top), At(Right, Bottom) })
        {
            Assert.Equal(0, Cost(cell));
            var w = BroughtIn(cell);
            Breaths(w, 5);
            Assert.Equal(Scenario.Full - 5, HeroOf(w).Air);
        }
    }

    [Fact]
    public void ChargesAUnitMoreOneCellPastTheFieldEveryWay()
    {
        foreach (int cell in new[] { At(Left - 1, MidY), At(Right + 1, MidY), At(MidX, Top - 1), At(MidX, Bottom + 1), At(Left - 1, Top - 1) })
        {
            Assert.Equal(1, Cost(cell));
            var w = Breaths(BroughtIn(cell), 1);
            Assert.Equal(Scenario.Full - 2, HeroOf(w).Air);
        }
    }

    [Fact]
    public void ABareHeroPastTheFieldPaysItsOwnColdAndTheEdgesBoth()
    {
        var w = ToBreath(new Scenario(Layout.Parse(PodRoom).Doubled().InWorld()) { Bare = true }).Run();
        w.Do(new Enter(At(Left - 1, MidY)) { From = 0 }).Run();
        Breaths(w, 1);
        Assert.Equal(Content.Crew.Lungs - 2 - Content.Crew.BareCold, HeroOf(w).Air);
        Assert.Equal(1 + Content.Crew.BareCold, w.M.Crew.Cold(0));
    }

    [Fact]
    public void ChargesTwelveMoreHalfWayToTheEdgeByTheDeeperAxis()
    {
        // 192 of 384 cells sideways, 64 of 128 up or down: 500 thousandths, 45 × ¼ rounded up.
        foreach (int cell in new[] { At(Left / 2, MidY), At(Right + Left / 2, MidY), At(MidX, Top / 2), At(MidX, Bottom + Top / 2), At(Left / 2, Top - 10) })
        {
            Assert.Equal(12, Cost(cell));
            var w = Breaths(BroughtIn(cell), 1);
            Assert.Equal(Scenario.Full - 13, HeroOf(w).Air);
        }
    }

    [Fact]
    public void EmptiesAFullSuitInOneBreathAtTheEdgeAndTheHeroWakesAtThePod()
    {
        foreach (int cell in new[] { At(0, MidY), At(Width - 1, MidY), At(MidX, 0), At(Width - 1, Height - 1) })
            Assert.Equal(FieldSpec.EdgeCost, Cost(cell));
        var w = World().Do(new Enter(At(0, MidY)) { From = 0 }).Run();
        ToBreath(w).Run();
        Assert.Equal((0, Content.Crew.PassOut), (HeroOf(w).Air, HeroOf(w).Out));
        w.Run(Content.Crew.PassOut);
        Assert.Equal((w.M.Crew.Spawn, Scenario.Full, 0), (HeroOf(w).Cell, HeroOf(w).Air, HeroOf(w).Out));
        Assert.True(w.M.Crew.ByPod(HeroOf(w).Cell));
    }

    [Fact]
    public void StopsDrainingOnTheTickTheReportFromInsideLands()
    {
        var w = Breaths(BroughtIn(At(Left / 2, MidY)), 1);
        Assert.Equal(Scenario.Full - 13, HeroOf(w).Air);
        ToBreath(w).Do(new Enter(At(Left + 10, MidY)) { From = 0 }).Run();
        Assert.Equal(Scenario.Full - 14, HeroOf(w).Air);
    }

    [Fact]
    public void AddsTheColdToWhatThrustVents()
    {
        var w = ToBreath(World().Wear(0, JetpackSpec.Item)).Run();
        int cell = At(Left / 2, MidY);
        w.Do(new Enter(cell) { From = 0 }).Do(new Enter(cell) { From = 1 }).Do(new Thrust(true) { From = 0 }).Run();
        Breaths(w, 1);
        // Hero 1 breathes and freezes; hero 0 too, and vents 20 ticks of thrust on top.
        Assert.Equal(Scenario.Full - 13, HeroOf(w, 1).Air);
        Assert.Equal(Scenario.Full - 13 - Content.Crew.BreathEvery / JetpackSpec.TicksPerUnit, HeroOf(w, 0).Air);
    }

    [Fact]
    public void DrainsACanisterBreathedPastTheField()
    {
        var w = BroughtIn(At(Left / 2, MidY)).GiveItems(0, (Content.Crew.Canister, 1));
        HeroOf(w).Air = 10;
        w.Do(new Refill { From = 0 }).Run();
        Assert.Equal(10 + Content.Crew.CanisterRefill, HeroOf(w).Air);
        ToBreath(w).Run();
        Assert.Equal(10 + Content.Crew.CanisterRefill - 13, HeroOf(w).Air);
    }

    [Fact]
    public void ChargesAWholeSuitOffTheGrid()
    {
        Assert.Equal(FieldSpec.EdgeCost, Cost(-1));
        Assert.Equal(FieldSpec.EdgeCost, Cost(Width * Height));
        var w = World().Do(new Enter(-5) { From = 0 }).Run();
        ToBreath(w).Run();
        Assert.Equal((0, Content.Crew.PassOut), (HeroOf(w).Air, HeroOf(w).Out));
    }

    [Fact]
    public void CostsNothingAnywhereOnAMapThatSetsNoZoneAndAWholeSuitOffIt()
    {
        // A test map's zone is the whole map, so its field reaches past the grid every way.
        var w = new Scenario(PodRoom);
        Assert.All(Enumerable.Range(0, w.Layout.Width * w.Layout.Height), cell => Assert.Equal(0, w.M.Crew.EdgeCost(cell)));
        Assert.Equal(FieldSpec.EdgeCost, w.M.Crew.EdgeCost(-1));
        Assert.Equal(FieldSpec.EdgeCost, w.M.Crew.EdgeCost(w.Layout.Width * w.Layout.Height));
    }

    // Invariants.

    private static readonly Gen<(int X, int Y)> Cells =
        from x in Gen.Choose(0, Width - 1) from y in Gen.Choose(0, Height - 1) select (x, y);

    [Property(MaxTest = 500)]
    public Property CostsNothingInsideTheFieldNeverMoreThanTheEdgeAndNeverLessFartherOut()
    {
        return Prop.ForAll(Cells.ToArbitrary(), c =>
        {
            int cost = Cost(At(c.X, c.Y));
            bool inside = c.X >= Left && c.X <= Right && c.Y >= Top && c.Y <= Bottom;
            if (inside && cost != 0) return false.Label($"{cost} inside the field");
            if (!inside && cost == 0) return false.Label("nothing past the field");
            if (cost < 0 || cost > FieldSpec.EdgeCost) return false.Label($"{cost}");
            // A cell farther from the middle on either axis costs no less.
            int outX = c.X < MidX ? c.X - 1 : c.X + 1, outY = c.Y < MidY ? c.Y - 1 : c.Y + 1;
            if (outX >= 0 && outX < Width && Cost(At(outX, c.Y)) < cost) return false.Label("less farther out sideways");
            if (outY >= 0 && outY < Height && Cost(At(c.X, outY)) < cost) return false.Label("less farther out up or down");
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
