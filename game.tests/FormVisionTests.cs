using System.Linq;
using Stillwake.Game.Features.Crew;
using Stillwake.Game.Features.Research;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>A form's vision (form-vision, the pick «Проступает схема»): every form a lesson teaches has one, and each shows what its pick shows when it does.</summary>
public class FormVisionTests
{
    [Fact]
    public void EveryFormALessonTeachesHasAVision()
    {
        foreach (var lesson in Content.Lessons.All) Assert.NotNull(FormVision.Of(lesson.Form));
        foreach (var form in Content.Lessons.Forms) Assert.NotNull(FormVision.Of(form));
    }

    /// <summary>The lesson's vision stage is the vision whole: the hero is let go as it ends, not before or after.</summary>
    [Fact]
    public void TheLessonsVisionStageIsTheVisionsLength() => Assert.Equal(FormVision.Length, FindScan.Vision);

    /// <summary>The void comes in and the HUD goes, both gone while it shows, and both back as it ends; the hero floats only in the void.</summary>
    [Fact]
    public void TheWorldGoesAndComesBack()
    {
        Assert.Equal((0.0, 1.0), (FormVision.Veil(0), FormVision.Hud(0)));
        Assert.Equal((1.0, 0.0), (FormVision.Veil(FormVision.Length / 2), FormVision.Hud(FormVision.Length / 2)));
        Assert.Equal(0, FormVision.Veil(FormVision.Length), 6);
        Assert.Equal(1, FormVision.Hud(FormVision.Length), 6);
        Assert.Equal(0, FormVision.Float(0).Lift, 6);
        Assert.True(FormVision.Float(2).Lift > 0);
        Assert.False(FormVision.Playing(FormVision.Length));
    }

    /// <summary>The scanner reads the whole piece, a full row, then hops to the cracked one, half a row, then lets go.</summary>
    [Fact]
    public void TheScannerReadsTheWholeThenTheCracked()
    {
        Assert.Null(FormVision.At(VisionForm.Scan, FormVision.ScanRead - 0.1).Read);
        var whole = FormVision.At(VisionForm.Scan, (FormVision.ScanRead + FormVision.ScanHop) / 2).Read!.Value;
        Assert.Equal((0, whole.Max), (whole.Piece, whole.Left));
        var cracked = FormVision.At(VisionForm.Scan, (FormVision.ScanHop + FormVision.ScanHome) / 2);
        Assert.Equal(1, cracked.Read!.Value.Piece);
        Assert.Equal(cracked.Read.Value.Max, 2 * cracked.Read.Value.Left);
        Assert.True(cracked.Second!.Value.Cracked);
        Assert.Null(FormVision.At(VisionForm.Scan, FormVision.ScanHome + 0.1).Read);
    }

    /// <summary>
    /// The scanner's gauge played through a scanner's vision at 60 frames a second, as Main feeds it: its whole row out over the whole piece 0.8 s
    /// into its read, all of it out over the cracked one 0.8 s after the hop, half lit, and gone 0.6 s after the dots go home (u 3.6): from 4.2 s in,
    /// 7.2 s into <c>--form-vision</c>, none shows.
    /// </summary>
    [Fact]
    public void TheGaugeRowsShowWhileTheScannerReads()
    {
        var dots = new Features.Scanner.ScanDots();
        var seats = new (float X, float Y)[Features.Scanner.ScanDots.Count];
        (bool Visible, int Out, int? Id) At(double until, ref double u)
        {
            for (; u < until; u += 1 / 60.0)
            {
                var read = FormVision.At(VisionForm.Scan, u).Read;
                dots.Step(read is { } r ? new Features.Scanner.Readout(r.Piece, r.Top.X / 32, r.Top.Y / 32, r.Left, r.Max) : null, seats, 1 / 60.0);
            }
            int out_ = 0;
            for (int j = 0; j < Features.Scanner.ScanDots.Count; j++) out_ += dots.Dot(j).Away >= 1 ? 1 : 0;
            return (dots.Visible, out_, dots.Shown?.Id);
        }
        double u = 0;
        Assert.False(At(FormVision.ScanRead - 0.05, ref u).Visible);
        Assert.Equal((true, Features.Scanner.ScanDots.Count, (int?)0), At(FormVision.ScanRead + 0.8, ref u));
        Assert.Equal((true, Features.Scanner.ScanDots.Count, (int?)1), At(FormVision.ScanHop + 0.8, ref u));
        Assert.Equal(Features.Scanner.ScanDots.Count / 2, Enumerable.Range(0, Features.Scanner.ScanDots.Count).Count(j => Features.Scanner.ScanDots.Lit(150, 300, j) >= 1));
        Assert.False(At(FormVision.ScanHome + 0.6, ref u).Visible);
    }

    /// <summary>The vision's zoom: whole picture px on the screen, the hero about a quarter of its height.</summary>
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

    /// <summary>The crate is unseen till the beam is on and finds it, and its diagram stays once found; the beam is on only in its window.</summary>
    [Fact]
    public void TheLampsBeamFindsTheCrate()
    {
        var dark = FormVision.At(VisionForm.Lamp, FormVision.LampAt);
        Assert.Equal((0.0, 0.0), (dark.Lamp!.Value.Light, dark.Lamp.Value.Seen));
        var found = FormVision.At(VisionForm.Lamp, FormVision.SweepTo + 0.2);
        Assert.Equal((1.0, 1.0), (found.Lamp!.Value.Light, found.Lamp.Value.Seen));
        Assert.True(found.Lamp.Value.InCone > 0.99);
        var after = FormVision.At(VisionForm.Lamp, FormVision.LightOff + 0.3);
        Assert.Equal((0.0, 1.0), (after.Lamp!.Value.Light, after.Second!.Value.Trace));
    }

    /// <summary>The alarms: the cold's glyph, then the meteor's with the drone darting red at it; nothing more.</summary>
    [Fact]
    public void TheAlarmsShowTheColdThenTheMeteor()
    {
        Assert.Equal(DroneThought.Cold, FormVision.At(VisionForm.Alarms, 1.8).Drone.Thought);
        var meteor = FormVision.At(VisionForm.Alarms, 2.68);
        Assert.Equal(DroneThought.Meteor, meteor.Drone.Thought);
        Assert.True(meteor.Drone.Dart.X > 8);
        Assert.NotNull(meteor.Meteor);
        Assert.True(FormVision.At(VisionForm.Alarms, 3).Drone.Red > 0.5);
        Assert.Equal(0, FormVision.At(VisionForm.Alarms, 1.68).Drone.Dart.X, 6);
        Assert.Null(FormVision.At(VisionForm.Alarms, 4.5).Drone.Thought);
    }
}
