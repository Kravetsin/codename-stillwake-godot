using System;
using Stillwake.Game.Features.Crew;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>The near finger pokes the drone's hologram as its player works the window (drone-holo).</summary>
public class SheetPokeTests
{
    [Fact]
    public void APokeRaisesTheArmJabsOnceAndLetsItDownAfter()
    {
        var poke = new SheetPoke();
        poke.Poke(0.5, 0);
        poke.Step(SheetPoke.InMs, shows: true);
        Assert.Equal(1, poke.Weight);
        Assert.True(poke.Depth > 0);
        poke.Step(SheetPoke.JabMs, shows: true);
        Assert.Equal(0, poke.Depth);
        Assert.Equal(1, poke.Weight);
        poke.Step(SheetPoke.HoldMs, shows: true);
        poke.Step(SheetPoke.OutMs, shows: true);
        Assert.Equal(0, poke.Weight);
    }

    [Fact]
    public void FromHangingTheFingerGoesStraightToTheSpot()
    {
        var poke = new SheetPoke();
        poke.Poke(-0.8, 0.3);
        Assert.Equal((-0.8, 0.3), (poke.Along, poke.Across));
    }

    [Fact]
    public void QuickPokesSlideTheFingerAndJabNoOftenerThanTheirRestart()
    {
        var poke = new SheetPoke();
        poke.Poke(0, 0);
        poke.Step(SheetPoke.RestartMs / 2, shows: true);
        double deeper = poke.Depth;
        poke.Poke(1, 0);
        Assert.Equal(0, poke.Along);
        // Not restarted: the jab goes on from where it was.
        poke.Step(1, shows: true);
        Assert.True(poke.Depth != 0 && Math.Abs(poke.Depth - deeper) < 0.1);
        poke.Step(SheetPoke.SlideMs * 10, shows: true);
        Assert.Equal(1, poke.Along, 3);
        poke.Poke(-1, 0);
        poke.Step(1, shows: true);
        Assert.True(poke.Depth > 0 && poke.Depth < 0.1);
    }

    [Fact]
    public void ABandGoneTakesTheArmDownAtOnce()
    {
        var poke = new SheetPoke();
        poke.Poke(0, 0);
        poke.Step(SheetPoke.InMs, shows: true);
        poke.Step(1, shows: false);
        Assert.Equal(0, poke.Weight);
        Assert.Equal(0, poke.Depth);
    }

    [Fact]
    public void TheHandStopsShortOfTheSpotByTheFingerAndGoesPastInAJab()
    {
        var poke = new SheetPoke();
        Assert.Equal(10 - SheetPoke.Finger - SheetPoke.Hover, poke.HandFor((10, 0)).X, 9);
        poke.Poke(0, 0);
        poke.Step(SheetPoke.JabMs * 0.35, shows: true);
        Assert.Equal(10 - SheetPoke.Finger + SheetPoke.Past, poke.HandFor((10, 0)).X, 6);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(-1)]
    public void ASpotsTopIsUpWhicheverWayTheHeroFaces(int facing)
    {
        double toward = facing == 1 ? 0.3 : Math.PI - 0.3, half = 0.6;
        Assert.True(Math.Sin(SheetHoloLook.AngleOf(toward, half, -1, facing)) < Math.Sin(SheetHoloLook.AngleOf(toward, half, 1, facing)));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(-1)]
    public void TheLineToAPlannedCellCrossesTheBandWhereItsSpotIs(int facing)
    {
        double toward = facing == 1 ? 0.3 : Math.PI - 0.3, half = 0.6;
        double angle = SheetHoloLook.AngleOf(toward, half, 0.4, facing);
        var cell = (Math.Cos(angle) * 50, Math.Sin(angle) * 50);
        Assert.Equal(0.4, SheetHoloLook.AlongTo((0, 0), toward, half, cell, facing), 9);
        Assert.Equal(-1, SheetHoloLook.AlongTo((0, 0), toward, half, (0, -50), facing));
    }
}
