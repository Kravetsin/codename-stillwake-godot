using System.Collections.Generic;
using System.Linq;
using Stillwake.Game.Features.Crew;
using Stillwake.Game.Shared;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>The thread icons (the pick «Одна нить») in every state, on the wheels and the edge, and the wheels' coming and going.</summary>
public class ThreadIconTests
{
    public static readonly TheoryData<string> Icons = new(["lamp", "scan", "yes", "no", "wave", "air", "build", "research", "market", "journal"]);

    private static IconLines Of(string name) => name switch
    {
        "lamp" => DroneModes.IconOf(DroneMode.Lamp),
        "scan" => DroneModes.IconOf(DroneMode.Scan),
        "yes" => Gestures.IconOf(Gesture.Yes),
        "no" => Gestures.IconOf(Gesture.No),
        "wave" => Gestures.IconOf(Gesture.Wave),
        _ => ThreadIcon.Named(name),
    };

    private static EmberMarks Drawn(string name, IconLook look)
    {
        var marks = new EmberMarks();
        ThreadIcon.Draw(marks, Of(name), look, 50, 50, 9, 1);
        return marks;
    }

    /// <summary>The spark's three squares, where the first stands; none without one.</summary>
    private static (float X, float Y)? Spark(EmberMarks marks) =>
        marks.Squares.Count == 3 ? (marks.Squares[0].X, marks.Squares[0].Y) : marks.Squares.Count == 0 ? null : throw new Xunit.Sdk.XunitException("not one spark");

    [Theory]
    [MemberData(nameof(Icons))]
    public void EachIconIsOneUnbrokenThreadInItsBox(string name)
    {
        // In its box -1..1, the wave's motion lines a little past it as the pick drew them.
        var steps = Of(name).One.Steps;
        Assert.NotEmpty(steps);
        for (int i = 1; i < steps.Count; i++) Assert.Equal((steps[i - 1].Bx, steps[i - 1].By), (steps[i].Ax, steps[i].Ay));
        Assert.All(steps, s => Assert.InRange(System.Math.Max(System.Math.Abs(s.Ax), System.Math.Abs(s.Ay)), 0, 1.1f));
    }

    [Theory]
    [MemberData(nameof(Icons))]
    public void AtRestItSmouldersWithNoSpark(string name)
    {
        var marks = Drawn(name, new IconLook(0.34f, Time: 3));
        Assert.True(marks.Lit);
        Assert.Null(Spark(marks));
        Assert.Empty(marks.Halos);
    }

    [Theory]
    [MemberData(nameof(Icons))]
    public void UnderThePointerASparkRunsItThenRests(string name)
    {
        var first = Spark(Drawn(name, new IconLook(1.1f, Hover: 1, Time: 0.2f)));
        var later = Spark(Drawn(name, new IconLook(1.1f, Hover: 1, Time: 0.6f)));
        Assert.NotNull(first);
        Assert.NotEqual(first, later);
        Assert.Single(Drawn(name, new IconLook(1.1f, Hover: 1, Time: 0.6f)).Halos);
        // 0.9 s along, 0.3 s at rest.
        Assert.Null(Spark(Drawn(name, new IconLook(1.1f, Hover: 1, Time: 1.0f))));
        Assert.NotNull(Spark(Drawn(name, new IconLook(1.1f, Hover: 1, Time: 1.25f))));
    }

    [Theory]
    [MemberData(nameof(Icons))]
    public void ARunningSparkKindlesAtTheStartAndDiesOutAtTheEnd(string name)
    {
        float Brightest(IconLook look) => Drawn(name, look).Squares.Select(q => q.Alpha).DefaultIfEmpty(0).Max();
        // Under the pointer it runs the thread in 0.9 s: just begun, halfway, nearly done.
        float start = Brightest(new IconLook(1.1f, Hover: 1, Time: 0.01f));
        float middle = Brightest(new IconLook(1.1f, Hover: 1, Time: 0.45f));
        float end = Brightest(new IconLook(1.1f, Hover: 1, Time: 0.895f));
        Assert.True(start < middle * 0.25f, $"start {start} vs {middle}");
        Assert.True(end < middle * 0.25f, $"end {end} vs {middle}");
        // Walking while on, it wraps round without a jump in light.
        Assert.True(Brightest(new IconLook(0.85f, On: true, Time: 2.39f)) < Brightest(new IconLook(0.85f, On: true, Time: 1.2f)) * 0.25f);
    }

    [Theory]
    [MemberData(nameof(Icons))]
    public void OnASparkWalksItSlowlyAndHeldItStands(string name)
    {
        var on = Enumerable.Range(0, 6).Select(k => Spark(Drawn(name, new IconLook(0.85f, On: true, Time: 0.4f + k * 0.3f)))).ToList();
        Assert.All(on, s => Assert.NotNull(s));
        Assert.True(on.Distinct().Count() >= 5);
        var held = Enumerable.Range(0, 6).Select(k => Spark(Drawn(name, new IconLook(0.77f, Hover: 1, Held: true, Time: k * 0.4f)))).ToList();
        Assert.All(held, s => Assert.Equal(held[0], s));
        Assert.NotNull(held[0]);
    }

    [Theory]
    [MemberData(nameof(Icons))]
    public void WaitingItPulsesAndASparkWalksIt(string name)
    {
        float Glow(float t) => Drawn(name, new IconLook(0.85f, Waiting: true, Time: t)).Lines[0].Alpha;
        // 1.2 pulses a second: up at a quarter of one, down at three quarters.
        Assert.True(Glow(0.25f / 1.2f) > Glow(0.75f / 1.2f) * 1.3f);
        Assert.NotNull(Spark(Drawn(name, new IconLook(0.85f, Waiting: true, Time: 0.5f))));
    }

    [Theory]
    [MemberData(nameof(Icons))]
    public void NotKnownYetItIsABrokenDimThreadWithNoSpark(string name)
    {
        var whole = Drawn(name, new IconLook(1, Time: 0.3f));
        var shut = Drawn(name, new IconLook(1, Hover: 1, Shut: true, Time: 0.3f));
        Assert.Null(Spark(shut));
        // The glow and then dashes, each a short piece: far more lines than the whole thread has.
        Assert.True(shut.Lines.Count > 3 * whole.Lines.Count);
        Assert.All(shut.Lines.Skip(1), l => Assert.Equal(2, l.Count));
        Assert.True(shut.Lines.Max(l => l.Alpha) < whole.Lines.Max(l => l.Alpha));
    }

    [Theory]
    [MemberData(nameof(Icons))]
    public void PickedItFlaresAndItsSparkRunsBackAsItFades(string name)
    {
        var bright = Drawn(name, new IconLook(1, Flare: 1, Time: 0));
        var fading = Drawn(name, new IconLook(1, Flare: 0.3f, Time: 0));
        var still = Drawn(name, new IconLook(1, Time: 0));
        Assert.True(bright.Lines.Max(l => l.Alpha) >= still.Lines.Max(l => l.Alpha));
        Assert.True(bright.Lines.Max(l => l.Pale) > still.Lines.Max(l => l.Pale));
        Assert.NotEqual(Spark(bright), Spark(fading));
    }

    [Fact]
    public void ARimHasASparkOnlyUnderThePointer()
    {
        foreach (var rim in new[] { IconInk.Circle, IconInk.Square, IconInk.Cross })
        {
            var marks = new EmberMarks();
            ThreadIcon.Draw(marks, rim, new IconLook(0.5f, Rim: true, On: true, Time: 0.4f), 0, 0, 20, 1);
            Assert.True(marks.Lit);
            Assert.Empty(marks.Squares);
            marks.Clear();
            ThreadIcon.Draw(marks, rim, new IconLook(0.5f, Hover: 1, Rim: true, Time: 0.4f), 0, 0, 20, 1);
            Assert.Equal(3, marks.Squares.Count);
            Assert.Empty(marks.Halos);
        }
    }

    [Fact]
    public void EveryEdgeKeyHasItsIconAndNoOtherNameDoes()
    {
        Assert.All(EdgeRow.Keys, k => Assert.NotNull(ThreadIcon.Named(k)));
        Assert.Throws<System.ArgumentException>(() => ThreadIcon.Named("suit"));
    }

    [Fact]
    public void AWheelComesInATenthLetGoItsSideFlaresAsTheRestGoes()
    {
        var fade = new WheelFade();
        Assert.False(fade.Drawn);
        fade.Step(0.06, true, 1);
        Assert.InRange(fade.Shown, 0.4f, 0.6f);
        fade.Step(0.1, true, 1);
        Assert.Equal(1, fade.Shown);
        fade.Step(0, false, null);
        Assert.Equal(1, fade.Picked);
        fade.Step(ThreadIcon.WheelOut, false, null);
        Assert.Equal(0, fade.Shown);
        Assert.True(fade.Flare > 0);
        Assert.True(fade.Drawn);
        fade.Step(ThreadIcon.FlareS, false, null);
        Assert.False(fade.Drawn);
        // Let go in the middle, nothing flares.
        fade.Step(0.2, true, null);
        fade.Step(0.01, false, null);
        Assert.Null(fade.Picked);
        Assert.Equal(0, fade.Flare);
    }
}
