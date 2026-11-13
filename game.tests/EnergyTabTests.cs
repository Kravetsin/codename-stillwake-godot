using System;
using System.Collections.Generic;
using System.Linq;
using Stillwake.Game.Features.Energy;
using Stillwake.Game.Shared;
using Stillwake.Sim.Items;
using Xunit;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Tests;

/// <summary>The charge row (energy-ring, hud-vitals): the belt's stores as the row shows them and the beating bolt.</summary>
public class EnergyTabTests
{
    private static readonly int Full = Spec.Charge.Battery, S = Spec.Charge.Scale;
    private static (int, int)? Store(int charge) => (charge, Full);

    private static void Run(Action<double> step, double seconds, double frame = 0.005)
    {
        for (int i = 0, n = (int)Math.Round(seconds / frame); i < n; i++) step(frame);
    }

    [Fact]
    public void AToolDrainsTheFirstStoreHoldingATickOfItsDrawAsTheSimPays()
    {
        Assert.Equal(1, EnergyRing.DrainingOf([null, Store(Full), Store(Full)], 1));
        // Short of a tick of the grinder's 2: passed by, as the sim's Drain does.
        Assert.Equal(1, EnergyRing.DrainingOf([Store(2 * S - 1), Store(Full)], 2));
        Assert.Equal(0, EnergyRing.DrainingOf([Store(2 * S - 1), Store(Full)], 1));
        // With nothing drawing, the first holding any.
        Assert.Equal(0, EnergyRing.DrainingOf([Store(1), Store(Full)], 0));
        Assert.Equal(-1, EnergyRing.DrainingOf([null, Store(0)], 1));
    }

    [Fact]
    public void WhenDrainingPassesToTheNextStoreASparkHopsThereAndItsArcLightsUp()
    {
        var ring = new EnergyRing();
        ring.Read([Store(S), Store(Full)], 1);
        Run(ring.Step, 1);
        Assert.Equal((0, EnergyRing.LitDrawing, EnergyRing.LitOther), (ring.Draining, ring.Lit(0), ring.Lit(1)));
        Assert.Null(ring.Hop);
        ring.Read([Store(0), Store(Full - S)], 1);
        Assert.Equal((0, 1, 0.0), ring.Hop);
        Run(ring.Step, EnergyRing.HopS);
        Assert.True(ring.Hop?.Gone >= 1 - 1e-6);
        Run(ring.Step, 1);
        Assert.Null(ring.Hop);
        Assert.Equal(EnergyRing.LitDrawing, ring.Lit(1), 2);
        Assert.Equal(EnergyRing.LitOther, ring.Lit(0), 2);
        Assert.Equal(0, ring.Share(0));
    }

    [Fact]
    public void AStorePutOnFillsItsArcInWhileADrainGlidesDown()
    {
        var ring = new EnergyRing();
        ring.Read([Store(Full), null], 1);
        ring.Read([Store(Full - 5 * S), Store(Full)], 1);
        Assert.True(ring.FillingIn(1));
        Assert.Equal(0, ring.Share(1));
        Assert.False(ring.FillingIn(0));
        Assert.Equal(1, ring.Share(0), 3);
        Run(ring.Step, EnergyRing.FillInS);
        Assert.Equal(1, ring.Share(1), 3);
        Assert.Equal((float)(Full - 5 * S) / Full, ring.Share(0), 3);
    }

    [Theory]
    [InlineData(1, 10, 1.0)]
    [InlineData(2, 20, 0.85)]
    public void TheBoltBeatsTenTimesASecondAPowerUnitOfDraw(int draw, int beats, double light)
    {
        var pulse = new BoltPulse((9, 6, 14, 18), 30, new Random(1));
        pulse.Read(1800, draw, low: false);
        int seen = 0;
        double last = 0, most = 0;
        Run(s =>
        {
            pulse.Step(s);
            if (pulse.Beat > last) seen++;
            most = Math.Max(most, pulse.Beat);
            last = pulse.Beat;
        }, 1, 0.005);
        Assert.InRange(seen, beats - 1, beats + 1);
        pulse.Step(0.005);
        Assert.True(pulse.Level > 0.9);
        Assert.Equal(light, most, 6);
    }

    [Theory]
    [InlineData(1, BoltPulse.JoltPx)]
    [InlineData(2, BoltPulse.HarshJoltPx)]
    public void ABeatJoltsTheBoltWithinItsPixelsAndItRestsInPlace(int draw, float most)
    {
        var pulse = new BoltPulse((9, 6, 14, 18), 30, new Random(5));
        pulse.Read(1800, draw, low: false);
        float moved = 0;
        Run(s =>
        {
            pulse.Step(s);
            Assert.InRange(MathF.Abs(pulse.Jolt.X), 0, most);
            Assert.InRange(MathF.Abs(pulse.Jolt.Y), 0, most);
            moved = MathF.Max(moved, MathF.Abs(pulse.Jolt.X) + MathF.Abs(pulse.Jolt.Y));
        }, 2);
        Assert.True(moved > most / 2, $"{moved}: it rattles");
        pulse.Read(1800, 0, low: false);
        Run(pulse.Step, 0.5);
        Assert.Equal((0f, 0f), pulse.Jolt);
    }

    [Fact]
    public void APressOfAFlatBeltBlinksTheBoltRedThreeTimesJoltingItOnceThenRests()
    {
        var blink = new FlatBlink();
        Assert.Equal(0, blink.Red);
        blink.Press();
        Assert.Equal(1, blink.Red);
        int blinks = 0;
        bool lit = true, jolted = false;
        Run(s =>
        {
            blink.Step(s);
            jolted |= blink.Jolt != (0, 0);
            bool now = blink.Red > 0.5;
            if (now && !lit) blinks++;
            lit = now;
        }, FlatBlink.Blinks * FlatBlink.BlinkS + 0.1);
        Assert.Equal(FlatBlink.Blinks - 1, blinks);
        Assert.True(jolted);
        Assert.Equal((0.0, (0, 0)), (blink.Red, blink.Jolt));
    }

    [Fact]
    public void AtRestItNeverBeatsNorThrowsADischarge()
    {
        var pulse = new BoltPulse((9, 6, 14, 18), 30, new Random(2));
        pulse.Read(1800, 0, low: false);
        bool beat = false, thrown = false;
        Run(s =>
        {
            pulse.Step(s);
            beat |= pulse.Beat > 0;
            thrown |= pulse.Arcs.Count > 0;
        }, 10);
        Assert.False(beat || thrown);
        Assert.Equal((true, 0.78), (pulse.Look().On, Math.Round(pulse.Look().Bright, 2)));
    }

    [Fact]
    public void TheGrinderThrowsDischargesOntoTheFrameThatDieOutOnceItStops()
    {
        var pulse = new BoltPulse((9, 6, 14, 18), 30, new Random(3));
        pulse.Read(1800, 2, low: false);
        var ends = new List<(float X, float Y)>();
        Run(s =>
        {
            pulse.Step(s);
            ends.AddRange(pulse.Arcs.Select(a => a.End));
        }, 10);
        Assert.NotEmpty(ends);
        // Every one strikes the frame: its left edge, or its top or bottom.
        Assert.All(ends, e => Assert.True(e.X < 0 || e.Y < 0 || e.Y > 30, $"{e}"));
        pulse.Read(1800, 0, low: false);
        Run(pulse.Step, 1);
        Assert.Empty(pulse.Arcs);
    }

    [Fact]
    public void GoingFlatItSputtersOutAndAStorePutOnKindlesItFromBelow()
    {
        var pulse = new BoltPulse((9, 6, 14, 18), 30, new Random(4));
        pulse.Read(5, 1, low: true);
        pulse.Read(0, 1, low: true);
        Assert.Equal(0, pulse.Sputter);
        Assert.False(pulse.Dead);
        Run(pulse.Step, BoltPulse.SputterS + 0.05);
        Assert.Null(pulse.Sputter);
        Assert.True(pulse.Dead);
        Assert.False(pulse.Look().On);
        pulse.Read(1800, 0, low: false);
        Assert.Equal((0.0, false), (pulse.Kindle, pulse.Dead));
        Run(pulse.Step, BoltPulse.KindleS + 0.05);
        Assert.Null(pulse.Kindle);
        Assert.True(pulse.Look().On);
    }

    [Fact]
    public void TheReadHasAStoreForEachOpenBeltSlotAndTheRunningToolsDraw()
    {
        var belt = EnergyRead.BeltSlots;
        var inventory = new Stack?[Spec.Inventory.Total];
        inventory[belt[0].Slot] = new Stack("battery", 1, Full / 2);
        inventory[belt[2].Slot] = new Stack("accumulator", 1, 7);
        bool Open(Place p) => p != belt[1];
        var read = EnergyRead.Of(inventory, (Full / 2 + 7, Full + Spec.Charge.Accumulator), Open, "grinder");
        Assert.Equal([(Full / 2, Full), (7, Spec.Charge.Accumulator)], read.Stores);
        Assert.Equal(["battery", "accumulator"], read.Items);
        Assert.Equal(Spec.Items.Get("grinder").Draw, read.Draw);
        Assert.Equal(Charges.BeltMost(2), read.Belt.Most);
        Assert.Equal(0, EnergyRead.Of(inventory, (0, 0), _ => true, null).Draw);
        Assert.Equal([(Full / 2, Full), null, (7, Spec.Charge.Accumulator)], EnergyRead.Of(inventory, (0, 0), _ => true, "fireAxe").Stores);
    }
}
