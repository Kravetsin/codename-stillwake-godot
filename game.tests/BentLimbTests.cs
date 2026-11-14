using Stillwake.Game.Features.Crew;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>A limb's picture bent as one piece in a band at its joint (bent-limbs).</summary>
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
        var limb = new BentLimb(-2.5, -2.5, 5, Joint + 3, Joint, lower: false, (3, 3));
        limb.Bend(2.8);
        int across = BentLimb.Columns;
        for (int r = 0; r < limb.Points.Length; r += across)
            Assert.InRange(Apart(limb.Points[r], limb.Points[r + across - 1]), 5 - 1e-9, 5 + 1e-9);
    }

    [Fact]
    public void ASharpKneeIsDrawnWhereItBendsNotFannedIntoStreaks()
    {
        // A thigh's picture with its margin, the knee bent as far as a climb takes it. Each quad is drawn as two triangles mapped flat:
        // where its diagonal's middle lands against where that point of the picture belongs is how far the picture is kinked there.
        // Two points a row put the outside of the knee 0.7 px off, the watercolour drawn out into radial streaks.
        const double left = -6, top = -5.5, width = 12;
        var limb = new BentLimb(left, top, width, 18, Joint, lower: false, (7, 7));
        limb.Bend(-2.6);
        int across = BentLimb.Columns, rows = limb.Points.Length / across;
        double worst = 0;
        for (int r = 0; r < rows - 1; r++)
            for (int c = 0; c < across - 1; c++)
            {
                int a = r * across + c, b = a + across + 1;
                Vec Rest(int i) => new(left + limb.Uvs[i].X, top + limb.Uvs[i].Y);
                var middle = new Vec((Rest(a).X + Rest(b).X) / 2, (Rest(a).Y + Rest(b).Y) / 2);
                var drawn = new Vec((limb.Points[a].X + limb.Points[b].X) / 2, (limb.Points[a].Y + limb.Points[b].Y) / 2);
                worst = Math.Max(worst, Apart(BentLimb.Warp(middle, Joint, -2.6, lower: false), drawn));
            }
        Assert.InRange(worst, 0, 0.25);
    }

    [Fact]
    public void HasRowsAtItsEdgesAndDenselyOnlyInTheBand()
    {
        // An upper arm's picture, its bottom 3 px past the elbow: its top, its bottom and the band's rows a quarter pixel apart.
        var limb = new BentLimb(-5.5, -5.5, 11, Joint + 8.5, Joint, lower: false, (3, 3));
        var rows = limb.Uvs.Where((_, i) => i % BentLimb.Columns == 0).Select(p => p.Y - 5.5).ToList();
        Assert.Equal(-5.5, rows[0]);
        Assert.Equal(Joint + 3, rows[^1], 9);
        Assert.Equal(2 + BentLimb.BandRows, rows.Count);
        Assert.All(rows.Skip(1).SkipLast(1), y => Assert.InRange(y, Joint - BentLimb.Band / 2, Joint + BentLimb.Band / 2));
        Assert.Equal((rows.Count - 1) * (BentLimb.Columns - 1) * 6, limb.Triangles.Length);
        Assert.All(limb.Triangles, i => Assert.InRange(i, 0, limb.Points.Length - 1));
    }

    // Rows a quarter pixel each from 4 px above the joint: the upper full till a pixel past it, then its rounded end (an elbow's cap,
    // a knee's pad) narrowing to its bottom 4 px past; the lower's rounded top narrow till a pixel and a half above the joint.
    private static readonly float[] Upper = [.. Enumerable.Range(0, 32).Select(r => r < 20 ? 4f : 1.5f)];
    private static readonly float[] Lower = [.. Enumerable.Range(0, 48).Select(r => r < 10 ? 1.5f : 4f)];

    [Fact]
    public void NoRoundedEndShowsWhereTheElbowOrKneeIs()
    {
        // Drawn whole, the upper's rounded end bent round the joint's outside and showed where the elbow was, a capsule: the
        // user wants a limb that does not show it. The upper fades out over the lower across the seam, gone before its end narrows.
        var seam = BentLimb.SeamOf(Upper, -4, Lower, -4, 0.25);
        var upper = new BentLimb(-3, Joint - 4, 6, 8, Joint, lower: false, seam);
        var lower = new BentLimb(-3, -4, 6, 12, 0, lower: true, seam);
        for (int i = 0; i < upper.Uvs.Length; i++)
        {
            double along = upper.Uvs[i].Y - 4;
            int row = Math.Min(Upper.Length - 1, (int)Math.Floor((along + 4) / 0.25));
            if (Upper[row] < 4 * 0.85) Assert.Equal(0, upper.Alphas[i]);
            if (along <= seam.From) Assert.Equal(1, upper.Alphas[i]);
        }
        // The lower's narrow top is left out: it starts at the seam, full width there.
        Assert.Equal(4 + seam.From, lower.Uvs[0].Y, 9);
        Assert.All(lower.Alphas, a => Assert.Equal(1, a));
    }

    [Fact]
    public void TheSeamRunsFromWhereTheLowerIsFullToWhereTheUpperStillIs()
    {
        var (from, to) = BentLimb.SeamOf(Upper, -4, Lower, -4, 0.25);
        Assert.Equal(-1.5, from, 9);
        Assert.Equal(1, to, 9);
        // Apart, they meet at the gap's middle; without widths, a pixel round the joint.
        float[] early = [.. Enumerable.Range(0, 32).Select(r => r < 12 ? 4f : 1.5f)];
        float[] late = [.. Enumerable.Range(0, 32).Select(r => r < 18 ? 1.5f : 4f)];
        Assert.Equal((-0.25, -0.25), BentLimb.SeamOf(early, -4, late, -4, 0.25));
        Assert.Equal(BentLimb.Plain, BentLimb.SeamOf(null, -4, Lower, -4, 0.25));
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
