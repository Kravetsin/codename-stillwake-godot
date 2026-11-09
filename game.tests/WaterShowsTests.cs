using Stillwake.Game.Features.Water;
using Xunit;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Tests;

/// <summary>How water shows without a word and what the pointer says of it (water).</summary>
public class WaterShowsTests
{
    [Fact]
    public void ACisternShowsItsWaterUpItsGlassByQuartersAnyWaterAtLeastOne()
    {
        Assert.Equal(0, WaterShows.CisternFrame(0));
        Assert.Equal(1, WaterShows.CisternFrame(1));
        Assert.Equal(1, WaterShows.CisternFrame(Spec.Water.CisternCapacity / 4));
        Assert.Equal(2, WaterShows.CisternFrame(Spec.Water.CisternCapacity / 4 + 1));
        Assert.Equal(WaterShows.Full, WaterShows.CisternFrame(Spec.Water.CisternCapacity));
        // A cistern saved holding 225 litres, before it held 150 (liquid-volumes), shows full.
        Assert.Equal(WaterShows.Full, WaterShows.CisternFrame(225_000));
    }

    [Fact]
    public void BubblesCycleThroughTheirFrames()
    {
        Assert.Equal([0, 1, 2, 3, 0], new[] { 0, 1, 2, 3, 4 }.Select(i => WaterShows.WorkFrame(i * WaterShows.FrameMs + 1)));
    }

    [Fact]
    public void ThePointerTellsACisternsBottlesAndWhatAnElectrolyserDoesOrWhyItStands()
    {
        var cistern = Spec.Machines.Get("cistern");
        var electrolyser = Spec.Machines.Get("electrolyser");
        Assert.Equal(["Water · 10.50/150.00 l · 3 bottles"], WaterShows.Says(cistern, Spec.Water.Bottle * 7 / 2, null)!);
        Assert.Equal(["Water · 0.00/150.00 l · 0 bottles"], WaterShows.Says(cistern, 0, null)!);
        Assert.Equal(["Splitting 3.600 l/min of water into oxygen"], WaterShows.Says(electrolyser, 0, null)!);
        Assert.Equal(["Splitting 1.800 l/min of water into oxygen"], WaterShows.Says(electrolyser, 0, null, 500)!);
        Assert.Equal(["The tanks on its output are full"], WaterShows.Says(electrolyser, 0, "water.tanksFull")!);
        Assert.Null(WaterShows.Says(Spec.Machines.Get("tank"), 0, null));
    }

    [Fact]
    public void EveryFrameItShowsHasAPicture()
    {
        var names = Enumerable.Range(0, WaterShows.Full + 1).Select(i => $"cistern_fill_{i}")
            .Concat(Enumerable.Range(0, WaterShows.Frames).Select(i => $"electrolyser_work_{i}")).Append("electrolyser_stalled");
        Assert.All(names, name => Assert.True(Png.Has(name), name));
    }
}
