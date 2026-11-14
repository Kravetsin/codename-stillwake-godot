using System.Linq;
using Stillwake.Game.Features.Crew;
using Stillwake.Game.Shared;
using Stillwake.Sim;
using Xunit;
using Enter = Stillwake.Sim.Crew.Enter;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Tests;

/// <summary>The vitals' thermometer (the-thermometer): what it reads, how deep its frost, what its hover says.</summary>
public class ThermometerTests
{
    [Fact]
    public void WritesTheDegreesWithATrueMinusAPlusAboveZeroAndTheDegreeSign()
    {
        Assert.Equal("−50°", Thermometers.Reading(-50));
        Assert.Equal("0°", Thermometers.Reading(0));
        Assert.Equal("+20°", Thermometers.Reading(20));
    }

    [Fact]
    public void ItsFrostIsNoneInsideTheLimitFullAtThirtyDegreesFeltAndEasedOutBetween()
    {
        Assert.Equal(0f, Thermometers.Frost(0));
        Assert.Equal(0.75f, Thermometers.Frost(15), 3);
        Assert.Equal(1f, Thermometers.Frost(Report.FeltFull));
        Assert.Equal(1f, Thermometers.Frost(50));
    }

    [Fact]
    public void ItsHoverSaysTheReadingAndTheLimitRedOnlyWhileTheColdIsFelt()
    {
        var past = Thermometers.Tip(-50, -40, 10);
        Assert.Equal("−50°", past.Name);
        Assert.Equal("Withstands down to −40°", past.Lines.Single().Text);
        Assert.Equal(Tone.Bad, past.Lines.Single().Tone);
        Assert.Equal(Tone.Dim, Thermometers.Tip(-30, -40, 0).Lines.Single().Tone);
    }

    [Fact]
    public void ReadsTheDegreesAtTheBodyTheHerosLimitAndWhatItFeelsWhichASuitTakesAway()
    {
        var w = new TestWorld(Layout.Parse(["S"]).InWorld());
        int spawn = w.M.Crew.Spawn, suit = Spec.Inventory.Slots + Spec.Inventory.Equipment.ToList().IndexOf(Spec.Suit.Slot);
        w.Step(new Enter(spawn) { From = 0 });
        Assert.Equal((Spec.Temperature.Space, Spec.Temperature.BareLow, 40), Thermometers.Read(w.M.Crew, 0, spawn));
        w.Step(new Sim.Items.SetCreative(true), new Sim.Items.Spawn(Spec.Suit.Basic, 1) { From = 0 });
        int bag = w.M.Items.Inventory(0).ToList().FindIndex(s => s?.Item == Spec.Suit.Basic);
        w.Step(new Sim.Items.Move(new Sim.Items.Place(bag), new Sim.Items.Target(suit), 1) { From = 0 });
        Assert.Equal((-40, -40, 0), Thermometers.Read(w.M.Crew, 0, spawn));
    }
}
