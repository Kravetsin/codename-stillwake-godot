using System.Linq;
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
        var cuts = StripCuts.Of(length, 0, length);
        Assert.Equal(-M, cuts[0].A, 4);
        Assert.Equal(length + M, cuts[^1].B, 4);
        for (int i = 1; i < cuts.Count; i++) Assert.Equal(cuts[i - 1].B, cuts[i].A, 4);
        Assert.All(cuts, c =>
        {
            Assert.InRange(M + c.A + c.Shift, -1e-3f, Strip + 1e-3f);
            Assert.InRange(M + c.B + c.Shift, -1e-3f, Strip + 1e-3f);
        });
        // Its last columns are the strip's own end, its first the strip's start.
        Assert.Equal(Strip, M + cuts[^1].B + cuts[^1].Shift, 3);
        Assert.Equal(0, M + cuts[0].A + cuts[0].Shift, 3);
    }

    [Fact]
    public void APieceNoLongerThanTheStripIsItsStartThenItsEnd()
    {
        var cuts = StripCuts.Of(200, 0, 200);
        Assert.Equal([(-M, 192f, 0f), (192f, 200 + M, 104f)], cuts);
    }

    [Fact]
    public void APartOfALongPieceIsCutFromTheSameColumnsAsTheWhole()
    {
        var whole = StripCuts.Of(700, 0, 700);
        var part = StripCuts.Of(700, 250, 520);
        Assert.Equal(250, part[0].A, 4);
        Assert.Equal(520, part[^1].B, 4);
        foreach (var (a, b, shift) in part)
            Assert.Contains(whole, w => w.A <= a + 1e-3f && b <= w.B + 1e-3f && w.Shift == shift);
        Assert.Empty(StripCuts.Of(700, 300, 300));
    }
}
