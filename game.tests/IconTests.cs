using System;
using System.Collections.Generic;
using System.Linq;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using Xunit;
using static Stillwake.Game.IconLooks;

namespace Stillwake.Game.Tests;

/// <summary>
/// The glowing line icons: a module technology's (0086), the shared hexagon, its glyph crackling on twos, painted as the
/// page drew it; and a station's marks (0095), the shared ring, each its glyph, colours and motion.
/// </summary>
public class IconTests
{
    private static readonly Look Arc = TechLooks.Arc;

    public static TheoryData<SlotMark> Marks => new(MarkLooks.All.Keys);

    [Fact]
    public void StillTheIconIsTheClosedFrameThenTheGlyphAsDrawn()
    {
        var lines = Lines(Arc, 0).Lines;
        Assert.Equal(1 + Arc.Glyph.Count, lines.Count);
        Assert.True(lines[0].Frame);
        Assert.Same(Hexagon, lines[0].Points);
        Assert.Equal(lines[0].Points[0], lines[0].Points[^1]);
        Assert.Equal(Arc.Glyph[0].Points, lines[1].Points);
        Assert.False(lines[1].Frame);
    }

    [Fact]
    public void ACrackleKinksEachSegmentNearItsLineAndKeepsItsEnds()
    {
        var glyph = Arc.Glyph[0].Points;
        for (int seed = 1; seed <= 500; seed++)
        {
            var bolt = Lines(Arc, seed).Lines[1].Points;
            Assert.Equal(glyph[0], bolt[0]);
            Assert.Equal(glyph[^1], bolt[^1]);
            int at = 0;
            for (int i = 1; i < glyph.Count; i++)
            {
                var (a, b) = (glyph[i - 1], glyph[i]);
                float length = MathF.Sqrt(Sq(b.U - a.U) + Sq(b.V - a.V));
                int kinks = length > LongSegment ? 2 : 1;
                for (int k = 1; k <= kinks; k++)
                    Assert.True(OffLine(bolt[at + k], a, b) <= Stray * length + 1e-5f, $"seed {seed}");
                at += kinks + 1;
                Assert.Equal(b, bolt[at]);
            }
        }
    }

    [Fact]
    public void ACrackleIsTheSameForItsSeedAndAnotherForTheNext()
    {
        Assert.Equal(Lines(Arc, 7).Lines[1].Points, Lines(Arc, 7).Lines[1].Points);
        Assert.NotEqual(Lines(Arc, 7).Lines[1].Points, Lines(Arc, 8).Lines[1].Points);
    }

    [Fact]
    public void NowAndThenACrackleBranchesThinnerAndNeverLeavesTheIcon()
    {
        int branched = 0, seeds = 4000;
        for (int seed = 1; seed <= seeds; seed++)
        {
            var pose = Lines(Arc, seed);
            var lines = pose.Lines;
            if (lines.Count > 2)
            {
                branched++;
                Assert.Equal(Arc.Glyph[0].Weight * 0.6f, lines[2].Weight, 4);
                Assert.Contains(lines[2].Points[0], lines[1].Points);
            }
            Assert.All(lines.SelectMany(l => l.Points), p => Assert.True(p.U is >= 0 and <= 1 && p.V is >= 0 and <= 1, $"seed {seed}: {p}"));
            Assert.InRange(pose.Intensity, Dimmest, Brightest);
        }
        Assert.Equal(BranchChance, branched / (double)seeds, 1);
    }

    [Theory]
    [InlineData(IconShow.Mark, 14)]
    [InlineData(IconShow.Ghost, 32)]
    [InlineData(IconShow.Hot, 32)]
    [InlineData(IconShow.Lit, 12)]
    public void EveryWayItIsShownPaintsTheIconAndLeavesItsCornersClear(IconShow show, int side)
    {
        foreach (var look in MarkLooks.All.Values.Prepend(Arc))
        {
            var canvas = Paint(look, show, side, 0);
            Assert.Contains(Alphas(canvas), a => a > 100);
            foreach (int p in new[] { 0, side - 1, side * (side - 1), side * side - 1 })
                Assert.True(canvas.Rgba[p * 4 + 3] < 40, $"{show}: corner {p} at {canvas.Rgba[p * 4 + 3]}");
        }
    }

    [Fact]
    public void AGhostIsOnlyTheRimFaintWhileLitTheGlyphBurnsWhiteHot()
    {
        foreach (var look in MarkLooks.All.Values.Prepend(Arc))
        {
            var ghost = Paint(look, IconShow.Ghost, 32, 0);
            uint rim = look.Rim;
            for (int p = 0; p < 32 * 32; p++)
            {
                if (ghost.Rgba[p * 4 + 3] == 0) continue;
                Assert.InRange(ghost.Rgba[p * 4], (rim >> 16 & 0xff) - 1, (rim >> 16 & 0xff) + 1);
                Assert.InRange(ghost.Rgba[p * 4 + 2], (rim & 0xff) - 1, rim & 0xff);
                // Faint: one stroke's 0.48, or two laid over each other where a glyph's strokes meet.
                Assert.True(ghost.Rgba[p * 4 + 3] <= Math.Round((1 - 0.52 * 0.52) * 255) + 1, $"{look.Motion} at {p}: {ghost.Rgba[p * 4 + 3]}");
            }
            // The arc's one bolt never meets itself: its ghost stays as faint as the page drew it.
            if (look == Arc) Assert.True(Enumerable.Range(0, 32 * 32).All(p => ghost.Rgba[p * 4 + 3] <= Math.Round(0.48 * 255) + 1));
            var lit = Paint(look, IconShow.Lit, 32, 0);
            Assert.Contains(Enumerable.Range(0, 32 * 32), p => lit.Rgba[p * 4 + 3] > 240 && lit.Rgba[p * 4 + 1] > 230);
        }
    }

    [Fact]
    public void AMarkSitsOnADarkHaloAndCarriedOverASocketTheIconGlowsSofterThanOnACard()
    {
        foreach (var look in MarkLooks.All.Values.Prepend(Arc))
        {
            var mark = Paint(look, IconShow.Mark, 14, 3);
            var lit = Paint(look, IconShow.Lit, 14, 3);
            Assert.True(Alphas(mark).Sum() > Alphas(lit).Sum());
            Assert.Contains(Enumerable.Range(0, 14 * 14), p => mark.Rgba[p * 4 + 3] > 150 && mark.Rgba[p * 4] < 40);
            Assert.True(Alphas(Paint(look, IconShow.Hot, 32, 3)).Sum() < Alphas(Paint(look, IconShow.Lit, 32, 3)).Sum());
        }
    }

    [Fact]
    public void TheHexagonIsAModulesTechAndTheRingAStationsWork()
    {
        Assert.All(TechLooks.All.Values, look => Assert.Same(Hexagon, look.Frame));
        Assert.All(MarkLooks.All.Values, look => Assert.Same(Ring, look.Frame));
        Assert.Equal(RingPoints + 1, Ring.Count);
        Assert.Equal(Ring[0], Ring[^1]);
        // As wide as the hexagon, so a module's mark and a station's sit alike in a corner.
        Assert.All(Ring, p => Assert.Equal(.43f, Distance(p, (.5f, .5f)), 4));
    }

    [Fact]
    public void EveryMarkHasItsOwnGlyphColoursAndMotionAndNoneIsTheArcs()
    {
        var looks = MarkLooks.All.Values.Prepend(Arc).ToList();
        Assert.Equal(looks.Count, looks.Select(l => l.Rim).Distinct().Count());
        Assert.Equal(looks.Count, looks.Select(l => l.Glow).Distinct().Count());
        Assert.Equal(looks.Count, looks.Select(l => l.Motion).Distinct().Count());
        Assert.All(looks, l => Assert.NotEmpty(l.Glyph));
        // Fits and None have no look of their own: a module's mark is its tech's.
        Assert.False(MarkLooks.All.ContainsKey(SlotMark.Fits));
        Assert.False(MarkLooks.All.ContainsKey(SlotMark.None));
    }

    [Theory]
    [MemberData(nameof(Marks))]
    public void AMarkMovesInsideItsRingAndEachFrameDrawsAlike(SlotMark mark)
    {
        var look = MarkLooks.Of(mark);
        for (int frame = 0; frame <= 240; frame++)
        {
            var pose = Lines(look, frame);
            Assert.All(pose.Lines.Skip(1).SelectMany(l => l.Points), p => Assert.True(Distance(p, (.5f, .5f)) < .43f - 0.05f, $"{mark} frame {frame}: {p}"));
            Assert.InRange(pose.Intensity, 0.8f, 1.2f);
            Assert.Equal(Flat(pose), Flat(Lines(look, frame)));
        }
    }

    [Fact]
    public void TheQuestionMarkStandsStillForItMarksSoManySlots()
    {
        var rest = Flat(Lines(MarkLooks.Unknown, 0));
        for (int frame = 1; frame <= 60; frame++) Assert.Equal(rest, Flat(Lines(MarkLooks.Unknown, frame)));
    }

    [Fact]
    public void AFlameLicksAtItsTipAndKeepsItsFoot()
    {
        var flame = MarkLooks.Burns;
        var rest = Lines(flame, 0).Lines[1].Points;
        int foot = Enumerable.Range(0, rest.Count).MaxBy(i => rest[i].V);
        int tip = Enumerable.Range(0, rest.Count).MinBy(i => rest[i].V);
        var tips = new HashSet<(float, float)>();
        float brightest = 0, dimmest = 2;
        for (int frame = 1; frame <= 60; frame++)
        {
            var pose = Lines(flame, frame);
            var points = pose.Lines[1].Points;
            Assert.Equal(rest[foot].V, points[foot].V, 3);
            Assert.True(MathF.Abs(points[foot].U - rest[foot].U) < 0.01f);
            Assert.True(Distance(points[tip], rest[tip]) <= MathF.Sqrt(Sq(FlickerSway) + Sq(FlickerLift)) + 1e-4f);
            tips.Add(points[tip]);
            (brightest, dimmest) = (MathF.Max(brightest, pose.Intensity), MathF.Min(dimmest, pose.Intensity));
        }
        Assert.True(tips.Count > 50, "a fresh lick each frame");
        Assert.True(brightest > 1.05f && dimmest < 0.95f, "it flickers");
    }

    [Fact]
    public void ASproutSwaysFromItsFootOneWayAndBackOverItsRoundAndItsGroundStaysPut()
    {
        var sprout = MarkLooks.Grows;
        var rest = Flat(Lines(sprout, 0));
        int ground = sprout.Glyph.Count;
        var stemTop = Lines(sprout, 0).Lines[1].Points[^1];
        float left = 0, right = 0;
        for (int frame = 1; frame <= SwayFrames; frame++)
        {
            var lines = Lines(sprout, frame).Lines;
            Assert.Equal(sprout.Glyph[^1].Points, lines[ground].Points);
            Assert.Equal(sprout.Pivot, lines[1].Points[0]);
            float off = lines[1].Points[^1].U - stemTop.U;
            (left, right) = (MathF.Min(left, off), MathF.Max(right, off));
        }
        Assert.True(left < -0.02f && right > 0.02f, $"{left}..{right}");
        Assert.Equal(rest, Flat(Lines(sprout, SwayFrames)));
    }

    [Fact]
    public void AnIngotStaysAndItsHeatRisesInWavesFromTheFootOfEachWisp()
    {
        var ingot = MarkLooks.Smelts;
        var rest = Lines(ingot, 0).Lines;
        var wisps = new HashSet<string>();
        for (int frame = 1; frame <= SimmerFrames; frame++)
        {
            var pose = Lines(ingot, frame);
            for (int i = 0; i < ingot.Glyph.Count; i++)
            {
                var (was, now) = (rest[i + 1].Points, pose.Lines[i + 1].Points);
                if (!ingot.Glyph[i].Moves) Assert.Equal(was, now);
                else
                {
                    Assert.Equal(was[0], now[0]);
                    Assert.All(was.Zip(now), p => Assert.True(p.First.V == p.Second.V && MathF.Abs(p.First.U - p.Second.U) <= SimmerBob + 1e-5f));
                    if (i == 2) wisps.Add(string.Join(";", now));
                }
            }
            Assert.InRange(pose.Intensity, 1 - SimmerBreath - 1e-5f, 1 + SimmerBreath + 1e-5f);
        }
        Assert.Equal(SimmerFrames, wisps.Count);
    }
    [Fact]
    public void ABatteryChargesBarByBarFromTheBottomThenEmptiesAndItsGhostIsFull()
    {
        var battery = MarkLooks.Charges;
        int steps = battery.Glyph.Max(s => s.Step);
        Assert.Equal(3, steps);
        Assert.Equal(1 + battery.Glyph.Count, Lines(battery, 0).Lines.Count);
        var counts = Enumerable.Range(0, (steps + 2) * FillFrames).Select(f => Lines(battery, f + (steps + 2) * FillFrames).Lines.Count).ToList();
        int body = 1 + battery.Glyph.Count(s => s.Step == 0);
        Assert.Equal(body, counts[0]);
        for (int step = 1; step <= steps; step++) Assert.Equal(body + step, counts[step * FillFrames]);
        Assert.Equal(body + steps, counts[(steps + 1) * FillFrames]);
        // The bars light from the bottom up.
        var bars = battery.Glyph.Where(s => s.Step > 0).OrderBy(s => s.Step).ToList();
        Assert.True(bars.Zip(bars.Skip(1)).All(p => p.First.Points[0].V > p.Second.Points[0].V));
    }

    [Fact]
    public void ADronesBladesSpinInTheirRingsNeighboursTheOtherWayAndItsFrameStaysPut()
    {
        var drone = MarkLooks.Docks;
        var rest = Lines(drone, 0).Lines;
        var blades = Enumerable.Range(0, drone.Glyph.Count).Where(i => drone.Glyph[i].Moves).ToList();
        Assert.Equal(4, blades.Count);
        var angles = new HashSet<float>();
        for (int frame = 1; frame <= 360 / SpinStep; frame++)
        {
            var lines = Lines(drone, frame).Lines;
            for (int i = 0; i < drone.Glyph.Count; i++)
            {
                var (was, now) = (rest[i + 1].Points, lines[i + 1].Points);
                if (!drone.Glyph[i].Moves)
                {
                    Assert.Equal(was, now);
                    continue;
                }
                // Turned about its middle, as long as it was.
                Assert.Equal(Distance(was[0], was[1]), Distance(now[0], now[1]), 4);
                Assert.Equal((was[0].U + was[1].U) / 2, (now[0].U + now[1].U) / 2, 4);
                Assert.Equal((was[0].V + was[1].V) / 2, (now[0].V + now[1].V) / 2, 4);
            }
            var (a, b) = (lines[blades[0] + 1].Points, lines[blades[1] + 1].Points);
            float slopeA = (a[1].V - a[0].V) / (a[1].U - a[0].U + 1e-6f), slopeB = (b[1].V - b[0].V) / (b[1].U - b[0].U + 1e-6f);
            if (frame % 3 != 0) Assert.Equal(-slopeA, slopeB, 2);
            angles.Add(MathF.Round(slopeA, 2));
        }
        Assert.True(angles.Count >= 3, "it turns");
    }

    [Fact]
    public void AMotionThatComesRoundDrawsItsCycleAgainAndOnlyTheCrackleNeverDoes()
    {
        Assert.Null(Cycle(Arc));
        Assert.Equal(1, Cycle(MarkLooks.Unknown));
        foreach (var look in MarkLooks.All.Values.Where(l => l.Motion is not (IconMotion.Still or IconMotion.Flicker)))
        {
            int cycle = Cycle(look)!.Value;
            for (int frame = 1; frame <= cycle; frame++) Assert.Equal(Flat(Lines(look, frame)), Flat(Lines(look, frame + cycle)));
        }
        // A flicker's frames are as many as its cycle, each its own.
        var flames = Enumerable.Range(1, FlickerFrames).Select(f => Flat(Lines(MarkLooks.Burns, f))).ToList();
        Assert.Equal(FlickerFrames, flames.Distinct().Count());
    }
    [Fact]
    public void AMarkSitsInItsSlotsTopRightClearOfTheDigitTheCountAndTheCharge()
    {
        foreach (var scale in Enum.GetValues<SlotScale>())
        {
            int slot = Slots.SizeOf(scale);
            var (x, y) = Slots.MarkAt(slot);
            Assert.Equal(slot - Slots.MarkMargin, x + Slots.MarkSide);
            Assert.Equal(Slots.MarkMargin, y);
            // The top-left quarter is the hotbar's digit, the bottom the count and the charge's bar.
            Assert.True(x >= slot / 2 - 2, $"{scale}: {x}");
            Assert.True(y + Slots.MarkSide <= slot / 2, $"{scale}: {y}");
            Assert.Equal(Slots.IconOf(scale), Slots.GhostSide(slot));
        }
    }

    private static IconCanvas Paint(Look look, IconShow show, int side, int frame)
    {
        var canvas = new IconCanvas(side);
        var pose = Lines(look, frame);
        canvas.Paint(look, pose.Lines, show, 1);
        return canvas;
    }

    private static string Flat(Pose pose) =>
        string.Join("|", pose.Lines.Select(l => string.Join(";", l.Points))) + $"@{pose.Intensity}";

    private static int[] Alphas(IconCanvas canvas) => Enumerable.Range(0, canvas.Side * canvas.Side).Select(p => (int)canvas.Rgba[p * 4 + 3]).ToArray();

    private static float Sq(float v) => v * v;

    private static float Distance((float U, float V) a, (float U, float V) b) => MathF.Sqrt(Sq(a.U - b.U) + Sq(a.V - b.V));

    private static float OffLine((float U, float V) p, (float U, float V) a, (float U, float V) b)
    {
        float du = b.U - a.U, dv = b.V - a.V;
        return MathF.Abs((p.U - a.U) * dv - (p.V - a.V) * du) / MathF.Sqrt(du * du + dv * dv);
    }
}
