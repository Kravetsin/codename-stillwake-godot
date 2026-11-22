using System;
using System.Collections.Generic;
using System.Linq;
using Stillwake.Game.Features.Crew;
using Stillwake.Game.Shared;
using Xunit;
using A = Stillwake.Game.Features.Crew.FormVision.AlarmBeats;
using L = Stillwake.Game.Features.Crew.FormVision.LampBeats;
using S = Stillwake.Game.Features.Crew.FormVision.ScanBeats;

namespace Stillwake.Game.Tests;

/// <summary>
/// A form's vision (form-vision, the picks «Проступает схема» and «Кадр следует»): every form a lesson teaches has one, paced as its page's check
/// holds it: one thing at a time, held while it is read, the result held, the view gliding to each.
/// </summary>
public class FormVisionTests
{
    private const double Frame = 1 / 60.0;

    public static TheoryData<VisionForm> Forms => new() { VisionForm.Scan, VisionForm.Lamp, VisionForm.Alarms };

    [Fact]
    public void EveryFormALessonTeachesHasAVision()
    {
        foreach (var lesson in Content.Lessons.All) Assert.NotNull(FormVision.Of(lesson.Form));
        foreach (var form in Content.Lessons.Forms) Assert.NotNull(FormVision.Of(form));
    }

    [Theory]
    [InlineData(VisionForm.Scan, 13.8)]
    [InlineData(VisionForm.Lamp, 12.7)]
    [InlineData(VisionForm.Alarms, 13.5)]
    public void EachVisionLastsAsItsPagePacesIt(VisionForm form, double seconds) => Assert.Equal(seconds, FormVision.SecondsOf(form), 9);

    /// <summary>The sim spares a hero through the longest vision and a second's trip to the host, one window for every form.</summary>
    [Theory]
    [MemberData(nameof(Forms))]
    public void TheSimSparesTheHeroThroughEveryVision(VisionForm form) =>
        Assert.True(Content.Crew.VisionTicks >= (FormVision.SecondsOf(form) + 1) * Sim.World.TicksPerSecond - 1e-6);

    /// <summary>The void comes in and the HUD goes, both gone while it shows, and both back as it ends; the hero floats only in the void.</summary>
    [Theory]
    [MemberData(nameof(Forms))]
    public void TheWorldGoesAndComesBack(VisionForm form)
    {
        double length = FormVision.SecondsOf(form);
        Assert.Equal((0.0, 1.0), (FormVision.Veil(form, 0), FormVision.Hud(form, 0)));
        for (double u = FormVision.In; u <= length - FormVision.Out; u += Frame) Assert.Equal((1.0, 0.0), (FormVision.Veil(form, u), FormVision.Hud(form, u)));
        Assert.Equal(0, FormVision.Veil(form, length), 6);
        Assert.Equal(1, FormVision.Hud(form, length), 6);
        Assert.Equal(0, FormVision.Float(form, 0).Lift, 6);
        Assert.True(FormVision.Float(form, length / 2).Lift > 0);
        Assert.True(FormVision.Playing(form, length - Frame));
        Assert.False(FormVision.Playing(form, length));
    }

    /// <summary>What shows of a form's things: how present each is, where, how far the drone has taken it (its diagram, its dots out).</summary>
    private static (double Alpha, Vec At, double Taken)[] Things(VisionFrame f)
    {
        double Dots(int piece)
        {
            double sum = 0;
            for (int j = 0; j < DroneInk.Dots; j++)
                if (FormVision.DotAt(j, f.U) is { } d && (d.To == piece || d.From == piece)) sum += d.Away;
            return sum;
        }
        return f.Form switch
        {
            VisionForm.Scan => [(f.First!.Value.Alpha, f.First.Value.At, f.First.Value.Trace + Dots(0)), (f.Second!.Value.Alpha, f.Second.Value.At, f.Second.Value.Trace + Dots(1))],
            VisionForm.Lamp => [(f.First!.Value.Alpha, f.First.Value.At, f.First.Value.Trace), (f.Lamp!.Value.Light * f.Lamp.Value.InCone, f.Second!.Value.At, f.Second.Value.Trace)],
            _ => [(f.Frost!.Value.Alpha, new Vec(f.Frost.Value.Front, 270), f.Frost.Value.Trace), (f.Meteor?.Alpha ?? 0, f.Meteor?.Head ?? default, f.Meteor?.Trace ?? 0)],
        };
    }

    /// <summary>One thing at a time: never two to look at.</summary>
    [Theory]
    [MemberData(nameof(Forms))]
    public void OneThingAtATime(VisionForm form)
    {
        for (double u = 0; u < FormVision.SecondsOf(form); u += Frame)
            Assert.True(Things(FormVision.At(form, u)).Count(q => q.Alpha > 0.02) <= 1, $"two at once at {u:0.00} s");
    }

    /// <summary>The page's holds: a thing arrived read 1.2 s, a result 1 s, a shape whole 0.35 s, 0.2 s of stillness before a thing appears.</summary>
    public static IEnumerable<object[]> Holds =>
    [
        [VisionForm.Scan, S.ARead, 1.2], [VisionForm.Scan, S.FormHold, 0.35], [VisionForm.Scan, S.AResult, 1.0], [VisionForm.Scan, S.AGap, 0.2],
        [VisionForm.Scan, S.BRead, 1.2], [VisionForm.Scan, S.BResult, 1.0],
        [VisionForm.Lamp, L.LRead, 1.2], [VisionForm.Lamp, L.LResult, 1.0], [VisionForm.Lamp, L.LGap, 0.2], [VisionForm.Lamp, L.CResult, 1.0],
        [VisionForm.Alarms, A.CRead, 1.2], [VisionForm.Alarms, A.CHold, 0.35], [VisionForm.Alarms, A.CResult, 1.0], [VisionForm.Alarms, A.MGap, 0.2],
        [VisionForm.Alarms, A.MRead, 1.2], [VisionForm.Alarms, A.MHold, 0.35], [VisionForm.Alarms, A.MResult, 1.0],
    ];

    /// <summary>Through each hold nothing moves but the things' slow float: no presence, place, taking, shape, drone, aim, light or frame.</summary>
    [Theory]
    [MemberData(nameof(Holds))]
    public void NothingMovesThroughAHold(VisionForm form, Beat hold, double atLeast)
    {
        Assert.True(hold.To - hold.From >= atLeast - 1e-9, $"held {hold.To - hold.From:0.00} s, under {atLeast}");
        var was = FormVision.At(form, hold.From + 1e-4);
        for (double u = hold.From + 1e-4 + Frame; u < hold.To; u += Frame)
        {
            var now = FormVision.At(form, u);
            var (before, after) = (Things(was), Things(now));
            for (int k = 0; k < after.Length; k++)
            {
                Assert.True(Math.Abs(after[k].Alpha - before[k].Alpha) / Frame <= 0.08, $"thing {k}'s presence at {u:0.00} s");
                Assert.Equal(before[k].Taken, after[k].Taken, 5);
                if (after[k].Alpha > 0.02) Assert.True(Apart(after[k].At, before[k].At) / Frame <= 12, $"thing {k}'s place at {u:0.00} s");
            }
            Assert.Equal((was.Drone.Shape, was.Drone.Thought), (now.Drone.Shape, now.Drone.Thought));
            Assert.True(Apart(now.Drone.At, was.Drone.At) / Frame <= 12, $"the drone at {u:0.00} s");
            Assert.True(FormVision.AngDiff(now.Drone.Look, was.Drone.Look) / Frame <= 0.15, $"the aim at {u:0.00} s");
            Assert.Equal(was.Lamp?.Light ?? 0, now.Lamp?.Light ?? 0, 5);
            Assert.True(Apart(now.Cam.Middle, was.Cam.Middle) / Frame <= 12, $"the frame at {u:0.00} s");
            Assert.True(Math.Abs(now.Cam.Zoom - was.Cam.Zoom) / Frame <= 0.05, $"the frame's zoom at {u:0.00} s");
            was = now;
        }
    }

    /// <summary>The scanner: the whole piece, then the cracked one, each taken by the reticle, opened meanwhile.</summary>
    [Fact]
    public void TheScannerTakesTheWholeThenTheCracked()
    {
        var read = FormVision.At(VisionForm.Scan, Mid(S.ARead));
        Assert.Equal((1.0, 0.0, SwarmShape.Idle), (read.First!.Value.Alpha, read.First.Value.Trace, read.Drone.Shape));
        var formed = FormVision.At(VisionForm.Scan, Mid(S.Forms));
        Assert.Equal((SwarmShape.Scan, false), (formed.Drone.Shape, formed.Drone.Reads));
        Assert.Equal(0.5, formed.Drone.ShapeOver, 9);
        var whole = FormVision.At(VisionForm.Scan, Mid(S.AResult));
        Assert.Equal((1.0, false, true), (whole.First!.Value.Trace, whole.First.Value.Cracked, whole.Drone.Reads));
        var cracked = FormVision.At(VisionForm.Scan, Mid(S.BResult));
        Assert.Equal((0.0, 1.0, true), (cracked.First!.Value.Alpha, cracked.Second!.Value.Trace, cracked.Second.Value.Cracked));
    }

    /// <summary>
    /// The gauge played at 60 frames a second: its dots leave their marks one by one in order, the whole piece's row holds all 26 lit, they come
    /// home, the cracked piece's row is a whole 26 again, 13 lit and the rest dim, and they are home as the vision ends.
    /// </summary>
    [Fact]
    public void TheGaugeFliesOutDotByDot()
    {
        int Out(double u) => Enumerable.Range(0, DroneInk.Dots).Count(j => FormVision.DotAt(j, u) != null);
        Assert.Equal(0, Out(S.ADots.From - Frame));
        foreach (var beat in new[] { S.ADots, S.BDots })
        {
            int was = 0;
            for (double u = beat.From; u < beat.To; u += Frame)
            {
                int out_ = Out(u);
                Assert.InRange(out_, was, was + 1);
                for (int j = 0; j < out_; j++) Assert.NotNull(FormVision.DotAt(j, u));
                was = out_;
            }
            Assert.Equal(DroneInk.Dots, was);
        }
        foreach (var (beat, piece, lit) in new[] { (S.AResult, 0, 26), (S.BResult, 1, 13) })
            for (double u = beat.From + 1e-4; u < beat.To; u += Frame)
                for (int j = 0; j < DroneInk.Dots; j++)
                {
                    var dot = FormVision.DotAt(j, u)!.Value;
                    Assert.Equal((piece, 1.0, j < lit ? 1.0 : 0.0), (dot.To, dot.Away, dot.Lit));
                }
        Assert.Equal(0, Out(Mid(S.AGap)));
        Assert.Equal(0, Out(S.End.To));
    }

    /// <summary>The crate is unseen till the beam is on and finds it, and its diagram comes only then; the light only with the dish formed.</summary>
    [Fact]
    public void TheLampsBeamFindsTheCrate()
    {
        for (double u = 0; u < L.End.To; u += Frame)
        {
            var f = FormVision.At(VisionForm.Lamp, u);
            if (u < L.LightOn.From) Assert.Equal((0.0, 0.0), (f.Lamp!.Value.Light, f.Second!.Value.Trace));
            if (f.Second!.Value.Trace > 0 && u < L.End.From) Assert.Equal((1.0, true), (f.Lamp!.Value.Light, f.Lamp.Value.InCone > 0.99));
            if (f.Lamp!.Value.Light > 0) Assert.Equal(SwarmShape.Lamp, f.Drone.Shape);
        }
        var found = FormVision.At(VisionForm.Lamp, Mid(L.CResult));
        Assert.Equal((1.0, 1.0), (found.Lamp!.Value.Light, found.Second!.Value.Trace));
        Assert.Equal(0, FormVision.At(VisionForm.Lamp, L.End.To).Lamp!.Value.Light, 6);
    }

    /// <summary>The alarms: the cold's glyph, then the slow far meteor's with the drone darting red at it; no red or dart but with its glyph.</summary>
    [Fact]
    public void TheAlarmsShowTheColdThenTheMeteor()
    {
        var seen = new List<DroneThought>();
        double reddest = 0;
        for (double u = 0; u < A.End.To; u += Frame)
        {
            var f = FormVision.At(VisionForm.Alarms, u);
            if (f.Drone.Thought is { } glyph && (seen.Count == 0 || seen[^1] != glyph)) seen.Add(glyph);
            if (u < A.CGlyph.From || u >= A.COut.From + 0.6 && u < A.Dart.From) Assert.Equal(0, f.Drone.Red);
            if (u < A.Dart.From) Assert.Equal(0, Apart(f.Drone.At, FormVision.Void), 6);
            else if (u < A.Pass.From) reddest = Math.Max(reddest, f.Drone.Red);
        }
        Assert.Equal([DroneThought.Cold, DroneThought.Meteor], seen);
        Assert.True(reddest > 0.9);
        var held = FormVision.At(VisionForm.Alarms, Mid(A.MResult));
        Assert.Equal(40, Apart(held.Drone.At, FormVision.Void), 6);
        var (far, near) = (FormVision.At(VisionForm.Alarms, A.MIn.To).Meteor!.Value, FormVision.At(VisionForm.Alarms, A.MResult.To - Frame).Meteor!.Value);
        Assert.True(Apart(far.Head, near.Head) / (A.MResult.To - A.MIn.To) < 5, "the meteor far off creeps");
        Assert.True(far.Scale < near.Scale && near.Scale <= 0.41);
    }

    /// <summary>The frame: at rest whole, 1.3 in on a thing as it is read, kept in the pick's frame, back at rest between the things and at the end.</summary>
    [Fact]
    public void TheFrameFollowsAndComesBack()
    {
        foreach (var (form, beat) in new[] { (VisionForm.Scan, S.ARead), (VisionForm.Scan, S.BRead), (VisionForm.Lamp, L.LRead), (VisionForm.Alarms, A.CRead), (VisionForm.Alarms, A.MRead) })
            Assert.Equal(1.3, FormVision.At(form, Mid(beat)).Cam.Zoom, 9);
        var piece = FormVision.At(VisionForm.Scan, Mid(S.ARead)).Cam;
        Assert.Equal(500 / 1.3, piece.Middle.X, 6);
        Assert.Equal(236, piece.Middle.Y, 6);
        foreach (var (form, u) in new[]
        {
            (VisionForm.Scan, Mid(S.AGap)), (VisionForm.Scan, S.End.To), (VisionForm.Lamp, L.End.To), (VisionForm.Alarms, Mid(A.MGap)), (VisionForm.Alarms, A.End.To),
        })
        {
            var cam = FormVision.At(form, u).Cam;
            Assert.Equal(1, cam.Zoom, 9);
            Assert.Equal(0, Apart(cam.Middle, FormVision.Middle), 6);
        }
    }

    /// <summary>The frame takes in the drone and what it acts on: both are in view as each result holds.</summary>
    [Fact]
    public void TheFrameTakesInTheDroneAsItActs()
    {
        foreach (var (form, beat, thing) in new[]
        {
            (VisionForm.Scan, S.AResult, 0), (VisionForm.Scan, S.BResult, 1), (VisionForm.Lamp, L.LResult, 0),
            (VisionForm.Lamp, L.CResult, 1), (VisionForm.Alarms, A.MResult, 1),
        })
        {
            var f = FormVision.At(form, Mid(beat));
            double halfW = 500 / f.Cam.Zoom, halfH = 270 / f.Cam.Zoom;
            foreach (var at in new[] { f.Drone.At, Things(f)[thing].At })
            {
                Assert.InRange(at.X, f.Cam.Middle.X - halfW, f.Cam.Middle.X + halfW);
                Assert.InRange(at.Y, f.Cam.Middle.Y - halfH, f.Cam.Middle.Y + halfH);
            }
        }
    }

    /// <summary>It only glides: never a jump, never out of the pick's frame, so no shake.</summary>
    [Theory]
    [MemberData(nameof(Forms))]
    public void TheFrameGlidesWithNoJump(VisionForm form)
    {
        var was = FormVision.At(form, 0).Cam;
        for (double u = 0; u < FormVision.SecondsOf(form); u += Frame)
        {
            var cam = FormVision.At(form, u).Cam;
            Assert.True(Apart(cam.Middle, was.Middle) < 14 && Math.Abs(cam.Zoom - was.Zoom) < 0.02, $"a jump at {u:0.00} s");
            Assert.InRange(cam.Zoom, 1 - 1e-9, 1.3 + 1e-9);
            Assert.InRange(cam.Middle.X, 500 / cam.Zoom - 1e-6, 1000 - 500 / cam.Zoom + 1e-6);
            Assert.InRange(cam.Middle.Y, 270 / cam.Zoom - 1e-6, 540 - 270 / cam.Zoom + 1e-6);
            was = cam;
        }
    }

    /// <summary>The vision's zoom at rest: whole picture px on the screen, the hero about a quarter of its height.</summary>
    [Theory]
    [InlineData(720, 4)]
    [InlineData(1080, 8)]
    [InlineData(1440, 8)]
    [InlineData(2160, 12)]
    public void TheVisionFillsTheScreen(double height, float zoom)
    {
        Assert.Equal(zoom, FormVision.Zoom(height));
        Assert.InRange(FormVision.HeroTall * zoom / height, 0.15, 0.32);
    }

    /// <summary>A pod arriving or a hero waking mid-vision is held where the sim put it, not at the old place on another surface.</summary>
    [Fact]
    public void AVisionHoldsTheHeroWhereTheSimPutItMeanwhile()
    {
        var began = new Hero(10_000, 20_000, 0, 0, false, -1);
        var drifted = began with { Y = 20_400, Vy = 90 };
        var arrived = new Hero(900_000, 300_000, 40, 90, false, 1);
        Assert.Equal(began, FormVision.HeldAt(began, drifted, put: false));
        Assert.Equal(arrived with { Vx = 0, Vy = 0 }, FormVision.HeldAt(began, arrived, put: true));
    }

    private static double Mid(Beat beat) => (beat.From + beat.To) / 2;

    private static double Apart(Vec a, Vec b) => Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));
}
