using System.Text.Json.Nodes;
using Stillwake.Sim.Crew;
using Xunit;

namespace Stillwake.Sim.Tests;

/// <summary>Skins chosen (skins): a peer's set, worn at once, replaced whole by its next choice; the sim checks no id.</summary>
public class SkinsTests
{
    private const string Room = """
        #######
        #..S..#
        #######
        """;

    private static Scenario World() => new(Room);
    private static Enter EnterOf(Scenario w, int from = 0) => new(w.M.Crew.Spawn) { From = from };
    private static ChooseSkins Choose(int from, params string[] ids) => new(ids) { From = from };
    private static IReadOnlyList<string> SkinsOf(Scenario w, int peer = 0) => w.M.Players.SkinsOf(peer);

    [Fact]
    public void WearsNoSkinNeverHavingChosen()
    {
        var w = World();
        w.Do(EnterOf(w)).Run();
        Assert.Empty(SkinsOf(w));
    }

    [Fact]
    public void RecordsSkinsChosenBeforeAnyHero()
    {
        var w = World();
        w.Do(Choose(0, "suit_a")).Run();
        Assert.Equal(["suit_a"], SkinsOf(w));
        Assert.Empty(w.Refusals);
    }

    [Fact]
    public void ANextChoiceReplacesTheWholeSetAtOnce()
    {
        var w = World();
        w.Do(EnterOf(w)).Do(Choose(0, "suit_a", "effect_a")).Run();
        w.Do(Choose(0, "head_a")).Run();
        Assert.Equal(["head_a"], SkinsOf(w));
        w.Do(Choose(0)).Run();
        Assert.Empty(SkinsOf(w));
    }

    [Fact]
    public void KeepsEachPeersSkinsApartWhateverOrderTheyChooseIn()
    {
        var w = World();
        w.Do(Choose(2, "c")).Do(Choose(0, "a")).Do(Choose(1, "b")).Run();
        Assert.Equal([["a"], ["b"], ["c"]], [SkinsOf(w, 0), SkinsOf(w, 1), SkinsOf(w, 2)]);
    }

    [Fact]
    public void APeerThatLeftKeepsItsSkins()
    {
        var w = World();
        w.Do(Choose(1, "suit_a")).Do(EnterOf(w, 1)).Run();
        w.Do(new Leave { From = 1 }).Run();
        Assert.Equal(["suit_a"], SkinsOf(w, 1));
    }

    [Fact]
    public void KeepsSkinsThroughASaveAndLoadsASaveFromBeforeSkinsWithNone()
    {
        var w = World();
        w.Do(Choose(0, "suit_a", "effect_a")).Do(EnterOf(w)).Run();
        w.Reload(w.World.Save());
        Assert.Equal(["suit_a", "effect_a"], SkinsOf(w));
        var root = JsonNode.Parse(w.World.Save())!;
        Assert.True(root["slices"]!["players"]!.AsObject().Remove("skins"));
        w.Reload(root.ToJsonString());
        Assert.Empty(SkinsOf(w));
        w.Do(Choose(0, "head_a")).Run();
        Assert.Equal(["head_a"], SkinsOf(w));
    }
}
