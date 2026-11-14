using Stillwake.Game.Features.Creatures;
using Stillwake.Game.Shared;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>
/// A creature's legs, neck and tail bent whole (creatures-bend), as the hero's limbs are (bent-limbs): one picture each, bent in a
/// band at its knee or along itself, posed from today's gait with the knee folding as the foot lifts.
/// </summary>
public class CreatureBendTests
{
    /// <summary>Every part of an ink design that bends: its design's look and its name.</summary>
    public static TheoryData<string, string> Bent()
    {
        var all = new TheoryData<string, string>();
        foreach (var (look, design) in CreatureArt.InkDesigns)
            foreach (var (name, part) in design.Parts)
                if (part.Bend != null) all.Add(look, name);
        return all;
    }

    /// <summary>The part's picture as <see cref="CreatureBody"/> draws it: its own frame from its pivot, one picture with no seam.</summary>
    private static BentLimb Drawn(CreatureArt.Part part, (double From, double To) seam) =>
        new(-part.PivotX, -part.PivotY, part.W, part.H, part.Bend!.Joint, lower: false, seam, part.Bend.Band, part.Bend.Axis);

    /// <summary>The most part <paramref name="name"/> bends either way, as its node is driven: a knee folded as in a leap, a neck grazing or reared, a tail tucked or raised.</summary>
    private static double[] Bends(CreatureArt.Design design, string name) => design.Rig.First(n => n.Part == name).Drive switch
    {
        CreatureArt.Drive.Leg => [design.Pose.Knee * 1.6, -design.Pose.Knee * 1.6],
        CreatureArt.Drive.Head => [design.Pose.Graze + 0.3, -0.4],
        _ => [Math.Abs(design.Pose.TailFlee) * 1.6 + design.Pose.TailAmp, -(Math.Abs(design.Pose.TailFlee) * 1.6 + design.Pose.TailAmp)],
    };

    [Fact]
    public void TheLoperBendsItsFourLegsItsNeckAndItsTail()
    {
        var loper = CreatureArt.InkDesigns["classic"];
        var bent = loper.Rig.Where(n => loper.Parts.TryGetValue(n.Part, out var p) && p.Bend != null).Select(n => n.Drive).ToList();
        Assert.Equal(4, bent.Count(d => d == CreatureArt.Drive.Leg));
        Assert.Contains(CreatureArt.Drive.Head, bent);
        Assert.Contains(CreatureArt.Drive.Tail, bent);
        Assert.True(loper.Pose.Knee > 0);
    }

    [Theory]
    [MemberData(nameof(Bent))]
    public void NoEndOfAPictureShowsAtABendItIsOnePictureDrawnWholeAndNeverFoldedOverItself(string look, string name)
    {
        // The hero's upper picture once ended at the knee in its rounded cap; the user wants a bend that does not show where it is.
        // A creature's part is one picture through its bend: drawn whole, opaque everywhere, its every row in order, and where it is
        // painted, no triangle of it turned over onto another however far it bends.
        var design = CreatureArt.InkDesigns[look];
        var part = design.Parts[name];
        var (w, h, pixels) = Png.Pixels(Path.Combine(Png.Hd, "creatures", design.Id, name + ".png"));
        double k = w / (double)part.W;
        bool Painted(Vec uv)
        {
            int x = (int)(uv.X * k), y = (int)(uv.Y * k);
            return x >= 0 && y >= 0 && x < w && y < h && pixels[(y * w + x) * 4 + 3] > 128;
        }
        foreach (double bend in Bends(design, name))
        {
            var limb = Drawn(part, BentLimb.Seamless);
            var rest = Drawn(part, BentLimb.Seamless);
            limb.Bend(-bend);
            Assert.All(limb.Alphas, a => Assert.Equal(1, a));
            for (int t = 0; t < limb.Triangles.Length; t += 3)
            {
                int a = limb.Triangles[t], b = limb.Triangles[t + 1], c = limb.Triangles[t + 2];
                var middle = new Vec((limb.Uvs[a].X + limb.Uvs[b].X + limb.Uvs[c].X) / 3, (limb.Uvs[a].Y + limb.Uvs[b].Y + limb.Uvs[c].Y) / 3);
                if (!Painted(middle)) continue;
                double Area(Vec[] p) => (p[b].X - p[a].X) * (p[c].Y - p[a].Y) - (p[b].Y - p[a].Y) * (p[c].X - p[a].X);
                Assert.True(Area(limb.Points) * Area(rest.Points) > 0, $"{look} {name} bent {bend:F2}: folded over at {middle}");
            }
        }
        // Its rows run once from the picture's start to its end, wherever its bone runs.
        var whole = Drawn(part, BentLimb.Seamless);
        var corners = whole.Uvs;
        Assert.True(corners.Min(p => p.X) <= 0.01 && corners.Min(p => p.Y) <= 0.01, $"{name} starts inside its picture");
        Assert.True(corners.Max(p => p.X) >= part.W - 0.01 && corners.Max(p => p.Y) >= part.H - 0.01, $"{name} ends inside its picture");
    }

    [Fact]
    public void AHerosSeamAtAKneeWouldFadeTheLegOutWhereItBends()
    {
        // Proves the test above bites: drawn as the hero's upper picture is, with a seam round the joint, the leg would end there.
        var leg = CreatureArt.InkDesigns["classic"].Parts["foreLeg"];
        Assert.Contains(Drawn(leg, BentLimb.Plain).Alphas, a => a < 1);
    }

    [Fact]
    public void WhatHangsPastABendingNeckRidesItWhole()
    {
        // The eye and ears sit on the skull past the neck's band: turned about its joint, they stay where the skull's picture goes.
        var loper = CreatureArt.InkDesigns["classic"];
        var head = loper.Parts["head"];
        var bend = head.Bend!;
        var eye = loper.Rig.Single(n => n.Drive == CreatureArt.Drive.Eye);
        var joint = CreaturePose.JointOf(bend);
        foreach (double turn in (double[])[0.5, loper.Pose.Graze, -0.3])
        {
            var onBone = BentLimb.Turned(new Vec(eye.X, eye.Y), -bend.Axis);
            Assert.True(onBone.Y > bend.Joint + bend.Band / 2, "the eye lies past the neck's band");
            var drawn = BentLimb.Turned(BentLimb.Warp(onBone, bend.Joint, -turn, lower: false, bend.Band), bend.Axis);
            var ridden = BentLimb.Turned(new Vec(eye.X - joint.X, eye.Y - joint.Y), turn);
            Assert.Equal(drawn.X, ridden.X + joint.X, 6);
            Assert.Equal(drawn.Y, ridden.Y + joint.Y, 6);
        }
    }

    [Fact]
    public void AKneeFoldsBackAndAHockForwardOnlyAsTheFootLiftsStraightOnTheGround()
    {
        var loper = CreatureArt.InkDesigns["classic"];
        var pose = new CreaturePose(loper, CreatureLooks.Runs[loper.Id]);
        var legs = Enumerable.Range(0, loper.Rig.Count).Where(i => loper.Rig[i].Drive == CreatureArt.Drive.Leg).ToList();
        double middle = legs.Average(i => loper.Rig[i].X);
        var folded = new HashSet<int>();
        for (int k = 0; k < 40; k++)
        {
            pose.Set(new CreaturePose.Doing(k / 40.0, 1, 0, 0, 0, 0, 0, 0, 0, 1, null));
            foreach (int i in legs)
            {
                var j = pose.Joints[i];
                bool fore = loper.Rig[i].X > middle;
                // On the ground (no lift) it stands straight; lifted, a foreleg's foot folds back (clockwise), a hind one's forward.
                if (j.Y == 0) Assert.Equal(0, j.Bend);
                else Assert.True(fore ? j.Bend > 0 : j.Bend < 0, $"{loper.Rig[i].Id} bent {j.Bend:F2} at {k}");
                Assert.InRange(Math.Abs(j.Bend), 0, loper.Pose.Knee);
                if (Math.Abs(j.Bend) > loper.Pose.Knee * 0.9) folded.Add(i);
            }
        }
        Assert.Equal(legs.Count, folded.Count);
    }

    [Fact]
    public void ABentNeckGrazesAndATailSwaysByBendingTheirRootsStayingOnTheBody()
    {
        var loper = CreatureArt.InkDesigns["classic"];
        var pose = new CreaturePose(loper, CreatureLooks.Runs[loper.Id]);
        pose.Set(new CreaturePose.Doing(0, 0, 0, 0, 1, 1, 0, 0, 0, 1, null));
        int head = loper.Rig.ToList().FindIndex(n => n.Drive == CreatureArt.Drive.Head), tail = loper.Rig.ToList().FindIndex(n => n.Drive == CreatureArt.Drive.Tail);
        Assert.Equal(0, pose.Joints[head].A);
        Assert.True(pose.Joints[head].Bend > 1, $"grazing it bends its neck {pose.Joints[head].Bend:F2}");
        Assert.Equal(0, pose.Joints[tail].A);
        Assert.True(pose.Joints[tail].Bend < -0.3, $"fleeing it tucks its tail {pose.Joints[tail].Bend:F2}");
        // A design whose head does not bend turns it whole, as before.
        var tapir = CreatureArt.InkDesigns["tapir"];
        var still = new CreaturePose(tapir, CreatureLooks.Runs[tapir.Id]);
        still.Set(new CreaturePose.Doing(0, 0, 0, 0, 0, 1, 0, 0, 0, 1, null));
        int its = tapir.Rig.ToList().FindIndex(n => n.Drive == CreatureArt.Drive.Head);
        Assert.Equal(0, still.Joints[its].Bend);
        Assert.True(still.Joints[its].A > 0.3);
    }

    [Fact]
    public void ARotatedBoneBendsAsTheSamePictureHangingStraightTurned()
    {
        // A tail lying back along its axis is the same limb as one hanging down, turned: its grid, its picture and its bend alike.
        const double axis = Math.PI / 2, joint = 4, band = 6;
        var down = new BentLimb(-2, 0, 4, 10, joint, lower: false, BentLimb.Seamless, band);
        var back = new BentLimb(-10, -2, 10, 4, joint, lower: false, BentLimb.Seamless, band, axis);
        down.Bend(0.8);
        back.Bend(0.8);
        Assert.Equal(down.Points.Length, back.Points.Length);
        for (int i = 0; i < down.Points.Length; i++)
        {
            var turned = BentLimb.Turned(down.Points[i], axis);
            Assert.Equal(turned.X, back.Points[i].X, 9);
            Assert.Equal(turned.Y, back.Points[i].Y, 9);
            // The same spot of the picture: the straight one's (x, y) is the turned one's (10 − y, x).
            Assert.Equal(down.Uvs[i].X, back.Uvs[i].Y, 9);
            Assert.Equal(10 - down.Uvs[i].Y, back.Uvs[i].X, 9);
        }
    }
}
