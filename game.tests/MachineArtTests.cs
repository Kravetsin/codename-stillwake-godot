using Xunit;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Tests;

/// <summary>Every machine has its picture: the grid draws <c>machine_&lt;kind&gt;</c>, and a missing one throws on the first frame it is seen.</summary>
public class MachineArtTests
{
    [Fact]
    public void EveryMachineHasItsPicture() =>
        Assert.All(Spec.Machines.All, m => Assert.True(Png.Has($"machine_{m.Kind}"), $"game/art/hd/machine_{m.Kind}.png"));

    /// <summary>Every crop a grow station grows has its plant at each of the four stages the station shows (<c>{kind}_{crop}_{stage}</c>).</summary>
    [Fact]
    public void EveryCropHasItsPlant()
    {
        var crops = (from s in Spec.Stations.All where s.Process == "grow" from i in Spec.Items.All where i.Process == "grow" select (s.Machine, i.Id)).ToList();
        Assert.NotEmpty(crops);
        foreach (var (kind, crop) in crops)
            for (int stage = 0; stage < 4; stage++) Assert.True(Png.Has($"{kind}_{crop}_{stage}"), $"game/art/hd/{kind}_{crop}_{stage}.png");
    }

    /// <summary>One with an exhaust is drawn as much wider as its nozzle reaches out of its left side (the engine), its footprint's height.</summary>
    [Fact]
    public void AMachineWithAnExhaustIsDrawnItsNozzleWider()
    {
        var exhausting = Spec.Machines.All.Where(m => m.Exhaust != null).ToList();
        Assert.NotEmpty(exhausting);
        foreach (var m in exhausting) Assert.Equal(((m.Width + m.Exhaust!.Value) * 8, m.Height * 8), Png.SizeOf($"machine_{m.Kind}"));
    }
}
