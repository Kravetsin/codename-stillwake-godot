using System.Text.Json.Nodes;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Stillwake.Sim.Crew;
using Stillwake.Sim.Debris;
using Xunit;
using DebrisSpec = Stillwake.Content.Debris;
using Harpoon = Stillwake.Content.Harpoon;
using WreckageSpec = Stillwake.Content.Wreckage;

namespace Stillwake.Sim.Tests;

/// <summary>The harpoons' lines and the host player's feed (debris-physics): hooks, winding, letting go, drops, and <see cref="Moved"/>.</summary>
public class LinesTests
{
    private const int Width = 80, Height = 40, Unit = 1000;

    /// <summary>A sealed room in the top-left corner, ten cells across; space everywhere else, a cell a character.</summary>
    private static readonly Layout Map = Layout.Parse(Enumerable.Range(0, Height).Select(y => (y switch
    {
        0 or 9 => "##########",
        < 9 => "#........#",
        _ => "",
    }).PadRight(Width)).ToArray());

    private static int At(int x, int y) => y * Width + x;

    /// <summary>A cell inside the room.</summary>
    private static readonly int Room = At(4, 7);

    /// <summary>A world of <see cref="Map"/> whose stream spawns nothing, peer 0's hero out in space at (25, 20).</summary>
    private static Scenario Out()
    {
        var w = new Scenario(Map);
        foreach (var beat in State(w).Beats) beat.Next = int.MaxValue;
        return w.Do(new Enter(At(25, 20))).Run();
    }

    private static DebrisState State(Scenario w) => (DebrisState)w.M.Debris.Slice;

    /// <summary>A piece of <paramref name="item"/> put out by hand in the middle of cell (<paramref name="x"/>, <paramref name="y"/>), moving <paramref name="vx"/>, <paramref name="vy"/> a tick.</summary>
    private static Piece Put(Scenario w, int x, int y, string item = "scrapChunk", int vx = 0, int vy = 0, string? zone = null)
    {
        var s = State(w);
        var piece = new Piece
        {
            Id = s.NextId++,
            Zone = zone ?? DebrisSpec.Ship.Id,
            Item = item,
            X = x * Unit + Unit / 2,
            Y = y * Unit + Unit / 2,
            Vx = vx,
            Vy = vy,
            Speed = Math.Max(0, -vx),
            Start = x * Unit + Unit / 2,
            End = 0,
            SeenAt = s.Clock,
        };
        s.Pieces.Add(piece);
        return piece;
    }

    private static Scenario Hooked(Scenario w, Piece piece, int peer = 0) => w.Do(new Hook(piece.Id, 0, 0) { From = peer }).Run();

    private static IDebrisOps Ops(Scenario w) => w.M.Debris;

    /// <summary>
    /// What breaks the lines' invariants, or null: at most one line a hero and a piece, each on a live piece and held by a hero, the
    /// lookups agreeing with the list.
    /// </summary>
    internal static string? Broken(Scenario w)
    {
        var debris = w.M.Debris;
        var lines = debris.Lines;
        if (lines.Select(l => l.By).Distinct().Count() != lines.Count) return "a hero holds two lines";
        if (lines.Select(l => l.Piece).Distinct().Count() != lines.Count) return "a piece on two lines";
        foreach (var line in lines)
        {
            if (debris.PieceById(line.Piece) == null) return $"a line on piece {line.Piece}, gone";
            if (w.M.Crew.HeroOf(line.By) == null) return $"a line held by {line.By}, who has no hero";
            if (debris.LineOf(line.By) != line || debris.LineOn(line.Piece) != line) return $"the lookups lost {line.By}'s line on {line.Piece}";
            if (line.Length < 0) return $"{line.By}'s line {line.Length} long";
        }
        foreach (var piece in debris.Pieces)
            if (debris.LineOn(piece.Id) is { } on && !lines.Contains(on)) return $"a lookup holds a dropped line on {piece.Id}";
        return null;
    }

    // Hook.

    [Fact]
    public void AHookIsClampedIntoASmallPiecesCellAndAsLongAsTheWayFromTheHeroesCell()
    {
        var w = Out();
        var piece = Put(w, 20, 20);
        w.Do(new Hook(piece.Id, 3_000, -4_000)).Run();
        Assert.Empty(w.Refusals);
        var line = Assert.Single(w.M.Debris.Lines);
        Assert.Equal((0, piece.Id, 500, -500, false), (line.By, line.Piece, line.HookX, line.HookY, line.Winds));
        // From (25.5, 20.5) to (21, 20).
        Assert.Equal(TurnedBox.Root(4_500L * 4_500 + 500 * 500), line.Length);
        Assert.Same(line, w.M.Debris.LineOf(0));
        Assert.Same(line, w.M.Debris.LineOn(piece.Id));
    }

    [Fact]
    public void AHookIsClampedIntoAWrecksBoxAndALineIsNoLongerThanTheRange()
    {
        var w = Out();
        var hold = Put(w, 70, 25, "cargoHold");
        var box = DebrisModule.ShapeOf(hold)!.Value;
        w.Do(new Hook(hold.Id, 100_000, -100_000)).Run();
        var line = Assert.Single(w.M.Debris.Lines);
        Assert.Equal((box.HalfW, -box.HalfH), (line.HookX, line.HookY));
        Assert.Equal(Harpoon.Range, line.Length);
    }

    [Fact]
    public void AHookIsRefusedOnAPieceGoneOrFromARoom()
    {
        var w = Out();
        var piece = Put(w, 20, 20);
        w.Do(new Hook(piece.Id + 7, 0, 0)).Run();
        w.Do(new Enter(Room)).Run().Do(new Hook(piece.Id, 0, 0)).Run();
        Assert.Equal(["debris.gone", "debris.notOutside"], w.Refusals.Select(r => r.Reason));
        Assert.Empty(w.M.Debris.Lines);
    }

    [Fact]
    public void AHookReplacesTheSendersOwnLine()
    {
        var w = Out();
        var (first, second) = (Put(w, 20, 20), Put(w, 20, 24));
        Hooked(Hooked(w, first), second);
        Assert.Equal(second.Id, Assert.Single(w.M.Debris.Lines).Piece);
        Assert.Null(w.M.Debris.LineOn(first.Id));
    }

    [Fact]
    public void TheLastHookOnAPieceWinsAndTheOtherHeroesLineDrops()
    {
        var w = Out().Do(new Enter(At(30, 20)) { From = 1 }).Run();
        var piece = Put(w, 20, 20);
        Hooked(Hooked(w, piece), piece, peer: 1);
        Assert.Equal(1, Assert.Single(w.M.Debris.Lines).By);
        Assert.Null(w.M.Debris.LineOf(0));
        // On one tick the later wins too.
        w.Do(new Hook(piece.Id, 0, 0)).Do(new Hook(piece.Id, 0, 0) { From = 1 }).Run();
        Assert.Equal(1, Assert.Single(w.M.Debris.Lines).By);
    }

    // Wind and let go.

    [Fact]
    public void WindWindsTheSendersLineAndStops()
    {
        var w = Out();
        var piece = Put(w, 20, 20);
        Hooked(w, piece).Do(new Wind(true)).Run();
        Assert.True(w.M.Debris.LineOf(0)!.Winds);
        w.Do(new Wind(false)).Run();
        Assert.False(w.M.Debris.LineOf(0)!.Winds);
        Assert.Empty(w.Refusals);
    }

    [Fact]
    public void LetGoDropsTheSendersLine()
    {
        var w = Out();
        var piece = Put(w, 20, 20);
        Hooked(w, piece).Do(new LetGo()).Run();
        Assert.Empty(w.Refusals);
        Assert.Empty(w.M.Debris.Lines);
        Assert.NotNull(w.M.Debris.PieceById(piece.Id));
    }

    [Fact]
    public void WindAndLetGoAreRefusedWithNoLine()
    {
        var w = Out();
        Hooked(w, Put(w, 20, 20));
        w.Do(new Wind(true) { From = 1 }).Do(new LetGo() { From = 1 }).Run();
        Assert.Equal(["debris.noLine", "debris.noLine"], w.Refusals.Select(r => r.Reason));
        Assert.Single(w.M.Debris.Lines);
    }

    // Drops.

    [Fact]
    public void ALineDropsWhenItsHolderLeaves()
    {
        var w = Out();
        Hooked(w, Put(w, 20, 20)).Do(new Leave()).Run();
        Assert.Empty(w.M.Debris.Lines);
    }

    [Fact]
    public void ALineDropsWhenItsHolderPassesOut()
    {
        var w = Out();
        Hooked(w, Put(w, 20, 20));
        w.M.Crew.HeroOf(0)!.Out = 100;
        w.Run();
        Assert.Empty(w.M.Debris.Lines);
    }

    [Fact]
    public void ALineDropsWhenItsHolderIsReportedInARoom()
    {
        var w = Out();
        Hooked(w, Put(w, 20, 20)).Do(new Enter(Room)).Run();
        Assert.Empty(w.M.Debris.Lines);
    }

    [Fact]
    public void ALineDropsWithItsPieceGrabbed()
    {
        var w = Out();
        var piece = Put(w, 20, 20);
        Hooked(w, piece).Do(new Grab(piece.Id)).Run();
        Assert.Empty(w.Refusals);
        Assert.Null(w.M.Debris.PieceById(piece.Id));
        Assert.Empty(w.M.Debris.Lines);
    }

    [Fact]
    public void ALineDropsWithItsWreckBroken()
    {
        var w = Out();
        var hold = Put(w, 40, 25, "cargoHold");
        Hooked(w, hold);
        Assert.True(Ops(w).Hit(0, hold.Id, WreckageSpec.HpOf(hold.Item)!.Value));
        Assert.Null(w.M.Debris.PieceById(hold.Id));
        Assert.Empty(w.M.Debris.Lines);
    }

    [Fact]
    public void ALineDropsWithItsPieceCaught()
    {
        var w = Out();
        var piece = Put(w, 20, 20);
        Hooked(w, piece);
        Assert.NotNull(Ops(w).Catch(piece.Id));
        Assert.Empty(w.M.Debris.Lines);
    }

    [Fact]
    public void ALineDropsWhenASentryCutsIt()
    {
        var w = Out();
        var piece = Put(w, 20, 20);
        Hooked(w, piece);
        Ops(w).Unreel(piece.Id);
        Assert.Empty(w.M.Debris.Lines);
        Assert.NotNull(w.M.Debris.PieceById(piece.Id));
    }

    [Fact]
    public void APieceOffTheGridOnAnySideIsGoneItsLineWithIt()
    {
        var w = Out();
        var held = Put(w, 0, 20, vx: -600);
        Hooked(w, held);
        Assert.Null(w.M.Debris.PieceById(held.Id));
        Assert.Empty(w.M.Debris.Lines);
        Piece[] gone = [Put(w, Width - 1, 20, vx: 600), Put(w, 30, 0, vy: -600), Put(w, 30, Height - 1, vy: 600)];
        var stays = Put(w, 30, 30, vx: 400, vy: -400);
        w.Run();
        Assert.All(gone, p => Assert.Null(w.M.Debris.PieceById(p.Id)));
        Assert.Equal((30_900, 30_100), (stays.X, stays.Y));
    }

    // The far field.

    [Fact]
    public void TheFarFieldKeepsAHeldPieceItWouldLeaveBehind()
    {
        var around = (Content.AroundHeroes)DebrisSpec.Far.Anchor;
        // Space wide enough to leave a piece out of the hero's keep box but inside its hard one.
        int wide = around.Keep.X + 20;
        var w = new Scenario(Layout.Parse([.. Enumerable.Repeat(new string(' ', wide), 30)])).Do(new Enter(5 + 15 * wide)).Run();
        // Out of every hero's keep box: the line holds one, the other lingers and goes.
        var held = Put(w, around.Keep.X + 10, 15, zone: DebrisSpec.Far.Id);
        var loose = Put(w, around.Keep.X + 12, 15, zone: DebrisSpec.Far.Id);
        Hooked(w, held);
        Assert.Empty(w.Refusals);
        w.Run(around.Linger + 2);
        Assert.NotNull(w.M.Debris.PieceById(held.Id));
        Assert.Null(w.M.Debris.PieceById(loose.Id));
    }

    // Moved.

    [Fact]
    public void AFedPoseIsSetAndCarriedOnFromItsTick()
    {
        var w = Out();
        var piece = Put(w, 20, 20, vx: -100);
        int clock = w.M.Debris.Clock;
        w.Do(new Moved(clock - 2, [new Body(piece.Id, 30_000, 12_000, 50, -20, 1_000, 30)], [])).Run();
        Assert.Empty(w.Refusals);
        // Two ticks carried, then this tick's own drift.
        Assert.Equal((30_150, 11_940, 50, -20, 1_090, 30), (piece.X, piece.Y, piece.Vx, piece.Vy, piece.Angle, piece.Spin));
        w.Run();
        Assert.Equal((30_200, 11_920), (piece.X, piece.Y));
    }

    [Fact]
    public void AFeedSkipsAPieceGoneAndALineNoLongerThere()
    {
        var w = Out().Do(new Enter(At(30, 20)) { From = 1 }).Run();
        var piece = Put(w, 20, 20);
        Hooked(w, piece);
        int length = w.M.Debris.LineOf(0)!.Length;
        w.Do(new Moved(w.M.Debris.Clock, [new Body(piece.Id + 9, 1_000, 1_000, 0, 0, 0, 0), new Body(piece.Id, 21_000, 20_500, 0, 0, 0, 0)],
            [new LineLength(1, piece.Id, 7_000), new LineLength(0, piece.Id + 9, 7_000)])).Run();
        Assert.Empty(w.Refusals);
        Assert.Equal(21_000, piece.X);
        Assert.Equal(length, w.M.Debris.LineOf(0)!.Length);
        w.Do(new Moved(w.M.Debris.Clock, [], [new LineLength(0, piece.Id, 7_000)])).Run();
        Assert.Equal(7_000, w.M.Debris.LineOf(0)!.Length);
    }

    [Fact]
    public void AFeedCarryingAPieceOffTheGridTakesItAndItsLine()
    {
        var w = Out();
        var piece = Put(w, 20, 20);
        Hooked(w, piece);
        w.Do(new Moved(w.M.Debris.Clock - 3, [new Body(piece.Id, 500, 20_500, -1_000, 0, 0, 0)], [])).Run();
        Assert.Empty(w.Refusals);
        Assert.Null(w.M.Debris.PieceById(piece.Id));
        Assert.Empty(w.M.Debris.Lines);
    }

    [Fact]
    public void AFeedFromTheHostPlayerOnAPlanetMovesTheShipsPieces()
    {
        var w = Out();
        var piece = Put(w, 20, 20);
        w.Do(new Stillwake.Sim.OpenSurface(Content.Surfaces.TestStrip.Id)).Run();
        w.Do(new Transfer(w.World.Surfaces[^1].Id)).Run();
        Assert.NotEqual(DebrisModule.BodiesOn, w.World.SurfaceOf(DebrisModule.HostPlayer));
        // As its client sends it: addressed to the ship, though its hero stands on the strip.
        w.Do(new Moved(w.M.Debris.Clock, [new Body(piece.Id, 30_000, 12_000, 0, 0, 0, 0)], []) { Surface = DebrisModule.BodiesOn }).Run();
        Assert.Empty(w.Refusals);
        Assert.Equal((30_000, 12_000), (piece.X, piece.Y));
    }

    [Fact]
    public void AFeedFromAGuestIsRefused()
    {
        var w = Out().Do(new Enter(At(30, 20)) { From = 1 }).Run();
        var piece = Put(w, 20, 20);
        w.Do(new Moved(w.M.Debris.Clock, [new Body(piece.Id, 30_000, 12_000, 0, 0, 0, 0)], []) { From = 1 }).Run();
        Assert.Equal(["debris.notHost"], w.Refusals.Select(r => r.Reason));
        Assert.Equal(20_500, piece.X);
    }

    // Saves.

    [Fact]
    public void ALineIsSavedAndItsLookupsRebuiltOnLoad()
    {
        var w = Out();
        var piece = Put(w, 20, 20);
        Hooked(w, piece).Do(new Wind(true)).Run();
        var line = w.M.Debris.LineOf(0)!;
        w.Reload();
        var loaded = w.M.Debris.LineOn(piece.Id)!;
        Assert.Same(loaded, w.M.Debris.LineOf(0));
        Assert.Equal((line.By, line.Piece, line.HookX, line.HookY, line.Length, true), (loaded.By, loaded.Piece, loaded.HookX, loaded.HookY, loaded.Length, loaded.Winds));
    }

    [Fact]
    public void AFormatSeventyThreeSaveGivesEveryPieceItsDriftAndDropsItsReels()
    {
        var old = JsonNode.Parse(
            """
            {"slices":{"debris":{"pieces":[{"id":1,"speed":110,"reel":{"by":0,"toX":1,"toY":2,"left":3}},{"id":2,"speed":0}]}},
             "surfaces":[{"slices":{"debris":{"pieces":[{"id":4,"speed":90}]}}},{"closed":true}]}
            """)!.AsObject();
        var upgraded = Migrations.Steps[73 - Migrations.Oldest](old);
        var ship = upgraded["slices"]!["debris"]!["pieces"]!.AsArray();
        Assert.Equal([(-110, 0, false), (0, 0, false)], ship.Select(p => (p!["vx"]!.GetValue<int>(), p["vy"]!.GetValue<int>(), p.AsObject().ContainsKey("reel"))));
        Assert.Equal(-90, upgraded["surfaces"]![0]!["slices"]!["debris"]!["pieces"]![0]!["vx"]!.GetValue<int>());

        // The wreck played at format 73: every piece drifts as it did, no line out.
        var m = new Registry();
        World.Load(Migrations.Upgrade(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "format-73.json"))), m.All);
        Assert.NotEmpty(m.Debris.Pieces);
        Assert.All(m.Debris.Pieces, p => Assert.Equal((-p.Speed, 0), (p.Vx, p.Vy)));
        Assert.Empty(m.Debris.Lines);
    }

    // The invariants.

    private sealed record Step(int Kind, int Peer, int A, int B, int Ticks);

    [Property(MaxTest = 80)]
    public Property AtMostOneLineAHeroAndAPieceEachOnALivePieceAndEveryPieceOnTheGrid()
    {
        var step = from kind in Gen.Choose(0, 11)
                   from peer in Gen.Choose(0, 2)
                   from a in Gen.Choose(-3_000, 90_000)
                   from b in Gen.Choose(-3_000, 50_000)
                   from ticks in Gen.Frequency((4, Gen.Choose(1, 5)), (1, Gen.Choose(20, 200)))
                   select new Step(kind, peer, a, b, ticks);
        var cases = from seed in Gen.Choose(1, 10_000) from steps in step.ArrayOf() select ((uint)seed, steps);
        return Prop.ForAll(cases.ToArbitrary(), c =>
        {
            var w = new Scenario(Map, c.Item1);
            foreach (var s in c.steps)
            {
                var pieces = w.M.Debris.Pieces;
                int id = pieces.Count > 0 ? pieces[Math.Abs(s.A) % pieces.Count].Id : s.A % 5;
                switch (s.Kind)
                {
                    case 0: w.Do(new Enter(Math.Abs(s.A) % (Width * Height)) { From = s.Peer }); break;
                    case 1: w.Do(new Leave { From = s.Peer }); break;
                    case 2: case 3: w.Do(new Hook(id, s.A % 4_000, s.B % 4_000) { From = s.Peer }); break;
                    case 4: w.Do(new Wind(s.B % 2 == 0) { From = s.Peer }); break;
                    case 5: w.Do(new LetGo { From = s.Peer }); break;
                    case 6:
                        var lines = w.M.Debris.Lines.Select(l => new LineLength(l.By, l.Piece, s.B)).Append(new LineLength(s.Peer, id, s.A)).ToList();
                        w.Do(new Moved(w.M.Debris.Clock - s.Ticks % 4, [new Body(id, s.A, s.B, s.B % 1_500 - 750, s.A % 1_500 - 750, s.A, s.B % 200 - 100)], lines) { From = s.Peer % 2 == 0 ? 0 : s.Peer });
                        break;
                    case 7: w.Do(new Grab(id) { From = s.Peer }); break;
                    case 8: if (w.M.Debris.PieceById(id) is { } hit && WreckageSpec.HpOf(hit.Item) is { } hp) Ops(w).Hit(s.Peer, id, hp / 2 + 1); break;
                    case 9: Ops(w).Unreel(id); break;
                    case 10: if (w.M.Debris.PieceById(id) != null) Ops(w).Catch(id); break;
                }
                if (Broken(w) is { } before) return false.Label($"before the tick: {before}");
                for (int t = 0; t < (s.Kind == 11 ? s.Ticks : 1); t++)
                {
                    w.Run();
                    if (Broken(w) is { } broken) return false.Label(broken);
                    foreach (var p in w.M.Debris.Pieces)
                        if (p.X < 0 || p.X >= Width * Unit || p.Y < 0 || p.Y >= Height * Unit) return false.Label($"piece {p.Id} off the grid at ({p.X}, {p.Y})");
                }
            }
            return true.ToProperty();
        });
    }
}
