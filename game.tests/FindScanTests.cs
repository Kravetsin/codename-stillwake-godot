using System.Collections.Generic;
using Stillwake.Game.Features.Research;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>The drone scanning a find over the hero's head, its record flying to the research key.</summary>
public class FindScanTests
{
    private const double Single = FindScan.Lead + FindScan.Rise + FindScan.Sweep + FindScan.Fold + FindScan.Fly;
    private static readonly object Board = new();

    private static BoardBatch Found(params string[] items) => new(BoardSeen.CauseFound, [.. items], [.. items]);

    /// <summary>A scan that has read <paramref name="batches"/> already, with the tablet away.</summary>
    private static (FindScan Scan, List<BoardBatch> Batches) Seen(params BoardBatch[] batches)
    {
        var scan = new FindScan();
        var list = new List<BoardBatch>(batches);
        scan.Read(Board, list, open: false);
        return (scan, list);
    }

    [Fact]
    public void WhatWaitedWhenTheWorldOpenedPlaysNothing()
    {
        var (scan, _) = Seen(Found("iron"), Found("wire"));
        scan.Step(0.1, drone: true);
        Assert.Null(scan.Over);
        Assert.Equal(0, scan.Holding);
        Assert.False(scan.Busy);
    }

    [Fact]
    public void AFindComesInOverTheHeadIsSweptFoldsAndFliesToTheKeyIn135Seconds()
    {
        var (scan, batches) = Seen();
        batches.Add(Found("iron"));
        scan.Read(Board, batches, open: false);
        Assert.Equal(1, scan.Holding);
        scan.Step(FindScan.Lead + FindScan.Rise / 2, drone: true);
        Assert.Equal("iron", scan.Over!.Value.Item);
        Assert.InRange(scan.Over!.Value.In, 0.01, 0.99);
        Assert.Null(scan.Over!.Value.Sweep);
        scan.Step(FindScan.Rise / 2 + FindScan.Sweep / 2, drone: true);
        Assert.Equal(1, scan.Over!.Value.In);
        Assert.Equal(0.5, scan.Over!.Value.Sweep!.Value, 6);
        scan.Step(FindScan.Sweep / 2 + FindScan.Fold / 2, drone: true);
        Assert.Null(scan.Over);
        Assert.Equal(1, scan.Records);
        Assert.Equal(new FindRecord("iron", 0.5, null), Rounded(scan.RecordAt(0)));
        Assert.Equal(1, scan.Holding);
        scan.Step(FindScan.Fold / 2 + FindScan.Fly / 2, drone: true);
        Assert.Equal(0.25, scan.RecordAt(0).Fly!.Value, 6);
        Assert.Equal(0, scan.Glow);
        scan.Step(Single - (FindScan.Lead + FindScan.Rise + FindScan.Sweep + FindScan.Fold + FindScan.Fly / 2) + 1e-6, drone: true);
        Assert.Equal(0, scan.Records);
        Assert.Equal(0, scan.Holding);
        Assert.Equal(1, scan.Glow, 6);
        scan.Step(FindScan.Flare + 1e-6, drone: true);
        Assert.Equal(0, scan.Glow);
        Assert.False(scan.Busy);
    }

    [Fact]
    public void ItemsFoundTogetherAreScannedOneAfterAnotherEachRecordLeavingAfterItsScan()
    {
        var (scan, batches) = Seen();
        batches.Add(Found("iron", "wire", "gear"));
        scan.Read(Board, batches, open: false);
        Assert.Equal(1, scan.Holding);
        scan.Step(FindScan.Lead + FindScan.Rise + FindScan.Sweep + 1e-6, drone: true);
        Assert.Equal("wire", scan.Over!.Value.Item);
        Assert.Equal(1, scan.Records);
        Assert.Equal("iron", scan.RecordAt(0).Item);
        // The next comes in as the one before folds, then is swept in turn.
        scan.Step(FindScan.Fold, drone: true);
        Assert.Equal(1, scan.Over!.Value.In, 3);
        Assert.NotNull(scan.Over!.Value.Sweep);
        scan.Step(FindScan.Fly, drone: true);
        Assert.Equal(0, scan.Holding);
        Assert.Equal(1, scan.Glow, 3);
        scan.Step(FindScan.Sweep, drone: true);
        Assert.Equal("gear", scan.Over!.Value.Item);
        scan.Step(FindScan.Fold + FindScan.Sweep + FindScan.Fold + FindScan.Fly, drone: true);
        Assert.Null(scan.Over);
        Assert.Equal(0, scan.Records);
    }

    [Fact]
    public void EachBatchJoinsTheNumberAsItsFirstRecordLands()
    {
        var (scan, batches) = Seen();
        batches.Add(Found("iron"));
        scan.Read(Board, batches, open: false);
        scan.Step(0.5, drone: true);
        batches.Add(Found("wire"));
        scan.Read(Board, batches, open: false);
        Assert.Equal(2, scan.Holding);
        scan.Step(Single - 0.5 + 1e-6, drone: true);
        Assert.Equal(1, scan.Holding);
        scan.Step(Single, drone: true);
        Assert.Equal(0, scan.Holding);
    }

    [Fact]
    public void ABatchWithNoItemIsAnEmberFromTheEye()
    {
        var (scan, batches) = Seen();
        batches.Add(new BoardBatch(BoardSeen.CauseBuilt, ["workbench"], ["workbench"]));
        scan.Read(Board, batches, open: false);
        scan.Step(0.01, drone: true);
        Assert.Null(scan.Over);
        Assert.Equal(1, scan.Records);
        Assert.Null(scan.RecordAt(0).Item);
        scan.Step(FindScan.Fold + FindScan.Fly + 0.001, drone: true);
        Assert.Equal(0, scan.Holding);
        Assert.True(scan.Glow > 0.9);
    }

    [Fact]
    public void WithNoDroneAFindLandsAtOnceTheKeyFlaringAndCounting()
    {
        var (scan, batches) = Seen();
        batches.Add(Found("iron", "wire"));
        batches.Add(new BoardBatch(BoardSeen.CauseOpened, [], ["solarPanel"]));
        scan.Read(Board, batches, open: false);
        scan.Step(0.01, drone: false);
        Assert.Null(scan.Over);
        Assert.Equal(0, scan.Records);
        Assert.Equal(0, scan.Holding);
        Assert.Equal(1, scan.Glow);
    }

    [Fact]
    public void OpeningTheTabletPutsItAllOutAndAFindWhileOpenPlaysNothing()
    {
        var (scan, batches) = Seen();
        batches.Add(Found("iron"));
        scan.Read(Board, batches, open: false);
        scan.Step(0.5, drone: true);
        scan.Read(Board, batches, open: true);
        Assert.Null(scan.Over);
        Assert.Equal(0, scan.Holding);
        batches.Add(Found("wire"));
        scan.Read(Board, batches, open: true);
        scan.Read(Board, batches, open: false);
        scan.Step(0.5, drone: true);
        Assert.False(scan.Busy);
    }

    [Fact]
    public void AnotherWorldsBoardStartsOverWithNothingNew()
    {
        var (scan, _) = Seen();
        scan.Read(new object(), [Found("iron")], open: false);
        scan.Step(0.5, drone: true);
        Assert.False(scan.Busy);
    }

    [Fact]
    public void BatchesDroppedFromTheFrontLeaveOnlyTheNewOnesToPlay()
    {
        var (scan, batches) = Seen(Found("iron"), Found("wire"));
        batches.RemoveAt(0);
        batches.Add(Found("gear"));
        scan.Read(Board, batches, open: false);
        Assert.Equal(1, scan.Holding);
    }

    private static FindRecord Rounded(FindRecord r) => r with { Fold = System.Math.Round(r.Fold, 6) };
}
