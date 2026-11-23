using Stillwake.Game.Features.Crew;
using Stillwake.Sim;
using Stillwake.Sim.Debris;
using Xunit;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Tests;

/// <summary>A thrown weapon (companion-fights): two legs, how often each strikes and rests, cells out only, nothing past a wall, and what ends it.</summary>
public class WeaponThrowTests
{
    private const int Unit = Hero.Unit, Wall = 20;
    private const double Frame = 1000 / 60.0, PaceMs = 500;
    private static readonly Spec.Abilities.Throw Axe = Spec.Abilities.Axe;
    private static readonly ThrowHands Hands = new(Axe.Item, Free: true, Held: false, Surface: 0);
    private static readonly Func<Piece, double, double, bool> Opaque = (p, x, y) =>
        Math.Abs(x - p.X) <= Aiming.PictureOf(p.Item).HalfW && Math.Abs(y - p.Y) <= Aiming.PictureOf(p.Item).HalfH;

    /// <summary>64 cells by 24 of open space, a wall at 20 and 21 down all of it, a still hull section at each of <paramref name="wrecksAt"/>, row 12.</summary>
    private static TestWorld Space(params int[] wrecksAt)
    {
        string row = new string(' ', Wall / 2) + "#" + new string(' ', 31 - Wall / 2);
        var w = new TestWorld(Layout.Parse([.. Enumerable.Repeat(row, 12)]).Doubled(), 1);
        for (int i = 0; i < 4 * Spec.Debris.Every && w.M.Debris.Pieces.Count == 0; i++) w.Step();
        w.Edit(root =>
        {
            var pieces = root["slices"]!["debris"]!["pieces"]!.AsArray();
            var made = pieces[0]!.DeepClone();
            pieces.Clear();
            for (int i = 0; i < wrecksAt.Length; i++)
            {
                var piece = made.DeepClone();
                piece["id"] = i + 1;
                piece["item"] = "hullSection";
                (piece["speed"], piece["vx"], piece["vy"]) = (0, 0, 0);
                piece["x"] = wrecksAt[i] * Unit;
                piece["y"] = 12 * Unit;
                pieces.Add(piece);
            }
        });
        return w;
    }

    /// <summary>A hero whose hand is on row 12 of column <paramref name="x"/>.</summary>
    private static Hero At(int x)
    {
        var hero = new Hero(0, 0, 0, 0, false, 1, null);
        var hand = Aiming.HandOf(hero);
        return hero with { X = x * Unit - hand.X, Y = 12 * Unit + 500 - hand.Y };
    }

    private static WeaponThrow Thrown(Hero from, int toX, Spec.Abilities.Throw? spec = null)
    {
        var thrown = new WeaponThrow();
        Assert.True(thrown.Begin(Hands with { Wielded = (spec ?? Axe).Item }, PaceMs));
        Assert.True(thrown.Leave(from, new Point(toX * Unit, 12 * Unit + 500)));
        return thrown;
    }

    private static List<(ThrowLeg Leg, EdgeHit Hit)> Flown(TestWorld w, WeaponThrow thrown, Hero hero, Func<ThrowLeg, Hero>? later = null)
    {
        var hits = new List<(ThrowLeg, EdgeHit)>();
        for (int i = 0; i < 300 && thrown.Flying; i++)
        {
            var leg = thrown.Leg;
            if (thrown.Step(Frame, w.M, later?.Invoke(leg) ?? hero, outside: true, 0, Opaque) is { } hit) hits.Add((leg, hit));
        }
        Assert.False(thrown.Flying);
        return hits;
    }

    [Fact]
    public void ItGoesItsRangeAndComesHome()
    {
        var w = Space();
        var hero = At(30);
        var thrown = Thrown(hero, 60);
        double farthest = 0;
        for (int i = 0; i < 300 && thrown.Flying; i++)
        {
            thrown.Step(Frame, w.M, hero, true, 0, Opaque);
            farthest = Math.Max(farthest, thrown.At.X);
        }
        Assert.Equal(Aiming.HandOf(hero).X + Axe.Range, farthest, 1.0);
        Assert.Equal(Aiming.HandOf(hero).X, thrown.At.X, 1.0);
    }

    [Fact]
    public void ItSlowsOutAndGathersSpeedHome()
    {
        var w = Space();
        var hero = At(30);
        var thrown = Thrown(hero, 60);
        var moved = new List<(ThrowLeg Leg, double Way)>();
        while (thrown.Flying && moved.Count < 300)
        {
            var (leg, was) = (thrown.Leg, thrown.At.X);
            thrown.Step(Frame, w.M, hero, true, 0, Opaque);
            moved.Add((leg, Math.Abs(thrown.At.X - was)));
        }
        var away = moved.Where(s => s.Leg == ThrowLeg.Out).Select(s => s.Way).ToList();
        var home = moved.Where(s => s.Leg == ThrowLeg.Home).Select(s => s.Way).ToList();
        Assert.True(away[0] > 4 * away[^2]);
        Assert.True(home[^2] > 4 * home[1]);
    }

    [Fact]
    public void TheThreadHoldsItByTheTimeItStops()
    {
        var w = Space();
        var hero = At(30);
        var thrown = Thrown(hero, 60);
        double hauled = 0;
        for (int i = 0; i < 300 && thrown.Leg == ThrowLeg.Out; i++)
        {
            if (thrown.Hauled) hauled += Frame;
            thrown.Step(Frame, w.M, hero, true, 0, Opaque);
        }
        Assert.InRange(hauled, WeaponThrow.CastMs - 2 * Frame, WeaponThrow.CastMs + Frame);
    }

    [Fact]
    public void ItCarriesTheSpeedOfItsHero()
    {
        var w = Space();
        Hero still = At(30), flying = still with { Vx = 300, Vy = -100 };
        var (standing, carried) = (Thrown(still, 60), Thrown(flying, 60));
        standing.Step(Frame, w.M, still, true, 0, Opaque);
        carried.Step(Frame, w.M, flying, true, 0, Opaque);
        double steps = Frame / Hero.StepMs;
        Assert.Equal(300 * steps, carried.At.X - standing.At.X, 1.0);
        Assert.Equal(-100 * steps, carried.At.Y - standing.At.Y, 1.0);
    }

    [Fact]
    public void ItCurvesHomeAndIsStillCaught()
    {
        var w = Space();
        var thrower = At(30);
        var stopped = thrower with { Vy = 0 };
        var rising = thrower with { Vy = -250 };
        var thrown = Thrown(rising, 60);
        for (int i = 0; i < 300 && thrown.Leg == ThrowLeg.Out; i++) thrown.Step(Frame, w.M, rising, true, 0, Opaque);
        // Its hero stopped dead: the speed it left with still carries it up a little before the thread turns it.
        double top = thrown.At.Y;
        for (int i = 0; i < 3; i++) thrown.Step(Frame, w.M, stopped, true, 0, Opaque);
        Assert.True(thrown.At.Y < top);
        for (int i = 0; i < 300 && thrown.Flying; i++) thrown.Step(Frame, w.M, stopped, true, 0, Opaque);
        Assert.False(thrown.Flying);
        Assert.Equal(Aiming.HandOf(stopped).Y, thrown.At.Y, 1.0);
    }

    [Fact]
    public void AWallStopsItAndOnlyTheWayOutCuts()
    {
        var w = Space(26);
        var hero = At(6);
        var thrown = Thrown(hero, 60);
        var hits = Flown(w, thrown, hero);
        int wall = 12 * w.M.Grid.Width + Wall;
        Assert.Equal([wall], hits.Where(h => h.Leg == ThrowLeg.Out).SelectMany(h => h.Hit.Cells));
        Assert.Empty(hits.Where(h => h.Leg == ThrowLeg.Home).SelectMany(h => h.Hit.Cells));
        Assert.Empty(hits.SelectMany(h => h.Hit.Wrecks));
    }

    public static TheoryData<string> AllThrown() => [.. Spec.Abilities.Throws.Select(t => t.Item)];

    public static TheoryData<string> Once() => [.. Spec.Abilities.Throws.Where(t => !t.Twice).Select(t => t.Item)];

    [Theory]
    [MemberData(nameof(AllThrown))]
    public void EachThrownWeaponIsStruckWithGoesItsRangeAndComesHome(string item)
    {
        Assert.NotNull(Spec.Weapons.Of(item));
        Assert.NotNull(Strokes.Of(item));
        var spec = Spec.Abilities.ThrowOf(item)!;
        var w = Space();
        var hero = At(30);
        var thrown = Thrown(hero, 60, spec);
        double farthest = 0, ms = 0;
        for (; ms < 2000 && thrown.Flying; ms += Frame)
        {
            thrown.Step(Frame, w.M, hero, true, 0, Opaque);
            farthest = Math.Max(farthest, thrown.At.X);
        }
        Assert.False(thrown.Flying);
        Assert.Equal(Aiming.HandOf(hero).X + spec.Range, farthest, 1.0);
    }

    [Fact]
    public void TheAxeStrikesBothWays()
    {
        var w = Space(38);
        var hero = At(30);
        var hits = Flown(w, Thrown(hero, 60), hero);
        Assert.Equal([ThrowLeg.Out, ThrowLeg.Home], hits.Where(h => h.Hit.Wrecks.Count > 0).Select(h => h.Leg));
    }

    /// <summary>Open space to the wall, a breaker still with its middle on row 12 of column <paramref name="x"/>.</summary>
    private static TestWorld SpaceWithBreaker(int x) => Space().Edit(root =>
    {
        var one = new System.Text.Json.Nodes.JsonObject
        {
            ["id"] = 1,
            ["species"] = Spec.Creatures.Breaker.Id,
            ["x"] = x * Unit + 500,
            ["y"] = 13 * Unit + 500,
        };
        root["slices"]!["creatures"]!["creatures"] = new System.Text.Json.Nodes.JsonArray(one);
    });

    [Theory]
    [MemberData(nameof(AllThrown))]
    public void ItStrikesACreatureInItsPathEachWayItStrikes(string item)
    {
        var spec = Spec.Abilities.ThrowOf(item)!;
        var w = SpaceWithBreaker(38);
        var hero = At(30);
        ThrowLeg[] legs = spec.Twice ? [ThrowLeg.Out, ThrowLeg.Home] : [ThrowLeg.Out];
        Assert.Equal(legs, Flown(w, Thrown(hero, 60, spec), hero).Where(h => h.Hit.Creatures.Count > 0).Select(h => h.Leg));
    }

    [Fact]
    public void ItStrikesNoCreaturePastAWall()
    {
        var w = SpaceWithBreaker(26);
        var hero = At(6);
        Assert.Empty(Flown(w, Thrown(hero, 60), hero).SelectMany(h => h.Hit.Creatures));
    }

    [Fact]
    public void WhatItTurnsInIsYankedThroughNotHitTwiceAtOnce()
    {
        var w = Space(48);
        var hero = At(30);
        var thrown = Thrown(hero, 60);
        var struck = new List<(double Ms, ThrowLeg Leg, double X)>();
        double turned = 0;
        for (double ms = 0; ms < 2000 && thrown.Flying; ms += Frame)
        {
            var leg = thrown.Leg;
            var hit = thrown.Step(Frame, w.M, hero, true, 0, Opaque);
            if (leg == ThrowLeg.Out && thrown.Leg == ThrowLeg.Home) turned = thrown.At.X;
            if (hit is { Wrecks.Count: > 0 }) struck.Add((ms, leg, thrown.At.X));
        }
        Assert.Equal([ThrowLeg.Out, ThrowLeg.Home], struck.Select(s => s.Leg));
        Assert.True(struck[1].Ms - struck[0].Ms > WeaponThrow.CastMs / 2);
        // Not where it turned, but half a cell along the yank.
        Assert.True(turned - struck[1].X >= 500, $"again {turned - struck[1].X} from the turn");
    }

    [Fact]
    public void AHeroMovedAcrossTheWorldLeavesNoThrowBehindIt()
    {
        var w = Space();
        var hero = At(30);
        var thrown = Thrown(hero, 60);
        thrown.Step(Frame, w.M, hero, true, 0, Opaque);
        // A seam of the fog, crossed, moves its hero by the planet's period.
        var moved = hero with { X = hero.X + 3000 * Unit };
        Assert.Null(thrown.Step(Frame, w.M, moved, true, 0, Opaque));
        Assert.False(thrown.Flying);
    }

    public static TheoryData<string, int, int> PointBlank()
    {
        var data = new TheoryData<string, int, int>();
        foreach (var spec in Spec.Abilities.Throws)
            foreach (int cells in new[] { 1, 2, 3, 6 })
                foreach (int fps in new[] { 30, 60, 144 }) data.Add(spec.Item, cells, fps);
        return data;
    }

    [Theory]
    [MemberData(nameof(PointBlank))]
    public void AWallPointBlankKnocksItNoFurtherThanTheHand(string item, int cells, int fps)
    {
        var w = Space();
        var hero = At(Wall - cells);
        var thrown = Thrown(hero, 60, Spec.Abilities.ThrowOf(item));
        double least = double.PositiveInfinity;
        for (int i = 0; i < 1000 && thrown.Flying; i++)
        {
            thrown.Step(1000.0 / fps, w.M, hero, true, 0, Opaque);
            least = Math.Min(least, thrown.At.X);
        }
        Assert.False(thrown.Flying);
        Assert.InRange(least, Aiming.HandOf(hero).X - 1, Wall * Unit);
    }

    /// <summary>A map a character a cell, as drawn.</summary>
    private static TestWorld Drawn(int width, int height, Func<int, int, char> at) =>
        new(Layout.Parse([.. Enumerable.Range(0, height).Select(y => new string([.. Enumerable.Range(0, width).Select(x => at(x, y))]))]), 1);

    [Theory]
    [InlineData(1, 1)]
    [InlineData(-1, 1)]
    [InlineData(1, -1)]
    [InlineData(-1, -1)]
    public void AThrowSlipsByNoCornerASwingStopsAt(int sx, int sy)
    {
        var w = Drawn(16, 16, (x, y) => (x, y) == (8 + 2 * sx, 8 + sy) ? '#' : ' ');
        var from = (8 * Unit + 500.0, 8 * Unit + 500.0);
        var (_, stopped) = ToolRays.Segment(w.M, from, (from.Item1 + sx * 4 * Unit, from.Item2 + sy * 4 * Unit), false, 0, Opaque);
        Assert.True(stopped);
    }

    [Fact]
    public void AThrowWalksTheGridAsEveryRayDoes()
    {
        const int size = 16;
        var w = Drawn(size, size, (x, y) => (x * 7 + y * 3) % 11 == 0 ? '#' : ' ');
        var grid = w.M.Grid;
        foreach (var (fx, fy) in new[] { (8500, 8500), (2500, 13500), (5000, 5000), (12250, 4750) })
            for (int ty = 500; ty < size * Unit; ty += 500)
                for (int tx = 500; tx < size * Unit; tx += 500)
                {
                    var walked = new List<int>();
                    bool stops = false;
                    Rays.Walk(size, size, fx, fy, tx - fx, ty - fy, cell =>
                    {
                        walked.Add(cell);
                        bool wall = grid.WallAt(cell) != null || grid.Blocks(cell);
                        stops |= wall;
                        return wall;
                    });
                    var (hit, stopped) = ToolRays.Segment(w.M, (fx, fy), (tx, ty), false, 0, Opaque);
                    Assert.Equal(walked.Distinct(), hit.Cells);
                    Assert.Equal(stops, stopped);
                }
    }

    [Fact]
    public void TheWallBehindWhatItStruckIsHeard()
    {
        // A tank four cells square and a wall right behind it.
        var w = Drawn(40, 24, (x, y) => x == Wall ? '#' : x is >= Wall - 4 and < Wall && y is >= 10 and < 14 ? 'T' : ' ');
        var hero = At(6);
        var hits = Flown(w, Thrown(hero, 60), hero);
        int wall = 12 * w.M.Grid.Width + Wall;
        var bit = hits.Single(h => h.Hit.Cells.Contains(wall)).Hit;
        Assert.Equal(wall, bit.Ground?.Cell);
    }

    [Fact]
    public void AWallTurnsItWithOneHitAndOnePace()
    {
        var w = Space(14);
        var hero = At(6);
        var thrown = Thrown(hero, 60);
        Assert.Equal([1], Flown(w, thrown, hero).SelectMany(h => h.Hit.Wrecks).Select(p => p.Id));
        thrown.Wait(PaceMs);
        Assert.False(thrown.Rests(Axe.Item));
    }

    [Theory]
    [MemberData(nameof(Once))]
    public void OneThatStrikesOnceStrikesEachTargetOnceOverBothLegs(string item)
    {
        var w = Space(38);
        var hero = At(30);
        var thrown = Thrown(hero, 60, Spec.Abilities.ThrowOf(item));
        Assert.Equal([1], Flown(w, thrown, hero).SelectMany(h => h.Hit.Wrecks).Select(p => p.Id));
        thrown.Wait(PaceMs);
        Assert.False(thrown.Rests(item));
    }

    [Theory]
    [InlineData(1.0)]
    [InlineData(5.0)]
    public void TwoHitsRestTwoPacesHoweverSlowTheBody(double slow)
    {
        var w = Space();
        var hero = At(30);
        var thrown = new WeaponThrow();
        Assert.True(thrown.Begin(Hands, PaceMs * slow));
        Assert.True(thrown.Leave(hero, new Point(60 * Unit, 12 * Unit + 500)));
        Flown(w, thrown, hero);
        thrown.Wait(2 * PaceMs * slow - 1);
        Assert.True(thrown.Rests(Axe.Item));
        Assert.False(thrown.Begin(Hands, PaceMs * slow));
        thrown.Wait(1);
        Assert.True(thrown.Begin(Hands, PaceMs * slow));
    }

    [Fact]
    public void NoSwingWhileItIsOutOrRestsButTheRaisedOnes()
    {
        var w = Space();
        var hero = At(30);
        var thrown = new WeaponThrow();
        Assert.True(thrown.LetsSwing(Axe.Item));
        Assert.True(thrown.Begin(Hands, PaceMs));
        Assert.True(thrown.LetsSwing(Axe.Item));
        Assert.True(thrown.Leave(hero, new Point(60 * Unit, 12 * Unit + 500)));
        Assert.False(thrown.LetsSwing(Spec.Weapons.Spear.Item));
        Flown(w, thrown, hero);
        Assert.False(thrown.LetsSwing(Axe.Item));
        Assert.True(thrown.LetsSwing(Spec.Weapons.Spear.Item));
        thrown.Wait(2 * PaceMs);
        Assert.True(thrown.LetsSwing(Axe.Item));
    }

    [Fact]
    public void ItsRestCountsFromItsStroke()
    {
        var thrown = new WeaponThrow();
        Assert.True(thrown.Begin(Hands, PaceMs));
        thrown.Wait(300);
        thrown.Swung();
        thrown.Wait(PaceMs - 1);
        Assert.True(thrown.Rests(Axe.Item));
        thrown.Wait(1);
        Assert.False(thrown.Rests(Axe.Item));
    }

    [Fact]
    public void EachWeaponRestsByItself()
    {
        var thrown = Thrown(At(30), 60);
        thrown.Cancel();
        Assert.False(thrown.Rests(Spec.Weapons.Spear.Item));
        Assert.True(thrown.Begin(Hands with { Wielded = Spec.Weapons.Spear.Item }, PaceMs));
    }

    [Fact]
    public void HomeItStrikesWhatTheWayOutMissed()
    {
        var w = Space(38);
        var thrower = At(30);
        // Thrown left over open cells to the wall, the hero gone right of the wreck meanwhile.
        var hits = Flown(w, Thrown(thrower, 0), thrower, leg => leg == ThrowLeg.Home ? At(50) : thrower);
        Assert.Equal([1], hits.Where(h => h.Leg == ThrowLeg.Home).SelectMany(h => h.Hit.Wrecks).Select(p => p.Id));
    }

    [Fact]
    public void HomeItStrikesNothingPastAWall()
    {
        var w = Space(10);
        var thrower = At(30);
        var hits = Flown(w, Thrown(thrower, 60), thrower, leg => leg == ThrowLeg.Home ? At(4) : thrower);
        Assert.Empty(hits.SelectMany(h => h.Hit.Wrecks));
        Assert.Empty(hits.SelectMany(h => h.Hit.Cells));
    }

    [Fact]
    public void NoneLeavesWhileOneIsOutNorSoonerThanThePace()
    {
        var thrown = Thrown(At(30), 60);
        Assert.False(thrown.Begin(Hands, PaceMs));
        thrown.Cancel();
        thrown.Wait(PaceMs - 1);
        Assert.False(thrown.Begin(Hands, PaceMs));
        thrown.Wait(1);
        Assert.True(thrown.Begin(Hands, PaceMs));
    }

    [Fact]
    public void OnlyWhatHasAThrowBeginsOne() =>
        Assert.False(new WeaponThrow().Begin(Hands with { Wielded = Spec.Weapons.Pickaxe.Item }, PaceMs));

    public static TheoryData<string> Cancels() => ["switched", "dropped", "plan", "held", "surface"];

    [Theory]
    [MemberData(nameof(Cancels))]
    public void ItEndsAtOnceWhenTheHandsCannotKeepIt(string why)
    {
        var hands = why switch
        {
            "switched" => Hands with { Wielded = Spec.Weapons.Spear.Item },
            "dropped" => Hands with { Wielded = null },
            "plan" => Hands with { Free = false },
            "held" => Hands with { Held = true },
            _ => Hands with { Surface = 1 },
        };
        var w = Space(38);
        var hero = At(30);
        var thrown = Thrown(hero, 60);
        thrown.Keep(Hands);
        Assert.True(thrown.Flying);
        thrown.Keep(hands);
        Assert.False(thrown.Flying);
        Assert.Null(thrown.Step(Frame, w.M, hero, true, 0, Opaque));
    }
}
