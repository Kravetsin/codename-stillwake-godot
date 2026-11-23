using Stillwake.Game.Features.Crew;
using Stillwake.Game.Shared;
using Xunit;
using I = Stillwake.Game.Shared.DroneInk;

namespace Stillwake.Game.Tests;

/// <summary>The player's colour in the drone's embers, the pick «С облачком».</summary>
public class SaltsTests
{
    private static readonly Glint Ember = Glint.Of(I.Ember), Red = Glint.Of(I.Red);

    public static TheoryData<int> Colours => new(Suits.All);

    private static void Near(Glint want, Glint got)
    {
        Assert.Equal(want.R, got.R, 4);
        Assert.Equal(want.G, got.G, 4);
        Assert.Equal(want.B, got.B, 4);
    }

    [Theory]
    [MemberData(nameof(Colours))]
    public void WhatLeavesTheCoalBurnsFromEmberThroughAPaleFlameIntoThePlayersColour(int rgb)
    {
        var salts = Salts.Of(rgb);
        Near(Ember, salts.Spark(0));
        Near(Ember, salts.Spark(I.BurnEmber - 0.01f));
        Near(Ember.Paled(I.Cream), salts.Spark(I.BurnCream));
        Near(Glint.Of(rgb), salts.Spark(I.BurnSalt));
        Near(Glint.Of(rgb), salts.Spark(1));
        Near(Glint.Of(rgb), salts.Line());
        Near(Ember, salts.Coal());
    }

    [Theory]
    [MemberData(nameof(Colours))]
    public void EachColourHoldsALightOnePaledLessADarkOneLitMore(int rgb)
    {
        var salts = Salts.Of(rgb);
        Assert.InRange(salts.Room, I.RoomLeast, 1);
        Assert.InRange(salts.Gain, I.GainLeast, I.GainMost);
        bool lighter = Glint.Of(rgb).Brightness > Ember.Brightness;
        Assert.Equal(lighter, salts.Room < 1);
        Assert.Equal(lighter, salts.Gain < 1);
    }

    [Theory]
    [MemberData(nameof(Colours))]
    public void AFullAlarmIsTheBareRedWithNoCloud(int rgb)
    {
        var alarmed = Salts.Of(rgb).Reddened(1.4f);
        foreach (float burn in new[] { 0, 0.4f, 1 }) Near(Red, alarmed.Spark(burn));
        Near(Red, alarmed.Line());
        Near(Red, alarmed.Coal());
        Near(Red.Paled(I.TailPale), alarmed.Spark(1, I.TailPale));
        Assert.Equal(1, alarmed.Gain, 4);
        Assert.Equal(1, alarmed.Lift(I.OrbitLift), 4);
        Assert.Equal(0, alarmed.Cloud, 4);
    }

    [Fact]
    public void ASparkByTheCoreIsEmberOneFarOutThePlayersAndOneOnAShapeIsBornAgain()
    {
        Assert.Equal(0, Salts.SparkBurn(3, 1, 0, 2, shaped: false));
        Assert.Equal(1, Salts.SparkBurn(3, 1, 0, 9, shaped: false));
        var lives = Enumerable.Range(0, 100).Select(k => Salts.SparkBurn(3, k * 0.02, 0, 19, shaped: true)).ToList();
        Assert.True(lives.Min() < I.BurnEmber);
        Assert.Equal(1, lives.Max());
        Assert.Equal(0, Salts.DotBurn(5, 1, away: 0));
        Assert.Equal(1, Enumerable.Range(0, 100).Max(k => Salts.DotBurn(5, k * 0.05, away: 1)));
    }
}
