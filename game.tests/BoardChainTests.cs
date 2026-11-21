using System.Linq;
using Stillwake.Game.Features.Research;
using Stillwake.Sim;
using Stillwake.Sim.Grid;
using Xunit;
using Surfaces = Stillwake.Content.Surfaces;

namespace Stillwake.Game.Tests;

/// <summary>A goal's chain down to raw, the frames' three states, and a machine standing on any surface (board-islands).</summary>
public class BoardChainTests
{
    private static BoardRead Read(string[] found, string[] granted, params string[] standing) =>
        new(found, granted, found.Contains, standing.Contains);

    [Fact]
    public void AGoalListsTheRawItemsAndMachinesItStillMisses()
    {
        var missing = BoardChain.Missing("nail", Read([], []));
        Assert.Contains("scrap", missing);
        Assert.Contains("scrapChunk", missing);
        Assert.Contains("workbench", missing);
        Assert.Contains(missing, id => TechLinks.Of("scrap").MadeAt.Contains(id));
        Assert.DoesNotContain("nail", missing);
    }

    [Fact]
    public void WhatIsHadOrStandsLeavesTheList()
    {
        var read = Read(["scrapChunk", "scrap"], [], "workbench");
        var missing = BoardChain.Missing("nail", read);
        Assert.DoesNotContain("scrap", missing);
        Assert.DoesNotContain("workbench", missing);
        Assert.Contains("workbench", BoardChain.Of("nail", read).Ids);
    }

    [Fact]
    public void ASmeltedItemTakesOneSourceTheOneHadElseTheShallowest()
    {
        var sources = BoardThings.SourcesOf("iron");
        Assert.True(sources.Count > 1);
        string[] From(BoardRead read) => [.. BoardChain.Of("plate", read).Ties.Where(t => t.To == "iron" && sources.Contains(t.From)).Select(t => t.From)];
        var shallowest = Assert.Single(From(Read([], [])));
        Assert.Equal(sources.Min(BoardLayout.DepthOf), BoardLayout.DepthOf(shallowest));
        string other = sources.First(s => s != shallowest);
        Assert.Equal([other], From(Read([other], [])));
    }

    [Fact]
    public void AThingIsUnknownAlmostOrKnown()
    {
        Assert.Equal(BoardState.Unknown, BoardView.StateOf("nail", Read([], [])));
        Assert.Equal(BoardState.Almost, BoardView.StateOf("nail", Read(["scrap"], [])));
        Assert.Equal(["workbench"], BoardView.MissingOf("nail", Read(["scrap"], [])));
        Assert.Equal(BoardState.Known, BoardView.StateOf("nail", Read(["scrap"], ["nail"])));
        Assert.Equal(BoardState.Known, BoardView.StateOf("scrapChunk", Read(["scrapChunk"], [])));
        Assert.Equal(BoardState.Almost, BoardView.StateOf("scrap", Read(["scrapChunk"], [])));
    }

    [Fact]
    public void AMarketGoodIsNeverAlmost()
    {
        string good = BoardThings.All.First(t => t.Market && t.Parts.Count > 0).Id;
        var read = Read([.. BoardThings.Of(good)!.Parts], [], [.. BoardThings.Machines]);
        Assert.Equal(BoardState.Unknown, BoardView.StateOf(good, read));
    }

    [Fact]
    public void AMachineStandingOnlyOnAPlanetCounts()
    {
        var world = new TestWorld("""
            ......
            .#S#..
            .###..
            """).Step(new OpenSurface(Surfaces.TestStrip.Id));
        Assert.False(BoardRead.Of(world.World, 0).Stands("workbench"));
        string before = BoardRead.KeyOf(world.World, 0);
        var grid = Registry.On(world.World, 1).Grid;
        var bench = new Placement("machine", "workbench");
        grid.Place(Enumerable.Range(0, grid.Width * grid.Height).First(c => grid.PlacementProblem(c, bench) == null), bench);
        Assert.Empty(world.M.Grid.MachinesOf("workbench"));
        Assert.True(BoardRead.Of(world.World, 0).Stands("workbench"));
        Assert.NotEqual(before, BoardRead.KeyOf(world.World, 0));
    }

    [Fact]
    public void AClosedSurfaceIsSkipped()
    {
        var world = new TestWorld("""
            ......
            .#S#..
            .###..
            """).Step(new OpenSurface(Surfaces.TestStrip.Id)).Edit(root =>
        {
            var left = root["surfaces"]![0]!.AsObject();
            left.Remove("slices");
            left["closed"] = true;
        });
        Assert.True(world.World.Surfaces[1].Closed);
        Assert.False(BoardRead.Of(world.World, 0).Stands("workbench"));
        Assert.NotEmpty(BoardRead.KeyOf(world.World, 0));
    }

    [Fact]
    public void ASilhouetteCountsEveryPartMissingNotJustTheFirst()
    {
        var thing = BoardThings.All.First(t => !t.Market && t.Parts.Count >= 2);
        var read = Read([thing.Parts[0]], [], [.. BoardThings.Machines]);
        Assert.Equal(thing.Parts.Where(p => p != thing.Parts[0]), BoardView.MissingOf(thing.Id, read));
    }
}
