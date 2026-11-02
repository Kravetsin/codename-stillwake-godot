using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Stillwake.Content;
using Stillwake.Sim.Items;
using Stillwake.Sim.Trade;
using Xunit;

namespace Stillwake.Sim.Tests;

/// <summary>Selling goods for the ship's credits by capsule (2026-10-30-trade-capsule).</summary>
public class TradeTests
{
    private const int Width = 10;
    private static int At(int x, int y) => 2 * y * 2 * Width + 2 * x;

    private static (Scenario W, int Id) SetUp(char station = '$')
    {
        string[] rows =
        [
            "##########",
            $"#{station}{station}......#",
            $"#{station}{station}......#",
            "##########",
        ];
        var w = new Scenario(Layout.Parse(rows, null), doubled: true);
        return (w, w.M.Grid.MachineAt(At(1, 1))?.Id ?? 0);
    }

    [Fact]
    public void EverythingPricedLeavesAtOnceAndPaysWhenTheCapsuleComesBack()
    {
        var (w, id) = SetUp();
        w.FillCrate(id, ("scrapChunk", 3), ("frame", 2));
        int worth = 3 * Market.PriceOf("scrapChunk")!.Value + 2 * Market.PriceOf("frame")!.Value;
        Assert.Equal(worth, w.M.Trade.Worth(id));
        w.Do(new SendCapsule(id)).Run();
        Assert.All(w.M.Items.Crate(id), s => Assert.Null(s));
        Assert.Equal(new Flight(id, Market.CapsuleFlight - 1, worth), w.M.Trade.FlightOf(id));
        w.Run(Market.CapsuleFlight - 2);
        Assert.Equal(0, w.M.Research.Credits);
        w.Run();
        Assert.Equal(worth, w.M.Research.Credits);
        Assert.Null(w.M.Trade.FlightOf(id));
    }

    [Fact]
    public void ACapsuleFliesThirtySeconds() => Assert.Equal(30 * 20, Market.CapsuleFlight);

    [Fact]
    public void NoLimitOnWhatACapsuleCarries()
    {
        var (w, id) = SetUp();
        w.FillCrate(id, ("wire", 250), ("wire", 250), ("wire", 250), ("wire", 250));
        w.Do(new SendCapsule(id)).Run(Market.CapsuleFlight);
        Assert.Equal(1000 * Market.PriceOf("wire"), w.M.Research.Credits);
    }

    [Fact]
    public void NothingSellsUntilItIsSent()
    {
        var (w, id) = SetUp();
        w.FillCrate(id, ("wire", 3));
        w.Run(2 * Market.CapsuleFlight);
        Assert.Equal(0, w.M.Research.Credits);
        Assert.Equal(3, w.M.Items.Crate(id)[0]?.Count);
    }

    [Fact]
    public void RefusedForNoStationNothingPricedOrACapsuleAway()
    {
        var (w, id) = SetUp();
        w.Do(new SendCapsule(9999)).Do(new SendCapsule(id)).Run();
        w.FillCrate(id, ("fireAxe", 1));
        w.Do(new SendCapsule(id)).Run();
        w.FillCrate(id, ("fireAxe", 1), ("wire", 1));
        w.Do(new SendCapsule(id)).Run();
        w.FillCrate(id, ("fireAxe", 1), ("wire", 1));
        w.Do(new SendCapsule(id)).Run();
        Assert.Equal(new[] { "trade.notStation", "trade.nothingToSell", "trade.nothingToSell", "trade.capsuleAway" }, w.Refusals.Select(r => r.Reason));
    }

    [Fact]
    public void WhatHasNoPriceStaysBehind()
    {
        var (w, id) = SetUp();
        Assert.Null(Market.PriceOf("fireAxe"));
        w.FillCrate(id, ("fireAxe", 1), ("wire", 2));
        w.Do(new SendCapsule(id)).Run(Market.CapsuleFlight);
        Assert.Equal(2 * Market.PriceOf("wire"), w.M.Research.Credits);
        Assert.Equal("fireAxe", w.M.Items.Crate(id)[0]?.Item);
        Assert.Null(w.M.Items.Crate(id)[1]);
    }

    [Fact]
    public void SlotsFillWhileItIsAwayAndGoWithTheNextAtTheirOwnWorth()
    {
        var (w, id) = SetUp();
        w.FillCrate(id, ("wire", 1));
        w.Do(new SendCapsule(id)).Run(10);
        w.FillCrate(id, ("wire", 2));
        w.Run(Market.CapsuleFlight - 10);
        Assert.Equal(Market.PriceOf("wire"), w.M.Research.Credits);
        Assert.Equal(2, w.M.Items.Crate(id)[0]?.Count);
        w.Do(new SendCapsule(id)).Run(Market.CapsuleFlight);
        Assert.Equal(3 * Market.PriceOf("wire"), w.M.Research.Credits);
    }

    [Fact]
    public void TwoStationsFlyEachItsOwn()
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
        w.Do(new SendCapsule(a)).Run(5);
        w.Do(new SendCapsule(b)).Run(Market.CapsuleFlight - 5);
        Assert.Equal(Market.PriceOf("wire"), w.M.Research.Credits);
        w.Run(5);
        Assert.Equal(2 * Market.PriceOf("wire"), w.M.Research.Credits);
    }

    [Fact]
    public void AnAutoStationSendsWhatItHoldsAsSoonAsItsCapsuleIsHome()
    {
        var (w, id) = SetUp('A');
        w.Run(5);
        Assert.Null(w.M.Trade.FlightOf(id));
        w.FillCrate(id, ("wire", 1));
        w.Run();
        Assert.Equal(Market.CapsuleFlight - 1, w.M.Trade.FlightOf(id)?.Left);
        w.FillCrate(id, ("wire", 2));
        w.Run(Market.CapsuleFlight - 1);
        Assert.Equal(Market.PriceOf("wire"), w.M.Research.Credits);
        Assert.Null(w.M.Trade.FlightOf(id));
        w.Run();
        Assert.Equal(2 * Market.PriceOf("wire"), w.M.Trade.FlightOf(id)?.Credits);
    }

    [Fact]
    public void AStationTakenApartStillGetsItsCapsulesCredits()
    {
        var (w, id) = SetUp();
        w.FillCrate(id, ("wire", 4));
        w.Do(new SendCapsule(id)).Run();
        w.Do(new Stillwake.Sim.Building.SetInstant(true)).Do(new Stillwake.Sim.Building.Deconstruct(At(1, 1), "machine")).Run();
        int plan = w.M.Building.Plans.Single().Id;
        while (w.M.Building.Plans.Count > 0) w.Do(new Stillwake.Sim.Building.Work(plan)).Run();
        Assert.Null(w.M.Grid.MachineById(id));
        w.Run(Market.CapsuleFlight);
        Assert.Equal(4 * Market.PriceOf("wire"), w.M.Research.Credits);
    }

    [Fact]
    public void TheSlotRemembersWhatItSentOutOf()
    {
        var (w, id) = SetUp();
        w.GiveItems(0, ("wire", 1));
        int slot = w.M.Items.Inventory(0).ToList().FindIndex(s => s?.Item == "wire");
        w.Do(new Items.Move(new Items.Place(slot), new Target(Crate: id), 1)).Run();
        w.Do(new SendCapsule(id)).Run();
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

    /// <summary>A save mid-flight loads onto the same tick of it.</summary>
    [Fact]
    public void SavesAFlightAndLoadsOntoIt()
    {
        var (w, id) = SetUp();
        w.FillCrate(id, ("wire", 5));
        w.Do(new SendCapsule(id)).Run(100);
        var before = w.World.Save();
        w.Reload();
        Assert.Equal(before, w.World.Save());
        w.Run(Market.CapsuleFlight - 101);
        Assert.Equal(0, w.M.Research.Credits);
        w.Run();
        Assert.Equal(5 * Market.PriceOf("wire"), w.M.Research.Credits);
    }

    /// <summary>Goods put in and sent at random: once every capsule is home, the credits are exactly what left the slots was worth.</summary>
    [Property(MaxTest = 30)]
    public Property CreditsAreWhatLeftTheSlotsWasWorth()
    {
        string[] kinds = ["wire", "frame", "hullPlate", "scrapChunk"];
        var gen =
            from count in Gen.Choose(1, 25)
            from steps in Gen.Zip(Gen.Choose(0, 3), Gen.Choose(0, 200)).ArrayOf(count)
            select steps;
        return Prop.ForAll(gen.ToArbitrary(), steps =>
        {
            var (w, id) = SetUp();
            w.FillCrate(id, ("fireAxe", 1));
            int sent = 0;
            foreach (var (kind, wait) in steps)
            {
                if (w.M.Items.Crate(id)[1] == null) w.FillCrate(id, (kinds[kind], 1 + wait % 5));
                int worth = w.M.Trade.Worth(id);
                if (w.M.Trade.SendProblem(id) == null) sent += worth;
                w.Do(new SendCapsule(id)).Run(wait + 1);
            }
            w.Run(Market.CapsuleFlight);
            return (w.M.Research.Credits == sent).Label($"{w.M.Research.Credits} credits for {sent} sent");
        });
    }
}
