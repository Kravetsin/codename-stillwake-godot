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
/// The escort aboard a liner (derelict-guards, G-3): woken in its stand by a hero walking into its hall or by a blow, let down to the floor,
/// then hunting the crew on its deck: walking always to close in, a lunge at a hero near on its deck, a gauss on the way at one in line within
/// its notice, up too, at most every so often and never while it lunges; one off its deck in sight, under him; out of sight, off through a
/// door; prying doors, never a hatch nor
/// the way in; never off its deck's rows.
/// </summary>
public class EscortAboardTests
{
    private const int U = CreaturesModule.Unit, M = Content.Vessels.Margin;

    private static readonly Spec.Patrol Patrol = Spec.Escort.Brain.OfType<Spec.Patrol>().Single();

    private static readonly int Wake = Spec.Escort.Brain.OfType<Spec.Dock>().Single().Wake;

    private static readonly Spec.Shoot Gauss = Spec.Escort.Moves.OfType<Spec.Shoot>().Single();

    /// <summary>
    /// The first liner boarded, its escort in the hold on the bottom deck whose hall is 30 columns, a door either side 16 columns off its
    /// middle and the well's ladder at its left end (liner 0's room 41).
    /// </summary>
    private sealed class Aboard
    {
        public readonly Scenario W;
        public readonly Registry V;
        public readonly Built Built;

        public Vessel Vessel => Built.Vessel;
        public readonly Creature Escort;
        public readonly Vessel.Compartment Room;

        /// <param name="left">Its hall's left column in the vessel: 34 the hold on the bottom deck, 203 the hall over the hold on the right.</param>
        public Aboard(int left = 34)
        {
            (W, V, Built) = GuardsAboardTests.Boarded("liner");
            Escort = V.Creatures.Creatures.Where(c => c.Species == Spec.Escort.Id).Single(c => Of(c).Left == M + left && Of(c).Decks == 2);
            Room = Of(Escort);
        }

        /// <summary>Dock <paramref name="c"/>'s room, in the world.</summary>
        public Vessel.Compartment Of(Creature c)
        {
            var r = Vessel.Compartments[V.Creatures.Docks[CreaturesModule.DockOf(c)!.Value].Room];
            return r with { Left = M + r.Left, Top = M + r.Top };
        }

        public int Feet => Room.Floor - 1;

        /// <summary>The row a hero standing on its floor reports: its body's middle's, 2.6 cells over its feet.</summary>
        public int Body => Room.Floor - 3;

        public int Column => Escort.X / U;

        public int Cell(int x, int y) => y * V.Grid.Width + x;

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

        public bool RunTill(Func<bool> done, int most)
        {
            for (int t = 0; t < most && !done(); t++) W.Run();
            return done();
        }

        /// <summary>Woken by a blow, its hero out past the way in, and let go: awake on its floor, standing, no hero aboard.</summary>
        public Aboard Woken()
        {
            var door = Shipyard.WayIn(Built);
            Hero(M + door.X - 4, M + door.Y + Content.Vessels.Open - 1);
            W.Do(new Strike(Escort.Id, Content.Weapons.Axe.Item));
            Run(Wake + 2);
            Assert.True(Awake(Escort), $"{Escort.Posture}");
            return this;
        }

        public bool Shut(int x, int y) => Doorways.IsClosed(V.Grid.StructureAt(Cell(x, y)));

        public int Air => V.Crew.HeroOf(0)!.Air;

        /// <summary>The doors of its deck in the world, the way in left out: each its left column and top row.</summary>
        public IEnumerable<(int X, int Y)> Doors => Vessel.Doors.Where(d => d != Shipyard.WayIn(Built) && M + d.Y == Room.Floor - Content.Vessels.Open)
            .Select(d => (M + d.X, M + d.Y));

        /// <summary>Its act's move, by its record's type; null for none.</summary>
        public Type? Act => Escort.Act is { } act ? Spec.Escort.Moves[act.Move].GetType() : null;
    }

    private static bool Awake(Creature c) => c.Posture is not (Posture.Docked or Posture.Waking);

    [Fact]
    public void SeenFromTheDoorwayItHangsOnAndAStepInWakesItAndItIsLetDownOntoItsFloor()
    {
        var a = new Aboard();
        var door = a.Doors.Single(d => d.X == a.Room.Left - 2);
        a.W.Do(new SetDoor(a.Cell(door.X, door.Y), true));
        a.Hero(door.X - 3, a.Body).Run(40).Hero(door.X + 1, a.Body).Run(40);
        Assert.Equal(Posture.Docked, a.Escort.Posture);
        a.Hero(a.Room.Left, a.Body).Run();
        Assert.Equal(Posture.Waking, a.Escort.Posture);
        a.Run(Wake - 2);
        Assert.Equal(Posture.Waking, a.Escort.Posture);
        a.Run();
        Assert.True(Awake(a.Escort), $"{a.Escort.Posture}");
        Assert.Equal(a.Room.Floor * U, a.Escort.Y);
        Assert.True(a.V.Grid.Solid(a.Cell(a.Column, a.Room.Floor)));
        Assert.Equal(1, a.Escort.Recall(DockDrive.Woke));
    }

    [Fact]
    public void ABlowFromAnywhereWakesItAndHurtsIt()
    {
        var a = new Aboard();
        a.W.Do(new Strike(a.Escort.Id, Content.Weapons.Axe.Item));
        a.Run();
        Assert.Empty(a.W.Refusals);
        Assert.Equal((Posture.Waking, Content.Weapons.Axe.Damage), (a.Escort.Posture, a.Escort.Damage));
        a.Run(Wake);
        Assert.True(Awake(a.Escort));
    }

    private static readonly Spec.Lunge Claw = Spec.Escort.Moves.OfType<Spec.Lunge>().Single();

    private static readonly int Pace = ((Spec.Walker)Spec.Escort.Body).Run;

    private static bool Fired(Aboard a) => a.V.Creatures.Ended.Any(e => e.Projectile == Gauss.Projectile) || a.V.Creatures.Shots.Any(s => s.Projectile == Gauss.Projectile);

    [Fact]
    public void AFarHeroInLineOnItsFloorIsShotAsItWalksAtHimAndThenLungedAt()
    {
        var a = new Aboard().Woken();
        // Out of its claw's reach, on its floor, in line: it charges its gun and walks on at him, firing on the way.
        a.Hero(a.Room.Left + 1, a.Body).Run();
        Assert.True(a.Column - (a.Room.Left + 1) > Patrol.Strike + 6, $"at {a.Column}");
        Assert.Equal(typeof(Spec.Shoot), a.Act);
        int air = a.Air, x = a.Escort.X;
        Assert.True(a.RunTill(() => a.V.Creatures.Ended.Any(e => e.Projectile == Gauss.Projectile), Gauss.WindUp + 2));
        var end = a.V.Creatures.Ended.Single(e => e.Projectile == Gauss.Projectile);
        Assert.True(end.Met);
        // It met him about his middle's height, aimed from its gun down to there.
        Assert.InRange(end.Cell / a.V.Grid.Width, a.Body - 1, a.Body + 1);
        Assert.Equal(-1, end.Burst);
        Assert.Equal(air - Content.Shots.Gauss.Damage, a.Air);
        Assert.True(a.Escort.X <= x - (Gauss.WindUp - 2) * Pace, $"it walked {x - a.Escort.X} as it charged");
        Assert.Empty(a.V.Creatures.Shots);
        // On it walks till its claw reaches him.
        Assert.True(a.RunTill(() => a.Act == typeof(Spec.Lunge), 40), $"at {a.Column}");
        Assert.InRange(a.Column - (a.Room.Left + 1), 0, Patrol.Strike);
    }

    [Fact]
    public void ItNeverFiresWhileItLungesAndALungeDropsAShotItWasCharging()
    {
        var a = new Aboard().Woken();
        // Its gun ready, a hero in line within its claw: a lunge, and no shot through all of it.
        a.Hero(a.Column - Patrol.Strike + 1, a.Body).Run();
        Assert.Equal(typeof(Spec.Lunge), a.Act);
        for (int t = 0; t < Claw.WindUp + Claw.Ticks && a.Act == typeof(Spec.Lunge); t++)
        {
            a.Run();
            Assert.False(Fired(a), $"fired at tick {t} of its lunge");
            Assert.NotEqual(typeof(Spec.Shoot), a.Act);
        }

        // Charging at a far hero when he comes within its claw: the shot is dropped for the lunge, never fired.
        var b = new Aboard().Woken();
        b.Hero(b.Room.Left + 1, b.Body).Run(2);
        Assert.Equal(typeof(Spec.Shoot), b.Act);
        b.Hero(b.Column - Patrol.Strike + 1, b.Body).Run();
        Assert.Equal(typeof(Spec.Lunge), b.Act);
        for (int t = 0; t < Gauss.WindUp + Claw.WindUp + Claw.Ticks; t++)
        {
            b.Run();
            Assert.False(Fired(b), $"fired at tick {t} after its lunge began");
        }
    }

    [Fact]
    public void ItsShotsBeginAtLeastItsDelayApartAndItFiresAgainOnceReady()
    {
        var a = new Aboard().Woken();
        Assert.True(Gauss.Every > Gauss.WindUp + Gauss.Recover);
        var doors = a.Doors.Where(d => d.X == a.Room.Left - 2 || d.X == a.Room.Left + a.Room.Width).ToList();
        foreach (var d in doors) a.W.Do(new SetDoor(a.Cell(d.X, d.Y), false));
        // Up the well's ladder, off its deck and in line: shot at from below as it comes under him, never clawed.
        a.Hero(a.Room.Left, a.Room.Top + 3);
        var shots = new List<int>();
        for (int t = 0; t < 3 * Gauss.Every; t++)
        {
            a.Run();
            Assert.NotEqual(typeof(Spec.Lunge), a.Act);
            if (a.V.Creatures.Ended.Any(e => e.Projectile == Gauss.Projectile)) shots.Add(t);
        }
        Assert.True(shots.Count >= 2, $"shots at {string.Join(",", shots)}");
        Assert.All(shots.Zip(shots.Skip(1)), p => Assert.Equal(Gauss.Every, p.Second - p.First));
    }

    [Fact]
    public void AHeroUpTheLadderOutOfItsClawsReachIsShotUpAt()
    {
        var a = new Aboard().Woken();
        // On the well's ladder at the hall's left end, two decks' worth up: right over the claw's columns would be no reach at all.
        a.Hero(a.Room.Left, a.Room.Top + 3).Run();
        Assert.Equal(typeof(Spec.Shoot), a.Act);
        int air = a.Air;
        Assert.True(a.RunTill(() => a.V.Creatures.Ended.Any(e => e.Projectile == Gauss.Projectile), Gauss.WindUp + 2));
        var end = a.V.Creatures.Ended.Single(e => e.Projectile == Gauss.Projectile);
        Assert.True(end.Cell / a.V.Grid.Width < a.Feet - 2, $"ended at row {end.Cell / a.V.Grid.Width}");
        Assert.Equal(air - Content.Shots.Gauss.Damage, a.Air);
        // Right over it, within its strike's columns but off its deck: still a shot once its gun is ready, never a lunge.
        a.Hero(a.Column, a.Room.Top + 3);
        Assert.True(a.RunTill(() =>
        {
            Assert.NotEqual(typeof(Spec.Lunge), a.Act);
            return a.Act == typeof(Spec.Shoot);
        }, Gauss.Every), $"at {a.Column}");
    }

    [Fact]
    public void AHeroUpTheLadderInItsSightKeepsItOnItsDeckUnderHimShootingNeverOffToADoor()
    {
        var a = new Aboard().Woken();
        var doors = a.Doors.Where(d => d.X == a.Room.Left - 2 || d.X == a.Room.Left + a.Room.Width).ToList();
        foreach (var d in doors) a.W.Do(new SetDoor(a.Cell(d.X, d.Y), false));
        // On the well's ladder in its tall hall, off its deck but in its sight: it walks under him and stands there, shooting up.
        a.Hero(a.Room.Left, a.Room.Top + 3);
        int hits = 0;
        for (int t = 0; t < 400; t++)
        {
            a.Run();
            OnItsRows(a);
            Assert.NotEqual(typeof(Spec.Lunge), a.Act);
            Assert.All(doors, d => Assert.True(a.Shut(d.X, d.Y), $"opened the door at {d.X} at tick {t}"));
            hits += a.V.Creatures.Ended.Count(e => e.Projectile == Gauss.Projectile && e.Met);
        }
        Assert.InRange(a.Column, a.Room.Left, a.Room.Left + 4);
        Assert.True(hits >= 2, $"shot him {hits} times");
    }

    /// <summary>
    /// Its hero off its deck but in its sight, up its tall hall on the side away from its nearest door: it runs at its <c>Run</c> under
    /// him, not off to that door.
    /// </summary>
    [Fact]
    public void AHeroSeenOffItsDeckAwayFromItsNearestDoorHasItRunUnderHim()
    {
        var body = (Spec.Walker)Spec.Escort.Body;
        var a = new Aboard().Woken();
        int door = a.V.Creatures.DoorOn(a.Room.Floor, a.Escort.X)!.Value;
        int x = door <= a.Escort.X ? a.Room.Left + a.Room.Width - 3 : a.Room.Left + 2, y = a.Room.Top + 3;
        Assert.NotEqual(a.V.Creatures.Level(y), a.V.Creatures.Level(a.Feet));
        Assert.True(PatrolDrive.Sees(a.V.Creatures, a.Escort, Spec.Escort, (x, y)));
        int was = a.Escort.X;
        a.Hero(x, y).Run();
        Assert.Equal(Math.Sign(x * U - was) * body.Run, a.Escort.X - was);
        Assert.True(a.RunTill(() => Math.Abs(a.Escort.X / U - x) <= 1, 300), $"at {a.Column}, he at {x}");
    }

    [Fact]
    public void AHeroGoneUpTheLadderOutOfItsSightSendsItToItsDoor()
    {
        var a = new Aboard().Woken();
        var door = a.Doors.Single(d => d.X == a.Room.Left - 2);
        a.W.Do(new SetDoor(a.Cell(door.X, door.Y), false));
        // Seen on the ladder, it comes under him; up the well past its hall's ceiling, out of its sight, it is off to its nearest door.
        a.Hero(a.Room.Left, a.Room.Top + 3).Run(200);
        Assert.True(a.Shut(door.X, door.Y));
        a.Hero(a.Room.Left, a.Room.Top - 2);
        Assert.False(PatrolDrive.Sees(a.V.Creatures, a.Escort, Spec.Escort, (a.Room.Left, a.Room.Top - 2)));
        Assert.True(a.RunTill(() => !a.Shut(door.X, door.Y), 300), $"at {a.Column}");
    }

    [Fact]
    public void AHeroNearOnItsFloorIsLungedAtAndClawed()
    {
        var a = new Aboard().Woken();
        a.Hero(a.Column - Patrol.Strike + 1, a.Body).Run();
        Assert.Equal(typeof(Spec.Lunge), a.Act);
        int x = a.Escort.X;
        var lunge = Spec.Escort.Moves.OfType<Spec.Lunge>().Single();
        a.Run(lunge.WindUp + 2);
        Assert.True(a.Escort.X < x, "it dashed at its hero");
        // Touching it, the hero's client says so: the claw lands as any ram does.
        a.W.Do(new HitBy(a.Escort.Id));
        int air = a.Air;
        a.Run();
        Assert.Empty(a.W.Refusals);
        Assert.Equal(air - lunge.Damage, a.Air);
    }

    /// <summary>Its feet on its deck's rows: never off them.</summary>
    private static void OnItsRows(Aboard a) =>
        Assert.InRange((a.Escort.Y - 1) / U, a.Room.Floor - Content.Vessels.Open, a.Room.Floor - 1);

    [Fact]
    public void AnOpenHatchInItsFloorIsWalkedOverToItsHeroNeverDownIt()
    {
        // The hall over the hold on the right, its deck's floor holding the well's hatch at the left end, its hero past it.
        var a = new Aboard(left: 203);
        var hatch = a.Vessel.Hatches.Select(h => (X: M + h.X, Y: M + h.Y)).Where(h => h.Y == a.Room.Floor && h.X < a.Column).MaxBy(h => h.X);
        Assert.True(((Spec.Walker)Spec.Escort.Body).Width > Doorways.Thick);
        foreach (var d in a.Doors.Where(d => d.X >= hatch.X - Doorways.Thick && d.X < a.Column)) a.W.Do(new SetDoor(a.Cell(d.X, d.Y), true));
        a.W.Do(new SetDoor(a.Cell(hatch.X, hatch.Y), true));
        a.Woken().Run(Content.Doors.SwingTicks);
        Assert.False(a.Shut(hatch.X, hatch.Y));
        int hero = hatch.X - 10;
        a.Hero(hero, a.Body);
        Assert.True(a.RunTill(() =>
        {
            OnItsRows(a);
            return a.Escort.X / U < hatch.X - 2;
        }, 1200), $"stuck at {a.Escort.X},{a.Escort.Y}, the hatch at column {hatch.X}");
        Assert.True(a.RunTill(() =>
        {
            OnItsRows(a);
            return a.Act == typeof(Spec.Lunge);
        }, 200), $"never lunged: at {a.Column}");
        Assert.InRange(a.Column - hero, 0, Patrol.Strike);
        a.Run(Claw.WindUp + Claw.Ticks + 2);
        OnItsRows(a);
        Assert.False(a.Shut(hatch.X, hatch.Y));
    }

    /// <summary>Woken, its hero out past the way in, then peer 1's hero come aboard too, out past it as well.</summary>
    private static Aboard WithTwo()
    {
        var a = new Aboard().Woken();
        var door = Shipyard.WayIn(a.Built);
        GuardsAboardTests.Board(a.W, 1, a.Cell(M + door.X - 4, M + door.Y + Content.Vessels.Open - 1));
        return a;
    }

    /// <summary>How many hurts peer <paramref name="peer"/>'s hero has taken.</summary>
    private static int HurtsOf(Aboard a, int peer) => a.V.Crew.HeroOf(peer)!.Hurts;

    [Fact]
    public void ItHuntsTheNearerOfTwoHeroesAndAtATieTheFirstInTheCrewsOrder()
    {
        var a = WithTwo();
        int x = a.Column;
        // Peer 0 ten columns left, peer 1 fourteen right, both out of its claw's reach: at peer 0; then the other way about.
        a.Hero(x - 10, a.Body, 0).Hero(x + 14, a.Body, 1).Run();
        Assert.Equal((x - 10, a.Body), a.V.Creatures.Quarry(a.Escort));
        Assert.True(a.Escort.VX < 0, $"walking {a.Escort.VX}");
        x = a.Column;
        a.Hero(x + 14, a.Body, 0).Hero(x - 10, a.Body, 1).Run();
        Assert.Equal((x - 10, a.Body), a.V.Creatures.Quarry(a.Escort));
        Assert.True(a.Escort.VX < 0, $"walking {a.Escort.VX}");
        // As near either way: the first in the crew's order, whichever side he is on.
        x = a.Column;
        a.Hero(x + 12, a.Body, 0).Hero(x - 12, a.Body, 1).Run();
        Assert.Equal((x + 12, a.Body), a.V.Creatures.Quarry(a.Escort));
        Assert.True(a.Escort.VX > 0, $"walking {a.Escort.VX}");
        x = a.Column;
        a.Hero(x - 12, a.Body, 0).Hero(x + 12, a.Body, 1).Run();
        Assert.Equal((x - 12, a.Body), a.V.Creatures.Quarry(a.Escort));
        Assert.True(a.Escort.VX < 0, $"walking {a.Escort.VX}");
    }

    [Fact]
    public void ItsShotGoesAtTheHeroItSawNotOneNearerByTheBoxBehindTheFloor()
    {
        var a = WithTwo();
        int x = a.Column;
        var door = a.Doors.Single(d => d.X == a.Room.Left + a.Room.Width);
        a.W.Do(new SetDoor(a.Cell(door.X, door.Y), true));
        a.Run(Content.Doors.SwingTicks);
        // Peer 1 in line through the open door 20 columns right, its quarry (400 cells squared); peer 0 on the deck over it, 13 columns
        // left and 16 rows up (425), nearer by the box (16) all through its charge, and first at a tie.
        int over = a.Room.Top - 1 - 3;
        a.Hero(x - 13, over, 0).Hero(x + 20, a.Body, 1).Run();
        Assert.Equal((x + 20, a.Body), a.V.Creatures.Quarry(a.Escort));
        Assert.Equal(typeof(Spec.Shoot), a.Act);
        var (one, two) = (HurtsOf(a, 0), HurtsOf(a, 1));
        Assert.True(a.RunTill(() => a.V.Creatures.Ended.Any(e => e.Projectile == Gauss.Projectile), Gauss.WindUp + 2));
        var end = a.V.Creatures.Ended.Single(e => e.Projectile == Gauss.Projectile);
        Assert.InRange(end.Cell / a.V.Grid.Width, a.Body - 1, a.Body + 1);
        Assert.Equal((one, two + 1), (HurtsOf(a, 0), HurtsOf(a, 1)));
    }

    [Fact]
    public void TheGaussPassesAHeroPassedOutAndStopsInTheFirstAwakeOneInLine()
    {
        // Peer 0 passed out in line before peer 1: the shot passes him to peer 1.
        var a = WithTwo();
        int x = a.Column;
        a.V.Crew.HeroOf(0)!.Out = 1000;
        a.Hero(x - 12, a.Body, 0).Hero(x - 15, a.Body, 1).Run();
        Assert.Equal(typeof(Spec.Shoot), a.Act);
        var (one, two) = (HurtsOf(a, 0), HurtsOf(a, 1));
        Assert.True(a.RunTill(() => a.V.Creatures.Ended.Any(e => e.Projectile == Gauss.Projectile), Gauss.WindUp + 2));
        Assert.Equal((one, two + 1), (HurtsOf(a, 0), HurtsOf(a, 1)));
        // Both awake, one behind the other in line: it stops in the first.
        var b = WithTwo();
        x = b.Column;
        b.Hero(x - 12, b.Body, 0).Hero(x - 15, b.Body, 1).Run();
        Assert.Equal(typeof(Spec.Shoot), b.Act);
        (one, two) = (HurtsOf(b, 0), HurtsOf(b, 1));
        Assert.True(b.RunTill(() => b.V.Creatures.Ended.Any(e => e.Projectile == Gauss.Projectile), Gauss.WindUp + 2));
        Assert.Equal((one + 1, two), (HurtsOf(b, 0), HurtsOf(b, 1)));
    }

    [Fact]
    public void TheGaussStopsAtAShutDoorBetweenWithNoBurst()
    {
        var a = new Aboard().Woken();
        var door = a.Doors.Single(d => d.X == a.Room.Left - 2);
        a.W.Do(new SetDoor(a.Cell(door.X, door.Y), true));
        a.Run(30).Hero(door.X - 4, a.Body).Run();
        Assert.Equal(typeof(Spec.Shoot), a.Act);
        // Its aim taken, the door shut: the shot meets it.
        a.Run(Gauss.WindUp - Gauss.Lock + 1);
        a.W.Do(new SetDoor(a.Cell(door.X, door.Y), false));
        int air = a.Air;
        Assert.True(a.RunTill(() => a.V.Creatures.Ended.Any(e => e.Projectile == Gauss.Projectile), Gauss.Lock + 2));
        var end = a.V.Creatures.Ended.Single(e => e.Projectile == Gauss.Projectile);
        Assert.Equal((true, -1), (end.Met, end.Burst));
        Assert.InRange(end.Cell % a.V.Grid.Width, door.X, door.X + 1);
        Assert.Equal(air, a.Air);
        Assert.True(a.Shut(door.X, door.Y));
    }

    [Fact]
    public void AHeroOutOfLineOnAnotherDeckSendsItToItsNearestDoorTheLowerAtATieAndOnThroughIt()
    {
        var a = new Aboard().Woken();
        var doors = a.Doors.Where(d => d.X == a.Room.Left - 2 || d.X == a.Room.Left + a.Room.Width).ToList();
        Assert.Equal(2, doors.Count);
        // Both doors equally far: the lower column's.
        Assert.Equal(a.Escort.X - (doors[0].X + 1) * U, (doors[1].X + 1) * U - a.Escort.X);
        foreach (var d in doors) a.W.Do(new SetDoor(a.Cell(d.X, d.Y), false));
        // A deck up, in a room past the hall's ceiling: out of line, off its deck.
        a.Hero(a.Column + 20, a.Room.Top - 2).Run();
        Assert.True(a.RunTill(() => !a.Shut(doors[0].X, doors[0].Y), 400), $"at {a.Column}");
        Assert.True(a.Shut(doors[1].X, doors[1].Y));
        Assert.True(a.RunTill(() => a.Escort.X / U < doors[0].X - 3, 200), $"at {a.Column}");
    }

    /// <summary>Off to its door with its hero out of sight, it walks at its <c>Walk</c>; going for him seen on its deck, at its <c>Run</c>.</summary>
    [Fact]
    public void ItWalksToItsDoorAtItsWalkAndGoesForAHeroAtItsRun()
    {
        var body = (Spec.Walker)Spec.Escort.Body;
        Assert.True(body.Walk < body.Run);
        var a = new Aboard().Woken();
        a.Hero(a.Column + 20, a.Room.Top - 2).Run();
        var walked = new HashSet<int>();
        for (int t = 0; t < 40; t++)
        {
            int x = a.Escort.X;
            a.Run();
            walked.Add(Math.Abs(a.Escort.X - x));
        }
        Assert.Equal([body.Walk], walked);
        // Seen on its floor at the hall's far end, out of its claw's reach: it goes for him.
        int end = a.Column - a.Room.Left > a.Room.Left + a.Room.Width - 1 - a.Column ? a.Room.Left + 1 : a.Room.Left + a.Room.Width - 2;
        Assert.True(Math.Abs(end - a.Column) > Patrol.Strike + 2, $"at {a.Column}, he at {end}");
        int was = a.Escort.X;
        a.Hero(end, a.Body).Run();
        Assert.Equal(body.Run, Math.Abs(a.Escort.X - was));
    }

    [Fact]
    public void AHeroBehindAShutDoorOnItsDeckHasItPryTheDoorAndShootThroughIt()
    {
        var a = new Aboard().Woken();
        var door = a.Doors.Single(d => d.X == a.Room.Left + a.Room.Width);
        a.W.Do(new SetDoor(a.Cell(door.X, door.Y), false));
        a.Hero(door.X + 8, a.Body).Run();
        Assert.Null(a.Act);
        Assert.True(a.RunTill(() => !a.Shut(door.X, door.Y), 300), $"at {a.Column}");
        // Through it in line: shot.
        Assert.True(a.RunTill(() => a.Act == typeof(Spec.Shoot), 5));
    }

    [Fact]
    public void WithNoHeroAboardItStandsWhereItIs()
    {
        var a = new Aboard().Woken();
        var door = Shipyard.WayIn(a.Built);
        a.Hero(M + door.X - 4, M + door.Y + Content.Vessels.Open - 1).Run(40);
        var (x, y) = (a.Escort.X, a.Escort.Y);
        a.Run(200);
        Assert.True(Awake(a.Escort));
        Assert.Equal((x, y), (a.Escort.X, a.Escort.Y));
    }

    /// <summary>A blow wakes it, so none dies asleep: struck on and on, it dies still waking in its stand.</summary>
    [Fact]
    public void StruckDeadWhileItWakesItGivesMoreCircuitsAndScrapThanTheSentryAndItsStandIsLeftEmpty()
    {
        int Least(Spec.Species s, string item) => s.Yields.Single(y => y.Item == item).Min;
        int Most(Spec.Species s, string item) => s.Yields.Single(y => y.Item == item).Max;
        Assert.True(Least(Spec.Escort, "circuit") > Most(Spec.Sentry, "circuit") && Least(Spec.Escort, "scrapChunk") > Most(Spec.Sentry, "scrapChunk"));
        Assert.Equal(2 * Spec.Sentry.Life.Hp, Spec.Escort.Life.Hp);
        var a = new Aboard();
        int dock = CreaturesModule.DockOf(a.Escort)!.Value;
        var postures = new List<Posture>();
        Assert.True(a.RunTill(() =>
        {
            if (!a.V.Creatures.Creatures.Contains(a.Escort)) return true;
            postures.Add(a.Escort.Posture);
            a.W.Do(new Strike(a.Escort.Id, Content.Weapons.Axe.Item));
            return false;
        }, Wake - 1), $"damage {a.Escort.Damage}");
        Assert.Equal(Posture.Docked, postures[0]);
        Assert.All(postures.Skip(1), p => Assert.Equal(Posture.Waking, p));
        Assert.DoesNotContain(a.V.Creatures.Creatures, c => CreaturesModule.DockOf(c) == dock);
    }

    private static Arbitrary<(uint Index, uint Seed)> Plays() =>
        Gen.Zip(Gen.Choose(0, 2), ArbMap.Default.GeneratorFor<uint>()).Select(t => ((uint)t.Item1, t.Item2)).ToArbitrary();

    /// <summary>
    /// A play aboard a liner, its hero now on a room's floor, now up the hall, its doors set at random: every escort keeps to its deck's rows
    /// on its dock's room's floor's deck, and every hatch and the way in stay shut, as the play left them.
    /// </summary>
    [Property(MaxTest = 10)]
    public Property AnEscortNeverLeavesItsDecksRowsNorOpensAHatchOrTheWayIn() => Prop.ForAll(Plays(), play =>
    {
        var (w, v, built) = GuardsAboardTests.Boarded("liner", play.Index);
        var vessel = built.Vessel;
        var rng = new Rng(new RngState { S = play.Seed | 1 });
        var rooms = vessel.Compartments;
        int surface = w.World.SurfaceOf(0), width = v.Grid.Width;
        // What it may never pry: a hatch's cells, the way in's. A spider opens hatches, so the hatches themselves tell nothing.
        var wayIn = Shipyard.WayIn(built);
        var never = vessel.Hatches.SelectMany(h => new[] { (M + h.Y) * width + M + h.X, (M + h.Y) * width + M + h.X + 1 })
            .Concat(Enumerable.Range(0, Content.Vessels.Open).SelectMany(y => new[] { (M + wayIn.Y + y) * width + M + wayIn.X, (M + wayIn.Y + y) * width + M + wayIn.X + 1 }))
            .ToHashSet();
        for (int t = 0; t < 1500; t++)
        {
            if (t % 30 == 0)
            {
                var r = rooms[(int)rng.Below((uint)rooms.Count)];
                int row = M + r.Floor - 1 - (int)rng.Below((uint)Math.Min(r.Height, 9));
                w.Do(new Enter(row * width + M + r.Left + (int)rng.Below((uint)r.Width)));
                var all = v.Creatures.Creatures;
                if (rng.Below(3) == 0 && all.Count > 0) w.Do(new Strike(all[(int)rng.Below((uint)all.Count)].Id, Content.Weapons.Axe.Item));
                if (rng.Below(2) == 0)
                {
                    var d = vessel.Doors[(int)rng.Below((uint)vessel.Doors.Count)];
                    if (d != Shipyard.WayIn(built)) w.Do(new SetDoor((M + d.Y) * width + M + d.X, rng.Below(2) == 0));
                }
            }
            w.Run();
            if (w.World.SurfaceOf(0) != surface) break;
            foreach (var c in v.Creatures.Creatures.Where(c => c.Species == Spec.Escort.Id))
            {
                var dock = v.Creatures.Docks[CreaturesModule.DockOf(c)!.Value];
                int floor = M + dock.Y + dock.Height, feet = (c.Y - 1) / U;
                Assert.True(feet < floor && feet >= floor - Content.Vessels.Open, $"escort {c.Id} at {c.X},{c.Y} off its deck's rows over {floor}: {c.Posture}");
                int pried = c.Recall(ProwlDrive.PryKey) - 1;
                Assert.False(never.Contains(pried), $"escort {c.Id} pries {pried % width},{pried / width}");
            }
            Assert.True(Doorways.IsClosed(v.Grid.StructureAt((M + wayIn.Y) * width + M + wayIn.X)));
        }
        return true.ToProperty();
    });

    /// <summary>
    /// Two worlds fed the same commands aboard a liner, alike every tick while an escort wakes, shoots, lunges and pries a door after its hero;
    /// then a second hero comes aboard and a peer joins: the one world's save loaded, played on beside the other.
    /// </summary>
    [Fact]
    public void TwoWorldsStayAlikeThroughAWakeAShotALungeAndADoorAndAJoinMidFight()
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
        Both(new Find("liner", Wrecked: false));
        var contact = a.Places.Contacts[^1];
        Both(new Jump(contact.Id));
        int surface = first.SurfaceOf(0);
        var v = Registry.On(first, surface);
        var vessel = Shipyard.Build(Content.Vessels.Class("liner")!, contact.Seed, contact.Wreck).Vessel;
        int width = v.Grid.Width;
        var escort = v.Creatures.Creatures.Where(c => c.Species == Spec.Escort.Id).MaxBy(c => c.Y)!;
        var room = vessel.Compartments[v.Creatures.Docks[CreaturesModule.DockOf(escort)!.Value].Room];
        int feet = M + room.Floor - 1, id = escort.Id, column = escort.X / U;
        // Woken by a step in; a hero far in line is shot, one near lunged at.
        Both(new Enter(feet * width + column - 3));
        Run(Wake + 2);
        Both(new Enter(feet * width + column - 14));
        Run(Gauss.WindUp + Gauss.Recover + 4);
        Both(new Enter(feet * width + column - 4));
        Run(30);
        // Its hero behind the door on the hall's right, shut: a pry.
        int door = M + room.Left + room.Width;
        Both(new SetDoor((M + room.Floor - Content.Vessels.Open) * width + door, false));
        Both(new Enter(feet * width + door + 8));
        Run(Patrol.Pry + 60);
        Both(new Strike(id, Content.Weapons.Axe.Item));
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
