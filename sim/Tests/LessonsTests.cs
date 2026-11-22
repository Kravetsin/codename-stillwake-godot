using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Stillwake.Sim.Creatures;
using Stillwake.Sim.Crew;
using Stillwake.Sim.Debris;
using Stillwake.Sim.Research;
using Xunit;
using Spec = Stillwake.Content;

namespace Stillwake.Sim.Tests;

/// <summary>
/// The drone learns its forms (drone-lessons): the scan from ten wreck pieces its player broke, the lamp from passing a lamp a player built,
/// the alarms from passing an emergency lamp. Each player's own, for good; a near lesson only with a drone to look.
/// </summary>
public class LessonsTests
{
    private static readonly string Scan = Spec.LightDrone.ScannerMode, Lamp = Spec.LightDrone.LampMode, Alarms = Spec.LightDrone.AlarmsMode;
    private static readonly string Broken = Spec.Lessons.WreckBroken;
    private const int Reach = Spec.Lessons.Reach;

    /// <summary>A lamp at the left end, an emergency lamp at the right, 59 characters of open space between: 122 cells by 6.</summary>
    private static readonly string Strip = string.Join('\n', "L" + new string('.', 59) + "i", new string('.', 61), new string('.', 61));

    /// <summary>The strip's width in cells.</summary>
    private const int Wide = 122;

    /// <summary>A ceiling over 120 cells of open space: where a hero builds a lamp hanging under it at row 2.</summary>
    private static Scenario Ceiling() =>
        new(Layout.Parse(string.Join('\n', new string('#', 61), new string('.', 61), new string('.', 61))), doubled: true);
    /// <summary>The cell at column <paramref name="x"/> of row 2, just under both lamps' footprints (rows 0 and 1).</summary>
    private static int Row2(int x) => 2 * Wide + x;

    private static Scenario Strip2(bool unpaired = false) => new(Strip) { Unpaired = unpaired };

    /// <summary>An engine block drifting in the strip, broken at once by <paramref name="peer"/>.</summary>
    private static void Break(Scenario w, int peer)
    {
        var debris = (DebrisState)w.M.Debris.Slice;
        var piece = new Piece { Id = debris.NextId++, Zone = Spec.Debris.Far.Id, Item = "engineBlock", X = 60_500, Y = 4_500, Start = 60_500, SeenAt = debris.Clock };
        debris.Pieces.Add(piece);
        Assert.True(w.M.Debris.Hit(peer, piece.Id, 1_000_000));
    }

    [Fact]
    public void TheLessonsAreDataAndTheScanTakesTenWrecks()
    {
        Assert.Equal([Scan, Lamp, Alarms], Spec.Lessons.Forms);
        Assert.Equal(new Spec.Deed(Broken, 10), Spec.Lessons.All[0].Source);
        Assert.Equal(new Spec.Near("lamp"), Spec.Lessons.All[1].Source);
        Assert.Equal(new Spec.Near("emergencyLamp"), Spec.Lessons.All[2].Source);
    }

    [Fact]
    public void TenWrecksBrokenTeachThatPeerTheScanNineDoNotAndAFriendLearnsNothing()
    {
        var w = Strip2().Do(new Enter(Row2(60)) { From = 1 }).Do(new Enter(Row2(62)) { From = 2 }).Run();
        for (int i = 0; i < 9; i++) Break(w, 1);
        Assert.Equal(9, w.M.Knowledge.DeedsOf(1, Broken));
        Assert.False(w.M.Knowledge.Knows(1, Scan));
        Break(w, 1);
        Assert.True(w.M.Knowledge.Knows(1, Scan));
        Assert.False(w.M.Knowledge.Granted(Scan));
        Assert.False(w.M.Knowledge.Knows(2, Scan));
        Assert.Equal(0, w.M.Knowledge.DeedsOf(2, Broken));
        // Counted on past the lesson, learned once.
        Break(w, 1);
        Assert.Equal(11, w.M.Knowledge.DeedsOf(1, Broken));
        Assert.Equal(1, w.M.Knowledge.KnownIds(1).Count(id => id == Scan));
    }

    [Fact]
    public void AShotThatBreaksAWreckCountsForTheShooter()
    {
        var w = ShotsTests.Space();
        var debris = (DebrisState)w.M.Debris.Slice;
        var wreck = new Piece { Id = debris.NextId++, Zone = Spec.Debris.Far.Id, Item = "engineBlock", X = 60_000, Y = 10_000, Start = 60_000, SeenAt = debris.Clock };
        wreck.Damage = Spec.Wreckage.HpOf(wreck.Item)!.Value - 1;
        debris.Pieces.Add(wreck);
        SentryTests.Guarded(w);
        w.Do(new Fire(Spec.Shots.Nailer.Item, 10 * CreaturesModule.Unit + CreaturesModule.Unit / 2, 10 * CreaturesModule.Unit + CreaturesModule.Unit / 2, 0)).Run(20);
        Assert.DoesNotContain(wreck, w.M.Debris.Pieces);
        Assert.Equal(1, w.M.Knowledge.DeedsOf(0, Broken));
    }

    [Fact]
    public void AWreckBrokenByNoHeroCountsForNoOne()
    {
        var w = Strip2().Run();
        Break(w, Items.FloorStack.Nobody);
        Assert.Empty(w.M.Knowledge.Learners);
    }

    [Fact]
    public void APairedHeroWithinReachOfABuiltLampLearnsItBeyondReachDoesNot()
    {
        // The lamp covers columns 0 and 1 of rows 0 and 1: row 2 is one under it, so column 1 + 23 is just in reach, a column further out.
        var w = Strip2().Do(new Enter(Row2(26)) { From = 1 }).Run(5);
        Assert.False(w.M.Knowledge.Knows(1, Lamp));
        w.Do(new Enter(Row2(24)) { From = 1 }).Run();
        Assert.True(w.M.Knowledge.Knows(1, Lamp));
        Assert.False(w.M.Knowledge.Knows(1, Alarms));
        Assert.Equal(Reach, 1 + 23);
    }

    [Fact]
    public void AnUnpairedHeroLearnsNothingByPassingTillItHasADrone()
    {
        var w = Strip2(unpaired: true).Do(new Enter(Row2(3)) { From = 1 }).Run(5);
        Assert.False(w.M.Knowledge.Knows(1, Lamp));
        w.M.Players.Pair(1);
        w.Run();
        Assert.True(w.M.Knowledge.Knows(1, Lamp));
    }

    [Fact]
    public void AnEmergencyLampTeachesTheAlarmsNotTheLamp()
    {
        var w = Strip2().Do(new Enter(Row2(115)) { From = 1 }).Run();
        Assert.True(w.M.Knowledge.Knows(1, Alarms));
        Assert.False(w.M.Knowledge.Knows(1, Lamp));
        Assert.False(w.M.Knowledge.Knows(1, Scan));
    }

    [Fact]
    public void AFriendWhoDidNotBuildTheLampLearnsItByPassingEachOnTheirOwn()
    {
        // A lamp built under the ceiling's middle, both heroes far off at the left; then each walks by it in turn.
        var w = Ceiling().Do(new Enter(4 * Wide + 2) { From = 1 }).Do(new Enter(4 * Wide + 2) { From = 2 }).Run();
        w.Do(new Place(2 * Wide + 60, new Grid.Placement("machine", "lamp"))).Run();
        Assert.Empty(w.Refusals);
        Assert.Single(w.M.Grid.MachinesOf("lamp"));
        Assert.False(w.M.Knowledge.Knows(1, Lamp) || w.M.Knowledge.Knows(2, Lamp));
        w.Do(new Enter(4 * Wide + 50) { From = 1 }).Run();
        Assert.True(w.M.Knowledge.Knows(1, Lamp));
        Assert.False(w.M.Knowledge.Knows(2, Lamp));
        w.Do(new Enter(4 * Wide + 70) { From = 2 }).Run();
        Assert.True(w.M.Knowledge.Knows(2, Lamp));
    }

    [Fact]
    public void ALampStillAPlanTeachesNothing()
    {
        var w = Ceiling().Do(new Enter(4 * Wide + 60) { From = 1 }).Run();
        w.Do(new Building.PlacePlan(2 * Wide + 60, new Grid.Placement("machine", "lamp")) { From = 1 }).Run(5);
        Assert.Empty(w.Refusals);
        Assert.Single(w.M.Building.Plans);
        Assert.Empty(w.M.Grid.MachinesOf("lamp"));
        Assert.Null(w.M.Lessons.Teacher(4 * Wide + 60, "lamp"));
        Assert.False(w.M.Knowledge.Knows(1, Lamp));
    }

    [Fact]
    public void TheTeacherIsTheNearestOfItsKindWithinReachElseNone()
    {
        var w = Strip2().Run();
        Assert.Equal(w.M.Grid.MachinesOf("lamp")[0], w.M.Lessons.Teacher(Row2(10), "lamp"));
        Assert.Null(w.M.Lessons.Teacher(Row2(60), "lamp"));
        Assert.Equal(w.M.Grid.MachinesOf("emergencyLamp")[0], w.M.Lessons.Teacher(Row2(100), "emergencyLamp"));
        Assert.Null(w.M.Lessons.Teacher(Row2(10), "nothing"));
    }

    /// <summary>The drone is curious about a teacher within <see cref="Spec.Lessons.Wonder"/> (drone-thoughts), yet learns only within reach.</summary>
    [Fact]
    public void ATeacherAskedFurtherIsFoundThereButTeachesOnlyWithinReach()
    {
        var w = Strip2().Do(new Enter(Row2(31)) { From = 1 }).Run(5);
        var lamp = w.M.Grid.MachinesOf("lamp")[0];
        Assert.Null(w.M.Lessons.Teacher(Row2(31), "lamp"));
        Assert.Equal(lamp, w.M.Lessons.Teacher(Row2(31), "lamp", Spec.Lessons.Wonder));
        Assert.Null(w.M.Lessons.Teacher(Row2(43), "lamp", Spec.Lessons.Wonder));
        Assert.False(w.M.Knowledge.Knows(1, Lamp));
    }

    /// <summary>
    /// The wreck's upper emergency lamp is beyond reach of the alien core yet within wonder (drone-thoughts): a hero just paired does not
    /// learn the alarms there, and its drone is curious about the lamp, pointing the way.
    /// </summary>
    [Fact]
    public void TheWrecksUpperEmergencyLampIsBeyondReachOfTheCoreButWithinWonder()
    {
        var w = new Scenario(Registry.Wreck with { Meteors = false });
        int width = w.M.Grid.Width, core = w.M.Grid.MachinesOf(Spec.LightDrone.CoreMachine)[0].Cell;
        var upper = w.M.Grid.MachinesOf("emergencyLamp").MinBy(l => l.Cell / width)!;
        var spec = Spec.Machines.Get(upper.Kind);
        long dx = Math.Max(0, core % width - (upper.Cell % width + spec.Width - 1)), dy = Math.Max(0, core / width - (upper.Cell / width + spec.Height - 1));
        Assert.InRange(dx * dx + dy * dy, (long)Reach * Reach + 1, (long)Spec.Lessons.Wonder * Spec.Lessons.Wonder);
        Assert.NotNull(w.M.Lessons.Teacher(core, "emergencyLamp", Spec.Lessons.Wonder));
        Assert.Null(w.M.Lessons.Teacher(core, "emergencyLamp"));
    }

    [Theory]
    [InlineData("scanner")]
    [InlineData("droneLamp")]
    public void TheMarketSellsNeitherTheLampNorTheScan(string form)
    {
        Assert.Null(Spec.Research.Node(form));
        var w = new Scenario(Strip).Run();
        w.M.Knowledge.Earn(100_000);
        w.Do(new BuyBlock(form)).Run();
        Assert.Equal(["research.locked"], w.Refusals.Select(r => r.Reason));
        Assert.Equal(100_000, w.M.Knowledge.Credits);
        Assert.Empty(w.M.Knowledge.Deliveries);
    }

    [Fact]
    public void CountsAndFormsAreSavedAndKeptByAHeroWhoLeavesAndComesBack()
    {
        var w = Strip2().Do(new Enter(Row2(3)) { From = 1 }).Run();
        for (int i = 0; i < 4; i++) Break(w, 1);
        w.Reload();
        Assert.Equal(4, w.M.Knowledge.DeedsOf(1, Broken));
        Assert.True(w.M.Knowledge.Knows(1, Lamp));
        w.Do(new Leave { From = 1 }).Run().Do(new Enter(Row2(60)) { From = 1 }).Run();
        Assert.Equal(4, w.M.Knowledge.DeedsOf(1, Broken));
        Assert.True(w.M.Knowledge.Knows(1, Lamp));
        for (int i = 0; i < 6; i++) Break(w, 1);
        Assert.True(w.M.Knowledge.Knows(1, Scan));
    }

    /// <summary>One step of the invariant: peer 1 or 2 walks to a column of row 2, or breaks a wreck.</summary>
    public sealed record Step(int Peer, bool Breaks, int X);

    /// <summary>After any walks and breaks of two paired peers, each knows a form exactly when its own source was met.</summary>
    [Property(MaxTest = 40)]
    public Property EachPeerKnowsAFormExactlyWhenItsSourceWasMet()
    {
        var steps = Gen.ListOf(Gen.Choose(1, 2).SelectMany(p => Gen.Choose(0, 2).SelectMany(k => Gen.Choose(0, Wide - 1).Select(x => new Step(p, k > 0, x)))))
            .Select(s => s.Take(60).ToList());
        return Prop.ForAll(steps.ToArbitrary(), list =>
        {
            var w = Strip2().Do(new Enter(Row2(60)) { From = 1 }).Do(new Enter(Row2(61)) { From = 2 }).Run();
            var breaks = new int[3];
            var lamp = new bool[3];
            var alarms = new bool[3];
            foreach (var step in list)
            {
                if (step.Breaks)
                {
                    Break(w, step.Peer);
                    breaks[step.Peer]++;
                    continue;
                }
                w.Do(new Enter(Row2(step.X)) { From = step.Peer }).Run();
                // Row 2 is one under the footprints: the lamp's columns 0 and 1, the emergency lamp's 120 and 121.
                long toLamp = Math.Max(0, step.X - 1), toAlarm = Math.Max(0, 120 - step.X);
                lamp[step.Peer] |= toLamp * toLamp + 1 <= Reach * Reach;
                alarms[step.Peer] |= toAlarm * toAlarm + 1 <= Reach * Reach;
            }
            for (int p = 1; p <= 2; p++)
            {
                if (w.M.Knowledge.Knows(p, Scan) != breaks[p] >= 10) return false;
                if (w.M.Knowledge.Knows(p, Lamp) != lamp[p]) return false;
                if (w.M.Knowledge.Knows(p, Alarms) != alarms[p]) return false;
            }
            return true;
        });
    }
}
