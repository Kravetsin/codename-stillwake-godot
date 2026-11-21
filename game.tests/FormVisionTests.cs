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
