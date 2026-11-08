using Stillwake.Game.Features.Creatures;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>
/// The cave lurkers in ink (2026-11-07-cave-hostiles): every picture their rigs place drawn at its size, their poses as the pick page
/// moves them, what shines of them following their pose, and their motion read from what the sim says.
/// </summary>
public class LurkerInkTests
{
    private static readonly CreatureArt.LurkerDesign Capback = CreatureArt.Lurkers["capback"], Snail = CreatureArt.Lurkers["geodeSnail"];

    // A struct's new() is all zeros: its glow too.
    private static readonly LurkerArt.Pose Rest = new(Glow: 1);

    private static string Art => Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "game", "art", "hd", "creatures");

    public static TheoryData<string> Looks() => [.. CreatureArt.Lurkers.Keys];

    [Theory]
    [MemberData(nameof(Looks))]
    public void EveryPictureIsPlacedAndDrawnAtFourTimes(string look)
    {
        var design = CreatureArt.Lurkers[look];
        var rig = new LurkerRig(design);
        rig.Place(Rest);
        Assert.Equal(design.Parts.Keys.Order(), rig.Slots.Select(s => s.Part).Distinct().Order());
        foreach (var (name, part) in design.Parts)
        {
            using var png = new BinaryReader(File.OpenRead(Path.Combine(Art, design.Id, name + ".png")));
            png.BaseStream.Seek(16, SeekOrigin.Begin);
            Assert.Equal((part.W * 4, part.H * 4), (BigEndian(png), BigEndian(png)));
        }
    }

    [Fact]
    public void HunkeredTheCapbacksBodyDropsWhileItsFeetStay()
    {
        var rig = new LurkerRig(Capback);
        var (body, legs) = BodyAndLegs(rig, Rest);
        var (low, lowLegs) = BodyAndLegs(rig, Rest with { Hunk = 1 });
        Assert.Equal(body + Capback.Pose.Drop, low, 3);
        Assert.Equal(legs, lowLegs);
    }

    [Fact]
    public void CrouchedTheCapbackRisesAndPitchesBack()
    {
        var rig = new LurkerRig(Capback);
        rig.Place(Rest with { Crouch = 1 });
        var body = rig.Slots.Single(s => s.Part == "body");
        Assert.Equal(-Capback.Pose.Pitch, body.A, 6);
        Assert.True(body.Y < Capback.Nodes.Single(n => n.Id == "body").Y, $"{body.Y}");
    }

    [Fact]
    public void FlaringItsTealTurnsViolet()
    {
        var rig = new LurkerRig(Capback);
        rig.Place(new LurkerArt.Pose(Glow: 0.8));
        var (teal, violet) = Moss(rig);
        Assert.Equal((0.8, 0), (teal.Alpha, violet.Alpha));
        rig.Place(new LurkerArt.Pose(Glow: 0.8, Flare: 1));
        (teal, violet) = Moss(rig);
        Assert.Equal(0, teal.Alpha);
        Assert.Equal(0.92, violet.Alpha, 6);
        Assert.Equal(LurkerArt.Violet, violet.Tint);
    }

    [Fact]
    public void DrawnInTheSnailsFootAndTailFadeAndItsPlugShuts()
    {
        var rig = new LurkerRig(Snail);
        rig.Place(Rest);
        Assert.Equal((1, 1, 0), (Slot(rig, "tail").Alpha, Slot(rig, "front").Alpha, Slot(rig, "plug").Alpha));
        rig.Place(Rest with { Retract = 1, Lid = 1, Stalk = 1 });
        Assert.Equal((0, 0, 1, 0), (Slot(rig, "tail").Alpha, Slot(rig, "front").Alpha, Slot(rig, "plug").Alpha, Slot(rig, "stalk").Alpha));
    }

    [Fact]
    public void ShutItsMouthsLightDimsAndCrackedItsShellGivesOne()
    {
        var rig = new LurkerRig(Snail);
        rig.Place(Rest);
        double open = rig.Lights[0].K;
        Assert.Equal(Snail.Lights.Length, rig.Lights.Count);
        rig.Place(Rest with { Lid = 1, Crack = 0.5 });
        Assert.Equal(open * 0.15, rig.Lights[0].K, 6);
        Assert.Equal(Snail.Lights.Length + 1, rig.Lights.Count);
        Assert.Equal(LurkerArt.Crack, rig.Lights[^1].Colour);
    }

    [Fact]
    public void ACapbackVentingSqueezesThenPuffsUpFlaringAndNeverLeaps()
    {
        var m = new LurkerMotion(false);
        Run(m, new LurkerMotion.Seen(true, false, 0, false, 0, 0, 0, 20), 1);
        Assert.True(m.Step(new LurkerMotion.Seen(false, false, 0, true, 0, 0, 0, 20, Venting: true), 0.01), "no vent began");
        Assert.True(m.Pose.Squash > 0.5, $"{m.Pose}");
        Run(m, new LurkerMotion.Seen(false, false, 0, true, 0, 0, 0, 20, Venting: true), 0.4);
        Assert.True(m.Pose.Squash < 0.01 && m.Pose.Hunk < 0.35 && m.Pose.Glow > 1.2 && m.Pose.Fly == 0, $"{m.Pose}");
        Run(m, new LurkerMotion.Seen(true, false, 0, false, 0, 0, 0, null), LurkerMotion.ComeToS + 2);
        Assert.True(m.Pose.Hunk > 0.85, $"{m.Pose}");
    }

    [Fact]
    public void ACapbackCrouchesFlaringToSpitThenRecoilsWhereItStands()
    {
        var m = new LurkerMotion(false);
        Run(m, new LurkerMotion.Seen(true, true, 0.3, false, 0, 0, 0, -40), 0.6);
        Assert.True(m.Pose.Crouch > 0.95 && m.Pose.Flare > 0.95, $"{m.Pose}");
        Assert.Equal(-1, m.Face);
        Run(m, new LurkerMotion.Seen(false, false, 0, true, 0, 0, 0, -40), 0.3);
        Assert.True(m.Pose.Crouch < 0.05 && m.Pose.Fly == 0 && m.Pose.Squash == 0, $"{m.Pose}");
        Run(m, new LurkerMotion.Seen(false, false, 0, false, 0, 0, 0, null), LurkerMotion.ComeToS + 2);
        Assert.True(m.Pose.Hunk > 0.95, $"{m.Pose}");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ItsCapsComeToRestAfterACutWindUpOrABlow(bool struck)
    {
        // The playtest's bug: a wind-up cut short, or a blow, left its caps shaking for good.
        var m = new LurkerMotion(false);
        if (struck) m.Struck();
        else Run(m, new LurkerMotion.Seen(true, true, 0.3, false, 0, 0, 0, -10), 0.4);
        Run(m, new LurkerMotion.Seen(true, false, 0, false, 0, 0, 0, -10), 3);
        Assert.True(m.Pose.Shudder == 0 && m.Pose.Flinch == 0 && m.Pose.Clock == 0, $"{m.Pose}");
    }

    [Fact]
    public void ASnailSitsShutAndOpensWhileTheSimSaysItIsOpen()
    {
        var m = new LurkerMotion(true);
        Run(m, new LurkerMotion.Seen(false, false, 0, false, 0, 0, 0, null, Shut: true), 2);
        Assert.True(m.Pose.Lid > 0.95 && m.Pose.Retract > 0.95 && m.Pose.EyeLit < 0.05, $"{m.Pose}");
        Run(m, new LurkerMotion.Seen(true, false, 0, false, 0, 0, 0, 20), 1.5);
        Assert.True(m.Pose.Lid < 0.01 && m.Pose.Retract < 0.01 && m.Pose.EyeLit > 0.9, $"{m.Pose}");
        Run(m, new LurkerMotion.Seen(false, false, 0, false, 0, 0, 0, null, Shut: true), 1.5);
        Assert.True(m.Pose.Lid > 0.95, $"{m.Pose}");
    }

    [Fact]
    public void ASnailBitesLeaningBackThenLurchingForwardAndNeverRolls()
    {
        var m = new LurkerMotion(true);
        Run(m, new LurkerMotion.Seen(true, false, 0, false, 0, 0, 0, 20), 1);
        Run(m, new LurkerMotion.Seen(true, true, 0.6, false, 0, 0, 0, 20), 0.5);
        Assert.True(m.Pose.Rock < -0.08, $"{m.Pose}");
        Assert.True(m.Step(new LurkerMotion.Seen(false, false, 0, true, 4, 160, 0, 20), 0.01), "no bite began");
        Run(m, new LurkerMotion.Seen(false, false, 0, true, 4, 160, 0, 20), 0.2);
        Assert.True(m.Pose.Rock > 0.12 && m.Pose.Spin == 0, $"{m.Pose}");
        Run(m, new LurkerMotion.Seen(true, false, 0, false, 0, 0, 0, 20), 1);
        Assert.True(Math.Abs(m.Pose.Rock) < 0.01 && m.Pose.Spin == 0, $"{m.Pose}");
    }

    [Fact]
    public void StruckASnailJoltsAndHurtItsShellCracks()
    {
        var m = new LurkerMotion(true);
        Run(m, new LurkerMotion.Seen(false, false, 0, false, 0, 0, 0.5, null), 2);
        Assert.True(m.Pose.Lid < 0.01 && m.Pose.Crack > 0.29, $"{m.Pose}");
        m.Struck();
        Assert.Equal((1, -0.12), (m.Flash, m.Pose.Rock));
        Run(m, new LurkerMotion.Seen(false, false, 0, false, 0, 0, 0.5, null), 1);
        Assert.True(Math.Abs(m.Pose.Rock) < 0.01 && m.Flash == 0, $"{m.Pose}");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void KilledItDiesForItsTimeThenIsDone(bool snail)
    {
        var m = new LurkerMotion(snail);
        m.Step(new LurkerMotion.Seen(false, false, 0, false, 0, 0, 0, null), 0.01);
        m.Die();
        Run(m, default, LurkerMotion.DieS);
        Assert.True(m.Dying >= LurkerMotion.DieS);
        Assert.True(snail ? m.Pose.Crack > 0.95 : m.Pose.Sag > 0.95, $"{m.Pose}");
    }

    private static void Run(LurkerMotion m, LurkerMotion.Seen s, double seconds)
    {
        for (double t = 0; t < seconds; t += 0.01) m.Step(s, 0.01);
    }

    private static InkSlot Slot(LurkerRig rig, string part) => rig.Slots.First(s => s.Part == part && !s.Shines);

    private static (InkSlot Teal, InkSlot Violet) Moss(LurkerRig rig)
    {
        var moss = rig.Slots.Where(s => s.Part == "mossLit").ToArray();
        return (moss[0], moss[1]);
    }

    private static (double Body, double[] Legs) BodyAndLegs(LurkerRig rig, LurkerArt.Pose p)
    {
        rig.Place(p);
        return (rig.Slots.Single(s => s.Part == "body").Y, [.. rig.Slots.Where(s => s.Part.StartsWith("leg", StringComparison.Ordinal)).Select(s => Math.Round(s.Y, 6))]);
    }

    private static int BigEndian(BinaryReader r)
    {
        var b = r.ReadBytes(4);
        return b[0] << 24 | b[1] << 16 | b[2] << 8 | b[3];
    }
}
