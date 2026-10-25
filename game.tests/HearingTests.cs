using System.Collections.Generic;
using Stillwake.Game.Features.Debris;
using Stillwake.Game.Features.Sound;
using Stillwake.Sim;
using Stillwake.Sim.Debris;
using Xunit;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Tests;

/// <summary>What is heard in space (ADR 0041): only what this player's hero touches, dulled; the world silent; a bed of music.</summary>
public class HearingTests
{
    /// <summary>An axe's hit in space striking <paramref name="pieces"/>, as <see cref="Struck"/> reads it back: a test's own key.</summary>
    private static HewWrecks Hit(params int[] pieces) => new(Spec.Weapons.Axe.Item, pieces[0], pieces.Length > 1 ? pieces[1] : -1, 0);

    /// <summary>The pieces <see cref="Hit"/> said its hit strikes, as the sim's query would.</summary>
    private static IReadOnlyList<int> Struck(HewWrecks hit) => hit.Y < 0 ? [hit.X] : [hit.X, hit.Y];

    [Fact]
    public void AnOwnHitIsHeardWhenItsTickHasRunAndNotWhileItFlies()
    {
        var own = new OwnHits();
        var hit = Hit(7);
        own.Read([hit], Struck);
        Assert.False(own.Ran(7));
        own.Read([], Struck);
        Assert.True(own.Ran(7));
        own.Read([], Struck);
        Assert.False(own.Ran(7));
    }

    [Fact]
    public void AnotherHerosHitIsNeverOwnForItWasNeverInFlightHere()
    {
        var own = new OwnHits();
        // This peer's grab flew and landed; the wreck's damage rose from a friend's hit meanwhile.
        own.Read([new Grab(7)], Struck);
        own.Read([], Struck);
        Assert.False(own.Ran(7));
    }

    [Fact]
    public void AHitRunsAsTheNextOnTheSamePieceIsSentAndOnlyItCounts()
    {
        var own = new OwnHits();
        var (first, second) = (Hit(7), Hit(7));
        own.Read([first], Struck);
        own.Read([second], Struck);
        Assert.True(own.Ran(7));
        own.Read([second], Struck);
        Assert.False(own.Ran(7));
    }

    [Fact]
    public void OneSwingThroughTwoWrecksHearsBoth()
    {
        var own = new OwnHits();
        own.Read(new List<Command> { Hit(3, 9), new Grab(5) }, Struck);
        own.Read([], Struck);
        Assert.True(own.Ran(3));
        Assert.True(own.Ran(9));
        Assert.False(own.Ran(5));
    }

    [Fact]
    public void AboardEveryBusIsClear()
    {
        var mix = Hearing.At(0);
        Assert.Equal(Sound.ClearHz, mix.WorldHz, 3);
        Assert.Equal(1, mix.WorldGain);
        Assert.Equal(Sound.ClearHz, mix.SuitHz, 3);
        Assert.Equal(1, mix.SuitLows);
    }

    [Fact]
    public void FloatingTheWorldFallsSilentAndTheSuitDullsToAThud()
    {
        var mix = Hearing.At(1);
        Assert.Equal(Sound.MuffledHz, mix.WorldHz, 3);
        Assert.Equal(Hearing.Faint, mix.WorldGain);
        Assert.Equal(Sound.SuitMuffledHz, mix.SuitHz, 3);
        Assert.Equal(Sound.SuitLowsGain, mix.SuitLows, 5);
        // Heard still: the thud keeps the lows, and only the world goes.
        Assert.InRange(mix.SuitHz, 500, 1500);
        Assert.True(mix.SuitLows > 1);
    }

    [Fact]
    public void HalfwayTheCutoffIsHalfTheOctavesDown()
    {
        Assert.Equal(System.Math.Sqrt(Sound.ClearHz * Sound.SuitMuffledHz), Hearing.At(0.5).SuitHz, 3);
    }

    [Fact]
    public void TheBedOfSpaceFadesInOverItsTimeAndOutOverItsOwn()
    {
        double share = 0;
        for (int i = 0; i < 10; i++) share = Hearing.Ease(share, true, Sound.SpaceInS / 20, Sound.SpaceInS, Sound.SpaceOutS);
        Assert.Equal(0.5, share, 6);
        share = Hearing.Ease(share, true, Sound.SpaceInS, Sound.SpaceInS, Sound.SpaceOutS);
        Assert.Equal(1, share);
        share = Hearing.Ease(share, false, Sound.SpaceOutS / 4, Sound.SpaceInS, Sound.SpaceOutS);
        Assert.Equal(0.75, share, 6);
        share = Hearing.Ease(share, false, Sound.SpaceOutS, Sound.SpaceInS, Sound.SpaceOutS);
        Assert.Equal(0, share);
    }
}
