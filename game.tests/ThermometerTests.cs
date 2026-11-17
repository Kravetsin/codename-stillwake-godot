using System.Linq;
using Stillwake.Game.Features.Crew;
using Stillwake.Game.Shared;
using Stillwake.Sim;
using Xunit;
using Enter = Stillwake.Sim.Crew.Enter;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Tests;

/// <summary>The vitals' thermometer (the-thermometer, body-temperature): the body it shows, how deep its frost and heat, what its hover says.</summary>
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
    public void WritesTheBodyToATenthOfADegree()
    {
        Assert.Equal(370, Thermometers.Shown(Thermometers.Normal));
        Assert.Equal("37.0°", Thermometers.Body(370));
        Assert.Equal("35.4°", Thermometers.Body(Thermometers.Shown(35.44f)));
        Assert.Equal("37.0°", Thermometers.Body(Thermometers.Shown(36.96f)));
        Assert.Equal("28.0°", Thermometers.Body(280));
    }

    [Fact]
    public void ItsFrostIsNoneAtTheNormalFullAtTheColdDeathAndEasedOutBetweenItsHeatTheSameAbove()
    {
        Assert.Equal(0f, Thermometers.Frost(37));
        Assert.Equal(0f, Thermometers.Frost(40));
        Assert.Equal(0.75f, Thermometers.Frost(32.5f), 3);
        Assert.Equal(1f, Thermometers.Frost(28));
        Assert.Equal(1f, Thermometers.Frost(20));
        Assert.Equal(0f, Thermometers.Hot(37));
        Assert.Equal(0f, Thermometers.Hot(30));
        Assert.Equal(0.75f, Thermometers.Hot(39.5f), 3);
        Assert.Equal(1f, Thermometers.Hot(42));
        // The body's own shares, as its suit shows them.
        Assert.Equal(Report.ColdOf(33), Thermometers.Frost(33));
        Assert.Equal(Report.HeatOf(40), Thermometers.Hot(40));
    }

    [Fact]
    public void ItsScaleRunsFromDeathToDeathANotchADegreeAndPinsATargetPastEitherToItsEnd()
    {
        Assert.Equal((0f, true), Thermometers.OnScale(28));
        Assert.Equal((1f, true), Thermometers.OnScale(42));
        Assert.Equal((0f, true), Thermometers.OnScale(-341));
        Assert.Equal((1f, true), Thermometers.OnScale(55));
        var (normal, past) = Thermometers.OnScale(37);
        Assert.False(past);
        Assert.Equal(9 / 14f, normal, 4);
        var notches = Thermometers.Notches().ToList();
        Assert.Equal(15, notches.Count);
        Assert.Equal([30, 35, 37, 40], notches.Select((n, i) => (n, d: 28 + i)).Where(x => x.n.Long).Select(x => x.d));
    }

    [Fact]
    public void AboveTheNormalTheStarGivesWayToTheFlameOverADegreeAndAHalf()
    {
        Assert.Equal(0f, Thermometers.Swap(30));
        Assert.Equal(0f, Thermometers.Swap(37));
        Assert.Equal(0.5f, Thermometers.Swap(37.75f), 3);
        Assert.Equal(1f, Thermometers.Swap(37 + Thermometers.SwapDegrees));
        Assert.Equal(1f, Thermometers.Swap(42));
    }

    [Fact]
    public void TheShownBodyGlidesToTheSimsThroughTheTenthsBetweenNeverPassingItAndSettlesOnIt()
    {
        // A body falling from 37° to 32°, a frame at a time.
        float shown = 37;
        var seen = new System.Collections.Generic.List<int>();
        double seconds = 0;
        while (shown != 32 && seconds < 10)
        {
            shown = Thermometers.Glide(shown, 32, 1 / 60.0);
            seconds += 1 / 60.0;
            Assert.True(shown >= 32, $"{shown} passed 32");
            seen.Add(Thermometers.Shown(shown));
        }
        Assert.Equal(32f, shown);
        // GlideS a quarter second: the body is the client's, read four times a second; the last 0.05° snapped some 1.15 s in.
        Assert.InRange(seconds, 1, 1.3);
        Assert.True(seen.Distinct().Count() > 25, $"it passes the tenths between: {seen.Distinct().Count()} shown");
        Assert.Equal(seen.OrderByDescending(d => d), seen);
        Assert.Equal(32f, Thermometers.Glide(32, 32, 1 / 60.0));
        Assert.Equal(36f, Thermometers.Glide(35.96f, 36, 1 / 60.0));
    }

    [Fact]
    public void ItsHoverNamesTheBodyThenThePlacesDegreesAndBothLimitsEachRedOnlyWhileItIsCrossed()
    {
        var cold = Thermometers.Tip(new ThermometerReading(-50, -40, 50, 10, 0, 3544));
        Assert.Equal("35.4°", cold.Name);
        Assert.Equal(["Here −50°", "Withstands down to −40°", "Withstands heat up to +50°"], cold.Lines.Select(l => l.Text));
        Assert.Equal([Tone.Dim, Tone.Bad, Tone.Dim], cold.Lines.Select(l => l.Tone));
        Assert.Equal([Tone.Dim, Tone.Dim, Tone.Dim], Thermometers.Tip(new ThermometerReading(-30, -40, 50, 0, 0)).Lines.Select(l => l.Tone));
        var hot = Thermometers.Tip(new ThermometerReading(60, -40, 50, 0, 10, 3810));
        Assert.Equal(("38.1°", "Here +60°", Tone.Dim, Tone.Bad), (hot.Name, hot.Lines[0].Text, hot.Lines[1].Tone, hot.Lines[2].Tone));
    }

    [Fact]
    public void ReadsTheBodyTheDegreesWhereItStandsTheHerosLimitsAndWhatItFeelsWhichASuitTakesAway()
    {
        var w = new TestWorld(Layout.Parse(["S"]).InWorld());
        int spawn = w.M.Crew.Spawn, suit = Spec.Inventory.Slots + Spec.Inventory.Equipment.ToList().IndexOf(Spec.Suit.Slot);
        w.Step(new Enter(spawn) { From = 0 });
        Assert.Equal(
            new ThermometerReading(Spec.Temperature.Hull, Spec.Temperature.BareLow, Spec.Temperature.BareHigh, -Spec.Temperature.Hull, 0, 3512,
                Spec.Temperature.Normal + Spec.Temperature.Hull * Spec.Temperature.Sway),
            Thermometers.Read(w.M.Crew, 0, spawn, 35.12f));
        w.Step(new Sim.Items.SetCreative(true), new Sim.Items.Spawn(Spec.Suit.Basic, 1) { From = 0 });
        int bag = w.M.Items.Inventory(0).ToList().FindIndex(s => s?.Item == Spec.Suit.Basic);
        w.Step(new Sim.Items.Move(new Sim.Items.Place(bag), new Sim.Items.Target(suit), 1) { From = 0 });
        Assert.Equal(new ThermometerReading(Spec.Temperature.Hull, -110, 50, 0, 0, Spec.Temperature.Normal), Thermometers.Read(w.M.Crew, 0, spawn, BodyHeat.Normal));
    }
}
