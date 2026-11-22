using Stillwake.Game.Features.Crew;
using Stillwake.Sim;
using Stillwake.Sim.Debris;
using Xunit;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Tests;

/// <summary>The thrown axe (companion-fights): two legs, each target once, cells out only, nothing past a wall, and what ends it.</summary>
public class AxeThrowTests
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
                piece["speed"] = 0;
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

    private static AxeThrow Thrown(Hero from, int toX)
    {
        var thrown = new AxeThrow();
        Assert.True(thrown.Begin(Hands, PaceMs));
        Assert.True(thrown.Leave(from, new Point(toX * Unit, 12 * Unit + 500)));
        return thrown;
    }

    private static List<(ThrowLeg Leg, EdgeHit Hit)> Flown(TestWorld w, AxeThrow thrown, Hero hero, Func<ThrowLeg, Hero>? later = null)
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
        while (thrown.Leg == ThrowLeg.Out)
        {
            if (thrown.Hauled) hauled += Frame;
            thrown.Step(Frame, w.M, hero, true, 0, Opaque);
        }
        Assert.InRange(hauled, AxeThrow.CastMs - 2 * Frame, AxeThrow.CastMs + Frame);
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
        while (thrown.Leg == ThrowLeg.Out) thrown.Step(Frame, w.M, rising, true, 0, Opaque);
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

    [Fact]
    public void EachTargetIsStruckOnceOverBothLegs()
    {
        var w = Space(38);
        var hero = At(30);
        var hits = Flown(w, Thrown(hero, 60), hero);
        Assert.Equal([1], hits.SelectMany(h => h.Hit.Wrecks).Select(p => p.Id));
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
        Assert.False(new AxeThrow().Begin(Hands with { Wielded = Spec.Weapons.Spear.Item }, PaceMs));

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
