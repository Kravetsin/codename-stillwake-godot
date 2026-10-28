using System.Text.Json.Nodes;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Stillwake.Sim.Crew;
using Stillwake.Sim.Players;
using Xunit;

namespace Stillwake.Sim.Tests;

/// <summary>Looks chosen and printed (2026-10-28-looks-are-printed): a look is worn from the pod's next print of the hero.</summary>
public class LooksTests
{
    /// <summary>A sealed room with no vent: never aired, so a hero in it breathes its suit dry, passes out and wakes.</summary>
    private const string Room = """
        #######
        #..S..#
        #######
        """;

    private static readonly Look Freckled = new(3, 2, 4, 4, 2), Plain = new(1, 1, 1, 1, 0);

    private static Scenario World() => new(Room);
    private static int Spawn(Scenario w) => w.M.Crew.Spawn;
    private static Enter EnterOf(Scenario w, int from = 0) => new(Spawn(w)) { From = from };
    private static ChooseLook Choose(Look look, int from = 0) => new(look) { From = from };
    private static Look LookOf(Scenario w, int peer = 0) => w.M.Players.LookOf(peer);

    /// <summary>Runs until the hero of <paramref name="peer"/> passes out, then until it wakes.</summary>
    private static void PassOut(Scenario w, int peer = 0)
    {
        for (int i = 0; i < 100_000 && w.M.Crew.HeroOf(peer)!.Out == 0; i++) w.Run();
        Assert.True(w.M.Crew.HeroOf(peer)!.Out > 0);
    }

    [Fact]
    public void WearsLookZeroNeverHavingChosen()
    {
        var w = World();
        w.Do(EnterOf(w)).Run();
        Assert.Equal(Look.Zero, LookOf(w));
        Assert.Equal(Look.Zero, LookOf(w, 7));
    }

    [Fact]
    public void PrintsAFirstHeroInTheLookChosenBeforeItCameIn()
    {
        var w = World();
        w.Do(Choose(Freckled)).Run();
        Assert.Equal(Look.Zero, LookOf(w));
        w.Do(EnterOf(w)).Run();
        Assert.Equal(Freckled, LookOf(w));
        Assert.Empty(w.Refusals);
    }

    [Fact]
    public void PrintsAFirstHeroInTheLookChosenInItsOwnTickBeforeItsEnter()
    {
        var w = World();
        w.Do(Choose(Freckled)).Do(EnterOf(w)).Run();
        Assert.Equal(Freckled, LookOf(w));
    }

    [Fact]
    public void KeepsALiveHerosLookThroughMovesTillItPassesOutAndWakes()
    {
        var w = World();
        w.Do(Choose(Plain)).Do(EnterOf(w)).Run();
        w.Do(Choose(Freckled)).Do(Choose(Freckled)).Run();
        w.Do(new Enter(Spawn(w) + 1)).Run();
        Assert.Equal(Plain, LookOf(w));
        PassOut(w);
        Assert.Equal(Plain, LookOf(w));
        w.Run(Content.Crew.PassOut);
        Assert.Equal((0, Freckled), (w.M.Crew.HeroOf(0)!.Out, LookOf(w)));
    }

    [Fact]
    public void PrintsAPeerComingBackAfterLeavingInItsLastChoice()
    {
        var w = World();
        w.Do(Choose(Plain, 1)).Do(EnterOf(w, 1)).Run();
        w.Do(Choose(Freckled, 1)).Do(new Leave { From = 1 }).Run();
        // Gone, it is still known by the look it wore: its body shows it.
        Assert.Equal(Plain, LookOf(w, 1));
        w.Do(EnterOf(w, 1)).Run();
        Assert.Equal(Freckled, LookOf(w, 1));
    }

    [Fact]
    public void KeepsEachPeersLookApartWhateverOrderTheyChooseIn()
    {
        var w = World();
        w.Do(Choose(Freckled, 2)).Do(Choose(Plain, 0)).Do(Choose(Freckled with { Skin = 6 }, 1)).Run();
        for (int peer = 2; peer >= 0; peer--) w.Do(EnterOf(w, peer));
        w.Run();
        Assert.Equal([Plain, Freckled with { Skin = 6 }, Freckled], [LookOf(w, 0), LookOf(w, 1), LookOf(w, 2)]);
    }

    [Fact]
    public void LoadsASaveFromBeforeLooksInLookZeroAndChoosesOn()
    {
        var w = World();
        w.Do(Choose(Freckled)).Do(EnterOf(w)).Run();
        var root = JsonNode.Parse(w.World.Save())!;
        var players = root["slices"]!["players"]!.AsObject();
        Assert.True(players.Remove("looks"));
        w.Reload(root.ToJsonString());
        Assert.Equal(Look.Zero, LookOf(w));
        w.Do(Choose(Plain)).Run();
        PassOut(w);
        w.Run(Content.Crew.PassOut);
        Assert.Equal(Plain, LookOf(w));
    }

    private sealed record Step(string Type, int Peer, int Ticks, Look Look);

    private static readonly Gen<Look> Looks =
        from s in Gen.Choose(-2, 9)
        from h in Gen.Choose(-2, 9)
        from c in Gen.Choose(-2, 9)
        from f in Gen.Choose(-2, 9)
        from b in Gen.Choose(-2, 9)
        select new Look(s, h, c, f, b);

    private static readonly Gen<Step> Steps =
        from type in Gen.Elements("choose", "enter", "leave", "run", "run", "drain")
        from peer in Gen.Choose(0, 2)
        from ticks in Gen.Choose(1, Content.Crew.PassOut - 1)
        from look in Looks
        select new Step(type, peer, ticks, look);

    /// <summary>
    /// A worn look changes only on a print — a hero made where it had none, or one waking — and then to the look chosen last.
    /// Runs shorter than a faint, so no step both passes a hero out and wakes it; a hero drained passes out on its next breath.
    /// </summary>
    [Property(MaxTest = 60)]
    public Property AWornLookChangesOnlyOnAPrintToTheLastChosen()
    {
        var all = from n in Gen.Choose(0, 40) from s in Steps.ArrayOf(n) select s;
        return Prop.ForAll(all.ToArbitrary(), steps =>
        {
            var w = World();
            var chosen = new Dictionary<int, Look>();
            foreach (var s in steps)
            {
                var before = Enumerable.Range(0, 3).Select(p => (Look: LookOf(w, p), Hero: w.M.Crew.HeroOf(p)?.Out)).ToArray();
                switch (s.Type)
                {
                    case "choose":
                        w.Do(Choose(s.Look, s.Peer)).Run();
                        chosen[s.Peer] = s.Look;
                        break;
                    case "enter":
                        w.Do(EnterOf(w, s.Peer)).Run();
                        break;
                    case "leave":
                        w.Do(new Leave { From = s.Peer }).Run();
                        break;
                    case "drain":
                        // Its air gone, so it passes out at its next breath and wakes a faint later.
                        if (w.M.Crew.HeroOf(s.Peer) is { } hero) hero.Air = 0;
                        break;
                    default:
                        w.Run(s.Ticks);
                        break;
                }
                for (int p = 0; p < 3; p++)
                {
                    int? was = before[p].Hero, now = w.M.Crew.HeroOf(p)?.Out;
                    bool printed = (was == null && now != null) || (was > 0 && now == 0);
                    var expected = printed && chosen.TryGetValue(p, out var look) ? look : before[p].Look;
                    if (LookOf(w, p) != expected) return false;
                }
            }
            return true;
        });
    }
}
