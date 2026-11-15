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
        Assert.Equal(0f, Thermometers.Depth(0));
        Assert.Equal(0.75f, Thermometers.Depth(15), 3);
        Assert.Equal(1f, Thermometers.Depth(Report.FeltFull));
        Assert.Equal(1f, Thermometers.Depth(50));
    }

    [Fact]
    public void PastTheHighTheStarGivesWayToTheFlameOverADegreeAndAHalf()
    {
        Assert.Equal(0f, Thermometers.Swap(0));
        Assert.Equal(0.5f, Thermometers.Swap(0.75f), 3);
        Assert.Equal(1f, Thermometers.Swap(Thermometers.SwapDegrees));
        Assert.Equal(1f, Thermometers.Swap(30));
    }

    [Fact]
    public void ItGlidesToANewReadingThroughTheDegreesBetweenAndSettlesOnIt()
    {
        // Out of the ship into space: +20° to −40°, a frame at a time.
        float shown = 20;
        var seen = new System.Collections.Generic.List<int>();
        double seconds = 0;
        while (shown != -40 && seconds < 5)
        {
            shown = Thermometers.Glide(shown, -40, 1 / 60.0);
            seconds += 1 / 60.0;
            seen.Add(Thermometers.Shown(shown));
        }
        Assert.Equal(-40f, shown);
        Assert.InRange(seconds, 1, 2);
        Assert.True(seen.Distinct().Count() > 30, $"it passes the degrees between: {seen.Distinct().Count()} shown");
        Assert.Equal(seen.OrderByDescending(d => d), seen);
        Assert.Equal(-40f, Thermometers.Glide(-40, -40, 1 / 60.0));
    }

    [Fact]
    public void ItsHoverSaysTheReadingAndBothLimitsEachRedOnlyWhileItIsCrossed()
    {
        var cold = Thermometers.Tip(new ThermometerReading(-50, -40, 50, 10, 0));
        Assert.Equal("−50°", cold.Name);
        Assert.Equal(["Withstands down to −40°", "Withstands heat up to +50°"], cold.Lines.Select(l => l.Text));
        Assert.Equal([Tone.Bad, Tone.Dim], cold.Lines.Select(l => l.Tone));
        Assert.Equal([Tone.Dim, Tone.Dim], Thermometers.Tip(new ThermometerReading(-30, -40, 50, 0, 0)).Lines.Select(l => l.Tone));
        var hot = Thermometers.Tip(new ThermometerReading(60, -40, 50, 0, 10));
        Assert.Equal(("+60°", Tone.Dim, Tone.Bad), (hot.Name, hot.Lines[0].Tone, hot.Lines[1].Tone));
    }

    [Fact]
    public void ReadsTheDegreesAtTheBodyTheHerosLimitAndWhatItFeelsWhichASuitTakesAway()
    {
        var w = new TestWorld(Layout.Parse(["S"]).InWorld());
        int spawn = w.M.Crew.Spawn, suit = Spec.Inventory.Slots + Spec.Inventory.Equipment.ToList().IndexOf(Spec.Suit.Slot);
        w.Step(new Enter(spawn) { From = 0 });
        Assert.Equal(new ThermometerReading(Spec.Temperature.Hull, Spec.Temperature.BareLow, Spec.Temperature.BareHigh, 20, 0), Thermometers.Read(w.M.Crew, 0, spawn));
        w.Step(new Sim.Items.SetCreative(true), new Sim.Items.Spawn(Spec.Suit.Basic, 1) { From = 0 });
        int bag = w.M.Items.Inventory(0).ToList().FindIndex(s => s?.Item == Spec.Suit.Basic);
        w.Step(new Sim.Items.Move(new Sim.Items.Place(bag), new Sim.Items.Target(suit), 1) { From = 0 });
        Assert.Equal(new ThermometerReading(-20, -40, 50, 0, 0), Thermometers.Read(w.M.Crew, 0, spawn));
    }
}
