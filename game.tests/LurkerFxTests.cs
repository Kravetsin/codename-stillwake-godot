using Stillwake.Game.Features.Creatures;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>The capback's attacks as drawn (lurker-attacks): its gas cloud with its spores, its glob, its puddle; every part one the export drew.</summary>
public class LurkerFxTests
{
    [Fact]
    public void APatchGoesOnlyOverItsLastSecond()
    {
        Assert.Equal(0, LurkerFx.Going(3));
        Assert.Equal(0, LurkerFx.Going(LurkerFx.GoS));
        Assert.Equal(0.5, LurkerFx.Going(LurkerFx.GoS / 2), 6);
        Assert.Equal(1, LurkerFx.Going(0));
    }

    [Fact]
    public void TheGasIsThrownFromTheCapsAndSettlesRoundTheFeetWithItsSpores()
    {
        var (early, _) = Gas(0.07, 0);
        Assert.All(early, s => Assert.True(Math.Abs(s.X - 100) < 12 && Math.Abs(s.Y - 38) < 12, $"{s.Part} at ({s.X}, {s.Y})"));
        var (hung, lights) = Gas(2, 0);
        Assert.Equal(8, hung.Count(s => s.Part.StartsWith("gasPuff")));
        Assert.Equal(22, hung.Count(s => s.Part == "spore"));
        Assert.All(hung.Where(s => s.Part == "spore"), s => Assert.True(s.Shine != null && s.Y < 50 + 4 && s.Y > 50 - 40));
        Assert.True(lights.Single().K > 0);
    }

    [Fact]
    public void ItsSporesBreatheAndWanderSmoothlyFrameToFrame()
    {
        var (a, _) = Gas(2, 0);
        var (b, _) = Gas(2 + 1.0 / 60, 0);
        var sa = a.Where(s => s.Part == "spore").ToList();
        var sb = b.Where(s => s.Part == "spore").ToList();
        for (int i = 0; i < sa.Count; i++)
        {
            Assert.True(Math.Abs(sa[i].X - sb[i].X) < 0.5 && Math.Abs(sa[i].Y - sb[i].Y) < 0.5);
            Assert.True(Math.Abs(sa[i].A - sb[i].A) < 0.05);
        }
    }

    [Fact]
    public void GoneTheGasShowsNothingAndGivesNoLight()
    {
        var (gone, lights) = Gas(5, 1);
        Assert.All(gone, s => Assert.Equal(0, s.A, 6));
        Assert.Equal(0, lights.Single().K, 6);
    }

    [Fact]
    public void APuddleShowsOnlyOnceItsGlobLandsAndSplatsFirst()
    {
        var sprites = new List<LurkerFx.Sprite>();
        var lights = new List<LurkerArt.Light>();
        LurkerFx.Puddle(sprites, lights, -0.1, 0, 40, 80);
        Assert.Empty(sprites);
        LurkerFx.Puddle(sprites, lights, 0.1, 0, 40, 80);
        Assert.Contains(sprites, s => s.Part == "splat");
        sprites.Clear();
        LurkerFx.Puddle(sprites, lights, 3, 0, 40, 80);
        Assert.DoesNotContain(sprites, s => s.Part == "splat");
        Assert.Equal(1, sprites.Single(s => s.Part == "pool").Sx, 6);
    }

    [Fact]
    public void EveryPartDrawnIsOneTheExportDrew()
    {
        var sprites = new List<LurkerFx.Sprite>();
        var lights = new List<LurkerArt.Light>();
        LurkerFx.Gas(sprites, lights, 2, 0, 0, 0, 0, -12);
        LurkerFx.Glob(sprites, lights, 0, 0, 0.3, 0.1);
        LurkerFx.Puddle(sprites, lights, 0.1, 0, 0, 0);
        Assert.All(sprites, s => Assert.True(CreatureArt.LurkerFx.ContainsKey(s.Part), s.Part));
        Assert.All(sprites, s => Assert.Equal(s.Shine != null, CreatureArt.LurkerFx[s.Part].Glow));
    }

    private static (List<LurkerFx.Sprite>, List<LurkerArt.Light>) Gas(double t, double going)
    {
        var sprites = new List<LurkerFx.Sprite>();
        var lights = new List<LurkerArt.Light>();
        LurkerFx.Gas(sprites, lights, t, going, 100, 50, 100, 38);
        return (sprites, lights);
    }
}
