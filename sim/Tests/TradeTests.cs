using Stillwake.Content;
using Stillwake.Sim.Items;
using Xunit;

namespace Stillwake.Sim.Tests;

/// <summary>Selling goods for the ship's credits at a trade station (2026-10-30-trade-and-tech).</summary>
public class TradeTests
{
    private const int Width = 10;
    private static int At(int x, int y) => 2 * y * 2 * Width + 2 * x;

    private static (Scenario W, int Id) SetUp()
    {
        string[] rows =
        [
            "##########",
            "#$$......#",
            "#$$......#",
            "##########",
        ];
        var w = new Scenario(Layout.Parse(rows, null), doubled: true);
        return (w, w.M.Grid.MachineAt(At(1, 1))?.Id ?? 0);
    }

    [Fact]
    public void SellsOneUnitEveryBeatAtItsPriceUntilNoneIsLeft()
    {
        var (w, id) = SetUp();
        w.FillCrate(id, ("scrapChunk", 3));
        w.Run(Market.SellEvery);
        Assert.Equal(2, w.M.Research.Credits);
        Assert.Equal(2, w.M.Items.Crate(id)[0]?.Count);
        w.Run(2 * Market.SellEvery);
        Assert.Equal(6, w.M.Research.Credits);
        Assert.Null(w.M.Items.Crate(id)[0]);
        w.Run(5 * Market.SellEvery);
        Assert.Equal(6, w.M.Research.Credits);
    }

    [Fact]
    public void APriceNeverFallsHoweverMuchIsSold()
    {
        var (w, id) = SetUp();
        w.FillCrate(id, ("frame", 50));
        w.Run(50 * Market.SellEvery);
        Assert.Equal(50 * Market.PriceOf("frame"), w.M.Research.Credits);
    }

    [Fact]
    public void WhatHasNoPriceSitsUnsoldAndWhatHasOneInTheNextSlotSells()
    {
        var (w, id) = SetUp();
        Assert.Null(Market.PriceOf("fireAxe"));
        w.FillCrate(id, ("fireAxe", 1), ("wire", 2));
        w.Run(2 * Market.SellEvery);
        Assert.Equal(2 * Market.PriceOf("wire"), w.M.Research.Credits);
        Assert.Equal("fireAxe", w.M.Items.Crate(id)[0]?.Item);
        Assert.Null(w.M.Items.Crate(id)[1]);
    }

    [Fact]
    public void TwoStationsSellEachOnTheBeat()
    {
        string[] rows =
        [
            "##########",
            "#$$$$....#",
            "#$$$$....#",
            "##########",
        ];
        var w = new Scenario(Layout.Parse(rows, null), doubled: true);
        int a = w.M.Grid.MachineAt(At(1, 1))!.Id, b = w.M.Grid.MachineAt(At(3, 1))!.Id;
        w.FillCrate(a, ("wire", 1));
        w.FillCrate(b, ("wire", 1));
        w.Run(Market.SellEvery);
        Assert.Equal(2 * Market.PriceOf("wire"), w.M.Research.Credits);
    }

    [Fact]
    public void TheSlotRemembersWhatItSoldOutOf()
    {
        var (w, id) = SetUp();
        w.GiveItems(0, ("wire", 1));
        int slot = w.M.Items.Inventory(0).ToList().FindIndex(s => s?.Item == "wire");
        w.Do(new Items.Move(new Items.Place(slot), new Target(Crate: id), 1)).Run();
        w.Run(Market.SellEvery);
        Assert.Null(w.M.Items.Crate(id)[0]);
        Assert.Equal("wire", w.M.Items.KeptAt(id, 0));
    }

    [Fact]
    public void AHandTakingASlotsWholeStackForgetsIt()
    {
        var (w, id) = SetUp();
        w.GiveItems(0, ("wire", 2));
        int slot = w.M.Items.Inventory(0).ToList().FindIndex(s => s?.Item == "wire");
        w.Do(new Items.Move(new Items.Place(slot), new Target(Crate: id), 2)).Run();
        Assert.Equal("wire", w.M.Items.KeptAt(id, 0));
        w.Do(new Items.Move(new Items.Place(0, id), new Target(), 2)).Run();
        Assert.Null(w.M.Items.Crate(id)[0]);
        Assert.Null(w.M.Items.KeptAt(id, 0));
    }

    [Fact]
    public void SellsFromTheFirstPricedSlot()
    {
        var (w, id) = SetUp();
        w.FillCrate(id, ("wire", 1), ("frame", 1));
        w.Run(Market.SellEvery);
        Assert.Equal(Market.PriceOf("wire"), w.M.Research.Credits);
        Assert.Equal("frame", w.M.Items.Crate(id)[1]?.Item);
    }

    /// <summary>A save mid-beat loads onto the same beat.</summary>
    [Fact]
    public void SavesItsBeatAndLoadsOntoIt()
    {
        var (w, id) = SetUp();
        w.FillCrate(id, ("wire", 5));
        w.Run(Market.SellEvery + 3);
        var before = w.World.Save();
        w.Reload();
        Assert.Equal(before, w.World.Save());
        w.Run(Market.SellEvery - 4);
        Assert.Equal(Market.PriceOf("wire"), w.M.Research.Credits);
        w.Run();
        Assert.Equal(2 * Market.PriceOf("wire"), w.M.Research.Credits);
    }
}
