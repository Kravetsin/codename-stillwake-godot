using Stillwake.Game.Render;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>The stages of effects (effects-on-their-layer): a layer's own, one a z, and the figures' held ones while they live.</summary>
public class StagesTests
{
    private static readonly Luminous Spark = Luminous.HitSpark;

    public StagesTests() => Stages.Reset();

    [Fact]
    public void ALayerHasOneStageItsPoolsItsOwnAndNamesSharingAZShareIt()
    {
        var fire = Stages.Of(Layers.Fire);
        Assert.Same(fire, Stages.Of(Layers.Fire));
        Assert.Same(fire, Stages.Of(Layers.Shimmer));
        Assert.Same(Stages.Of(Layers.Lava), Stages.Of(Layers.Flora));
        var machines = Stages.Of(Layers.Machines);
        Assert.True(machines.Empty);
        fire.Glims.Add(Spark, 0, 0, 0, 0);
        Assert.Single(fire.Glims.Live);
        Assert.Empty(machines.Glims.Live);
        Assert.False(fire.Empty);
        Assert.True(machines.Empty);
    }

    [Fact]
    public void AllListsTheWorldsInZOrderThenTheHeldAsTheyWereMadeTillDropped()
    {
        var effects = Stages.Of(Layers.Effects);
        var held = Stages.Held();
        var fire = Stages.Of(Layers.Fire);
        var later = Stages.Held();
        Assert.Equal([fire, effects, held, later], Stages.All);
        Stages.Drop(held);
        Assert.Equal([fire, effects, later], Stages.All);
    }

    [Fact]
    public void ClearEmptiesEveryStageAndResetForgetsThem()
    {
        var fire = Stages.Of(Layers.Fire);
        var held = Stages.Held();
        fire.Glims.Add(Spark, 0, 0, 0, 0);
        held.Flares.Glint(0, 0, 2, (255, 255, 255));
        held.Bolts.Add(() => (0, 0, 5, 0, 0), 10);
        Assert.Equal(1, Stages.Count);
        Stages.Clear();
        Assert.True(fire.Empty);
        Assert.True(held.Empty);
        Stages.Reset();
        Assert.Empty(Stages.All);
        Assert.NotSame(fire, Stages.Of(Layers.Fire));
    }
}
