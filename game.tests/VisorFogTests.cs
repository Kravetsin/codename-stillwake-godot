using System;
using Stillwake.Game.Features.Crew;
using Xunit;
using CrewSpec = Stillwake.Content.Crew;

namespace Stillwake.Game.Tests;

/// <summary>A low suit fogs the visor (2026-11-05-the-last-breath): in with each breath out, deeper and quicker as the air goes, evaporating once safe.</summary>
public class VisorFogTests
{
    private const double Frame = 1.0 / 60;
    private static readonly double LastBreathS = (double)CrewSpec.LastBreath / Stillwake.Sim.World.TicksPerSecond;

    /// <summary>Runs <paramref name="seconds"/> on in frames: the look at the end, the most it reached and how many breaths out it took.</summary>
    private static (FogLook Last, double Most, double MostDim, int Breaths) Run(VisorFog fog, double seconds)
    {
        FogLook look = default, before = default;
        double most = 0, mostDim = 0;
        int breaths = 0;
        bool rising = false;
        for (double t = 0; t < seconds - 1e-9; t += Frame)
        {
            look = fog.Advance(Frame);
            most = Math.Max(most, look.Reach);
            mostDim = Math.Max(mostDim, look.LastDim);
            if (rising && look.Reach < before.Reach - 1e-9) breaths++;
            rising = look.Reach > before.Reach + 1e-9 || (rising && look.Reach >= before.Reach - 1e-9);
            before = look;
        }
        return (look, most, mostDim, breaths);
    }

    [Fact]
    public void StaysClearWithAirToSpareOrInRoomAir()
    {
        var fog = new VisorFog();
        fog.Read(SuitDanger.None, 0.5);
        Assert.Equal(default, Run(fog, 3).Last);
        fog.Read(SuitDanger.None, 0.05);
        Assert.Equal(0, Run(fog, 3).Most);
    }

    [Fact]
    public void ComesInAsTheSuitRunsLowDeeperAndQuickerAsTheAirGoes()
    {
        var shallow = new VisorFog();
        shallow.Read(SuitDanger.Low, 0.24);
        Run(shallow, 2);
        var (_, shallowMost, _, shallowBreaths) = Run(shallow, 10);
        var deep = new VisorFog();
        deep.Read(SuitDanger.Low, 0.02);
        Run(deep, 2);
        var (_, deepMost, deepDim, deepBreaths) = Run(deep, 10);
        Assert.InRange(shallowMost, 0.1, 0.2);
        Assert.InRange(deepMost, 0.5, 0.65);
        Assert.InRange(shallowBreaths, 2, 4);
        Assert.InRange(deepBreaths, 9, 11);
        // The gauge's last lamp dims with each breath out, never out.
        Assert.InRange(deepDim, 0.6, 0.7);
    }

    [Fact]
    public void FlattensItsGaspsAsTheLastBreathRunsAndStaysStillWhileOut()
    {
        var fog = new VisorFog();
        fog.Read(SuitDanger.Low, 0);
        Run(fog, LastBreathS * 0.6);
        var dry = Run(fog, 3);
        Assert.True(dry.Last.Reach > 0.3);
        Assert.Equal(0, dry.MostDim, 3);
        fog.Read(SuitDanger.Out, 0);
        var @out = Run(fog, 3);
        Assert.Equal((0.0, 0), (@out.MostDim, @out.Breaths));
        Assert.True(@out.Last.Reach > 0.3);
    }

    [Fact]
    public void EvaporatesOnceOutOfDangerAndComesBackFromWhatIsLeft()
    {
        var fog = new VisorFog();
        fog.Read(SuitDanger.Low, 0.1);
        Run(fog, 2);
        fog.Read(SuitDanger.None, 0.5);
        var half = Run(fog, VisorFog.EvaporateS / 2).Last;
        Assert.InRange(half.Go, 0.45, 0.55);
        Assert.True(half.Reach > 0);
        Assert.Equal(default, Run(fog, VisorFog.EvaporateS / 2 + Frame).Last);
        // Low again halfway through: no jump back to a whole fog.
        fog.Read(SuitDanger.Low, 0.1);
        Run(fog, 2);
        fog.Read(SuitDanger.None, 0.5);
        Run(fog, VisorFog.EvaporateS / 2);
        fog.Read(SuitDanger.Low, 0.1);
        double go = fog.Advance(Frame).Go;
        Assert.InRange(go, 0.4, 0.5);
        Assert.True(fog.Advance(Frame).Go < go);
        Assert.Equal(0, Run(fog, VisorFog.ComeS).Last.Go);
    }
}
