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
/// The spider aboard an intact vessel (derelict-guards, G-2): woken by a hero walking into its room or by a blow, then hunting the crew
/// through the vessel by its decks, doorways and hatch well, prying shut doorways open, the way in never.
/// </summary>
public class SpiderAboardTests
{
    private const int U = CreaturesModule.Unit, M = Content.Vessels.Margin;

    private static readonly Spec.Prowl Prowl = Spec.Spider.Brain.OfType<Spec.Prowl>().Single();

    private static readonly int Wake = Spec.Spider.Brain.OfType<Spec.Dock>().Single().Wake;

    /// <summary>A vessel boarded (<see cref="GuardsAboardTests.Boarded"/>), a freighter unless said, its spiders asleep.</summary>
    internal sealed class Aboard
    {
        public readonly Scenario W;
        public readonly Registry V;
        public readonly Built Built;

        public Vessel Vessel => Built.Vessel;

        public Aboard(string cls = "freighter")
        {
            (W, V, Built) = GuardsAboardTests.Boarded(cls);
            Assert.True(Spiders.Count >= 2, $"{Spiders.Count} spiders");
        }

        public Creature Spider(int id) => Spiders.Single(s => s.Id == id);

        /// <summary>The way in's cells in the world: its left column and top row.</summary>
        public (int X, int Y) WayIn => (M + Shipyard.WayIn(Built).X, M + Shipyard.WayIn(Built).Y);

        /// <summary>Its hero out past the way in, in space: aboard no more.</summary>
        public Aboard Out(int peer = 0) => Hero(WayIn.X - 4, WayIn.Y + Content.Vessels.Open - 1, peer);

        /// <summary>Woken by a blow, its hero out past the way in: awake, no hero aboard.</summary>
        public Aboard Woken(Creature spider)
        {
            Out();
            W.Do(new Strike(spider.Id, Content.Weapons.Axe.Item));
            Run(Wake + 2);
            Assert.True(Awake(spider), $"{spider.Posture}");
            return this;
        }

        /// <summary>
        /// Its hero by <paramref name="spider"/>, on row <paramref name="y"/>, then walked to column <paramref name="x"/> six cells a second
        /// at a time: never out of its chase, it hunts him all the way.
        /// </summary>
        public Aboard Lure(Creature spider, int x, int y)
        {
            for (int at = spider.X / U; ; at += Math.Sign(x - at) * Math.Min(6, Math.Abs(x - at)))
            {
                Hero(at, y).Run(20);
                if (at == x) return this;
            }
        }

        /// <summary>Every inner door on the way in's deck opened, and the way in.</summary>
        public Aboard OpenWayInsDeck()
        {
            foreach (var d in Vessel.Doors.Where(d => M + d.Y == WayIn.Y)) W.Do(new SetDoor(Cell(M + d.X, M + d.Y), true));
            return Run(Content.Doors.SwingTicks);
        }

        /// <summary>Whether a body of <paramref name="c"/> lies wholly in the hull box, read from the vessel's sections.</summary>
        public bool InHull(Creature c)
        {
            var hull = GuardsAboardTests.HullOf(Built);
            int half = Spec.Spider.Body.Width * U / 2;
            return hull.Contains((c.X - half) / U, (c.Y - Spec.Spider.Body.Height * U) / U) && hull.Contains((c.X + half - 1) / U, (c.Y - 1) / U);
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
            Vessel.Doors.FirstOrDefault(d => M + d.X == r.Left - 2 && M + d.Y == r.Top && d != Shipyard.WayIn(Built)) is { } d ? (M + d.X, M + d.Y) : null;
    }

    internal static bool Awake(Creature c) => c.Posture is not (Posture.Docked or Posture.Waking);

    /// <summary>The row a hero standing on floor row <paramref name="floor"/> reports: its body's middle's (<c>Hero.BodyCell</c>), 2.6 cells over its feet.</summary>
    internal static int Body(int floor) => floor - 3;

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

    /// <summary>A blow wakes it, so none dies asleep: struck on and on, it dies still waking.</summary>
    [Fact]
    public void StruckDeadWhileItWakesItsDockIsLeftEmptyAndTheOthersSleepOn()
    {
        var a = new Aboard();
        var spider = a.Spiders[0];
        int dock = CreaturesModule.DockOf(spider)!.Value;
        var postures = new List<Posture>();
        Assert.True(a.RunTill(() =>
        {
            if (!a.V.Creatures.Creatures.Contains(spider)) return true;
            postures.Add(spider.Posture);
            a.W.Do(new Strike(spider.Id, Content.Weapons.Axe.Item));
            return false;
        }, Wake - 1));
        Assert.Equal(Posture.Docked, postures[0]);
        Assert.All(postures.Skip(1), p => Assert.Equal(Posture.Waking, p));
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
        Assert.NotNull(CreaturesModule.PryingAt(spider));
        int since = spider.Recall(ProwlDrive.PrySince);
        Assert.True(a.RunTill(() => !a.Shut(door.X, door.Y), Prowl.Pry + 1));
        Assert.Null(CreaturesModule.PryingAt(spider));
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
    public void APryBrokenOffByItsShotAtABlowStartsOverAfterIt()
    {
        var (a, spider, door, body) = BehindADoor();
        Assert.True(a.RunTill(() => spider.Recall(ProwlDrive.PryKey) != 0, 300));
        int since = spider.Recall(ProwlDrive.PrySince);
        // Its hero gone on, past its pounce's reach behind the door.
        a.Hero(door.X - 24, body).Run(Prowl.Pry / 2);
        // Struck mid-pry: it answers him with its shot where it stands, its pry let go, begun anew once the shot is over.
        a.W.Do(new Strike(spider.Id, Content.Weapons.Axe.Item));
        Assert.True(a.RunTill(() => spider.Act != null, 3));
        Assert.IsType<Spec.Shoot>(Spec.Spider.Moves[spider.Act!.Move]);
        Assert.Null(CreaturesModule.PryingAt(spider));
        Assert.True(a.RunTill(() => spider.Act == null, 60));
        int over = a.V.Creatures.Clock;
        Assert.True(a.RunTill(() => !a.Shut(door.X, door.Y), 300));
        Assert.True(a.V.Creatures.Clock >= over + Prowl.Pry, $"opened at {a.V.Creatures.Clock}, its shot over at {over}, first pried at {since}");
    }

    [Fact]
    public void ItHuntsTheNearerOfTwoHeroesAndAtATieTheFirstInTheCrewsOrder()
    {
        var a = new Aboard();
        var spider = a.Spiders.MaxBy(s => Math.Min(s.X / U - a.Room(s).Left, a.Room(s).Left + a.Room(s).Width - 1 - s.X / U))!;
        var room = a.Room(spider);
        a.Woken(spider);
        GuardsAboardTests.Board(a.W, 1, a.Cell(a.WayIn.X - 4, a.WayIn.Y + Content.Vessels.Open - 1));
        int body = Body(room.Floor);
        // Both on its room's floor either side of it: the nearer, then as near, the first in the crew's order whichever side.
        void Check(int zero, int one, int way)
        {
            int x = spider.X / U, d = Math.Min(x - room.Left, room.Left + room.Width - 1 - x);
            Assert.True(d >= 4, $"room {room.Left}+{room.Width}, at {x}");
            int at0 = x + zero * d / 4, at1 = x + one * d / 4;
            a.Hero(at0, body, 0).Hero(at1, body, 1).Run();
            Assert.Equal(Math.Abs(at0 - x) <= Math.Abs(at1 - x) ? (at0, body) : (at1, body), a.V.Creatures.Quarry(spider));
            Assert.True(Math.Sign(spider.VX) == way, $"going {spider.VX}, the heroes at {at0} and {at1}, it at {x}");
        }
        Check(-3, 4, -1);
        Check(4, -3, -1);
        Check(4, -4, 1);
        Check(-4, 4, -1);
    }

    /// <summary>The cells of what is solid inside room <paramref name="r"/>, off its floor: a shelf, a bunk.</summary>
    private static List<(int X, int Y)> Shelves(Aboard a, Vessel.Compartment r) =>
        [.. from y in Enumerable.Range(r.Top, r.Floor - r.Top - 1) from x in Enumerable.Range(r.Left, r.Width) where a.V.Grid.Solid(a.Cell(x, y)) select (x, y)];

    [Fact]
    public void AShelfInItsWayItCannotLeapUnderItsCeilingItClimbsAndGoesOver()
    {
        var a = new Aboard("liner");
        // A spider with a shelf in its room on one side of it, its hero past the shelf's far end.
        var (spider, room, shelf) = a.Spiders.Select(s => (s, r: a.Room(s))).Select(t => (t.s, t.r, sh: Shelves(a, t.r)))
            .Where(t => t.sh.Count > 0 && t.sh.Max(c => c.X) < t.s.X / U - 2 && t.sh.Min(c => c.X) - 2 >= t.r.Left).Select(t => (t.s, t.r, t.sh)).First();
        int left = shelf.Min(c => c.X);
        a.Woken(spider).Hero(left - 2, Body(room.Floor));
        bool climbed = false;
        Assert.True(a.RunTill(() =>
        {
            Assert.False(a.V.Creatures.Overlaps(Spec.Spider.Body, spider.X, spider.Y), $"in the shelf at {spider.X},{spider.Y}");
            climbed |= spider.Posture == Posture.Climbing && spider.Recall(WalkerBody.Wall) == -1;
            return spider.X / U < left && a.Grounded(spider);
        }, 300), $"never over it: at {spider.X},{spider.Y} {spider.Posture}");
        Assert.True(climbed, "it got past without climbing");
    }

    /// <summary>
    /// In a hall two decks high (the liner's hold, its escort struck dead), the hero past a wall built up from the floor: one three cells
    /// high it leaps, its jump clearing it with the ceiling far over it; one four high, more than its jump, it climbs.
    /// </summary>
    [Theory]
    [InlineData(3, false)]
    [InlineData(4, true)]
    public void InATallHallItLeapsAWallItsJumpClearsAndClimbsOneItDoesNot(int high, bool climbs)
    {
        var a = new Aboard("liner");
        var escort = a.V.Creatures.Creatures.Single(c => c.Species == Spec.Escort.Id && a.Room(c).Left == M + 34);
        var hall = a.Room(escort);
        Assert.True(a.RunTill(() =>
        {
            if (a.V.Creatures.Creatures.Contains(escort)) a.W.Do(new Strike(escort.Id, Content.Weapons.Axe.Item));
            return !a.V.Creatures.Creatures.Contains(escort);
        }, 100));
        // The well at the hall's left end, its hatches opened; the spider on the deck two over it.
        var well = a.Vessel.Hatches.Select(h => (X: M + h.X, Y: M + h.Y)).Where(h => h.X == hall.Left && h.Y < hall.Top).ToList();
        foreach (var h in well) a.W.Do(new SetDoor(a.Cell(h.X, h.Y), true));
        var spider = a.Spiders.Single(s => a.Room(s).Left == hall.Left && a.Room(s).Floor == well.Min(h => h.Y) + Content.Vessels.Pitch);
        int wall = hall.Left + 6, floor = hall.Floor;
        for (int y = floor - high; y < floor; y++)
            for (int x = wall; x < wall + 2; x++)
            {
                if (a.V.Grid.MachineAt(a.Cell(x, y)) != null) a.V.Grid.Clear(a.Cell(x, y), "machine");
                a.V.Grid.Place(a.Cell(x, y), Placement.Wall());
            }
        a.Woken(spider).Hero(wall + 8, Body(floor));
        bool leapt = false, climbed = false;
        Assert.True(a.RunTill(() =>
        {
            if (spider.Y / U > hall.Top && spider.Act == null && spider.Recall(WalkerBody.Wall) == 0 && spider.VY < 0) leapt = true;
            climbed |= spider.Y / U > hall.Top && spider.Recall(WalkerBody.Wall) == 1;
            return spider.X / U > wall + 2 && spider.Y == floor * U;
        }, 400), $"never past it: at {spider.X},{spider.Y} {spider.Posture}");
        Assert.Equal((!climbs, climbs), (leapt, climbed));
    }

    /// <summary>
    /// A hole taken out of the liner's roof over the hall under its top tier, the hatch between open: the hall's left wall runs up to the
    /// roof, the hull box's top. Its hero on the hull's roof past that wall, the spider meets the wall and stands: a climb would carry it
    /// out of the hull box before past the wall's top, out into space.
    /// </summary>
    [Fact]
    public void ItNeverClimbsAWallPastTheHullsTopThroughAHoleInTheRoof()
    {
        var a = new Aboard("liner");
        var hull = GuardsAboardTests.HullOf(a.Built);
        var tier = a.Vessel.Sections.Where(s => s.Kind == Vessel.SectionKind.Tier).MinBy(s => s.Top)!;
        Assert.Equal(hull.Top, M + tier.Top);
        int roof = M + a.Vessel.Sections.First(s => s.Kind == Vessel.SectionKind.Body).Top;
        var spider = a.Spiders.Single(s => a.Room(s).Left == M + tier.Left + 2 && a.Room(s).Top > M + tier.Bottom && a.Room(s).Floor <= roof);
        var room = a.Room(spider);
        var hatch = a.Vessel.Hatches.Select(h => (X: M + h.X, Y: M + h.Y)).Single(h => h.X == room.Left && h.Y < room.Top);
        a.W.Do(new SetDoor(a.Cell(hatch.X, hatch.Y), true));
        for (int y = hull.Top; y < hull.Top + 2; y++)
            for (int x = room.Left; x < room.Left + 2; x++) a.V.Grid.Clear(a.Cell(x, y), "structure");
        Assert.True(Enumerable.Range(hull.Top, room.Floor - hull.Top).All(y => a.V.Grid.Solid(a.Cell(room.Left - 1, y))), "no wall up to the roof");
        a.Woken(spider).Hero(room.Left - 12, roof - 3).Run();
        Assert.NotNull(a.V.Creatures.Quarry(spider));
        Assert.True(a.RunTill(() =>
        {
            Assert.True(a.InHull(spider), $"out of the hull at {spider.X},{spider.Y} {spider.Posture}");
            return spider.X / U <= room.Left + 1 && a.Grounded(spider);
        }, 300), $"never at the wall: at {spider.X},{spider.Y} {spider.Posture}");
        for (int t = 0; t < 200; t++)
        {
            a.Run();
            Assert.True(a.InHull(spider), $"out of the hull at {spider.X},{spider.Y} {spider.Posture} at tick {t}");
        }
    }

    [Fact]
    public void ItNeverPriesTheWayIn()
    {
        var a = new Aboard();
        var (x, y) = a.WayIn;
        Assert.True(a.Shut(x, y));
        // Its hero just inside the way in, then out past it: the spider comes, then stands, the way in shut.
        var (fx, fy) = Shipyard.Door(a.Built);
        var spider = a.Spiders[0];
        a.W.Do(new Strike(spider.Id, Content.Weapons.Axe.Item));
        a.Hero(M + fx, M + fy).Run(600);
        a.Hero(x - 2, M + fy).Run(600);
        Assert.True(a.Shut(x, y));
        Assert.Equal(0, spider.Recall(ProwlDrive.PryKey));
    }

    /// <summary>The spider of the liner's way in's deck, woken, the deck's doors and the way in open.</summary>
    private static (Aboard A, Creature Spider) ByTheWayIn()
    {
        var a = new Aboard("liner");
        var spider = a.Spiders.First(s => a.Room(s).Floor == a.WayIn.Y + Content.Vessels.Open);
        a.Woken(spider).OpenWayInsDeck();
        Assert.False(a.Shut(a.WayIn.X, a.WayIn.Y));
        return (a, spider);
    }

    /// <summary>Whether <paramref name="c"/>'s body has a column in the way in's.</summary>
    private static bool InWayIn(Aboard a, Creature c) =>
        (c.X - Spec.Spider.Body.Width * U / 2) / U <= a.WayIn.X + 1 && (c.X + Spec.Spider.Body.Width * U / 2 - 1) / U >= a.WayIn.X;

    [Fact]
    public void StandingInTheWayInWhenItsHeroShutsItFromInsideItNeverOpensIt()
    {
        var (a, spider) = ByTheWayIn();
        int body = a.WayIn.Y + Content.Vessels.Open - 3;
        // Its hero in the open way in: it comes to him, into the doorway.
        a.Lure(spider, a.WayIn.X + 1, body);
        Assert.True(a.RunTill(() => InWayIn(a, spider) && spider.Act == null && a.Grounded(spider), 900), $"at {spider.X},{spider.Y} {spider.Posture}");
        // He steps in past it and shuts the way in on it: it is shut in, its hero aboard, and never pries it.
        var (fx, fy) = Shipyard.Door(a.Built);
        a.Hero(M + fx, M + fy);
        a.W.Do(new SetDoor(a.Cell(a.WayIn.X, a.WayIn.Y), false));
        a.Run(Content.Doors.SwingTicks + 1);
        Assert.Empty(a.W.Refusals);
        Assert.True(a.Shut(a.WayIn.X, a.WayIn.Y));
        Assert.True(a.V.Creatures.InShutDoorway(Spec.Spider.Body, spider.X, spider.Y), $"at {spider.X},{spider.Y}, not shut in");
        Assert.NotNull(a.V.Creatures.Quarry(spider));
        for (int t = 0; t < 300; t++)
        {
            a.Run();
            Assert.True(a.Shut(a.WayIn.X, a.WayIn.Y), $"opened at tick {t}");
            Assert.Null(CreaturesModule.PryingAt(spider));
        }
    }

    [Fact]
    public void APounceAtAHeroInTheOpenWayInStopsAtTheHullNeverOutIntoSpace()
    {
        var (a, spider) = ByTheWayIn();
        var (fx, fy) = Shipyard.Door(a.Built);
        // Crouched at its hero inside, he steps back into the open way in: its pounce flies at him there, out at the way in.
        a.Lure(spider, M + fx + 12, M + fy);
        Assert.True(a.RunTill(() => spider.Act is { Move: 0, Phase: ActPhase.WindUp }, 600), $"it never crouched: at {spider.X},{spider.Y}");
        Assert.True(spider.X / U > M + fx + 4, $"crouched at {spider.X / U}");
        a.Hero(a.WayIn.X, a.WayIn.Y + Content.Vessels.Open - 3);
        bool flew = false;
        Assert.True(a.RunTill(() =>
        {
            Assert.True(a.InHull(spider), $"out of the hull at {spider.X},{spider.Y} {spider.Posture} v {spider.VX},{spider.VY}");
            if (spider.Act is { Move: 0, Phase: ActPhase.Active }) flew |= spider.VX < 0;
            return flew && spider.Act == null;
        }, 40), $"it never pounced out at the way in: at {spider.X},{spider.Y}");
        a.Run(60);
        Assert.True(a.InHull(spider), $"out of the hull at {spider.X},{spider.Y}");
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
                             where d.X + 2 == h.X && d.Y + Content.Vessels.Open == h.Y && d != Shipyard.WayIn(a.Built)
                             select ((X: M + h.X, Y: M + h.Y), (X: M + d.X, Y: M + d.Y))).First();
        var spider = a.Spiders.First(s => a.Room(s).Floor == hatch.Y && a.Room(s).Left > hatch.X);
        var room = a.Room(spider);
        foreach (var d in a.Vessel.Doors.Where(d => M + d.Y == door.Y && M + d.X > door.X && d != Shipyard.WayIn(a.Built)))
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

    /// <summary>
    /// What a spider may stand on, every tick of a play aboard: its body in the hull box; climbing, it holds a wall flush beside it or is in
    /// line with a hatch over its well; never two ticks running at one height in the air, held by nothing (a ceiling, a back wall).
    /// </summary>
    private static void Holds(Registry v, Vessel vessel, Box hull, Creature c, Dictionary<int, int> aloft)
    {
        var w = (Spec.Walker)Spec.Spider.Body;
        int half = w.Width * U / 2, left = (c.X - half) / U, right = (c.X + half - 1) / U, top = (c.Y - w.Height * U) / U, feet = (c.Y - 1) / U;
        Assert.True(hull.Contains(left, top) && hull.Contains(right, feet), $"spider {c.Id} at {c.X},{c.Y} out of the hull box {hull}");
        bool Solid(int x, int y) => v.Grid.Solid(y * v.Grid.Width + x);
        if (c.Posture == Posture.Climbing)
        {
            int side = c.Recall(WalkerBody.Wall);
            // A well reaches down to the floor of the room under its hatch: a hall two decks tall's, past the deck floor between.
            int Bottom(Vessel.Hatch h) => vessel.Compartments.Where(r => r.Top == h.Y + 1 && h.X >= r.Left && h.X < r.Left + r.Width).Select(r => r.Floor)
                .DefaultIfEmpty(vessel.Decks.Select(d => d.Floor).Where(f => f > h.Y).Min()).First();
            if (side == WalkerBody.Well)
                Assert.True(vessel.Hatches.Any(h => (M + h.X + 1) * U == c.X && c.Y > (M + h.Y - 1) * U && c.Y <= (M + Bottom(h)) * U),
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
        Play(play.Class, play.Index, play.Seed);
        return true.ToProperty();
    });

    /// <summary>
    /// The property's plays reach side-wall climbs, a shelf in the spider's way, a vessel's only inner wall: half of these six do (5 of 12 at
    /// random), the hero put by a shelf every other time; on rooms' floors at random alone, a third did. A vessel with no shelf has none.
    /// </summary>
    [Fact]
    public void ThePlaysClimbSideWalls()
    {
        var plays = new[] { ("freighter", 0u), ("freighter", 1u), ("freighter", 2u), ("liner", 0u), ("liner", 1u), ("liner", 2u) };
        int climbing = plays.Count(p => Play(p.Item1, p.Item2, 7) > 0);
        Assert.True(climbing >= plays.Length / 2, $"{climbing} of {plays.Length} plays climbed a side wall");
    }

    /// <summary>
    /// A play aboard <paramref name="cls"/>'s <paramref name="index"/>th vessel from <paramref name="seed"/>, every awake spider held to what
    /// it may stand on each tick (<see cref="Holds"/>): how many times a spider took hold of a side wall.
    /// </summary>
    private static int Play(string cls, uint index, uint seed)
    {
        var (w, v, built) = GuardsAboardTests.Boarded(cls, index);
        var vessel = built.Vessel;
        var hull = GuardsAboardTests.HullOf(built);
        var rng = new Rng(new RngState { S = seed | 1 });
        var rooms = vessel.Compartments;
        var aloft = new Dictionary<int, int>();
        var holding = new Dictionary<int, int>();
        int surface = w.World.SurfaceOf(0), climbs = 0;
        // Beside each end of a shelf, on its room's floor: where a spider coming from past its other end must climb it.
        var byShelves = new List<int>();
        foreach (var room in rooms)
        {
            var r = room with { Left = M + room.Left, Top = M + room.Top };
            for (int y = r.Top; y < r.Floor - 1; y++)
                for (int x = r.Left + 1; x < r.Left + r.Width - 1; x++)
                {
                    bool solid = v.Grid.Solid(y * v.Grid.Width + x);
                    if (solid && !v.Grid.Solid(y * v.Grid.Width + x - 1)) byShelves.Add(Body(r.Floor) * v.Grid.Width + x - 1);
                    if (solid && !v.Grid.Solid(y * v.Grid.Width + x + 1)) byShelves.Add(Body(r.Floor) * v.Grid.Width + x + 1);
                }
        }
        for (int t = 0; t < 1500; t++)
        {
            if (t % 30 == 0)
            {
                // The hero somewhere aboard: by a shelf every other time, else on a room's floor; now and then a blow on a spider, a doorway set.
                if (byShelves.Count > 0 && rng.Below(2) == 0) w.Do(new Enter(byShelves[(int)rng.Below((uint)byShelves.Count)]));
                else
                {
                    var r = rooms[(int)rng.Below((uint)rooms.Count)];
                    w.Do(new Enter((M + Body(r.Floor)) * v.Grid.Width + M + r.Left + (int)rng.Below((uint)r.Width)));
                }
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
            foreach (var c in v.Creatures.Creatures.Where(c => c.Species == Spec.Spider.Id && Awake(c)))
            {
                Holds(v, vessel, hull, c, aloft);
                int side = c.Recall(WalkerBody.Wall);
                if (side is 1 or -1 && holding.GetValueOrDefault(c.Id) != side) climbs++;
                holding[c.Id] = side;
            }
        }
        return climbs;
    }

    /// <summary>
    /// Two worlds fed the same commands aboard a freighter, alike every tick while a spider wakes, pries a door, is struck and hunts its
    /// hero up the well; as it climbs a second hero comes aboard, and a peer joins: the one world's save loaded, played on beside the other.
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
        Both(new Find("freighter", Wrecked: false));
        var contact = a.Places.Contacts[^1];
        Both(new Jump(contact.Id));
        int surface = first.SurfaceOf(0);
        var v = Registry.On(first, surface);
        var vessel = Shipyard.Build(Content.Vessels.Class("freighter")!, contact.Seed, contact.Wreck).Vessel;
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
        var hunting = v.Creatures.Creatures.Single(c => c.Id == id);
        for (int i = 0; i < 600 && hunting.Posture != Posture.Climbing; i++) Both();
        Both(new Enter(a.Crew.Spawn) { From = 1 });
        Both(new Transfer(surface) { From = 1 });
        // Saved as it climbs, its hold on the well kept in what is saved.
        Assert.Equal(Posture.Climbing, hunting.Posture);
        var joined = World.Load(first.Save(), new Registry().All);
        Assert.Equal(Posture.Climbing, Registry.On(joined, surface).Creatures.Creatures.Single(c => c.Id == id).Posture);
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
