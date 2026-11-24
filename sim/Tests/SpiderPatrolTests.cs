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
/// A vessel's spider with no hero near (derelict-guards): it patrols the vessel at random, room to room, and hunts again a hero come
/// back near; whatever it does it never leaves the hull box, out an open way in or a wreck's hole.
/// </summary>
public class SpiderPatrolTests
{
    private const int U = CreaturesModule.Unit, M = Content.Vessels.Margin;

    /// <summary>A vessel of <paramref name="cls"/>, a wreck or intact, the <paramref name="index"/>th found, jumped to by the new game's hero.</summary>
    private static (Scenario W, Registry V, Built Built) Boarded(string cls, bool wrecked, uint index)
    {
        var w = new Scenario(Registry.Wreck);
        w.Do(new Enter(w.M.Crew.Spawn)).Run();
        for (uint i = 0; i <= index; i++) w.Do(new Find(cls, Wrecked: wrecked)).Run();
        var contact = w.M.Places.Contacts[^1];
        w.Do(new Jump(contact.Id)).Run();
        Assert.Empty(w.Refusals);
        return (w, w.On(w.World.SurfaceOf(0)), Shipyard.Build(Content.Vessels.Class(cls)!, contact.Seed, contact.Wreck));
    }

    private static bool Awake(Creature c) => c.Posture is not (Posture.Docked or Posture.Waking);

    private static bool InHull(Box hull, Creature c)
    {
        int half = Spec.Spider.Body.Width * U / 2;
        return hull.Contains((c.X - half) / U, (c.Y - Spec.Spider.Body.Height * U) / U) && hull.Contains((c.X + half - 1) / U, (c.Y - 1) / U);
    }

    private static Arbitrary<(string Class, bool Wrecked, uint Index, uint Seed)> Plays() =>
        Gen.Zip(Gen.Zip(Gen.Elements("freighter", "liner"), ArbMap.Default.GeneratorFor<bool>()), Gen.Zip(Gen.Choose(0, 3), ArbMap.Default.GeneratorFor<uint>()))
            .Select(t => (t.Item1.Item1, t.Item1.Item2, (uint)t.Item2.Item1, t.Item2.Item2)).ToArbitrary();

    /// <summary>
    /// A play aboard with the way in open: every 20 ticks its hero somewhere at random, in a room, anywhere about the hull box (in space
    /// too) or out past the way in; now and then a blow on a spider. Every awake spider's body, every tick, in the hull box.
    /// </summary>
    [Property(MaxTest = 40)]
    public Property ASpiderNeverLeavesTheHullBoxHuntingOrPatrolling() => Prop.ForAll(Plays(), play =>
    {
        var (w, v, built) = Boarded(play.Class, play.Wrecked, play.Index);
        var hull = GuardsAboardTests.HullOf(built);
        var rng = new Rng(new RngState { S = play.Seed | 1 });
        var rooms = built.Vessel.Compartments;
        var wayIn = Shipyard.WayIn(built);
        int width = v.Grid.Width, surface = w.World.SurfaceOf(0);
        w.Do(new SetDoor((M + wayIn.Y) * width + M + wayIn.X, true));
        string at = $"{play.Class} {(play.Wrecked ? "wreck" : "intact")} {play.Index} {play.Seed}";
        var track = new List<string>();
        for (int t = 0; t < 1500; t++)
        {
            if (t % 20 == 0)
            {
                int cell = rng.Below(3) switch
                {
                    0 => rooms[(int)rng.Below((uint)rooms.Count)] is var r ? (M + r.Floor - 3) * width + M + r.Left + (int)rng.Below((uint)r.Width) : 0,
                    1 => (hull.Top - 6 + (int)rng.Below((uint)(hull.Bottom - hull.Top + 13))) * width + hull.Left - 6 + (int)rng.Below((uint)(hull.Right - hull.Left + 13)),
                    _ => (M + wayIn.Y + Content.Vessels.Open - 3) * width + M + wayIn.X - 1 - (int)rng.Below(4),
                };
                w.Do(new Enter(cell));
                var all = v.Creatures.Creatures;
                if (rng.Below(4) == 0 && all.Count > 0) w.Do(new Strike(all[(int)rng.Below((uint)all.Count)].Id, Content.Weapons.Axe.Item));
            }
            w.Run();
            if (w.World.SurfaceOf(0) != surface) break;
            foreach (var c in v.Creatures.Creatures.Where(c => c.Species == Spec.Spider.Id && Awake(c)))
            {
                track.Add($"{t} #{c.Id} {c.X},{c.Y} {c.Posture} v {c.VX},{c.VY} act {c.Act?.Move} mem {string.Join(',', (c.Mem ?? []).Select(n => n.Key + '=' + n.Value))}");
                Assert.True(InHull(hull, c), $"spider {c.Id} at {c.X},{c.Y} out of the hull box {hull} at tick {t} on {at}: {c.Posture} v {c.VX},{c.VY}\n{string.Join('\n', track.TakeLast(60))}");
            }
        }
        return true.ToProperty();
    });

    private static readonly Spec.Prowl Prowl = Spec.Spider.Brain.OfType<Spec.Prowl>().Single();

    private static readonly int Wake = Spec.Spider.Brain.OfType<Spec.Dock>().Single().Wake;

    private static bool Hunts(Creature c) => c.Recall(ProwlDrive.HuntKey) == 1;

    /// <summary>The squared distance in cells from <paramref name="c"/>'s cell to the cell (<paramref name="x"/>, <paramref name="y"/>).</summary>
    private static long Far(Creature c, int x, int y) => (long)(x - c.X / U) * (x - c.X / U) + (long)(y - (c.Y - 1) / U) * (y - (c.Y - 1) / U);

    /// <summary>The place in <paramref name="v"/>'s rooms of the one whose open cells hold <paramref name="c"/>'s feet' row; -1 for none (a doorway).</summary>
    private static int RoomOf(Vessel v, Creature c) =>
        v.Compartments.Select((r, i) => (r, i)).Where(t => t.r.Holds(c.X / U - M, (c.Y - 1) / U - M)).Select(t => t.i).DefaultIfEmpty(-1).First();

    /// <summary>The intact freighter's lowest spider woken by its hero walking into its room: hunting him.</summary>
    private static (SpiderAboardTests.Aboard A, Creature Spider) Hunting()
    {
        var a = new SpiderAboardTests.Aboard();
        var spider = a.Spiders.MaxBy(s => s.Y)!;
        var room = a.Room(spider);
        a.Hero(room.Left + 1, SpiderAboardTests.Body(room.Floor)).Run(Wake + 1);
        Assert.True(SpiderAboardTests.Awake(spider) && Hunts(spider), $"{spider.Posture}");
        return (a, spider);
    }

    /// <summary>A cell a hero stands in the room whose middle is farthest from <paramref name="spider"/>: far past its <c>Lose</c>.</summary>
    private static (int X, int Y) FarRoom(SpiderAboardTests.Aboard a, Creature spider)
    {
        var r = a.Vessel.Compartments.MaxBy(r => Math.Abs(M + r.Left + r.Width / 2 - spider.X / U))!;
        var at = (X: M + r.Left + r.Width / 2, Y: M + SpiderAboardTests.Body(r.Floor));
        Assert.True(Far(spider, at.X, at.Y) > 2L * Prowl.Lose * Prowl.Lose, $"the farthest room only {Math.Sqrt(Far(spider, at.X, at.Y))} cells off");
        return at;
    }

    [Fact]
    public void AHeroGotAwayItPatrolsRoomToRoomAtRandomNotAfterHim()
    {
        var (a, spider) = Hunting();
        var hero = FarRoom(a, spider);
        a.Hero(hero.X, hero.Y).Run();
        Assert.False(Hunts(spider));
        var rooms = new HashSet<int>();
        var drawn = new List<int>();
        for (int t = 0; t < 1500; t++)
        {
            a.Run();
            long far = Far(spider, hero.X, hero.Y);
            // Hunting again only where the rules say: come within its chase, kept while within its lose.
            Assert.True(!Hunts(spider) || far <= (long)Prowl.Lose * Prowl.Lose, $"hunting {Math.Sqrt(far)} cells off at tick {t}");
            if (Hunts(spider)) return;
            if (RoomOf(a.Vessel, spider) is int room and >= 0) rooms.Add(room);
            int to = spider.Recall(ProwlDrive.RoamX);
            if (to > 0 && (drawn.Count == 0 || drawn[^1] != to)) drawn.Add(to);
            if (rooms.Count >= 3 && drawn.Count >= 3) break;
        }
        Assert.True(rooms.Count >= 3, $"it patrolled {rooms.Count} rooms");
        Assert.True(drawn.Count >= 3, $"it drew {drawn.Count} cells");
    }

    private static readonly Spec.Walker Body = (Spec.Walker)Spec.Spider.Body;

    /// <summary>
    /// Patrolling it walks: never more than its <c>Walk</c> across a tick, nor up a wall more than its climb cut to the same share; it
    /// does walk that fast. Hunting it runs, past its walk, up to its <c>Run</c>.
    /// </summary>
    [Fact]
    public void ItPatrolsAtItsWalkAndHuntsAtItsRun()
    {
        Assert.True(Body.Walk < Body.Run);
        int up = Body.Climb * Body.Walk / Body.Run;
        var (a, spider) = Hunting();
        int fastest = 0;
        for (int t = 0; t < 200 && Hunts(spider); t++)
        {
            int x = spider.X;
            a.Run();
            if (spider.Act == null && Hunts(spider)) fastest = Math.Max(fastest, Math.Abs(spider.X - x));
        }
        Assert.True(fastest > Body.Walk && fastest <= Body.Run, $"hunting at most {fastest} a tick");
        var hero = FarRoom(a, spider);
        a.Hero(hero.X, hero.Y).Run();
        Assert.False(Hunts(spider));
        int walked = 0;
        for (int t = 0; t < 1500 && !Hunts(spider); t++)
        {
            int x = spider.X;
            bool free = spider.Act == null;
            a.Run();
            if (!free || spider.Act != null || Hunts(spider)) continue;
            Assert.True(Math.Abs(spider.X - x) <= Body.Walk && Math.Abs(spider.VX) <= Body.Walk, $"{spider.X - x} across, v {spider.VX} at tick {t}");
            if (CreaturesModule.Holds(spider) != 0) Assert.True(-spider.VY <= up, $"climbing {-spider.VY} at tick {t}");
            walked = Math.Max(walked, Math.Abs(spider.X - x));
        }
        Assert.Equal(Body.Walk, walked);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PatrollingItHuntsAgainAHeroComeWithinItsChase(bool wrecked)
    {
        Scenario w;
        Registry v;
        Creature spider;
        if (wrecked)
        {
            (w, v, _) = WreckGuardsTests.Boarded("freighter", Spec.Spider.Id);
            spider = v.Creatures.Creatures.First(c => c.Species == Spec.Spider.Id);
            v.Crew.HeroOf(0)!.Out = 1000;
        }
        else
        {
            var (a, s) = Hunting();
            (w, v, spider) = (a.W, a.V, s);
            var door = Shipyard.WayIn(a.Built);
            a.Hero(M + door.X - 4, M + door.Y + Content.Vessels.Open - 1);
        }
        w.Run(200);
        Assert.False(Hunts(spider));
        Assert.True(spider.Recall(ProwlDrive.RoamX) > 0);
        v.Crew.HeroOf(0)!.Out = 0;
        // A hero ten cells off on its row, where it may stand: it hunts him, and comes to him.
        for (int t = 0; t < 300 && !(spider.Act == null && Grounded(v, spider)); t++) w.Run();
        int row = (spider.Y - 1) / U, x = spider.X / U;
        int to = v.Creatures.Hull.Right - x > x - v.Creatures.Hull.Left ? x + 10 : x - 10;
        w.Do(new Enter(row * v.Grid.Width + to)).Run();
        Assert.True(Hunts(spider), $"{Math.Sqrt(Far(spider, to, row))} cells off");
        Assert.True(Enumerable.Range(0, 200).Any(_ =>
        {
            w.Run();
            return Math.Abs(spider.X / U - to) <= 3;
        }), $"never at him: at {spider.X / U}, he at {to}");
    }

    private static bool Grounded(Registry v, Creature c) => v.Grid.Solid((c.Y / U) * v.Grid.Width + c.X / U) || v.Grid.Solid((c.Y / U) * v.Grid.Width + (c.X - 1) / U);

    /// <summary>Lost past its lose, found within its chase: between the two it keeps what it does, hunting or patrolling.</summary>
    [Fact]
    public void ItLosesAHeroPastItsLoseAndFindsOneWithinItsChaseKeepingWhatItDoesBetween()
    {
        var (a, spider) = Hunting();
        // A hero on its row at the distance, toward the hull's wider side; its choice made the next tick it is not mid-move.
        bool At(int far)
        {
            Assert.True(a.RunTill(() => spider.Act == null, 200));
            int row = (spider.Y - 1) / U, x = spider.X / U;
            var hull = a.V.Creatures.Hull;
            a.Hero(hull.Right - x > x - hull.Left ? x + far : x - far, row).Run();
            return Hunts(spider);
        }
        Assert.True(At(Prowl.Lose));
        Assert.False(At(Prowl.Lose + 1));
        Assert.False(At(Prowl.Chase + 1));
        Assert.True(At(Prowl.Chase));
    }

    [Fact]
    public void WithNoHeroAboardItPatrols()
    {
        var (a, spider) = Hunting();
        var door = Shipyard.WayIn(a.Built);
        a.Hero(M + door.X - 4, M + door.Y + Content.Vessels.Open - 1).Run();
        Assert.Null(a.V.Creatures.Quarry(spider));
        var rooms = new HashSet<int>();
        for (int t = 0; t < 1500 && rooms.Count < 3; t++)
        {
            a.Run();
            Assert.False(Hunts(spider));
            if (RoomOf(a.Vessel, spider) is int room and >= 0) rooms.Add(room);
        }
        Assert.True(rooms.Count >= 3, $"it patrolled {rooms.Count} rooms");
    }

    /// <summary>
    /// A hole down through its deck's floor and the hull under it, between the spider and its hero across the room: it hunts him to the
    /// brink and stands there, never walking, leaping nor falling out of the hull box.
    /// </summary>
    [Fact]
    public void AtAHoleIntoSpaceInItsWayItStandsAtTheBrinkNeverFallingOut()
    {
        var a = new SpiderAboardTests.Aboard();
        var spider = a.Spiders.MaxBy(s => s.Y)!;
        var room = a.Room(spider);
        var hull = GuardsAboardTests.HullOf(a.Built);
        int x = spider.X / U, hero = x - room.Left > room.Left + room.Width - 1 - x ? room.Left + 1 : room.Left + room.Width - 2;
        int hole = (x + hero) / 2 - 1;
        Assert.True(Math.Abs(hero - x) >= 7, $"the room's ends only {Math.Abs(hero - x)} cells off");
        for (int y = room.Floor; y <= hull.Bottom; y++)
            for (int c = hole; c < hole + 3; c++) a.V.Grid.Clear(a.Cell(c, y), "structure");
        Assert.All(Enumerable.Range(room.Floor, hull.Bottom - room.Floor + 1), y => Assert.False(a.V.Grid.Solid(a.Cell(hole + 1, y))));
        a.Hero(hero, SpiderAboardTests.Body(room.Floor));
        for (int t = 0; t < 600; t++)
        {
            a.Run();
            Assert.True(InHull(hull, spider), $"out of the hull at {spider.X},{spider.Y} {spider.Posture} at tick {t}");
        }
        Assert.True(SpiderAboardTests.Awake(spider) && Hunts(spider));
        Assert.True(Math.Abs(spider.X / U - (hole + 1)) <= 3, $"at {spider.X / U}, the hole at {hole}");
    }

    /// <summary>
    /// Two worlds fed the same commands aboard a freighter, alike every tick while a spider wakes, loses its hero and patrols; mid-patrol a
    /// peer joins: the one world's save loaded, played on beside the other.
    /// </summary>
    [Fact]
    public void TwoWorldsStayAlikeThroughAPatrolAndAJoinMidPatrol()
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
        Both(new Enter(a.Crew.Spawn));
        Both(new Find("freighter", Wrecked: false));
        var contact = a.Places.Contacts[^1];
        Both(new Jump(contact.Id));
        int surface = first.SurfaceOf(0);
        var v = Registry.On(first, surface);
        var vessel = Shipyard.Build(Content.Vessels.Class("freighter")!, contact.Seed, contact.Wreck).Vessel;
        int width = v.Grid.Width;
        var spider = v.Creatures.Creatures.Where(c => c.Species == Spec.Spider.Id).MaxBy(c => c.Y)!;
        var room = vessel.Compartments[v.Creatures.Docks[CreaturesModule.DockOf(spider)!.Value].Room];
        int id = spider.Id;
        Both(new Enter((M + SpiderAboardTests.Body(room.Floor)) * width + M + room.Left + 1));
        for (int i = 0; i < Wake + 5; i++) Both();
        var far = vessel.Compartments.MaxBy(r => Math.Abs(r.Left + r.Width / 2 - room.Left))!;
        Both(new Enter((M + SpiderAboardTests.Body(far.Floor)) * width + M + far.Left + far.Width / 2));
        // On till it has patrolled two rooms and walks on to a cell it drew: mid-patrol.
        var rooms = new HashSet<int>();
        var patrolling = spider;
        for (int i = 0; i < 1500 && !(rooms.Count >= 2 && !Hunts(patrolling) && patrolling.Recall(ProwlDrive.RoamX) > 0 && patrolling.VX != 0); i++)
        {
            Both();
            patrolling = v.Creatures.Creatures.Single(c => c.Id == id);
            if (RoomOf(vessel, patrolling) is int r and >= 0) rooms.Add(r);
        }
        Assert.True(!Hunts(patrolling) && patrolling.Recall(ProwlDrive.RoamX) > 0 && patrolling.VX != 0, $"not patrolling: {patrolling.Posture}");
        Assert.True(rooms.Count >= 2, $"it patrolled {rooms.Count} rooms");
        Both(new Enter(a.Crew.Spawn) { From = 1 });
        Both(new Transfer(surface) { From = 1 });
        var joined = World.Load(first.Save(), new Registry().All);
        var loaded = Registry.On(joined, surface).Creatures.Creatures.Single(c => c.Id == id);
        Assert.Equal(patrolling.Recall(ProwlDrive.RoamX), loaded.Recall(ProwlDrive.RoamX));
        Assert.Equal(first.Hash(), joined.Hash());
        for (int i = 0; i < 400; i++)
        {
            first.Step([]);
            joined.Step([]);
            Assert.Equal(first.Hash(), joined.Hash());
        }
    }
}
