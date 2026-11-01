using Stillwake.Game.Features.Creatures;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>
/// The guards in ink (2026-11-01-guards-in-ink): every picture their rigs place drawn at its size, the tick's stilts on the ground as it crawls, folded in flight
/// and its cutter at the line, the Istukan folded and fallen as its keys say, what shines of each following its pose.
/// </summary>
public class GuardInkTests
{
    private static readonly TickArt.Pose TickStands = new(0, 0, 0, 0, TickMotion.Watching, 1, 0);
    private static readonly IstukanArt.Pose IstukanStands = new(0, 0, 0, 0, 0, IstukanMotion.Yellow, 1, 1);

    private static string Art => Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "game", "art", "hd", "creatures");

    private static int BigEndian(BinaryReader r)
    {
        var b = r.ReadBytes(4);
        return b[0] << 24 | b[1] << 16 | b[2] << 8 | b[3];
    }

    private static void EveryPictureIsDrawnAtFourTimes(string id, IReadOnlyDictionary<string, CreatureArt.Part> parts, InkSlot[] slots)
    {
        Assert.All(slots, s => Assert.True(parts.ContainsKey(s.Part), s.Part));
        foreach (var (name, part) in parts)
        {
            using var png = new BinaryReader(File.OpenRead(Path.Combine(Art, id, name + ".png")));
            png.BaseStream.Seek(16, SeekOrigin.Begin);
            Assert.Equal((part.W * 4, part.H * 4), (BigEndian(png), BigEndian(png)));
        }
    }

    [Fact]
    public void TheTicksPicturesAreAllDrawn()
    {
        var rig = new TickRig();
        rig.Place(TickStands);
        EveryPictureIsDrawnAtFourTimes(CreatureArt.InkTick.Id, CreatureArt.InkTick.Parts, rig.Slots);
    }

    [Fact]
    public void TheIstukansPicturesAreAllDrawn()
    {
        var rig = new IstukanRig();
        rig.Place(IstukanStands, 0);
        EveryPictureIsDrawnAtFourTimes(CreatureArt.InkIstukan.Id, CreatureArt.InkIstukan.Parts, rig.Slots);
    }

    [Fact]
    public void StandingTheTicksFeetAreOnTheGroundItsKneesOverItsBack()
    {
        var at = new TickRig().Place(TickStands);
        Assert.All(at.Feet, f => Assert.InRange(f.Y, -0.01, 0.01));
        Assert.True(at.Arcs.Take(5).Min(k => k.Y) < -19.4, "no knee stands over its back");
    }

    [Fact]
    public void CrawlingAPlantedFootStaysWhereItIsAsTheBodyGoesOn()
    {
        var rig = new TickRig();
        // A pixel of crawl: its body has gone on one, so a planted foot goes one back in its own pixels.
        double Foot(double crawled) => rig.Place(TickStands with { Walk = 1, Gait = crawled * TickMotion.GaitPerPixel }).Feet[0].X;
        Assert.Equal(-1, Foot(1) - Foot(0), 3);
        Assert.Equal(0, rig.Place(TickStands with { Walk = 1, Gait = TickMotion.GaitPerPixel }).Feet[0].Y, 3);
    }

    [Fact]
    public void ThroughAStrideNoFootGoesUnderTheGroundAndEachSwingsUp()
    {
        var rig = new TickRig();
        double highest = 0;
        for (double crawled = 0; crawled < CreatureArt.InkTick.Stride / 0.72; crawled += 0.1)
            foreach (var foot in rig.Place(TickStands with { Walk = 1, Gait = crawled * TickMotion.GaitPerPixel }).Feet)
            {
                Assert.True(foot.Y <= 0.01, $"{foot.Y}");
                highest = Math.Min(highest, foot.Y);
            }
        Assert.InRange(highest, -CreatureArt.InkTick.Lift - 0.01, -CreatureArt.InkTick.Lift * 0.8);
    }

    [Fact]
    public void FlyingTheTicksStiltsFoldOffTheGround()
    {
        var at = new TickRig().Place(TickStands with { Fly = 1 });
        Assert.All(at.Feet, f => Assert.True(f.Y < -5, $"a foot at {f.Y} still reaches for the ground"));
    }

    [Fact]
    public void SawingItsCutterReachesTheLine()
    {
        var rig = new TickRig();
        var rest = rig.Place(TickStands).Cut;
        var cut = rig.Place(TickStands with { Saw = 1 }).Cut;
        Assert.True(Math.Abs(cut.X - CreatureArt.InkTick.Cut.X) < 2 && Math.Abs(cut.Y - CreatureArt.InkTick.Cut.Y) < 3, $"{cut}");
        Assert.True(cut.Y < rest.Y, "its cutter stays down");
    }

    [Fact]
    public void ItsLampTakesItsEyesColourAndItsCracksShowOnlyDying()
    {
        var rig = new TickRig();
        rig.Place(TickStands with { Eye = TickMotion.Attacking });
        var lamp = rig.Slots.Single(s => s.Part == "lamp");
        Assert.True(lamp.Shines && lamp.Tint.R > lamp.Tint.G * 2, "a red eye shines red");
        Assert.Equal(0, rig.Slots.Single(s => s.Part == "cracks").Alpha);
        rig.Place(TickStands with { Cracks = 0.5 });
        Assert.Equal(0.5, rig.Slots.Single(s => s.Part == "cracks").Alpha);
    }

    [Fact]
    public void StandingTheIstukanIsAboutFiveCellsAndAsleepItsLampIsDark()
    {
        var rig = new IstukanRig();
        var at = rig.Place(IstukanStands, 0);
        Assert.InRange(at.Eye.Y, -46, -34);
        Assert.Equal(1, rig.Slots.Single(s => s.Part == "lamp").Alpha);
        rig.Place(IstukanStands with { Fold = 1, Lamp = null, Seam = 0.08 }, 0);
        Assert.Equal(0, rig.Slots.Single(s => s.Part == "lamp").Alpha);
        Assert.Equal(0.08, rig.Slots.Single(s => s.Part == "core").Alpha);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 0)]
    [InlineData(0, 1)]
    public void ItsLegsReachTheirFeetInEveryPoseItStandsIn(double fold, double crouch)
    {
        var rig = new IstukanRig();
        rig.Place(IstukanStands with { Fold = fold, Crouch = crouch }, 0);
        // The lower bone's end, then the ankle over its foot.
        var d = CreatureArt.InkIstukan;
        foreach (int upper in Enumerable.Range(0, d.Nodes.Length).Where(i => d.Nodes[i].Ik != null))
        {
            var ik = d.Nodes[upper].Ik!;
            var lower = Enumerable.Range(0, d.Nodes.Length).First(i => d.Nodes[i].Upper == upper);
            var shin = rig.Slots.First(s => s.Part == d.Nodes[lower].Part);
            var foot = rig.Slots.First(s => s.Part == d.Nodes[ik.Foot].Part);
            double endX = shin.X - Math.Sin(shin.A) * ik.L2, endY = shin.Y + Math.Cos(shin.A) * ik.L2;
            Assert.True(Math.Abs(endX - foot.X - ik.AnkleX) < 0.05 && Math.Abs(endY - foot.Y - ik.AnkleY) < 0.05, $"{endX},{endY} misses {foot.X},{foot.Y}");
        }
    }

    [Fact]
    public void DeadTheIstukanLiesLowAndItsLightIsOut()
    {
        var rig = new IstukanRig();
        var standing = rig.Place(IstukanStands, 0);
        var dead = rig.Place(IstukanStands with { Dead = 1, Lamp = null, Seam = 0, Dim = 0.22 }, 0);
        Assert.True(dead.Core.Y > standing.Core.Y + 10, $"{dead.Core.Y} against {standing.Core.Y}");
        Assert.All(rig.Slots.Where(s => s.Shines), s => Assert.Equal(0, s.Alpha));
    }

    [Fact]
    public void StuckItsArmsShiverAndHeldStillTheyDoNot()
    {
        var rig = new IstukanRig();
        var d = CreatureArt.InkIstukan;
        string fore = d.Nodes.First(n => n.Shiver > 0).Part;
        double Turn(double stuck, double clock)
        {
            rig.Place(IstukanStands with { Stuck = stuck }, clock);
            return rig.Slots.First(s => s.Part == fore).A;
        }
        Assert.NotEqual(Turn(1, 0.1), Turn(1, 0.12), 3);
        Assert.Equal(Turn(0, 0.1), Turn(0, 0.1001), 3);
    }
}
