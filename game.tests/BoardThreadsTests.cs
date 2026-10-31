using Stillwake.Game.Features.Research;
using Xunit;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Tests;

/// <summary>What a note's threads run to, and what the board shows (docs/plans/board.md).</summary>
public class BoardThreadsTests
{
    private static BoardThing Use(string id, bool market = false) => new(id, [("part", 10)], null, market);

    private static BoardRead Read(IReadOnlyList<string> found, IReadOnlyCollection<string>? granted = null, Dictionary<string, int>? counts = null, params string[] standing) =>
        new(found, granted ?? Spec.Research.StartKnown, item => counts?.GetValueOrDefault(item) ?? (found.Contains(item) ? 1 : 0), standing.Contains);

    private static BoardPicture Picture(BoardRead read, string? picked = null) =>
        BoardView.Of(BoardSlots.Of(read.Found, BoardThings.All, read.Granted), BoardThings.All, read, picked);

    private static string[] Notes(BoardPicture picture) => [.. picture.Shows.Where(s => s.Value == BoardShow.Note).Select(s => s.Key).Order(StringComparer.Ordinal)];

    private static string[] Shapes(BoardPicture picture) => [.. picture.Shows.Where(s => s.Value == BoardShow.Silhouette).Select(s => s.Key).Order(StringComparer.Ordinal)];

    [Fact]
    public void ThreadsRunToTheClosestToDoneTiesInContentOrder()
    {
        BoardThing[] things = [Use("a"), Use("b"), Use("c"), Use("d"), Use("e")];
        double[] pencils = [0.2, 0.5, 0.2, 0.9, 0.1];
        double Pencil(BoardThing t) => pencils[Array.IndexOf(things, t)];
        Assert.Equal(["d", "b", "a"], BoardThreads.Of("part", things, _ => true, Pencil));
    }

    [Fact]
    public void ThreadsNeverRunToWhatTheMarketOpensOrWhatIsNoShapeOrUsesSomethingElse()
    {
        BoardThing[] things = [Use("sold", market: true), Use("inked"), new BoardThing("other", [("else", 1)], null, false), Use("shape")];
        Assert.Equal(["shape"], BoardThreads.Of("part", things, id => id != "inked", _ => 0));
    }

    [Fact]
    public void APlatesManyUsesShowThreeThreads()
    {
        int uses = BoardThings.All.Count(t => t.Parts.Any(p => p.Item == "plate"));
        Assert.True(uses > 2 * BoardThreads.Most, $"a plate has {uses} uses");
        var picture = Picture(Read(["plate"]));
        Assert.Equal(BoardThreads.Most, picture.Threads.Count(t => t.Note == "plate"));
        Assert.Equal(BoardThreads.Most, Shapes(picture).Length);
    }

    [Fact]
    public void TheEngineShowsOnlyByAClick()
    {
        var read = Read(["plate"], counts: new() { ["plate"] = 3 });
        Assert.Equal(BoardShow.Unseen, Picture(read).Shows["engine"]);
        Assert.Equal(BoardShow.Silhouette, Picture(read, picked: "plate").Shows["engine"]);
    }

    [Fact]
    public void PickingAShapeShowsWhatIsOneStepFromItAndNoFurther()
    {
        var read = Read(["scrap", "iron", "gear"]);
        var picture = Picture(read, picked: "rotor");
        Assert.Equal(BoardShow.Silhouette, picture.Shows["rotor"]);
        Assert.Equal(BoardShow.Note, picture.Shows["gear"]);
        Assert.Equal(BoardShow.Silhouette, picture.Shows["wire"]);
        Assert.Equal(BoardShow.Unseen, Picture(read, picked: "gear").Shows["wire"]);
    }

    [Fact]
    public void TheSolarPanelHasNoSilhouette()
    {
        string[] parts = ["frame", "solarCell", "wire", "circuit", "photoPanel"];
        Assert.DoesNotContain("solarPanel", Picture(Read(parts)).Shows.Keys);
        Assert.DoesNotContain("solarPanel", Picture(Read(parts), picked: "frame").Shows.Keys);
        Assert.Equal(BoardShow.Note, Picture(Read(parts, [.. Spec.Research.StartKnown, "solarPanel"])).Shows["solarPanel"]);
    }

    [Fact]
    public void NothingShowsWithNothingFound()
    {
        var picture = Picture(Read([], [.. BoardThings.All.Select(t => t.Id)]));
        Assert.Empty(picture.Shows);
        Assert.Empty(picture.Threads);
    }

    [Fact]
    public void AFirstFindStandsAloneWithThreadsToTheScrapWallsFirst()
    {
        var picture = Picture(Read(["scrapChunk"], counts: new() { ["scrapChunk"] = 3 }));
        Assert.Equal(["scrapChunk"], Notes(picture));
        Assert.Equal(BoardThreads.Most, picture.Threads.Count);
        Assert.Contains("wallScrap", Shapes(picture));
        Assert.Contains("backWallScrap", Shapes(picture));
    }

    [Fact]
    public void ASecondPartNotesItselfReachesTheMachinesAndTheFirstShapesDarken()
    {
        var first = new Dictionary<string, int> { ["scrapChunk"] = 3 };
        var near = new Dictionary<string, int> { ["scrapChunk"] = 9, ["hullPlate"] = 4 };
        var picture = Picture(Read(["scrapChunk", "hullPlate"], counts: near));
        Assert.Equal(["hullPlate", "scrapChunk"], Notes(picture));
        Assert.Equal(["backWallScrap", "furnace", "tradeStation", "wallScrap", "workbench"], Shapes(picture));
        Assert.Subset(Shapes(picture).ToHashSet(), Shapes(Picture(Read(["scrapChunk"], counts: first))).ToHashSet());
        var furnace = BoardThings.All.Single(t => t.Id == "furnace");
        Assert.True(BoardPencil.Of(furnace, i => near.GetValueOrDefault(i), _ => false) > BoardPencil.Of(furnace, i => first.GetValueOrDefault(i), _ => false));
    }

    [Fact]
    public void WhatThePodMakesForNothingShowsOnlyOnceMade()
    {
        string[] pod = ["helmet", "suit", "screwdriver"];
        var before = Picture(Read(["scrapChunk"], pod));
        Assert.Equal(["scrapChunk"], Notes(before));
        Assert.DoesNotContain("helmet", before.Shows.Keys);
        Assert.Equal(BoardShow.Note, Picture(Read(["scrapChunk", "helmet"], pod)).Shows["helmet"]);
    }

    [Fact]
    public void WhatTheCrewUnderstoodIsANoteAndWhatItFoundIsOneToo()
    {
        var picture = Picture(Read(["iron", "circuit"], [.. Spec.Research.StartKnown, "plate"]));
        Assert.Equal(BoardShow.Note, picture.Shows["plate"]);
        Assert.Equal(BoardShow.Note, picture.Shows["circuit"]);
        Assert.Equal(BoardShow.Unseen, Picture(Read(["circuit"])).Shows.GetValueOrDefault("plate"));
    }

    [Fact]
    public void AThreadOnlyLeavesANoteAndOnlyEndsAtAShape()
    {
        var picture = Picture(Read(["scrapChunk", "hullPlate", "planks", "scrap", "iron"], [.. Spec.Research.StartKnown, "wire"]));
        Assert.NotEmpty(picture.Threads);
        Assert.All(picture.Threads, t =>
        {
            Assert.Equal(BoardShow.Note, picture.Shows[t.Note]);
            Assert.Equal(BoardShow.Silhouette, picture.Shows[t.Shape]);
        });
    }
}
