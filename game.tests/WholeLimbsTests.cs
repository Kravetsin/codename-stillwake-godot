using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Stillwake.Game.Features.Crew;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>
/// A hero's arm and leg are each one picture (whole-limbs): no half of a limb is left among the pictures, and every limb was joined
/// at <see cref="Rig"/>'s elbow and knee by <c>tools/anime-art/limbs.js</c>.
/// </summary>
public class WholeLimbsTests
{
    private static readonly string Limbs = Path.Combine(Png.Hd, "..", "..", "..", "tools", "anime-art", "limbs.js");

    [Fact]
    public void TheConvertersBonesAreTheRigs()
    {
        var bones = Regex.Match(File.ReadAllText(Limbs), @"const ARM = ([\d.]+), LEG = ([\d.]+);");
        Assert.True(bones.Success, "limbs.js names its bones");
        Assert.Equal(Rig.Arm.Upper, double.Parse(bones.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture), 9);
        Assert.Equal(Rig.Leg.Upper, double.Parse(bones.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture), 9);
    }

    [Fact]
    public void EveryHeroBodyHasAWholeArmAndLegAndNoHalves()
    {
        var names = Directory.GetFiles(Png.Hd, "hero_*.png").Select(Path.GetFileNameWithoutExtension).ToList();
        Assert.DoesNotContain(names, n => Regex.IsMatch(n!, "(upperArm|forearm|thigh|shin)(_lit|_skin)?$"));
        var arms = names.Where(n => n!.EndsWith("arm", StringComparison.Ordinal)).ToList();
        Assert.NotEmpty(arms);
        foreach (string? arm in arms)
        {
            string stem = arm![..^"arm".Length];
            Assert.True(Png.Has(stem + "leg"), stem + "leg");
            // The upper's box with the lower's below it, its joint at the elbow or knee (the halves' boxes in hero.js, margins round them).
            Assert.Equal((44, Whole(9, 2.5, Rig.Arm.Upper, 2.9, 10)), Png.Size(Png.Of(arm)));
            Assert.Equal((60, Whole(12, 2.5, Rig.Leg.Upper, 2.18, 11)), Png.Size(Png.Of(stem + "leg")));
        }
    }

    /// <summary>A whole limb's height in hd pixels: an upper half <paramref name="upper"/> tall over a lower <paramref name="lower"/> tall, joined at the bone's end.</summary>
    private static int Whole(double upper, double upperJoint, double bone, double lowerJoint, double lower)
    {
        int down = (int)Math.Round((upperJoint + bone - lowerJoint) * Png.Scale), margin = 2 * 3 * Png.Scale;
        int tall = Math.Max((int)(upper * Png.Scale) + margin, down + (int)(lower * Png.Scale) + margin);
        return (tall + Png.Scale - 1) / Png.Scale * Png.Scale;
    }
}
