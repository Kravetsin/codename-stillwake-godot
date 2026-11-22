using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Stillwake.Sim.Creatures;
using Stillwake.Sim.Crew;
using Stillwake.Sim.Debris;
using Stillwake.Sim.Items;
using Xunit;
using Spec = Stillwake.Content;
using Tune = Stillwake.Content.Creatures;

namespace Stillwake.Sim.Tests;

/// <summary>Nests (companion-fights): packs of breakers asleep on the far field's lesser wrecks.</summary>
public class NestTests
{
    private const int Width = 200, Height = 100, Unit = CreaturesModule.Unit;
    private static readonly Tune.Nest Kind = Tune.Dismantlers;
    private static readonly string Axe = Spec.Weapons.Axe.Item;

    private static char[][] Space() => [.. Enumerable.Range(0, Height).Select(_ => Enumerable.Repeat('.', Width).ToArray())];

    /// <summary>Open space a cell a character, hero 0 in creative at (<paramref name="x"/>, <paramref name="y"/>).</summary>
    private static Scenario World(int x = 20, int y = 50, char[][]? map = null, uint seed = 1)
    {
        var w = new Scenario(string.Join('\n', (map ?? Space()).Select(r => new string(r))), seed, cells: true).Breathing();
        return Hero(w, x, y).Run();
    }

    private static Scenario Hero(Scenario w, int x, int y, int peer = 0) => w.Do(new Enter(y * Width + x) { From = peer });

    private static CreaturesState State(Scenario w) => (CreaturesState)w.M.Creatures.Slice;

    private static Piece Out(Scenario w, string item, int x, int y, string? zone = null)
    {
        var debris = (DebrisState)w.M.Debris.Slice;
        var piece = new Piece { Id = debris.NextId++, Zone = zone ?? Spec.Debris.Far.Id, Item = item, X = x * Unit, Y = y * Unit, Start = x * Unit, SeenAt = debris.Clock };
        debris.Pieces.Add(piece);
        return piece;
    }

    /// <summary>A hull section at (<paramref name="x"/>, <paramref name="y"/>) with a nest of <paramref name="size"/>, put out by state: no piece draws from here on.</summary>
    private static Piece Nested(Scenario w, int x, int y, int size = 5, string item = "hullSection")
    {
        var piece = Out(w, item, x, y);
        State(w).Watermark = int.MaxValue;
        State(w).Nests.Add(new Nest { Piece = piece.Id, Size = size });
        return piece;
    }

    private static List<Creature> Pack(Scenario w) => [.. w.M.Creatures.Creatures.Where(c => c.Species == Tune.Breaker.Id)];

    [Fact]
    public void OnlyTheFourKindsOfTheFarFieldHaveOneAboutOneInTen()
    {
        var w = World(x: 4);
        string[] never = ["scrapChunk", "cargoCrate", "tankCluster", "cableRack", "engineBlock"];
        int nested = 0, could = 0;
        for (int round = 0; round < 60; round++)
        {
            var yes = Kind.Kinds.SelectMany(k => Enumerable.Range(0, 4).Select(i => Out(w, k, 150 + i, 50))).ToList();
            var no = never.Select((k, i) => Out(w, k, 160 + i, 50)).Concat(Kind.Kinds.Select((k, i) => Out(w, k, 170 + i, 50, Spec.Debris.Ship.Id))).ToList();
            w.Run();
            var nests = w.M.Creatures.Nests;
            Assert.DoesNotContain(nests, n => no.Any(p => p.Id == n.Piece));
            Assert.All(nests, n => Assert.InRange(n.Size, Kind.Least, Kind.Most));
            Assert.Equal(nests.Select(n => n.Piece).Order(), nests.Select(n => n.Piece));
            nested += nests.Count(n => yes.Any(p => p.Id == n.Piece));
            could += yes.Count;
            ((DebrisState)w.M.Debris.Slice).Pieces.Clear();
            State(w).Nests.Clear();
        }
        Assert.InRange(nested * 1000 / could, 70, 130);
    }

    [Fact]
    public void ItsFourKindsAreTheLesserWrecksAndEachHasNests()
    {
        string[] four = ["hullSection", "cargoContainer", "machineBlock", "shieldSlab"];
        Assert.Equal(four, Kind.Kinds);
        var w = World(x: 4);
        var of = four.ToDictionary(k => k, _ => 0);
        var sizes = new HashSet<int>();
        for (int round = 0; round < 80; round++)
        {
            var pieces = four.SelectMany(k => Enumerable.Range(0, 4).Select(i => Out(w, k, 150 + i, 50))).ToList();
            w.Run();
            foreach (var nest in w.M.Creatures.Nests)
            {
                if (pieces.Find(p => p.Id == nest.Piece) is not { } piece) continue;
                of[piece.Item] += 1;
                sizes.Add(nest.Size);
            }
            ((DebrisState)w.M.Debris.Slice).Pieces.Clear();
            State(w).Nests.Clear();
        }
        foreach (string kind in four) Assert.InRange(of[kind] * 1000 / 320, 50, 160);
        Assert.Equal(Enumerable.Range(4, 5), sizes.Order());
    }

    [Fact]
    public void APieceStruckBeforeItIsSeenHasNone()
    {
        var w = World(x: 4);
        for (int round = 0; round < 20; round++)
        {
            for (int i = 0; i < 8; i++) Out(w, "hullSection", 150 + i, 50).Struck = new Blow { By = FloorStack.Nobody, At = 1 };
            w.Run();
            Assert.Empty(w.M.Creatures.Nests);
            ((DebrisState)w.M.Debris.Slice).Pieces.Clear();
        }
    }

    [Fact]
    public void BreakersSleepOnTheirFourKindsOfTheFarFieldAlone()
    {
        var w = World();
        Assert.Equal(Tune.Breaker.Id, w.M.Creatures.SleeperOn(Out(w, "shieldSlab", 150, 50)));
        Assert.Null(w.M.Creatures.SleeperOn(Out(w, "cargoCrate", 150, 50)));
        Assert.Null(w.M.Creatures.SleeperOn(Out(w, "shieldSlab", 150, 50, Spec.Debris.Ship.Id)));
    }

    [Fact]
    public void ItWakesAtItsReachAndNotACellPast()
    {
        var past = World(x: 100 - Kind.Wake - 1);
        Nested(past, 100, 50);
        past.Run(3);
        Assert.Empty(Pack(past));
        var at = World(x: 100 - Kind.Wake);
        Nested(at, 100, 50);
        at.Run();
        Assert.Equal(5, Pack(at).Count);
    }

    [Fact]
    public void TheLargestNestWakesWholeInOpenSpace()
    {
        var w = World(x: 75);
        Nested(w, 100, 50, size: Kind.Most);
        w.Run();
        Assert.Equal(Kind.Most, Pack(w).Select(c => (c.X, c.Y)).Distinct().Count());
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public void TheStrikersBoxIsAskedNotTheNearestHeros(bool byStriker, bool wakes)
    {
        var w = World(x: 2);
        Hero(w, 198, 50, peer: 1);
        // Round the nearest hero alone a pack too big for one more.
        for (int i = 0; i < 4; i++) BreakerTests.Put(w, 192, 30 + i * 4);
        var piece = Nested(w, 160, 50, size: 5);
        piece.Struck = new Blow { By = byStriker ? 0 : FloorStack.Nobody, At = 1 };
        w.Run();
        Assert.Equal(wakes ? 9 : 4, Pack(w).Count);
    }

    [Fact]
    public void AHeroOutComingNearWakesItWhole()
    {
        var w = World();
        var piece = Nested(w, 100, 50, size: 5);
        w.Run(3);
        Assert.Empty(Pack(w));
        Hero(w, 100 - Kind.Wake + 1, 50).Run();
        Assert.Equal(5, Pack(w).Count);
        Assert.Empty(w.M.Creatures.Nests);
        Assert.All(Pack(w), c => Assert.InRange(Math.Abs(c.X - piece.X) + Math.Abs(c.Y - piece.Y), 1, 24 * Unit));
        Assert.Equal(5, Pack(w).Select(c => (c.X, c.Y)).Distinct().Count());
    }

    [Fact]
    public void AStrikeWakesItFromAfar()
    {
        var w = World();
        var piece = Nested(w, 100, 50);
        piece.Struck = new Blow { By = 0, At = 1 };
        w.Run();
        Assert.Equal(5, Pack(w).Count);
    }

    [Fact]
    public void AStrikeByNoOneWakesItRoundTheNearestHeroOut()
    {
        var w = World();
        var piece = Nested(w, 100, 50);
        piece.Struck = new Blow { By = FloorStack.Nobody, At = 1 };
        w.Run();
        Assert.Equal(5, Pack(w).Count);
    }

    [Fact]
    public void AHeroNotOutWakesNone()
    {
        var map = Space();
        for (int y = 40; y <= 60; y++)
            for (int x = 60; x <= 76; x++)
                map[y][x] = x is 60 or 76 || y is 40 or 60 ? '#' : ' ';
        var w = World(x: 68, map: map);
        var piece = Nested(w, 90, 50);
        piece.Struck = new Blow { By = 0, At = 1 };
        w.Run(5);
        Assert.Empty(Pack(w));
        Hero(w, 80, 50);
        ((CrewState)w.M.Crew.Slice).Heroes[0].Out = 1000;
        w.Run(5);
        Assert.Empty(Pack(w));
        Assert.Single(w.M.Creatures.Nests);
    }

    [Fact]
    public void NoneHatchesAboard()
    {
        var map = Space();
        // A room over the right half of where the pack would come: a wreck drifts through hulls.
        for (int y = 30; y <= 70; y++)
            for (int x = 100; x <= 130; x++)
                map[y][x] = x is 100 or 130 || y is 30 or 70 ? '#' : '.';
        var w = World(x: 75, map: map);
        Nested(w, 100, 50, size: 8);
        w.Run();
        var pack = Pack(w);
        Assert.InRange(pack.Count, 1, 7);
        Assert.All(pack, c => Assert.True(c.X < 100 * Unit, $"one hatched at {c.X / Unit}"));
        Assert.Empty(w.M.Creatures.Nests);
    }

    [Fact]
    public void WithNoFreePlaceItWaits()
    {
        var map = Space();
        for (int y = 38; y <= 62; y++)
            for (int x = 88; x <= 112; x++)
                map[y][x] = '#';
        var w = World(x: 75, map: map);
        Nested(w, 100, 50);
        w.Run(3);
        Assert.Empty(Pack(w));
        Assert.Single(w.M.Creatures.Nests);
    }

    [Fact]
    public void NoneHatchesInAFloor()
    {
        var map = Space();
        // A platform in open space is space to the rooms, and no less solid to a body.
        for (int y = 38; y <= 62; y++)
            for (int x = 88; x <= 112; x++)
                map[y][x] = '=';
        var w = World(x: 75, map: map);
        Nested(w, 100, 50);
        w.Run(3);
        Assert.Empty(Pack(w));
        Assert.Single(w.M.Creatures.Nests);
    }

    [Fact]
    public void ItWaitsWhileTheHerosKeepBoxIsFullOfThem()
    {
        var w = World(x: 80);
        Nested(w, 90, 50, size: 5);
        Nested(w, 95, 50, size: 5);
        w.Run();
        Assert.Equal(5, Pack(w).Count);
        Assert.Single(w.M.Creatures.Nests);
        State(w).Creatures.Clear();
        w.Run();
        Assert.Equal(5, Pack(w).Count);
        Assert.Empty(w.M.Creatures.Nests);
    }

    [Fact]
    public void ItWaitsAtTheSurfacesCap()
    {
        var w = World(x: 80);
        Nested(w, 90, 50, size: 5);
        var s = State(w);
        // Sentries without a wreck would go: catchers count against the cap and no breakers' box.
        for (int i = 0; i < Tune.Max - 4; i++)
        {
            var catcher = new Creature { Id = s.NextId++, Species = Tune.Catcher.Id, X = (5 + i * 5) * Unit + 500, Y = 10 * Unit, SeenAt = s.Clock };
            s.Creatures.Add(catcher);
            HuntDrive.Start(w.M.Creatures, catcher, Tune.Catcher, 0);
        }
        w.Run();
        Assert.Empty(Pack(w));
        Assert.Single(w.M.Creatures.Nests);
    }

    [Fact]
    public void APieceGoneTakesItsNestWithNothingGiven()
    {
        var w = World();
        var piece = Nested(w, 100, 50);
        ((DebrisState)w.M.Debris.Slice).Pieces.Remove(piece);
        w.Run();
        Assert.Empty(w.M.Creatures.Nests);
        Assert.Empty(Pack(w));
    }

    [Fact]
    public void ASaveKeepsItAsleepAndAwake()
    {
        var w = World();
        var piece = Nested(w, 100, 50, size: 6);
        w.Reload();
        var nest = Assert.Single(w.M.Creatures.Nests);
        Assert.Equal((piece.Id, 6), (nest.Piece, nest.Size));
        Hero(w, 80, 50).Run(8);
        Assert.Equal(6, Pack(w).Count);
        ulong hash = w.World.Hash();
        w.Reload();
        Assert.Equal(hash, w.World.Hash());
        Assert.Empty(w.M.Creatures.Nests);
        Assert.Equal(6, Pack(w).Count);
    }

    [Theory]
    [InlineData(4u)]
    [InlineData(22u)]
    public void AWorldLoadedEveryTickPlaysAPackTheSame(uint seed)
    {
        Scenario Seeded()
        {
            var w = World(x: 70, seed: seed);
            Hero(w, 130, 30, peer: 1).Run();
            for (int i = 0; i < 6; i++) Nested(w, 60 + i * 15, 25 + i % 3 * 20);
            return w;
        }
        var live = Seeded();
        var loaded = Seeded();
        var rng = new Rng(new RngState { S = seed });
        bool woke = false, cut = false, killed = false;
        for (int tick = 0; tick < 300; tick++)
        {
            var commands = new List<Command>();
            // The suites' random commands now and then, whatever they do to the two heroes; mostly a fight.
            if (rng.Below(8) == 0) commands.AddRange(ModulesTests.RandomCommands(live.World, rng));
            for (int peer = 0; peer < 2; peer++)
                if (tick % 20 == peer * 10) commands.Add(new Enter((20 + (int)rng.Below(60)) * Width + 50 + (int)rng.Below(100)) { From = peer });
            var all = live.M.Creatures.Creatures;
            var pack = Pack(live);
            if (pack.Count > 0 && rng.Below(5) == 0) commands.Add(new Strike(pack[(int)rng.Below((uint)pack.Count)].Id, Axe) { From = (int)rng.Below(2) });
            if (all.FirstOrDefault(c => c.Posture == Posture.Charge) is { } charging && rng.Below(6) == 0) commands.Add(new HitBy(charging.Id) { From = (int)rng.Below(2) });
            var acting = pack.Where(c => c.Act != null).Select(c => (c.Id, c.Damage)).ToList();
            var before = pack.Select(c => c.Id).ToList();
            loaded.Reload();
            foreach (var command in commands)
            {
                live.Do(command);
                loaded.Do(command);
            }
            live.Run();
            loaded.Run();
            Assert.Equal(live.World.Hash(), loaded.World.Hash());
            var after = Pack(live);
            woke |= after.Any(c => !before.Contains(c.Id));
            cut |= after.Any(c => c.Act == null && acting.Any(a => a.Id == c.Id && a.Damage < c.Damage));
            killed |= commands.OfType<Strike>().Any(s => before.Contains(s.Creature) && after.All(c => c.Id != s.Creature));
        }
        Assert.True(woke && cut && killed, $"woke {woke}, cut {cut}, killed {killed}: the run reached too little");
    }

    [Fact]
    public void AWorldLoadedEveryTickDrawsTheSameNests()
    {
        var (live, loaded) = (World(x: 4, seed: 7), World(x: 4, seed: 7));
        bool nested = false;
        for (int tick = 0; tick < 80; tick++)
        {
            foreach (var w in new[] { live, loaded }) Out(w, Kind.Kinds[tick % Kind.Kinds.Count], 150 + tick % 30, 50);
            loaded.Reload();
            live.Run();
            loaded.Run();
            Assert.Equal(live.World.Hash(), loaded.World.Hash());
            nested |= live.M.Creatures.Nests.Count > 0;
        }
        Assert.True(nested, "none drew a nest");
    }

    public sealed record Field(int Ground, int Nests, int Crowd, int Heroes, int Ticks);

    private static Gen<Field> Fields() =>
        from ground in Gen.Choose(0, int.MaxValue)
        from nests in Gen.Choose(1, 30)
        from crowd in Gen.OneOf(Gen.Constant(0), Gen.Choose(Tune.Max - 8, Tune.Max))
        from heroes in Gen.Choose(1, 4)
        from ticks in Gen.Choose(20, 120)
        select new Field(ground, nests, crowd, heroes, ticks);

    [Property(MaxTest = 40)]
    public Property NeverPastTheCapNorInWhatIsSolidAndIdsOnlyGrow() => Prop.ForAll(Arb.From(Fields()), field =>
    {
        var rng = new Rng(new RngState { S = (uint)field.Ground | 1 });
        var map = Space();
        for (int i = 0; i < 40; i++)
        {
            int left = (int)rng.Below(Width - 12), top = (int)rng.Below(Height - 12);
            int wide = 1 + (int)rng.Below(10), high = 1 + (int)rng.Below(10);
            for (int y = top; y < top + high; y++)
                for (int x = left; x < left + wide; x++)
                    map[y][x] = i % 3 == 0 ? '=' : '#';
        }
        var w = World(map: map, seed: (uint)field.Ground | 1);
        // Catchers out already, up to the surface's cap and in no pack's box: nests alone, eight a box, never come near it.
        for (int tries = 0, put = 0; put < field.Crowd && tries < 20 * Tune.Max; tries++)
        {
            int x = 1 + (int)rng.Below(Width - 2), y = 1 + (int)rng.Below(Height - 3);
            int atX = x * Unit + Unit / 2, atY = (y + 2) * Unit;
            if (w.M.Creatures.Overlaps(Tune.Catcher.Body, atX, atY)) continue;
            var catcher = new Creature { Id = State(w).NextId++, Species = Tune.Catcher.Id, X = atX, Y = atY, SeenAt = State(w).Clock };
            State(w).Creatures.Add(catcher);
            HuntDrive.Start(w.M.Creatures, catcher, Tune.Catcher, 0);
            put++;
        }
        for (int i = 0; i < field.Nests; i++)
            Nested(w, (int)rng.Below(Width), (int)rng.Below(Height), Kind.Least + (int)rng.Below((uint)(Kind.Most - Kind.Least + 1)), Kind.Kinds[i % Kind.Kinds.Count]);
        for (int peer = 0; peer < field.Heroes; peer++) Hero(w, (int)rng.Below(Width), (int)rng.Below(Height), peer);
        for (int tick = 0; tick < field.Ticks; tick++)
        {
            if (tick % 15 == 14) Hero(w, (int)rng.Below(Width), (int)rng.Below(Height), (int)rng.Below((uint)field.Heroes));
            w.Run();
            var all = w.M.Creatures.Creatures;
            if (all.Count > Tune.Max) return false.Label($"{all.Count} creatures");
            for (int i = 0; i < all.Count; i++)
            {
                if (i > 0 && all[i - 1].Id >= all[i].Id) return false.Label($"ids {all[i - 1].Id} then {all[i].Id}");
                // A catcher leaving its hunt flies through anything (CreaturesInvariantTests).
                if (all[i].Species == Tune.Breaker.Id && w.M.Creatures.Overlaps(Tune.Breaker.Body, all[i].X, all[i].Y)) return false.Label($"{all[i].Id} in what is solid");
            }
        }
        return true.Label("fine");
    });
}
