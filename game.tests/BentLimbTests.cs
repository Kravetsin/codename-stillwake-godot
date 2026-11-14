using Stillwake.Game.Features.Crew;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>A limb's picture bent as one piece in a band at its joint (exp/bent-limbs).</summary>
public class BentLimbTests
{
    private const double Joint = 6;

    /// <summary>A point of the lower bone's frame in the upper bone's, the lower turned <paramref name="bend"/> as the figure turns it.</summary>
    private static Vec Rigid(Vec lower, double bend)
    {
        double c = Math.Cos(-bend), s = Math.Sin(-bend);
        return new Vec(lower.X * c - lower.Y * s, Joint + lower.X * s + lower.Y * c);
    }

    private static void Near(Vec expected, Vec actual, double within = 1e-9)
    {
        Assert.InRange(actual.X, expected.X - within, expected.X + within);
        Assert.InRange(actual.Y, expected.Y - within, expected.Y + within);
    }

    private static double Apart(Vec a, Vec b) => Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));

    [Fact]
    public void AwayFromTheJointEachPartIsRigidWithItsBone()
    {
        foreach (double bend in (double[])[0.4, 1.6, 2.6, -2.2])
        {
            // The upper picture above the band does not move; below it, it turns with the lower bone.
            var above = new Vec(1.5, Joint - BentLimb.Band / 2 - 0.1);
            Near(above, BentLimb.Warp(above, Joint, bend, lower: false));
            var below = new Vec(-1, Joint + BentLimb.Band / 2 + 0.1);
            Near(Rigid(below with { Y = below.Y - Joint }, bend), BentLimb.Warp(below, Joint, bend, lower: false));
            // The lower picture below the band is its bone's; above it, held where the upper bone has it.
            var shin = new Vec(2, BentLimb.Band / 2 + 3);
            Near(shin, BentLimb.Warp(shin, 0, bend, lower: true));
            var top = new Vec(-2, -BentLimb.Band / 2 - 0.5);
            Near(new Vec(top.X, top.Y + Joint), Rigid(BentLimb.Warp(top, 0, bend, lower: true), bend));
        }
    }

    [Fact]
    public void BothPicturesBendAlikeSoTheyStayOneLimbThroughTheBand()
    {
        for (double y = -3; y <= 3; y += 0.25)
            foreach (double bend in (double[])[0.7, 2.4, -1.9])
            {
                var lower = new Vec(1.25, y);
                var upper = new Vec(1.25, Joint + y);
                Near(BentLimb.Warp(upper, Joint, bend, lower: false), Rigid(BentLimb.Warp(lower, 0, bend, lower: true), bend));
            }
    }

    [Fact]
    public void KeepsItsWidthAtTheJointThroughTheSharpestBend()
    {
        // Blending places would pinch the joint toward nothing as the bend nears a fold; turning rows keeps every row its length.
        var limb = new BentLimb(-2.5, -2.5, 5, Joint + 3, Joint, lower: false, (0, 3));
        limb.Bend(2.8);
        for (int r = 0; r < limb.Points.Length; r += 2) Assert.InRange(Apart(limb.Points[r], limb.Points[r + 1]), 5 - 1e-9, 5 + 1e-9);
    }

    [Fact]
    public void HasRowsAtItsEdgesAndDenselyOnlyInTheBand()
    {
        // An upper arm's picture, its bottom 3 px past the elbow, drawn down to the seam's end a pixel past it: no row past that.
        var limb = new BentLimb(-5.5, -5.5, 11, Joint + 8.5, Joint, lower: false, (-1, 1));
        var rows = limb.Uvs.Where((_, i) => i % 2 == 0).Select(p => p.Y - 5.5).ToList();
        Assert.Equal(-5.5, rows[0]);
        Assert.Equal(Joint + 1, rows[^1], 9);
        // Its top and the seam's end, and the band's rows a quarter pixel apart from 2 px over the elbow down to the seam's end.
        Assert.Equal(1 + 13, rows.Count);
        Assert.All(rows.Skip(1), y => Assert.InRange(y, Joint - BentLimb.Band / 2, Joint + 1));
        Assert.Equal((rows.Count - 1) * 6, limb.Triangles.Length);
        Assert.All(limb.Triangles, i => Assert.InRange(i, 0, limb.Points.Length - 1));
    }

    [Fact]
    public void TheUpperFadesOutAcrossTheSeamOverTheLowerDrawnFromItsStart()
    {
        var upper = new BentLimb(-3, -3, 6, Joint + 6, Joint, lower: false, (-1, 1));
        var lower = new BentLimb(-3, -4, 6, 12, 0, lower: true, (-1, 1));
        double AlphaAt(BentLimb limb, double y) => limb.Alphas[Array.FindIndex(limb.Uvs, p => Math.Abs(p.Y - y) < 1e-9)];
        Assert.Equal(1, AlphaAt(upper, 3 + Joint - 1));
        Assert.Equal(0.5, AlphaAt(upper, 3 + Joint), 9);
        Assert.Equal(0, AlphaAt(upper, 3 + Joint + 1));
        Assert.Equal(1, AlphaAt(upper, 0));
        // The lower from the seam's start, its rounded top above it left out, whole throughout.
        Assert.Equal(3, lower.Uvs[0].Y, 9);
        Assert.All(lower.Alphas, a => Assert.Equal(1, a));
    }

    [Fact]
    public void TheSeamRunsFromWhereTheLowerIsFullToWhereTheUpperStillIs()
    {
        // Rows a quarter pixel each: the upper full till a pixel past the joint, its end narrowing; the lower's top narrow till
        // a pixel and a half above the joint. Each picture's first row 4 px above the joint.
        float[] upper = [.. Enumerable.Range(0, 32).Select(r => r < 20 ? 4f : 1.5f)];
        float[] lower = [.. Enumerable.Range(0, 32).Select(r => r < 10 ? 1.5f : 4f)];
        var (from, to) = BentLimb.SeamOf(upper, -4, lower, -4, 0.25);
        Assert.Equal(-1.5, from, 9);
        Assert.Equal(1, to, 9);
        // Apart, they meet at the gap's middle; without widths, a pixel round the joint.
        float[] early = [.. Enumerable.Range(0, 32).Select(r => r < 12 ? 4f : 1.5f)];
        float[] late = [.. Enumerable.Range(0, 32).Select(r => r < 18 ? 1.5f : 4f)];
        var gap = BentLimb.SeamOf(early, -4, late, -4, 0.25);
        Assert.Equal(-0.25, gap.From, 9);
        Assert.Equal(-0.25, gap.To, 9);
        Assert.Equal(BentLimb.Plain, BentLimb.SeamOf(null, -4, lower, -4, 0.25));
    }

    [Fact]
    public void StraightItIsThePictureAsDrawnAndBendsOnlyWhenTheAngleChanges()
    {
        var limb = new BentLimb(-3, 0, 6, 9, 0, lower: true, (-3, 0));
        Assert.True(limb.Bend(0));
        for (int i = 0; i < limb.Points.Length; i++)
            Near(new Vec(limb.Uvs[i].X - 3, limb.Uvs[i].Y), limb.Points[i]);
        Assert.False(limb.Bend(0));
        Assert.True(limb.Bend(1.2));
        Assert.False(limb.Bend(1.2));
    }

    [Fact]
    public void ShareRisesSmoothlyAcrossTheBand()
    {
        Assert.Equal(0, BentLimb.Share(-BentLimb.Band / 2));
        Assert.Equal(0.5, BentLimb.Share(0), 9);
        Assert.Equal(1, BentLimb.Share(BentLimb.Band / 2));
        for (double s = -BentLimb.Band / 2; s < BentLimb.Band / 2; s += 0.1) Assert.True(BentLimb.Share(s + 0.1) >= BentLimb.Share(s));
    }
}
