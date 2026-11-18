using System;
using System.Collections.Generic;
using System.Linq;
using Stillwake.Game.Features.Research;
using Stillwake.Game.I18n;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>The board's fixed islands (board-islands): every thing in one district, a frame each, worked out from Content alone.</summary>
public class BoardLayoutTests
{
    private static readonly BoardLayout Layout = BoardLayout.Main;

    [Fact]
    public void EveryBoardThingHasExactlyOneDistrictAndNothingElseIsListed()
    {
        var listed = BoardDistricts.All.SelectMany(d => d.Things).ToList();
        var twice = listed.GroupBy(id => id, StringComparer.Ordinal).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        Assert.True(twice.Count == 0, $"listed twice: {string.Join(", ", twice)}");
        var missing = BoardThings.Ids.Except(listed, StringComparer.Ordinal).ToList();
        Assert.True(missing.Count == 0, $"no district: {string.Join(", ", missing)}");
        var strays = listed.Except(BoardThings.Ids, StringComparer.Ordinal).ToList();
        Assert.True(strays.Count == 0, $"not on the board: {string.Join(", ", strays)}");
    }

    [Fact]
    public void EveryDistrictHasACaption() =>
        Assert.All(BoardDistricts.All, d => Assert.True(En.Table.ContainsKey($"research.district.{d.Id}"), d.Id));

    [Fact]
    public void EveryThingIsPlacedExactlyOnceInItsOwnDistrictsBlock()
    {
        Assert.Equal(BoardThings.Ids.Order(StringComparer.Ordinal), Layout.Frames.Keys.Order(StringComparer.Ordinal));
        foreach (var frame in Layout.Frames.Values)
        {
            Assert.Equal(BoardDistricts.Of(frame.Id), frame.District);
            var block = Layout.Blocks.Single(b => b.District == frame.District);
            Assert.True(frame.X - BoardLayout.FrameWide / 2 >= block.X && frame.X + BoardLayout.FrameWide / 2 <= block.X + block.Wide, frame.Id);
            Assert.True(frame.Y - BoardLayout.FrameTall / 2 >= block.Y && frame.Y + BoardLayout.FrameTall / 2 <= block.Y + block.Tall, frame.Id);
        }
    }

    [Fact]
    public void NoTwoFramesNorBlocksOverlap()
    {
        var frames = Layout.Frames.Values.ToList();
        for (int a = 0; a < frames.Count; a++)
            for (int b = a + 1; b < frames.Count; b++)
                Assert.True(Math.Abs(frames[a].X - frames[b].X) >= BoardLayout.FrameWide || Math.Abs(frames[a].Y - frames[b].Y) >= BoardLayout.FrameTall,
                    $"{frames[a].Id} and {frames[b].Id} overlap");
        var blocks = Layout.Blocks;
        for (int a = 0; a < blocks.Count; a++)
            for (int b = a + 1; b < blocks.Count; b++)
                Assert.False(blocks[a].X < blocks[b].X + blocks[b].Wide && blocks[b].X < blocks[a].X + blocks[a].Wide
                    && blocks[a].Y < blocks[b].Y + blocks[b].Tall && blocks[b].Y < blocks[a].Y + blocks[a].Tall, $"{blocks[a].District} and {blocks[b].District}");
    }

    [Fact]
    public void BlocksFlowAlongTheirLaneInTableOrderAndLanesStackDown()
    {
        foreach (var lane in BoardDistricts.All.GroupBy(d => d.Lane))
        {
            var xs = lane.Select(d => Layout.Blocks.Single(b => b.District == d.Id).X).ToList();
            Assert.Equal(xs.Order(), xs);
        }
        double firstLaneBottom = BoardDistricts.All.Where(d => d.Lane == 0).Max(d => Layout.Blocks.Single(b => b.District == d.Id) is var b ? b.Y + b.Tall : 0);
        Assert.All(BoardDistricts.All.Where(d => d.Lane == 1), d => Assert.True(Layout.Blocks.Single(b => b.District == d.Id).Y > firstLaneBottom));
    }

    [Fact]
    public void RawSitsAtDepthNoughtACraftOneAboveItsDeepestPartASmeltedItemOneAboveItsShallowestSource()
    {
        int Depth(string id) => Layout.Frames[id].Depth;
        Assert.All(BoardThings.Ids.Where(id => BoardThings.Of(id) == null && BoardThings.SourcesOf(id).Count == 0), id => Assert.Equal(0, Depth(id)));
        Assert.Equal(1 + BoardThings.SourcesOf("iron").Min(Depth), Depth("iron"));
        Assert.Equal(1, Depth("iron"));
        Assert.Equal(1 + Depth("scrap"), Depth("nail"));
        var rotor = BoardThings.Of("rotor")!;
        Assert.Equal(1 + rotor.Parts.Max(Depth), Depth("rotor"));
        // Columns go from shallow to deep across a block.
        foreach (var district in BoardDistricts.All)
        {
            var frames = district.Things.Select(id => Layout.Frames[id]).OrderBy(f => f.X).ToList();
            Assert.Equal(frames.Select(f => f.Depth).Order(), frames.Select(f => f.Depth));
        }
    }

    [Fact]
    public void ARecipeCycleMeetsTheGuardAndEnds()
    {
        var parts = new Dictionary<string, IReadOnlyList<string>> { ["egg"] = ["hen"], ["hen"] = ["egg"], ["self"] = ["self", "egg"] };
        var depths = BoardLayout.Depths(["egg", "hen", "self"], id => parts.GetValueOrDefault(id), _ => []);
        Assert.Equal(2, depths["egg"]);
        Assert.Equal(1, depths["hen"]);
        Assert.Equal(3, depths["self"]);
        var smelted = BoardLayout.Depths(["seed"], _ => null, id => id == "seed" ? ["fruit"] : id == "fruit" ? ["seed"] : []);
        Assert.Equal(2, smelted["seed"]);
    }

    [Fact]
    public void TheLayoutComesFromContentAloneTheSameEveryTime()
    {
        var again = BoardLayout.Of(BoardDistricts.All, id => Layout.Frames[id].Depth);
        Assert.Equal(Layout.Frames.Values.OrderBy(f => f.Id, StringComparer.Ordinal), again.Frames.Values.OrderBy(f => f.Id, StringComparer.Ordinal));
        Assert.Equal(Layout.Blocks, again.Blocks);
        Assert.Equal((Layout.Wide, Layout.Tall), (again.Wide, again.Tall));
    }

    [Fact]
    public void EveryFrameStandsOnABigCellOfThePattern()
    {
        static bool On(double at, double pitch) => Math.Abs(at / pitch - Math.Round(at / pitch)) < 1e-9;
        Assert.All(Layout.Frames.Values, f => Assert.True(
            On(f.X - BoardLayout.FrameWide / 2 - BoardLayout.Pad, BoardLayout.Cell) && On(f.Y - BoardLayout.FrameTall / 2 - BoardLayout.Head, BoardLayout.Row), f.Id));
    }
}
