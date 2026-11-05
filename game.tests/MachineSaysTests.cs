using Stillwake.Game.Shared;
using Xunit;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Tests;

/// <summary>What the pointer's tip says of any machine (tip-signs): its name, its feature's lines, and its power whether it runs or not.</summary>
public class MachineSaysTests
{
    [Fact]
    public void GivesAMachineThatDrawsWhatItDrawsWorkingOrNotAndAMakerItsMakingAfterAPlus()
    {
        Assert.Equal(new TipLine("4", Sign: Sign.Power), MachineSays.Power(Spec.Machines.Get("crusher"), 4));
        // Its modules counted: what the power module says it draws.
        Assert.Equal(new TipLine("8", Sign: Sign.Power), MachineSays.Power(Spec.Machines.Get("crusher"), 8));
        var solar = Spec.Machines.Get("solarPanel");
        Assert.Equal(new TipLine($"+{solar.Makes}", Sign: Sign.Power), MachineSays.Power(solar, 0));
        Assert.Null(MachineSays.Power(Spec.Machines.Get("workbench"), 0));
    }

    [Fact]
    public void NamesTheMachineFirstThenWhatItsFeatureSaysThenItsPower()
    {
        var power = new TipLine("4", Sign: Sign.Power);
        var said = new TipLine("+1200/min", Sign: Sign.Oxygen);
        Assert.Equal([new TipLine("Oxygen vaporizer"), said, power], MachineSays.Of("generator", [said], power)!);
        Assert.Equal([new TipLine("Crusher"), power], MachineSays.Of("crusher", null, power)!);
        // The pod, never built, has no card to take its name from.
        Assert.Equal("Lifepod", MachineSays.Name("lifepod"));
        Assert.Null(MachineSays.Of("workbench", null, null));
    }
}
