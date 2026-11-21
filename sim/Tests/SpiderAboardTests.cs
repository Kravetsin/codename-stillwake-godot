using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Stillwake.Sim.Creatures;
using Stillwake.Sim.Crew;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Places;
using Xunit;
using Spec = Stillwake.Content.Creatures;

namespace Stillwake.Sim.Tests;

/// <summary>
/// The spider aboard an intact vessel (derelict-enemies, G-2): woken by a hero walking into its room or by a blow, then hunting the crew
/// through the vessel by its decks, doorways and hatch well, prying shut doorways open, the way in never.
/// </summary>
public class SpiderAboardTests
{
    private const int U = CreaturesModule.Unit, M = Content.Vessels.Margin;

    private static readonly Spec.Prowl Prowl = Spec.Spider.Brain.OfType<Spec.Prowl>().Single();

    private static readonly int Wake = Spec.Spider.Brain.OfType<Spec.Dock>().Single().Wake;

    /// <summary>A freighter boarded (<see cref="GuardsAboardTests.Boarded"/>), its spiders asleep.</summary>
    private sealed class Aboard
    {
        public readonly Scenario W;
        public readonly Registry V;
        public readonly Vessel Vessel;

        public Aboard()
        {
            (W, V, Vessel) = GuardsAboardTests.Boarded("freighter");
            Assert.True(Spiders.Count >= 2, $"{Spiders.Count} spiders");
        }

        public IReadOnlyList<Creature> Spiders => [.. V.Creatures.Creatures.Where(c => c.Species == Spec.Spider.Id)];

        public int Cell(int x, int y) => y * V.Grid.Width + x;

        /// <summary>Dock <paramref name="c"/>'s room, in the world.</summary>
        public Vessel.Compartment Room(Creature c)
        {
            var r = Vessel.Compartments[V.Creatures.Docks[CreaturesModule.DockOf(c)!.Value].Room];
            return r with { Left = M + r.Left, Top = M + r.Top };
        }

        /// <summary>Peer <paramref name="peer"/>'s hero's body's middle in cell (<paramref name="x"/>, <paramref name="y"/>), as its client reports it.</summary>
        public Aboard Hero(int x, int y, int peer = 0)
        {
            W.Do(new Enter(Cell(x, y)) { From = peer });
            return this;
        }

        public Aboard Run(int ticks = 1)
        {
            W.Run(ticks);
            return this;
        }

        /// <summary>Runs till <paramref name="done"/>, at most <paramref name="most"/> ticks: whether it came.</summary>
        public bool RunTill(Func<bool> done, int most)
        {
            for (int t = 0; t < most && !done(); t++) W.Run();
            return done();
        }

        public bool Shut(int x, int y) => Doorways.IsClosed(V.Grid.StructureAt(Cell(x, y)));

        public bool Grounded(Creature c) => V.Grid.Solid(Cell(c.X / U, c.Y / U)) || V.Grid.Solid(Cell((c.X - 1) / U, c.Y / U));

        /// <summary>The deck a row is on: how many of the vessel's floors (decks', hatches') lie above it.</summary>
        public int Level(int row) => Vessel.Decks.Select(d => M + d.Floor).Concat(Vessel.Hatches.Select(h => M + h.Y)).Distinct().Count(f => f < row);

        /// <summary>The door between room <paramref name="r"/> and the room on its left, in the world: its left column and top row.</summary>
        public (int X, int Y)? LeftDoor(Vessel.Compartment r) =>
            Vessel.Doors.FirstOrDefault(d => M + d.X == r.Left - 2 && M + d.Y == r.Top && d != Shipyard.WayIn(Vessel)) is { } d ? (M + d.X, M + d.Y) : null;
    }

    private static bool Awake(Creature c) => c.Posture is not (Posture.Docked or Posture.Waking);

    /// <summary>The row a hero standing on floor row <paramref name="floor"/> reports: its body's middle's (<c>Hero.BodyCell</c>), 2.6 cells over its feet.</summary>
    private static int Body(int floor) => floor - 3;

    /// <summary>A spider whose room has an inner door on its left, and that door.</summary>
    private static (Creature Spider, Vessel.Compartment Room, (int X, int Y) Door) WithLeftDoor(Aboard a)
    {
        foreach (var s in a.Spiders)
            if (a.LeftDoor(a.Room(s)) is { } door) return (s, a.Room(s), door);
        throw new InvalidOperationException("no spider's room has an inner door on its left");
    }

    [Fact]
    public void SeenThroughAnOpenDoorOrFromItsDoorwayItSleepsAndOneStepInWakesItOnItsFloor()
    {
        var a = new Aboard();
        var (spider, room, door) = WithLeftDoor(a);
        int body = Body(room.Floor);
        a.W.Do(new SetDoor(a.Cell(door.X, door.Y), true));
        a.Hero(door.X - 3, body).Run(60);
        Assert.False(a.Shut(door.X, door.Y));
        Assert.Equal(Posture.Docked, spider.Posture);
        a.Hero(door.X, body).Run(30).Hero(door.X + 1, body).Run(30);
        Assert.Equal(Posture.Docked, spider.Posture);
        a.Hero(room.Left, body).Run();
        Assert.Equal(Posture.Waking, spider.Posture);
        Assert.All(a.Spiders.Where(s => s != spider), s => Assert.Equal(Posture.Docked, s.Posture));
        a.Run(Wake - 2);
        Assert.Equal(Posture.Waking, spider.Posture);
        a.Run();
        Assert.True(Awake(spider), $"{spider.Posture}");
        Assert.Equal((room.Floor * U, true), (spider.Y, a.Grounded(spider)));
    }

    [Fact]
    public void ABlowFromAnywhereWakesItAndHurtsIt()
    {
        var a = new Aboard();
        var spider = a.Spiders[0];
        a.W.Do(new Strike(spider.Id, Content.Weapons.Axe.Item));
        a.Run();
        Assert.Empty(a.W.Refusals);
        Assert.Equal((Posture.Waking, Content.Weapons.Axe.Damage), (spider.Posture, spider.Damage));
        a.Run(Wake);
        Assert.True(Awake(spider));
    }

    [Fact]
    public void KilledAsleepItsDockIsLeftEmptyAndTheOthersSleepOn()
    {
        var a = new Aboard();
        var spider = a.Spiders[0];
        int dock = CreaturesModule.DockOf(spider)!.Value;
        Assert.True(a.RunTill(() =>
        {
            if (a.V.Creatures.Creatures.Contains(spider)) a.W.Do(new Strike(spider.Id, Content.Weapons.Axe.Item));
            return !a.V.Creatures.Creatures.Contains(spider);
        }, 40));
        Assert.Equal(Spec.Spider.Life.Hp, spider.Damage);
        Assert.DoesNotContain(a.V.Creatures.Creatures, c => CreaturesModule.DockOf(c) == dock);
        Assert.Equal(a.V.Creatures.Docks.Count - 1, a.V.Creatures.Creatures.Count);
        Assert.All(a.V.Creatures.Creatures, c => Assert.Equal(Posture.Docked, c.Posture));
    }

    [Fact]
    public void KilledItGivesTheSentrysYields() => Assert.Equal(Spec.Sentry.Yields, Spec.Spider.Yields);

    /// <summary>Woken by a step into its room, then its hero gone into the room on its left behind the door between, shut.</summary>
    private static (Aboard A, Creature Spider, (int X, int Y) Door, int Body) BehindADoor()
    {
        var a = new Aboard();
        var (spider, room, door) = WithLeftDoor(a);
        int body = Body(room.Floor);
        a.Hero(room.Left + 1, body).Run(Wake + 1);
        Assert.True(Awake(spider));
        a.Hero(door.X - 4, body);
        if (!a.Shut(door.X, door.Y)) a.W.Do(new SetDoor(a.Cell(door.X, door.Y), false));
        a.Run();
        Assert.True(a.Shut(door.X, door.Y));
        return (a, spider, door, body);
    }

    [Fact]
    public void AtAShutDoorOnItsWayItPriesItsTicksThenOpensIt()
    {
        var (a, spider, door, _) = BehindADoor();
        Assert.True(a.RunTill(() => spider.Recall(ProwlDrive.PryKey) != 0, 300), "it never pried");
        int since = spider.Recall(ProwlDrive.PrySince);
        Assert.True(a.RunTill(() => !a.Shut(door.X, door.Y), Prowl.Pry + 1));
        Assert.Equal(since + Prowl.Pry, a.V.Creatures.Clock);
        Assert.True(a.RunTill(() => spider.X / U < door.X, 100), "it never went through");
    }

    [Fact]
    public void ADoorOpenedAndShutAgainWhileItPriesStartsItsWaitOver()
    {
        var (a, spider, door, body) = BehindADoor();
        Assert.True(a.RunTill(() => spider.Recall(ProwlDrive.PryKey) != 0, 300));
        int since = spider.Recall(ProwlDrive.PrySince);
        a.Run(Prowl.Pry / 2);
        // Opened, its hero gone back behind it: it turns away; shut again once the door rests, the hero beyond it again.
        a.W.Do(new SetDoor(a.Cell(door.X, door.Y), true));
        a.Hero(spider.X / U + 6, body).Run();
        Assert.Equal(0, spider.Recall(ProwlDrive.PryKey));
        a.Run(Content.Doors.SwingTicks);
        a.W.Do(new SetDoor(a.Cell(door.X, door.Y), false));
        a.Hero(door.X - 4, body).Run();
        Assert.True(a.Shut(door.X, door.Y));
        int shut = a.V.Creatures.Clock;
        Assert.True(a.RunTill(() => !a.Shut(door.X, door.Y), 400));
        Assert.True(a.V.Creatures.Clock >= shut + Prowl.Pry, $"opened at {a.V.Creatures.Clock}, shut again at {shut}, first pried at {since}");
    }

    [Fact]
    public void ItNeverPriesTheWayIn()
    {
        var a = new Aboard();
        var door = Shipyard.WayIn(a.Vessel);
        var (x, y) = (M + door.X, M + door.Y);
        Assert.True(a.Shut(x, y));
        Assert.Null(a.V.Creatures.ShutDoorway(x, y, x + 1, y + Content.Vessels.Open - 1));
        // Its hero just inside the way in, then out past it: the spider comes, then stands, the way in shut.
        var (fx, fy) = Shipyard.Door(a.Vessel);
        var spider = a.Spiders[0];
        a.W.Do(new Strike(spider.Id, Content.Weapons.Axe.Item));
        a.Hero(M + fx, M + fy).Run(600);
        a.Hero(x - 2, M + fy).Run(600);
        Assert.True(a.Shut(x, y));
        Assert.Equal(0, spider.Recall(ProwlDrive.PryKey));
    }

    [Fact]
    public void ItFollowsItsHeroUpThroughTheHatchWellToAnotherDeckAndDownAgain()
    {
        var a = new Aboard();
        var spider = a.Spiders.MaxBy(s => s.Y)!;
        var room = a.Room(spider);
        a.Hero(room.Left + 1, Body(room.Floor)).Run(Wake + 1);
        Assert.True(Awake(spider));
        // The hero up a deck, in the room over the well: the spider climbs the well, prying its hatch.
        var hatch = a.Vessel.Hatches.Select(h => (X: M + h.X, Y: M + h.Y)).Where(h => h.Y < room.Floor && h.Y > room.Top - Content.Vessels.Pitch - 1).Single();
        a.Hero(hatch.X + 6, Body(hatch.Y));
        int up = a.Level(hatch.Y - 1);
        Assert.True(a.RunTill(() => a.Level(spider.Y / U - 1) == up && a.Grounded(spider) && spider.Posture != Posture.Climbing, 900),
            $"at {spider.X},{spider.Y} {spider.Posture}");
        Assert.False(a.Shut(hatch.X, hatch.Y));
        // Back down: it drops down through the hatch.
        a.Hero(room.Left + 2, Body(room.Floor));
        Assert.True(a.RunTill(() => a.Level(spider.Y / U - 1) == a.Level(room.Floor - 1) && a.Grounded(spider), 900), $"at {spider.X},{spider.Y} {spider.Posture}");
    }

    [Fact]
    public void PryingADoorBesideAnOpenHatchItStandsOnTheHatchNotDownTheWell()
    {
        var a = new Aboard();
        // A hatch right of an inner door, in the floor of that door's deck, and a spider docked on that deck right of it.
        var (hatch, door) = (from h in a.Vessel.Hatches
                             from d in a.Vessel.Doors
                             where d.X + 2 == h.X && d.Y + Content.Vessels.Open == h.Y && d != Shipyard.WayIn(a.Vessel)
                             select ((X: M + h.X, Y: M + h.Y), (X: M + d.X, Y: M + d.Y))).First();
        var spider = a.Spiders.First(s => a.Room(s).Floor == hatch.Y && a.Room(s).Left > hatch.X);
        var room = a.Room(spider);
        foreach (var d in a.Vessel.Doors.Where(d => M + d.Y == door.Y && M + d.X > door.X && d != Shipyard.WayIn(a.Vessel)))
            a.W.Do(new SetDoor(a.Cell(M + d.X, M + d.Y), true));
        if (!a.Shut(door.X, door.Y)) a.W.Do(new SetDoor(a.Cell(door.X, door.Y), false));
        a.W.Do(new SetDoor(a.Cell(hatch.X, hatch.Y), true));
        a.Hero(room.Left + 1, Body(room.Floor)).Run(Wake + 1);
        Assert.True(Awake(spider));
        a.Hero(door.X - 4, Body(hatch.Y)).Run(Content.Doors.SwingTicks);
        Assert.True(a.Shut(door.X, door.Y));
        Assert.False(a.Shut(hatch.X, hatch.Y));
        Assert.True(a.RunTill(() =>
        {
            Assert.True(spider.Y <= hatch.Y * U, $"down the well at {spider.X},{spider.Y}, the hatch at row {hatch.Y}, {spider.Posture}");
            return spider.X / U < door.X;
        }, 600), $"never through the door: at {spider.X},{spider.Y} {spider.Posture}");
    }

    [Fact]
    public void ItPouncesAtTheMiddleOfItsHerosBodyNotOverHisHead()
    {
        var a = new Aboard();
        var spider = a.Spiders.MaxBy(s => s.Y)!;
        var room = a.Room(spider);
        a.Hero(room.Left + 1, Body(room.Floor));
        Assert.True(a.RunTill(() => spider.Act is { Move: 0, Phase: ActPhase.WindUp }, 300), $"it never crouched: {spider.Posture}");
        // Crouched, its hero steps eight cells off on its floor: it flies at him there.
        int x = spider.X / U + (spider.X / U - room.Left > room.Width / 2 ? -8 : 8);
        a.Hero(x, Body(room.Floor));
        Assert.True(a.RunTill(() => spider.Act is { Phase: ActPhase.Active }, Spec.Sentry.Moves.OfType<Spec.Pounce>().Single().WindUp + 1));
        // Its flight's line, from its middle, passes within half a cell of his body's middle, 2.6 cells over his feet.
        long dx = x * U + U / 2 - spider.X, dy = room.Floor * U - Content.Hero.Height / 2 - (spider.Y - Spec.Spider.Body.Height * U / 2);
        long cross = spider.VX * dy - spider.VY * dx, run = (long)spider.VX * spider.VX + (long)spider.VY * spider.VY;
        Assert.True(cross * cross <= U / 2 * (U / 2) * run, $"flying {spider.VX},{spider.VY} from {spider.X},{spider.Y} at a hero {dx},{dy} off");
    }

    [Fact]
    public void ItKeepsHuntingItsHeroOutOfItsRoomAndNeverSleepsAgain()
    {
        var a = new Aboard();
        var spider = a.Spiders.MaxBy(s => s.Y)!;
        var room = a.Room(spider);
        a.Hero(room.Left + 1, Body(room.Floor)).Run(Wake + 1);
        int far = room.Left + room.Width + 12;
        a.Hero(far, Body(room.Floor)).Run(400);
        Assert.True(Awake(spider));
        Assert.True(Math.Abs(spider.X / U - far) < Math.Abs(room.Left + room.Width / 2 - far), $"at {spider.X / U}, its hero at {far}");
    }

    [Fact]
    public void WithNoHeroAboardItStandsWhereItIs()
    {
        var a = new Aboard();
        var spider = a.Spiders.MaxBy(s => s.Y)!;
        var room = a.Room(spider);
        a.Hero(room.Left + 1, Body(room.Floor)).Run(Wake + 1).Hero(room.Left + room.Width + 12, Body(room.Floor)).Run(20);
        // Out past the way in, into space: aboard no more.
        var door = Shipyard.WayIn(a.Vessel);
        a.Hero(M + door.X - 4, M + door.Y + Content.Vessels.Open - 1).Run(40);
        var (x, y) = (spider.X, spider.Y);
        a.Run(200);
        Assert.True(Awake(spider));
        Assert.Equal((x, y), (spider.X, spider.Y));
    }

    /// <summary>
    /// What a spider may stand on, every tick of a play aboard: its body in the hull box; climbing, it holds a wall flush beside it or is in
    /// line with a hatch over its well; never two ticks running at one height in the air, held by nothing (a ceiling, a back wall).
    /// </summary>
    private static void Holds(Registry v, Vessel vessel, Creature c, Dictionary<int, int> aloft)
    {
        var w = (Spec.Walker)Spec.Spider.Body;
        int half = w.Width * U / 2, left = (c.X - half) / U, right = (c.X + half - 1) / U, top = (c.Y - w.Height * U) / U, feet = (c.Y - 1) / U;
        var hull = v.Creatures.Hull;
        Assert.True(hull.Contains(left, top) && hull.Contains(right, feet), $"spider {c.Id} at {c.X},{c.Y} out of the hull box {hull}");
        bool Solid(int x, int y) => v.Grid.Solid(y * v.Grid.Width + x);
        if (c.Posture == Posture.Climbing)
        {
            int side = c.Recall(WalkerBody.Wall);
            if (side == WalkerBody.Well)
                Assert.True(vessel.Hatches.Any(h => (M + h.X + 1) * U == c.X && c.Y > (M + h.Y - 1) * U && c.Y <= (M + vessel.Decks.Select(d => d.Floor).Where(f => f > h.Y).Min()) * U),
                    $"spider {c.Id} climbing at {c.X},{c.Y} in no hatch's well");
            else
            {
                int beside = side > 0 ? right + 1 : left - 1;
                // Its wall beside its body, or the wall's top under its feet' row, which it pulls itself over.
                Assert.True((c.X + side * half) % U == 0 && Enumerable.Range(top, feet - top + 2).Any(y => Solid(beside, y)),
                    $"spider {c.Id} climbing at {c.X},{c.Y} on no wall to its {side}");
            }
        }
        // A hatch under its feet holds it, open too: one it spans (its way not down through it).
        bool held = c.Posture == Posture.Climbing || c.Act != null
            || Enumerable.Range(left, right - left + 1).Any(x => Solid(x, feet + 1) || Doorways.IsDoorway(v.Grid.StructureAt((feet + 1) * v.Grid.Width + x)))
            || Enumerable.Range(left, right - left + 1).Any(x => Enumerable.Range(top, feet - top + 1).Any(y => Solid(x, y)));
        if (held) aloft.Remove(c.Id);
        else if (aloft.TryGetValue(c.Id, out int was) && was == c.Y) Assert.Fail($"spider {c.Id} hangs in the air at {c.X},{c.Y}: {c.Posture} v {c.VX},{c.VY} drive {c.Drive} mem {string.Join(',', (c.Mem ?? []).Select(n => n.Key + '=' + n.Value))} clock {v.Creatures.Clock}");
        else aloft[c.Id] = c.Y;
    }

    private static Arbitrary<(string Class, uint Index, uint Seed)> Plays() =>
        Gen.Zip(Gen.Zip(Gen.Elements("freighter", "liner"), Gen.Choose(0, 2)), ArbMap.Default.GeneratorFor<uint>())
            .Select(t => (t.Item1.Item1, (uint)t.Item1.Item2, t.Item2)).ToArbitrary();

    [Property(MaxTest = 12)]
    public Property ASpiderIsNeverOnABackWallACeilingOrOutsideTheHullBox() => Prop.ForAll(Plays(), play =>
    {
        var (w, v, vessel) = GuardsAboardTests.Boarded(play.Class, play.Index);
        var rng = new Rng(new RngState { S = play.Seed | 1 });
        var rooms = vessel.Compartments;
        var aloft = new Dictionary<int, int>();
        int surface = w.World.SurfaceOf(0);
        for (int t = 0; t < 1500; t++)
        {
            if (t % 30 == 0)
            {
                // The hero somewhere aboard, on a room's floor; now and then a blow on a spider, a doorway set.
                var r = rooms[(int)rng.Below((uint)rooms.Count)];
                w.Do(new Enter((M + Body(r.Floor)) * v.Grid.Width + M + r.Left + (int)rng.Below((uint)r.Width)));
                var all = v.Creatures.Creatures;
                if (rng.Below(3) == 0 && all.Count > 0) w.Do(new Strike(all[(int)rng.Below((uint)all.Count)].Id, Content.Weapons.Axe.Item));
                if (rng.Below(3) == 0)
                {
                    var d = vessel.Doors[(int)rng.Below((uint)vessel.Doors.Count)];
                    w.Do(new SetDoor((M + d.Y) * v.Grid.Width + M + d.X, rng.Below(2) == 0));
                }
                if (rng.Below(4) == 0 && vessel.Hatches.Count > 0)
                {
                    var h = vessel.Hatches[(int)rng.Below((uint)vessel.Hatches.Count)];
                    w.Do(new SetDoor((M + h.Y) * v.Grid.Width + M + h.X, rng.Below(2) == 0));
                }
            }
            w.Run();
            // Its hero beaten and carried home, the vessel's world closes: the play is over.
            if (w.World.SurfaceOf(0) != surface) break;
            foreach (var c in v.Creatures.Creatures.Where(c => c.Species == Spec.Spider.Id && Awake(c))) Holds(v, vessel, c, aloft);
        }
        return true.ToProperty();
    });

    /// <summary>
    /// Two worlds fed the same commands aboard a freighter, alike every tick while a spider wakes, pries a door, is struck and hunts its
    /// hero up the well; mid-hunt a second hero comes aboard, and a peer joins: the one world's save loaded, played on beside the other.
    /// </summary>
    [Fact]
    public void TwoWorldsStayAlikeThroughAWakeAPryABlowAndAHuntAndAJoinMidHunt()
    {
        var (a, b) = (new Registry(), new Registry());
        var first = World.Create(1, Registry.Wreck, a.All);
        var second = World.Create(1, Registry.Wreck, b.All);
        void Both(params Command[] commands)
        {
            first.Step(commands);
            second.Step(commands);
            Assert.Equal(first.Hash(), second.Hash());
        }
        void Run(int ticks)
        {
            for (int i = 0; i < ticks; i++) Both();
        }
        Both(new Enter(a.Crew.Spawn));
        Both(new Find("freighter"));
        var contact = a.Places.Contacts[^1];
        Both(new Jump(contact.Id));
        int surface = first.SurfaceOf(0);
        var v = Registry.On(first, surface);
        var vessel = Shipyard.Describe(Content.Vessels.Class("freighter")!, contact.Seed);
        int width = v.Grid.Width;
        var spider = v.Creatures.Creatures.Where(c => c.Species == Spec.Spider.Id).MaxBy(c => c.Y)!;
        var dock = v.Creatures.Docks[CreaturesModule.DockOf(spider)!.Value];
        var room = vessel.Compartments[dock.Room];
        int body = M + Body(room.Floor), id = spider.Id;
        // Woken by a step in, its hero then behind the door on its left, shut: a pry.
        Both(new Enter(body * width + M + room.Left + 1));
        Run(Wake + 5);
        int door = (M + room.Top) * width + M + room.Left - 2;
        Both(new Enter(body * width + M + room.Left - 6));
        if (Doorways.IsOpen(v.Grid.StructureAt(door))) Both(new SetDoor(door, false));
        Run(Prowl.Pry + 60);
        Both(new Strike(id, Content.Weapons.Axe.Item));
        // Up a deck: a hunt up the well.
        var hatch = vessel.Hatches.Where(h => h.Y < room.Floor && h.Y > room.Top - Content.Vessels.Pitch - 1).Single();
        Both(new Enter((M + Body(hatch.Y)) * width + M + hatch.X + 6));
        Run(60);
        Both(new Enter(a.Crew.Spawn) { From = 1 });
        Both(new Transfer(surface) { From = 1 });
        Run(30);
        var joined = World.Load(first.Save(), new Registry().All);
        Assert.Equal(first.Hash(), joined.Hash());
        for (int i = 0; i < 300; i++)
        {
            first.Step([]);
            joined.Step([]);
            Assert.Equal(first.Hash(), joined.Hash());
        }
        var hunter = Registry.On(first, surface).Creatures.Creatures.Single(c => c.Id == id);
        Assert.True(hunter.Recall(DockDrive.Woke) == 1 && hunter.Damage > 0, $"{hunter.Posture} damage {hunter.Damage}");
    }
}
