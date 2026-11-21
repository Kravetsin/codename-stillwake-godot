using System;
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
    public void ItsRingRunsFromTheNormalAtTheTopToBothDeathsAtTheBottomANotchADegreeAndPinsATargetPastEither()
    {
        const float top = -MathF.PI / 2;
        // The normal at the top; the heat clockwise, the cold counter-clockwise, each linear to its death at the bottom.
        Assert.Equal(top, Thermometers.RingAngle(37), 4);
        Assert.Equal(top + MathF.PI, Thermometers.RingAngle(42), 4);
        Assert.Equal(top - MathF.PI, Thermometers.RingAngle(28), 4);
        Assert.Equal(top + MathF.PI / 2, Thermometers.RingAngle(39.5f), 4);
        Assert.Equal(top - MathF.PI / 3, Thermometers.RingAngle(34), 4);
        // A little cold: a short way counter-clockwise from the top, as 36.6° is.
        Assert.Equal(top - MathF.PI * 0.4f / 9, Thermometers.RingAngle(36.6f), 4);
        // Past either death, pinned at the bottom.
        Assert.Equal(top + MathF.PI, Thermometers.RingAngle(55), 4);
        Assert.Equal(top - MathF.PI, Thermometers.RingAngle(-341), 4);
        Assert.True(Thermometers.Past(28) && Thermometers.Past(42) && Thermometers.Past(-341) && Thermometers.Past(55));
        Assert.False(Thermometers.Past(37));
        // A notch each whole degree but the normal, which has a pointer; long at each fifth.
        var notches = Thermometers.RingNotches().ToList();
        Assert.Equal(14, notches.Count);
        Assert.Equal([Thermometers.RingAngle(30), Thermometers.RingAngle(35), Thermometers.RingAngle(40)], notches.Where(n => n.Long).Select(n => n.Angle));
    }

    [Fact]
    public void TheLastBreathRunsFromNoneThroughHalfToItsEndAndTheHeartSlowsInTheColdAndRacesInTheHeat()
    {
        Assert.Equal(-1f, Thermometers.Breath(0));
        Assert.Equal(0f, Thermometers.Breath(Spec.Crew.LastBreath));
        Assert.Equal(0.5f, Thermometers.Breath(Spec.Crew.LastBreath / 2), 3);
        Assert.Equal(1f, Thermometers.Breath(1), 1);
        // Beats in two seconds: a cold heart at 44 a minute beats once or twice, a hot one at 140 four or five times.
        static int Beats(bool cold)
        {
            int n = 0;
            for (double t = 0, last = 0; t < 2; t += 0.005)
            {
                double now = Thermometers.Beat(t, cold, 0);
                if (now > 0.9 && last <= 0.9) n++;
                last = now;
            }
            return n;
        }
        Assert.InRange(Beats(true), 1, 2);
        Assert.InRange(Beats(false), 4, 5);
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
    public void ReadsTheBodyTheDegreesWhereItStandsTheHerosLimitsAndWhatItFeelsWhichTheBasicPairTakesAway()
    {
        var w = new TestWorld(Layout.Parse(["S"]).InWorld());
        int spawn = w.M.Crew.Spawn;
        w.Step(new Enter(spawn) { From = 0 });
        Assert.Equal(
            new ThermometerReading(Spec.Temperature.Hull, Spec.Temperature.BareLow, Spec.Temperature.BareHigh, -Spec.Temperature.Hull, 0, 3512,
                Spec.Temperature.Normal + Spec.Temperature.Hull * Spec.Temperature.Sway),
            Thermometers.Read(w.M.Crew, 0, spawn, 35.12f));
        w.Step(new Sim.Items.SetCreative(true));
        // The helmet's and the suit's degrees add up (gear-roles).
        foreach (var (item, slot) in new[] { (Spec.Suit.Basic, Spec.Suit.Slot), (Spec.Helmet.Basic, Spec.Helmet.Slot) })
        {
            w.Step(new Sim.Items.Spawn(item, 1) { From = 0 });
            int bag = w.M.Items.Inventory(0).ToList().FindIndex(s => s?.Item == item);
            int worn = Spec.Inventory.Slots + Spec.Inventory.Equipment.ToList().IndexOf(slot);
            w.Step(new Sim.Items.Move(new Sim.Items.Place(bag), new Sim.Items.Target(worn), 1) { From = 0 });
        }
        Assert.Equal(new ThermometerReading(Spec.Temperature.Hull, -110, 50, 0, 0, Spec.Temperature.Normal), Thermometers.Read(w.M.Crew, 0, spawn, BodyHeat.Normal));
    }
}
