using FsCheck;
using FsCheck.Xunit;
using Stillwake.Game.Features.Flora;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>A bush or a tree swaying about its foot, and the wind that leans it now and then (plant-stirs).</summary>
public class SwayTests
{
    private const double Frame = 1 / 60.0;

    [Fact]
    public void ABrushedBushLeansItsWaySwingsBackAndStandsStillAgain()
    {
        var sway = new Sway(FloraLooks.Bush);
        sway.Kick(1);
        float most = 0, least = 0;
        int frames = 0;
        do
        {
            sway.Advance(Frame);
            most = Math.Max(most, sway.Lean);
            least = Math.Min(least, sway.Lean);
        }
        while (!sway.Still && ++frames < 1200);
        Assert.True(sway.Still, "settles within 20 s");
        Assert.True(most > 0.02f, $"leans the way it was pushed: {most}");
        Assert.True(least < 0, "and swings back past upright");
        Assert.True(-least < most, "less far back");
    }

    [Fact]
    public void ATreeSwaysLessAndSlowerThanABush()
    {
        static (float Most, int Peak) Swung(FloraLooks.Swing swing)
        {
            var sway = new Sway(swing);
            sway.Kick(1);
            float most = 0;
            int peak = 0;
            for (int i = 0; i < 240; i++)
            {
                sway.Advance(Frame);
                if (sway.Lean > most) (most, peak) = (sway.Lean, i);
            }
            return (most, peak);
        }
        var (bush, tree) = (Swung(FloraLooks.Bush), Swung(FloraLooks.Tree));
        Assert.True(tree.Most < bush.Most);
        Assert.True(tree.Peak > bush.Peak);
    }

    [Fact]
    public void TheWindHoldsItOverAndItComesBackWhenTheGustDies()
    {
        var sway = new Sway(FloraLooks.Bush) { Held = FloraLooks.Bush.Gust };
        for (int i = 0; i < 600; i++) sway.Advance(Frame);
        Assert.Equal(FloraLooks.Bush.Gust, sway.Lean, 3);
        Assert.False(sway.Still);
        sway.Held = 0;
        for (int i = 0; i < 1200 && !sway.Still; i++) sway.Advance(Frame);
        Assert.True(sway.Still);
    }

    /// <summary>However hard and often it is kicked, it never leans past its swing's most.</summary>
    [Property]
    public void NeverLeansPastItsMost(PositiveInt kicks, bool right)
    {
        var sway = new Sway(FloraLooks.Bush);
        for (int i = 0; i < Math.Min(kicks.Get, 50); i++)
        {
            sway.Kick(right ? 1 : -1);
            sway.Advance(Frame);
            Assert.InRange(sway.Lean, -FloraLooks.Bush.Most, FloraLooks.Bush.Most);
        }
    }

    [Fact]
    public void AGustComesAfterACalmRisesBlowsOneWayAndDies()
    {
        var wind = FloraLooks.Breeze;
        var gusts = new Gusts(wind, () => 0.5);
        double calm = (wind.Calm.From + wind.Calm.To) / 2, gust = (wind.Gust.From + wind.Gust.To) / 2;
        double seconds = 0;
        while (seconds < calm - 0.5)
        {
            gusts.Advance(Frame);
            seconds += Frame;
            Assert.False(gusts.Blowing);
            Assert.Equal(0, gusts.Strength);
        }
        float strongest = 0;
        int way = 0;
        while (seconds < calm + gust - 0.1)
        {
            gusts.Advance(Frame);
            seconds += Frame;
            if (!gusts.Blowing) continue;
            strongest = Math.Max(strongest, Math.Abs(gusts.Strength));
            way = way == 0 ? Math.Sign(gusts.Strength) : way;
            Assert.True(gusts.Strength == 0 || Math.Sign(gusts.Strength) == way, "one way the whole gust");
            Assert.InRange(Math.Abs(gusts.Strength), 0, 1);
        }
        Assert.True(strongest > 0.7f, $"a full gust: {strongest}");
        for (int i = 0; i < 60; i++) gusts.Advance(Frame);
        Assert.False(gusts.Blowing);
    }

    [Fact]
    public void AGustAskedForBlowsAtOnceAndRollsAlongTheGround()
    {
        var gusts = new Gusts(FloraLooks.Breeze, () => 0.5);
        gusts.Now();
        for (int i = 0; i < 120; i++) gusts.Advance(Frame);
        Assert.True(gusts.Blowing);
        float a = gusts.At(0), b = gusts.At(70);
        Assert.NotEqual(a, b);
        Assert.InRange(Math.Abs(a), 0.6f * Math.Abs(gusts.Strength) - 1e-4f, Math.Abs(gusts.Strength) + 1e-4f);
    }
}
