using Stillwake.Game.Shared;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>A piece of light read from the long strip (hud-vitals): whole at any length, never past the strip's ends.</summary>
public class StripCutsTests
{
    private const float M = StripCuts.Margin, Strip = StripCuts.Long + 2 * StripCuts.Margin;

    [Theory]
    [InlineData(6)]
    [InlineData(120)]
    [InlineData(304)]
    [InlineData(305)]
    [InlineData(477)]
    [InlineData(1100)]
    public void APieceIsDrawnWholeFromItsMarginToItsEndsAndReadsOnlyTheStrip(float length)
    {
        for (float a = -M; a <= length + M; a += 0.25f)
            Assert.InRange(M + StripCuts.Column(a, length), -1e-3f, Strip + 1e-3f);
        // Its last columns are the strip's own end, its first the strip's start.
        Assert.Equal(Strip, M + StripCuts.Column(length + M, length), 3);
        Assert.Equal(0, M + StripCuts.Column(-M, length), 3);
    }

    [Fact]
    public void APieceNoLongerThanTheStripIsItsStartThenItsEnd()
    {
        Assert.Equal(191.5f, StripCuts.Column(191.5f, 200));
        Assert.Equal(192f + 104, StripCuts.Column(192, 200));
        Assert.Equal(200f + 104, StripCuts.Column(200, 200));
    }

    [Fact]
    public void ALongPieceRepeatsTheBodyWithoutAJumpInside()
    {
        const float length = 1100, body = StripCuts.Long - 2 * StripCuts.Cap;
        // Within one body's stretch the columns run on one for one; a body's length on, they read the same again.
        for (float a = StripCuts.Long - StripCuts.Cap; a < length - StripCuts.Cap - body; a += 7.5f)
        {
            Assert.Equal(StripCuts.Column(a, length), StripCuts.Column(a + body, length), 3);
            Assert.InRange(StripCuts.Column(a, length), StripCuts.Cap - 1e-3f, StripCuts.Long - StripCuts.Cap + 1e-3f);
        }
        Assert.Equal(100, StripCuts.Column(100, length));
    }
}
