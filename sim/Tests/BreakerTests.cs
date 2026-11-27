using Stillwake.Sim.Creatures;
using Stillwake.Sim.Crew;
using Xunit;
using Spec = Stillwake.Content;
using Tune = Stillwake.Content.Creatures;

namespace Stillwake.Sim.Tests;

/// <summary>The breaker (companion-fights): a pack's dummy out in space, that rings a hero, darts at it and is flung by a blow.</summary>
public class BreakerTests
{
    private const int Width = 120, Height = 80, Unit = CreaturesModule.Unit;
    private static readonly Tune.Species Breaker = Tune.Breaker;
    private static readonly Tune.Swarm Swarm = (Tune.Swarm)Breaker.Brain[0];
    private static readonly Tune.Pounce Pounce = (Tune.Pounce)Breaker.Moves[0];
    private static readonly string Axe = Spec.Weapons.Axe.Item;

    /// <summary>Open space a cell a character, hero 0 in creative at (<paramref name="x"/>, <paramref name="y"/>).</summary>
    internal static Scenario World(int x = 40, int y = 40, char[][]? map = null)
    {
        map ??= Space();
        var w = new Scenario(string.Join('\n', map.Select(r => new string(r))), cells: true).Breathing();
        return Hero(w, x, y).Run();
    }

    private static char[][] Space() => [.. Enumerable.Range(0, Height).Select(_ => Enumerable.Repeat('.', Width).ToArray())];

    private static Scenario Hero(Scenario w, int x, int y, int peer = 0) => w.Do(new Enter(y * Width + x) { From = peer });

    /// <summary>A breaker put out by state with its middle in the middle of cell (<paramref name="x"/>, <paramref name="y"/>): no release lets a flyer out afloat.</summary>
    internal static Creature Put(Scenario w, int x, int y)
    {
        var state = (CreaturesState)w.M.Creatures.Slice;
        var c = new Creature { Id = state.NextId++, Species = Breaker.Id, X = x * Unit + Unit / 2, Y = y * Unit + Unit / 2 + Unit, SeenAt = state.Clock };
        state.Creatures.Add(c);
        return c;
    }

    private static int Far(Creature c, int x, int y) => Math.Max(Math.Abs(c.X - (x * Unit + Unit / 2)), Math.Abs(c.Y - Unit - (y * Unit + Unit / 2)));

    private static bool Pouncing(Creature c) => c.Act != null;

    private static int Run(Creature c) => Math.Max(Math.Abs(c.VX), Math.Abs(c.VY));

    [Fact]
    public void ItFliesToAHeroOutAtNoMoreThanItsSpeed()
    {
        var w = World();
        var c = Put(w, 100, 40);
        for (int i = 0; i < 30; i++)
        {
            w.Run();
            Assert.InRange(Run(c), 1, Swarm.Speed);
        }
        Assert.True(Far(c, 40, 40) < 45 * Unit);
    }

    [Fact]
    public void APackSpreadsRoundTheHeroRatherThanStackingOnIt()
    {
        var w = World();
        var pack = Enumerable.Range(0, 6).Select(i => Put(w, 60, 30 + 4 * i)).ToList();
        w.Run(100);
        Assert.All(pack, c => Assert.InRange(Far(c, 40, 40), 0, 16 * Unit));
        Assert.Contains(pack, c => c.X < 40 * Unit);
        Assert.Contains(pack, c => c.X > 41 * Unit);
        Assert.Equal(pack.Count, pack.Select(c => (c.X / Unit, c.Y / Unit)).Distinct().Count());
    }

    [Fact]
    public void AWallStopsIt()
    {
        var map = Space();
        foreach (var row in map) row[60] = '#';
        var w = World(map: map);
        var c = Put(w, 80, 40);
        w.Run(100);
        Assert.InRange(c.X, 62 * Unit, 63 * Unit);
        Assert.Equal(0, c.VX);
        Assert.False(Pouncing(c));
    }

    [Fact]
    public void AShortWallBetweenHoldsItsDart()
    {
        var map = Space();
        for (int y = 37; y <= 43; y++) map[y][43] = '#';
        var w = World(map: map);
        var c = Put(w, 47, 40);
        w.Run();
        Assert.False(Pouncing(c));
        // With nothing between, the same place is a dart's.
        var open = World();
        var other = Put(open, 47, 40);
        open.Run();
        Assert.True(Pouncing(other));
    }

    [Fact]
    public void AHeroGoneOutMidTellIsNotDartedAt()
    {
        var w = World();
        var c = Put(w, 46, 40);
        w.Run();
        Assert.True(Pouncing(c));
        ((CrewState)w.M.Crew.Slice).Heroes[0].Out = 1000;
        w.Run(Pounce.WindUp);
        Assert.False(Pouncing(c));
    }

    [Fact]
    public void LetBeItSlowsToRest()
    {
        var w = World();
        var c = Put(w, 80, 40);
        c.VX = Swarm.Speed;
        ((CrewState)w.M.Crew.Slice).Heroes[0].Out = 1000;
        w.Run();
        Assert.InRange(c.VX, 1, Swarm.Speed - 1);
        w.Run(40);
        Assert.Equal(0, Run(c));
    }

    [Theory]
    [InlineData(6, 3, 900, 450)]
    [InlineData(6, -2, 900, -300)]
    [InlineData(-3, 6, -450, 900)]
    public void ABlowOffTheDiagonalFlingsItAlongItsOwnLine(int dx, int dy, int vx, int vy)
    {
        var w = World();
        var c = Put(w, 40 + dx, 40 + dy);
        w.Do(new Strike(c.Id, Axe)).Run();
        Assert.Equal((vx, vy), (c.VX, c.VY));
    }

    [Fact]
    public void EveryOneKilledDropsAScrap()
    {
        var w = World();
        for (int i = 0; i < 8; i++)
        {
            var c = Put(w, 43, 40);
            w.Do(new Strike(c.Id, Axe)).Do(new Strike(c.Id, Axe)).Run();
            Assert.True(c.Dead || !w.M.Creatures.Creatures.Contains(c));
        }
        Assert.Empty(w.Refusals);
        Assert.Equal(8, w.M.Items.Floor.Where(s => s.Item == "scrapChunk").Sum(s => s.Count));
    }

    [Fact]
    public void ADartsReachCoversWhereItBegins() => Assert.True(Pounce.Ticks * Pounce.Speed >= Swarm.Strike * Unit);

    [Fact]
    public void ADartHitsForItsDamageAndItRecoils()
    {
        var w = new Scenario(string.Join('\n', Space().Select(r => new string(r))), cells: true).Suit(0).Suit(1);
        Hero(w, 40, 40);
        // Far off, the same suit breathed as long.
        Hero(w, 2, 78, peer: 1).Run();
        var c = Put(w, 46, 40);
        w.Run();
        Assert.True(Pouncing(c));
        w.Run(Pounce.WindUp);
        Assert.Equal(Posture.Charge, c.Posture);
        int vx = c.VX;
        Assert.True(vx < 0);
        w.Do(new HitBy(c.Id)).Run();
        Assert.Empty(w.Refusals);
        Assert.False(Pouncing(c));
        Assert.Equal(1, w.M.Crew.HeroOf(1)!.Air - w.M.Crew.HeroOf(0)!.Air);
        // Back at half the dart, a tick of its slowing run.
        int back = -vx / 2;
        Assert.Equal(back - back / 4, c.VX);
        Assert.Equal(SwarmDrive.Thrown, c.Step);
    }

    [Fact]
    public void ABlowCutsItsCrouchAndItsDart()
    {
        foreach (int ticks in new[] { 1, Pounce.WindUp + 1 })
        {
            var w = World();
            var c = Put(w, 46, 40);
            w.Run(ticks);
            Assert.True(Pouncing(c));
            w.Do(new Strike(c.Id, Axe)).Run();
            Assert.False(Pouncing(c));
            Assert.Equal(Ref.None, c.Target);
            w.Do(new HitBy(c.Id)).Run();
            Assert.Equal("creatures.notAttacking", w.Refusals.Single().Reason);
        }
    }

    [Fact]
    public void NoOtherPounceIsCutByABlow()
    {
        foreach (var other in Tune.All.Where(s => s.Id != Breaker.Id))
            foreach (var move in other.Moves.OfType<Tune.Pounce>())
            {
                Assert.False(RuleBook.Move(move).Interruptible(move), other.Id);
                Assert.False(move.AtTarget, other.Id);
            }
        // Nor is the catcher, the other flyer, knocked.
        Assert.Equal(0, Tune.Catcher.Life.Knock);
    }

    [Theory]
    [InlineData(1, 0)]
    [InlineData(-1, 0)]
    [InlineData(0, 1)]
    [InlineData(0, -1)]
    [InlineData(1, 1)]
    [InlineData(-1, -1)]
    public void ABlowFlingsItAlongTheLineFromTheHero(int dx, int dy)
    {
        foreach (int cells in new[] { 3, 18 })
        {
            var w = World();
            var c = Put(w, 40 + dx * cells, 40 + dy * cells);
            int x = c.X, y = c.Y;
            w.Do(new Strike(c.Id, Axe)).Run();
            // A tick of its slowing has run: a quarter off the knock.
            int knock = Breaker.Life.Knock - Breaker.Life.Knock / 4;
            Assert.Equal((dx * knock, dy * knock), (c.VX, c.VY));
            w.Run(6);
            Assert.InRange(Math.Max(Math.Abs(c.X - x), Math.Abs(c.Y - y)), 3 * Unit, 5 * Unit);
            Assert.True(Math.Sign(c.X - x) == dx && Math.Sign(c.Y - y) == dy, "it drifted off the blow's line");
        }
    }

    [Fact]
    public void KnockedItSteersAgainOnceSlowedToAQuarter()
    {
        var w = World();
        var c = Put(w, 50, 40);
        w.Do(new Strike(c.Id, Axe)).Run();
        Assert.Equal(SwarmDrive.Thrown, c.Step);
        for (int i = 0; i < 20 && c.Step == SwarmDrive.Thrown; i++) w.Run();
        Assert.Equal(0, c.Step);
        Assert.InRange(Run(c), 0, Swarm.Speed / 4);
        w.Run(10);
        Assert.True(c.VX < 0);
    }

    [Fact]
    public void OneBeginsADartAtAHeroEachGatesTicks()
    {
        var w = World();
        Hero(w, 70, 40, peer: 1).Run();
        var pack = Enumerable.Range(0, 3).Select(i => Put(w, 46, 36 + 4 * i))
            .Concat(Enumerable.Range(0, 3).Select(i => Put(w, 76, 36 + 4 * i))).ToList();
        var begun = new List<(int Tick, int Peer)>();
        for (int tick = 0; tick < 200; tick++)
        {
            var before = pack.Select(Pouncing).ToList();
            w.Run();
            for (int i = 0; i < pack.Count; i++)
                if (!before[i] && Pouncing(pack[i])) begun.Add((tick, pack[i].Target.Id));
        }
        foreach (int peer in new[] { 0, 1 })
        {
            var ticks = begun.Where(b => b.Peer == peer).Select(b => b.Tick).ToList();
            Assert.True(ticks.Count >= 3, $"hero {peer} was darted at {ticks.Count} times");
            for (int i = 1; i < ticks.Count; i++) Assert.True(ticks[i] - ticks[i - 1] >= Swarm.Every, $"hero {peer}: two begun too soon");
        }
    }

    [Fact]
    public void ItsDartGoesAtNoOtherHero()
    {
        var w = World();
        var c = Put(w, 46, 40);
        w.Run();
        Assert.Equal(Ref.Hero(0), c.Target);
        // The one it began at leaves its run; another comes into it.
        Hero(w, 10, 40);
        Hero(w, 49, 40, peer: 1).Run(Pounce.WindUp);
        Assert.False(Pouncing(c) && c.Posture == Posture.Charge);
    }

    [Fact]
    public void TwoOfTheAxesBlowsKillIt()
    {
        var w = World();
        var c = Put(w, 50, 40);
        w.Do(new Strike(c.Id, Axe)).Run();
        Assert.Contains(c, w.M.Creatures.Creatures);
        w.Do(new Strike(c.Id, Axe)).Run();
        Assert.Empty(w.Refusals);
        Assert.DoesNotContain(c, w.M.Creatures.Creatures);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AHeroInARoomIsLetBeWithNoBackWallToo(bool backed)
    {
        var map = Space();
        for (int y = 34; y <= 46; y++)
            for (int x = 34; x <= 46; x++)
                map[y][x] = x is 34 or 46 || y is 34 or 46 ? '#' : backed ? '.' : ' ';
        var w = World(map: map);
        // Whatever its back, it is a room: no breaker's business.
        Assert.NotEqual(Stillwake.Sim.Rooms.RoomsModule.Space, w.M.Rooms.RoomAt(40 * Width + 40));
        // The room with no back wall is afloat, and no less a room.
        Assert.Equal(!backed, w.M.Rooms.Afloat(40 * Width + 40));
        var c = Put(w, 49, 40);
        int x0 = c.X;
        w.Run(40);
        Assert.False(Pouncing(c));
        Assert.Equal(x0, c.X);
    }

    [Fact]
    public void AHeroPassedOutIsLetBe()
    {
        var w = World();
        var c = Put(w, 46, 40);
        ((CrewState)w.M.Crew.Slice).Heroes[0].Out = 1000;
        int x = c.X;
        w.Run(20);
        Assert.False(Pouncing(c));
        Assert.Equal(x, c.X);
    }
}
