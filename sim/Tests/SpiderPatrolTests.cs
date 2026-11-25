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

    /// <summary>
    /// Hunting, it never turns back on its way by decks to its hero's deck (sections laying their decks at other rows: up a well past it,
    /// down a hatch past it, for ever): having come down toward his deck, it stands rather than climb back up toward the same one.
    /// </summary>
    [Fact]
    public void HuntingItNeverTurnsBackOnItsWayByDecksToTheSameDeck()
    {
        var (a, spider) = Hunting();
        var room = a.Room(spider);
        var hatch = a.Vessel.Hatches.Select(h => (X: M + h.X, Y: M + h.Y)).Single(h => h.Y < room.Floor && h.Y > room.Top - Content.Vessels.Pitch - 1);
        Assert.True(a.RunTill(() => spider.Act == null && a.Grounded(spider), 200));
        int y = SpiderAboardTests.Body(hatch.Y), its = a.V.Creatures.Level(y), mine = a.V.Creatures.Level((spider.Y - 1) / U);
        Assert.True(its < mine, $"his deck {its}, its {mine}");
        // As if it had come down toward his deck past it: up toward the same deck is back the way it came.
        spider.Keep(ProwlDrive.RoamDeck, its + 1);
        int x = spider.X;
        a.Hero(x / U, y);
        for (int t = 0; t < 300; t++)
        {
            a.Run();
            Assert.True(Math.Abs(spider.X - x) < 2 * U && a.V.Creatures.Level((spider.Y - 1) / U) == mine,
                $"on its way back up at {spider.X},{spider.Y} {spider.Posture} at tick {t}, from {x}");
            Assert.True(Hunts(spider));
        }
    }

    /// <summary>Its patrol goal set to cell (<paramref name="x"/>, <paramref name="y"/>), a room's, drawn on the creatures' clock now.</summary>
    private static int Goal(SpiderAboardTests.Aboard a, Creature spider, int x, int y)
    {
        int till = a.V.Creatures.Clock + Prowl.Roam;
        foreach (var (key, value) in new[] { (ProwlDrive.RoamX, x + 1), (ProwlDrive.RoamY, y), (ProwlDrive.RoamTill, till), (ProwlDrive.RoamWall, 0),
                     (ProwlDrive.RoamSpot, 0), (ProwlDrive.ClingTill, 0), (ProwlDrive.RoamDeck, 0) })
            spider.Keep(key, value);
        return till;
    }

    /// <summary>The intact freighter's lowest spider woken, patrolling with no hero aboard; the hatch over its room and the room over that.</summary>
    private static (SpiderAboardTests.Aboard A, Creature Spider, (int X, int Y) Hatch, Vessel.Compartment Up) Patrolling()
    {
        var (a, spider) = Hunting();
        var room = a.Room(spider);
        var hatch = a.Vessel.Hatches.Select(h => (X: M + h.X, Y: M + h.Y)).Single(h => h.Y < room.Floor && h.Y > room.Top - Content.Vessels.Pitch - 1);
        var up = a.Vessel.Compartments.Single(r => r.Holds(hatch.X - M, hatch.Y - 1 - M));
        a.Out().Run();
        Assert.True(a.RunTill(() => spider.Act == null && a.Grounded(spider) && CreaturesModule.Holds(spider) == 0, 300));
        Assert.False(Hunts(spider));
        return (a, spider, hatch, up with { Left = M + up.Left, Top = M + up.Top });
    }

    /// <summary>
    /// With wall goals off its draws (<c>UpAWall</c> 0), about <c>OtherDeck</c> in a hundred of its goals lie on another deck; one in the
    /// room up its well, it climbs the well and stands on that deck.
    /// </summary>
    [Fact]
    public void WithWallGoalsOffAPatrolGoesToAnotherDeckUpItsWell()
    {
        var (a, spider, hatch, up) = Patrolling();
        var creatures = a.V.Creatures;
        int mine = creatures.Level((spider.Y - 1) / U), other = 0, draws = 400;
        for (int i = 0; i < draws; i++)
        {
            var to = creatures.RoamTo(spider, Body, Prowl with { UpAWall = 0 })!.Value;
            Assert.Equal(0, to.Wall);
            if (creatures.Level(to.Y) != mine) other++;
        }
        Assert.True(other > 0, "no goal on another deck");
        Assert.InRange(other, draws * Prowl.OtherDeck / 200, draws * Prowl.OtherDeck * 2 / 100);
        int floor = up.Floor - 1, its = creatures.Level(floor);
        Goal(a, spider, up.Left + up.Width / 2, floor);
        bool well = false;
        Assert.True(a.RunTill(() =>
        {
            well |= CreaturesModule.Holds(spider) == WalkerBody.Well;
            return creatures.Level((spider.Y - 1) / U) == its && a.Grounded(spider) && CreaturesModule.Holds(spider) == 0;
        }, Prowl.Roam), $"never up a deck: at {spider.X},{spider.Y} {spider.Posture}");
        Assert.True(well, "up a deck by no well");
    }

    /// <summary>
    /// A goal up its well with a floor laid across the well (a wreck's sunk deck) it cannot reach: up the well, let go, again; a goal
    /// drawn anew <c>Roam</c> ticks on, by the next.
    /// </summary>
    [Fact]
    public void AGoalItCannotReachIsRedrawnRoamTicksOn()
    {
        var (a, spider, hatch, up) = Patrolling();
        int plate = hatch.Y + 2;
        foreach (int x in new[] { hatch.X, hatch.X + 1 })
        {
            a.V.Grid.Clear(a.Cell(x, plate), "structure");
            a.V.Grid.Place(a.Cell(x, plate), Grid.Placement.Wall(Content.Vessels.Hull));
        }
        int till = Goal(a, spider, up.Left + up.Width / 2, up.Floor - 1);
        Assert.True(a.RunTill(() => spider.Recall(ProwlDrive.RoamTill) != till, Prowl.Roam + 1), $"its goal kept past {till} at {spider.X},{spider.Y}");
    }

    /// <summary>
    /// A patrol goal across a hole down through its floor and the hull under it: it stops at the brink and draws another, never stepping
    /// over (<c>Leaves</c>; a pounce's own check, <c>Floored</c> in <c>Walker.Fly</c>, holds only an act).
    /// </summary>
    [Fact]
    public void APatrolGoalAcrossAHoleIsDroppedAtTheBrink()
    {
        var (a, spider, _, _) = Patrolling();
        var room = a.Room(spider);
        var hull = GuardsAboardTests.HullOf(a.Built);
        int x = spider.X / U, far = x - room.Left > room.Left + room.Width - 1 - x ? room.Left + 1 : room.Left + room.Width - 2;
        int hole = (x + far) / 2 - 1;
        Assert.True(Math.Abs(far - x) >= 7, $"the room's ends only {Math.Abs(far - x)} cells off");
        for (int y = room.Floor; y <= hull.Bottom; y++)
            for (int c = hole; c < hole + 3; c++) a.V.Grid.Clear(a.Cell(c, y), "structure");
        int till = Goal(a, spider, far, room.Floor - 1);
        Assert.True(a.RunTill(() =>
        {
            Assert.True(InHull(hull, spider), $"out of the hull at {spider.X},{spider.Y} {spider.Posture}");
            return spider.Recall(ProwlDrive.RoamTill) != till;
        }, 300), $"its goal kept at {spider.X},{spider.Y}");
        Assert.True(Math.Abs(spider.X / U - (hole + 1)) <= 3 && spider.Y == room.Floor * U, $"dropped at {spider.X},{spider.Y}, the hole at {hole}, the floor at row {room.Floor}");
        for (int t = 0; t < 200; t++)
        {
            a.Run();
            Assert.True(InHull(hull, spider), $"out of the hull at {spider.X},{spider.Y} {spider.Posture}");
        }
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
        // What it keeps at 0 it keeps not at all: no save holds a number that reads as nothing.
        Assert.DoesNotContain(spider.Mem ?? [], n => n.Value == 0);
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

    /// <summary>A spider's tally over a patrol: the floor rows it stood on, its ticks in a hatch's well, its longest still cling up a side wall.</summary>
    private sealed class Tally
    {
        public readonly HashSet<int> Floors = [];
        public int Well, Clung, Still;
    }

    /// <summary>
    /// A tick of <paramref name="c"/>'s patrol tallied in <paramref name="t"/>: standing, its floor; in a well; clinging to the side wall it
    /// patrols up, flush on it, its head under its room's ceiling, in the hull box.
    /// </summary>
    private static void Count(Registry v, Vessel vessel, Creature c, Tally t)
    {
        int hold = CreaturesModule.Holds(c);
        // Standing: on what is solid under either column of its body.
        bool under = v.Grid.Solid(c.Y / U * v.Grid.Width + (c.X - U) / U) || v.Grid.Solid(c.Y / U * v.Grid.Width + c.X / U);
        if (hold == 0 && c.VY == 0 && c.Y % U == 0 && under) t.Floors.Add(c.Y / U);
        if (hold == WalkerBody.Well) t.Well++;
        if (hold is not (1 or -1) || c.Recall(ProwlDrive.RoamWall) != hold || c.Recall(ProwlDrive.ClingTill) == 0)
        {
            t.Still = 0;
            return;
        }
        Assert.True(InHull(v.Creatures.Hull, c), $"clinging out of the hull box at {c.X},{c.Y}");
        int half = Body.Width * U / 2, beside = hold > 0 ? (c.X + half) / U : (c.X - half) / U - 1, top = (c.Y - Body.Height * U) / U;
        Assert.Equal(0, (c.X + hold * half) % U);
        Assert.True(v.Grid.Solid((c.Y - 1) / U * v.Grid.Width + beside), $"clinging at {c.X},{c.Y} to no wall to its {hold}");
        var rooms = vessel.Compartments.Where(r => r.Holds(c.X / U - M, (c.Y - 1) / U - M)).ToList();
        Assert.True(rooms.Count == 1, $"clinging at {c.X},{c.Y} in {rooms.Count} rooms: {string.Join(" ", rooms.Select(r => $"[{r.Left}+{r.Width} {r.Top}-{r.Floor} of {r.Of}]"))}");
        var room = rooms[0];
        Assert.True(top >= M + room.Top, $"clinging at {c.X},{c.Y} above its room's ceiling {M + room.Top}");
        t.Still = c.VY == 0 ? t.Still + 1 : 0;
        t.Clung = Math.Max(t.Clung, t.Still);
    }

    private static readonly int ClingLeast = Prowl.Cling.Min;

    /// <summary>
    /// A long patrol aboard a wreck, its hero passed out: every spider goes from deck to deck, up its wells and down its hatches, and one
    /// climbs a room's side wall, clinging there still at least its least cling, never over its room's ceiling nor out of the hull box.
    /// </summary>
    [Theory]
    [InlineData("freighter")]
    [InlineData("liner")]
    public void OnALongPatrolItGoesDeckToDeckAndClingsUpASideWall(string cls)
    {
        var (w, v, built) = WreckGuardsTests.Boarded(cls, Spec.Spider.Id);
        var spiders = v.Creatures.Creatures.Where(c => c.Species == Spec.Spider.Id).ToList();
        var tallies = spiders.ToDictionary(s => s.Id, _ => new Tally());
        for (int t = 0; t < 6000; t++)
        {
            v.Crew.HeroOf(0)!.Out = 1000;
            w.Run();
            foreach (var s in spiders)
            {
                Assert.False(Hunts(s));
                Count(v, built.Vessel, s, tallies[s.Id]);
            }
        }
        Assert.All(spiders, s => Assert.True(tallies[s.Id].Floors.Count >= 2, $"spider {s.Id} stood on floors {string.Join(',', tallies[s.Id].Floors)}"));
        Assert.Contains(spiders, s => tallies[s.Id].Well > 0);
        Assert.Contains(spiders, s => tallies[s.Id].Clung >= ClingLeast);
        Assert.All(spiders, s => Assert.True(tallies[s.Id].Clung <= Prowl.Cling.Max, $"spider {s.Id} clung still {tallies[s.Id].Clung} ticks"));
    }

    /// <summary>
    /// A long patrol aboard a wrecked freighter, its hero passed out: a goal in a room on a deck no well rises to from its floor (the bridge
    /// over its hall) is dropped at once, not stood under till <c>Roam</c> runs out. No spider stands still longer than a cling or a pry.
    /// </summary>
    [Fact]
    public void APatrolGoalOnADeckWithNoWayUpIsDroppedAtOnce()
    {
        var (w, v, _) = Boarded("freighter", wrecked: true, 1);
        var still = new Dictionary<int, (int X, int Y, int Ticks)>();
        int most = Math.Max(Prowl.Cling.Max, Prowl.Pry) + 20;
        for (int t = 0; t < 3000; t++)
        {
            v.Crew.HeroOf(0)!.Out = 1000;
            w.Run();
            foreach (var s in v.Creatures.Creatures.Where(c => c.Species == Spec.Spider.Id))
            {
                var was = still.GetValueOrDefault(s.Id);
                still[s.Id] = was.X == s.X && was.Y == s.Y ? (s.X, s.Y, was.Ticks + 1) : (s.X, s.Y, 0);
                Assert.True(still[s.Id].Ticks <= most, $"spider {s.Id} still at {s.X},{s.Y} at tick {t}, its goal {s.Recall(ProwlDrive.RoamX) - 1},{s.Recall(ProwlDrive.RoamY)}");
            }
        }
    }

    /// <summary>
    /// A long patrol aboard a wrecked tanker, its hero passed out: no spider hangs still in a hatch's well longer than a pry, as one did
    /// climbed through a hatch and on up the well of the next hatch over it, a floor between them.
    /// </summary>
    [Fact]
    public void NoSpiderHangsInAWellUnderAFloor()
    {
        var (w, v, _) = Boarded("tanker", wrecked: true, 0);
        var still = new Dictionary<int, (int Y, int Ticks)>();
        for (int t = 0; t < 3000; t++)
        {
            v.Crew.HeroOf(0)!.Out = 1000;
            w.Run();
            foreach (var s in v.Creatures.Creatures.Where(c => c.Species == Spec.Spider.Id))
            {
                var was = still.GetValueOrDefault(s.Id);
                still[s.Id] = CreaturesModule.Holds(s) == WalkerBody.Well && was.Y == s.Y ? (s.Y, was.Ticks + 1) : (s.Y, 0);
                Assert.True(still[s.Id].Ticks <= Prowl.Pry + 20, $"spider {s.Id} still in a well at {s.X},{s.Y} at tick {t}");
            }
        }
    }

    /// <summary>
    /// Aboard wrecks and intact vessels of every class: the well a spider takes up from a floor (<see cref="CreaturesModule.WayUp"/>) is
    /// open from that floor to its hatch, never a floor across it.
    /// </summary>
    [Fact]
    public void AWayUpIsAWellOpenFromItsFloorToItsHatch()
    {
        foreach (var cls in Content.Vessels.Classes.Select(c => c.Id))
            foreach (bool wrecked in new[] { true, false })
                for (uint index = 0; index < 6; index++)
                {
                    var (_, v, built) = Boarded(cls, wrecked, index);
                    var vessel = built.Vessel;
                    var floors = vessel.Compartments.Select(r => M + r.Floor).Concat(vessel.Decks.Select(d => M + d.Floor)).Distinct().ToList();
                    foreach (int floor in floors)
                        foreach (var h in vessel.Hatches)
                        {
                            if (v.Creatures.WayUp(floor, (M + h.X + 1) * U) is not { } up) continue;
                            int x = up.X / U - 1;
                            for (int row = up.Row + 1; row < floor; row++)
                                Assert.False(v.Grid.Solid(row * v.Grid.Width + x) || v.Grid.Solid(row * v.Grid.Width + x + 1),
                                    $"{cls} {(wrecked ? "wreck" : "intact")} {index}: the well from {floor} up to the hatch at {x},{up.Row} shut at row {row}");
                        }
                }
    }

    /// <summary>
    /// Two worlds aboard a wrecked liner, its hero passed out in both, alike every tick of a long patrol up walls and wells and down hatches.
    /// </summary>
    [Fact]
    public void TwoWorldsStayAlikeThroughAPatrolUpWallsAndDecks()
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
        for (int i = 0; i < 20; i++)
        {
            Both(new Find("liner", Wrecked: true));
            var contact = a.Places.Contacts[^1];
            var built = Shipyard.Build(Content.Vessels.Class("liner")!, contact.Seed, contact.Wreck);
            if (built.Wreck == null || !Shipyard.Guards(built).Any(d => d.Species == Spec.Spider.Id)) continue;
            Both(new Jump(contact.Id));
            int surface = first.SurfaceOf(0);
            var (one, two) = (Registry.On(first, surface), Registry.On(second, surface));
            var tallies = new Dictionary<int, Tally>();
            for (int t = 0; t < 3000; t++)
            {
                one.Crew.HeroOf(0)!.Out = two.Crew.HeroOf(0)!.Out = 1000;
                Both();
                foreach (var s in one.Creatures.Creatures.Where(c => c.Species == Spec.Spider.Id))
                    Count(one, built.Vessel, s, tallies.TryGetValue(s.Id, out var tally) ? tally : tallies[s.Id] = new Tally());
            }
            Assert.Contains(tallies.Values, t => t.Well > 0);
            Assert.Contains(tallies.Values, t => t.Clung >= ClingLeast);
            Assert.All(tallies.Values, t => Assert.True(t.Clung <= Prowl.Cling.Max, $"clung still {t.Clung} ticks"));
            return;
        }
        Assert.Fail("no liner wreck with a spider");
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
