using System;
using System.IO;
using System.Linq;
using Stillwake.Game.Features.Crew;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>
/// The whole limbs joined by <c>tools/anime-art/limbs.js</c> (whole-limbs, a try): every pair of limb pictures has its whole beside
/// it, the lower half placed at the elbow or knee the rig has, so the converter's bones are <see cref="Rig"/>'s.
/// </summary>
public class WholeLimbsTests
{
    [Theory]
    // The halves' joints down their pictures, Figure.cs's Joints.
    [InlineData("upperArm", "forearm", "arm", 2.5, 2.9)]
    [InlineData("thigh", "shin", "leg", 2.5, 2.18)]
    public void EveryPairOfLimbPicturesHasItsWholeWithTheLowerAtTheRigsJoint(string upper, string lower, string whole, double upperJoint, double lowerJoint)
    {
        double bone = whole == "arm" ? Rig.Arm.Upper : Rig.Leg.Upper;
        var stems = Directory.GetFiles(Png.Hd, $"hero_*{upper}.png").Select(f => Path.GetFileNameWithoutExtension(f)[..^upper.Length]).ToList();
        Assert.NotEmpty(stems);
        foreach (string stem in stems)
        {
            foreach (string suffix in (string[])["", "_lit", "_skin"])
            {
                bool any = Png.Has(stem + upper + suffix) || Png.Has(stem + lower + suffix);
                Assert.True(any == Png.Has(stem + whole + suffix), stem + whole + suffix);
            }
            var (uw, uh, _) = Png.Pixels(Png.Of(stem + upper));
            var (lw, lh, lp) = Png.Pixels(Png.Of(stem + lower));
            var (w, h, p) = Png.Pixels(Png.Of(stem + whole));
            int down = (int)Math.Round((upperJoint + bone - lowerJoint) * Png.Scale);
            Assert.Equal(Math.Max(uw, lw), w);
            Assert.Equal((Math.Max(uh, down + lh) + Png.Scale - 1) / Png.Scale * Png.Scale, h);
            // Below the seam the whole is the lower half as it was, row for row: its coverage the same.
            for (int y = lh - 4 * Png.Scale; y < lh; y++)
                for (int x = 0; x < lw; x++)
                    Assert.True(lp[(y * lw + x) * 4 + 3] == p[((y + down) * w + x) * 4 + 3], $"{stem}{whole} at {x}, {y + down}");
        }
    }
}
