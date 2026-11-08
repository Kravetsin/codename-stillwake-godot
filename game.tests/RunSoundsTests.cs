using Stillwake.Game.Features.Crew;
using Xunit;
using S = Stillwake.Game.Sound;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Tests;

/// <summary>The salvage tools' own sounds (salvage-tools): the electric ones' run and bite, each tool's whoosh and wreck hit.</summary>
public class RunSoundsTests
{
    private const double Frame = 1.0 / 60;

    [Fact]
    public void ARunStartsAndStopsOnceEach()
    {
        var sounds = new RunSounds();
        Assert.Equal(new RunCue(true, false, false), sounds.Step(true, false, Frame, S.GrinderBiteEveryS));
        Assert.Equal(new RunCue(false, false, false), sounds.Step(true, false, Frame, S.GrinderBiteEveryS));
        Assert.Equal(new RunCue(false, true, false), sounds.Step(false, false, Frame, S.GrinderBiteEveryS));
        Assert.Equal(new RunCue(false, false, false), sounds.Step(false, false, Frame, S.GrinderBiteEveryS));
    }

    [Fact]
    public void TheWheelBitesAsItComesOnAndAgainAtItsPaceWhileOn()
    {
        var sounds = new RunSounds();
        sounds.Step(true, false, Frame, S.GrinderBiteEveryS);
        var bites = new List<int>();
        for (int i = 0; i < 120; i++)
            if (sounds.Step(true, true, Frame, S.GrinderBiteEveryS).Bites) bites.Add(i);
        // Two seconds on: at once, then every 0.6 s.
        Assert.Equal([0, 36, 72, 108], bites);
        // Off and on again, it bites at once.
        sounds.Step(true, false, Frame, S.GrinderBiteEveryS);
        Assert.True(sounds.Step(true, true, Frame, S.GrinderBiteEveryS).Bites);
        // Not running, nothing bites.
        Assert.False(new RunSounds().Step(false, true, Frame, S.GrinderBiteEveryS).Bites);
    }

    [Fact]
    public void EachStruckToolWhooshesAndHitsAWreckAsPicked()
    {
        Assert.Equal((S.Swing, S.HammerSwing, S.SpearThrust),
            (S.SwingOf(Spec.Weapons.Axe.Item), S.SwingOf(Spec.Weapons.Sledgehammer.Item), S.SwingOf(Spec.Weapons.Spear.Item)));
        Assert.Equal((S.Chop, S.HammerBlow, S.SpearHit),
            (S.WreckHitOf(Spec.Weapons.Axe.Item), S.WreckHitOf(Spec.Weapons.Sledgehammer.Item), S.WreckHitOf(Spec.Weapons.Spear.Item)));
        Assert.All(new[] { S.HammerSwing, S.SpearThrust, S.GrinderRun, S.GrinderBite, S.CutterStrike, S.CutterRun, S.CutterOff },
            cue => Assert.Equal(S.ToolIdleDb, cue.VolumeDb));
        Assert.True(S.GrinderRun.Loops && S.CutterRun.Loops);
        Assert.Equal(S.SpearThrustPeakS, Strokes.Spear.PeakS);
        Assert.Equal(S.HammerSwingPeakS, Strokes.Hammer.PeakS);
        Assert.Equal(S.SwingPeakS, Strokes.Axe.PeakS);
    }

    [Fact]
    public void OnlyTheDrillsHitsOnAWreckAreHeardAsTheChop()
    {
        Assert.True(S.ChopsWrecks(Spec.Weapons.Drill.Item));
        Assert.False(S.ChopsWrecks(Spec.Weapons.Grinder.Item));
        Assert.False(S.ChopsWrecks(Spec.Weapons.PlasmaCutter.Item));
    }
}
