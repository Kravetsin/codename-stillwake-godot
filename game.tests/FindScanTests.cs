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

    private static BoardBatch Learned(string form) => new(BoardSeen.CauseOpened, [], [form]);

    /// <summary>
    /// The lamp learned passing a lamp (drone-lessons): the drone goes to it, scans it 3 s, shows the hero the form in a vision 5 s (form-vision),
    /// and only then does the form's icon come in over the head and fly to the key as a find's, which flares longer.
    /// </summary>
    [Fact]
    public void ALessonGoesScansAndEnvisionsBeforeItsIconFliesAndItsLandingFlaresLonger()
    {
        var (scan, batches) = Seen();
        batches.Add(Learned("droneLamp"));
        scan.Read(Board, batches, open: false);
        Assert.Equal(1, scan.Holding);
        scan.Step(FindScan.Lead + FindScan.Go / 2, drone: true);
        Assert.Equal(new FindLesson("droneLamp", LessonStage.Go, 0.5), Rounded(scan.Learning!.Value));
        Assert.Null(scan.Over);
        scan.Step(FindScan.Go / 2 + FindScan.Study / 2, drone: true);
        Assert.Equal(new FindLesson("droneLamp", LessonStage.Study, 0.5), Rounded(scan.Learning!.Value));
        scan.Step(FindScan.Study / 2 + FindScan.Vision / 2, drone: true);
        Assert.Equal(new FindLesson("droneLamp", LessonStage.Vision, 0.5), Rounded(scan.Learning!.Value));
        Assert.Null(scan.Over);
        Assert.Equal(1, scan.Holding);
        scan.Step(FindScan.Vision / 2 + FindScan.Rise + FindScan.Sweep / 2, drone: true);
        Assert.Null(scan.Learning);
        Assert.Equal("droneLamp", scan.Over!.Value.Item);
        Assert.Equal(0.5, scan.Over!.Value.Sweep!.Value, 6);
        scan.Step(FindScan.Sweep / 2 + FindScan.Fold + FindScan.Fly + 1e-6, drone: true);
        Assert.Equal(0, scan.Holding);
        Assert.Equal(1, scan.Glow, 3);
        scan.Step(FindScan.Flare * 2, drone: true);
        Assert.InRange(scan.Glow, 0.1, 0.5);
        scan.Step(FindScan.LessonFlare, drone: true);
        Assert.False(scan.Busy);
    }

    /// <summary>The scan learned at the tenth wreck: its lesson comes before the wreck's loot, the batch counted once.</summary>
    [Fact]
    public void ALessonComesBeforeItsBatchsItems()
    {
        var (scan, batches) = Seen();
        batches.Add(new BoardBatch(BoardSeen.CauseFound, ["iron"], ["iron", "scanner"]));
        scan.Read(Board, batches, open: false);
        Assert.Equal(1, scan.Holding);
        scan.Step(FindScan.Lead + 0.01, drone: true);
        Assert.Equal("scanner", scan.Learning!.Value.Form);
        scan.Step(FindScan.Lesson + FindScan.Rise + FindScan.Sweep, drone: true);
        Assert.Equal("iron", scan.Over!.Value.Item);
        Assert.Equal(1, scan.Holding);
    }

    /// <summary>Finds while a lesson plays wait behind it, an ember from the eye too: nothing cuts it short.</summary>
    [Fact]
    public void FindsDuringALessonWaitBehindIt()
    {
        var (scan, batches) = Seen();
        batches.Add(Learned("droneAlarms"));
        scan.Read(Board, batches, open: false);
        scan.Step(FindScan.Lead + FindScan.Go + 0.5, drone: true);
        batches.Add(Found("iron"));
        batches.Add(new BoardBatch(BoardSeen.CauseBuilt, ["workbench"], ["workbench"]));
        scan.Read(Board, batches, open: false);
        Assert.Equal(3, scan.Holding);
        scan.Step(FindScan.Study, drone: true);
        Assert.Equal(LessonStage.Vision, scan.Learning!.Value.Stage);
        Assert.Equal(0, scan.Records);
        scan.Step(FindScan.Vision - 0.7, drone: true);
        Assert.Equal(0, scan.Records);
        scan.Step(0.4, drone: true);
        Assert.Equal("droneAlarms", scan.Over!.Value.Item);
        Assert.Equal(1, scan.Records);
        Assert.Null(scan.RecordAt(0).Item);
        scan.Step(FindScan.Rise + FindScan.Sweep, drone: true);
        Assert.Equal("iron", scan.Over!.Value.Item);
    }

    /// <summary>The tablet opened mid-lesson puts out the finds, but the lesson plays on, off the key's number, and one learned while it is open plays too.</summary>
    [Fact]
    public void OpeningTheTabletKeepsTheLessons()
    {
        var (scan, batches) = Seen();
        batches.Add(Learned("droneLamp"));
        batches.Add(Found("iron"));
        scan.Read(Board, batches, open: false);
        scan.Step(FindScan.Lead + FindScan.Go + 0.5, drone: true);
        batches.Add(Learned("droneAlarms"));
        scan.Read(Board, batches, open: true);
        Assert.Equal(("droneLamp", LessonStage.Study), (scan.Learning!.Value.Form, scan.Learning!.Value.Stage));
        Assert.Equal(0, scan.Holding);
        scan.Read(Board, batches, open: false);
        scan.Step(FindScan.Lesson + FindScan.Rise + FindScan.Sweep, drone: true);
        Assert.Equal("droneAlarms", scan.Learning!.Value.Form);
        scan.Step(FindScan.Lesson + FindScan.Rise + FindScan.Sweep + FindScan.Fold + FindScan.Fly, drone: true);
        Assert.Equal(0, scan.Records);
        scan.Step(FindScan.LessonFlare + 0.01, drone: true);
        Assert.False(scan.Busy);
    }

    /// <summary>
    /// A hero that cannot have the vision as its study ends (passed out) skips it: the icon comes in at once, and the hero able again meanwhile does
    /// not bring it back (form-vision).
    /// </summary>
    [Fact]
    public void ALessonWhoseHeroCannotHaveItsVisionSkipsIt()
    {
        var (scan, batches) = Seen();
        batches.Add(Learned("droneLamp"));
        scan.Read(Board, batches, open: false);
        scan.Step(FindScan.Lead + FindScan.Go + FindScan.Study / 2, drone: true, envisions: false);
        Assert.Equal(LessonStage.Study, scan.Learning!.Value.Stage);
        scan.Step(FindScan.Study / 2 + 0.01, drone: true, envisions: false);
        Assert.Null(scan.Learning);
        Assert.Equal("droneLamp", scan.Over!.Value.Item);
        scan.Step(0.01, drone: true);
        Assert.Null(scan.Learning);
        scan.Step(FindScan.Rise + FindScan.Sweep + FindScan.Fold + FindScan.Fly, drone: true);
        Assert.Equal(0, scan.Holding);
    }

    /// <summary>One whose hero could have it as its study ended plays it whole, though the hero could not meanwhile.</summary>
    [Fact]
    public void AVisionBegunPlaysWhole()
    {
        var (scan, batches) = Seen();
        batches.Add(Learned("droneLamp"));
        scan.Read(Board, batches, open: false);
        scan.Step(FindScan.Lead + FindScan.Go + FindScan.Study / 2, drone: true);
        scan.Step(FindScan.Study / 2 + 0.01, drone: true);
        scan.Step(FindScan.Vision - 0.1, drone: true, envisions: false);
        Assert.Equal(LessonStage.Vision, scan.Learning!.Value.Stage);
        Assert.Null(scan.Over);
    }

    [Fact]
    public void WithNoDroneALessonLandsAtOnce()
    {
        var (scan, batches) = Seen();
        batches.Add(Learned("droneAlarms"));
        scan.Read(Board, batches, open: false);
        scan.Step(0.01, drone: false);
        Assert.Null(scan.Learning);
        Assert.Equal(0, scan.Holding);
        Assert.Equal(1, scan.Glow, 3);
    }

    private static FindLesson Rounded(FindLesson l) => l with { Progress = System.Math.Round(l.Progress, 6) };

    private static FindRecord Rounded(FindRecord r) => r with { Fold = System.Math.Round(r.Fold, 6) };
}
