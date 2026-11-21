using FsCheck.Xunit;
using Stillwake.Game.Features.Scanner;
using Xunit;
using I = Stillwake.Game.Shared.DroneInk;

namespace Stillwake.Game.Tests;

/// <summary>The scanner's gauge (drone-threads, the pick «Нити», gauge dots): ember dots lit by the hit points left, each the spark of a mark on the drone's reticle (scan-form, «Прицел»).</summary>
public class ScanDotsTests
{
    private const double Frame = 1 / 60.0;
    private static readonly (float X, float Y)[] Seats = [.. Enumerable.Range(0, ScanDots.Count).Select(j => (40 + 4 * MathF.Cos(j * 0.24f), 70 + 4 * MathF.Sin(j * 0.24f)))];
    private static readonly Readout Wreck = new(7, 10, 5, 340, 520), Other = new(9, 30, 6, 100, 200);

    private static void Run(ScanDots dots, Readout? now, double seconds, (float X, float Y)[]? seats = null)
    {
        for (double t = 0; t < seconds - 1e-9; t += Frame) dots.Step(now, seats ?? Seats, Frame);
    }

    private static GaugeDot[] All(ScanDots dots) => Enumerable.Range(0, ScanDots.Count).Select(dots.Dot).ToArray();

    private static (float X, float Y) Home(Readout read, int j) =>
        ((float)(read.X * Camera.Cell) + (j - (ScanDots.Count - 1) / 2f) * I.DotStep * I.WorldPx, (float)(read.Y * Camera.Cell) - I.DotsAbove * I.WorldPx);

    /// <summary>Whatever is left of whatever maximum, the lit dots times a dot's share show exactly what is left.</summary>
    [Property(MaxTest = 500)]
    public void TheLitDotsShowExactlyWhatIsLeft(int left, ushort max)
    {
        int clamped = Math.Clamp(left, 0, (int)max);
        double shown = Enumerable.Range(0, ScanDots.Count).Sum(j => (double)ScanDots.Lit(left, max, j)) * max / ScanDots.Count;
        Assert.Equal(max == 0 ? 0 : clamped, shown, 3);
    }

    [Fact]
    public void ADotIsLitByItsShareTheLastPartlyNoneAtNoMaximum()
    {
        // 520 over 26 dots: 20 a dot; 340 lights 17, 230 eleven and a half.
        Assert.Equal(17, Enumerable.Range(0, 26).Count(j => ScanDots.Lit(340, 520, j) == 1));
        Assert.Equal(0.5f, ScanDots.Lit(230, 520, 11), 4);
        Assert.Equal(0, ScanDots.Lit(230, 520, 12));
        Assert.Equal(0, ScanDots.Lit(5, 0, 0));
        Assert.Equal(0, ScanDots.Lit(-3, 50, 0));
        Assert.Equal(1, ScanDots.Lit(80, 50, 25));
    }

    [Fact]
    public void EachDotLeavesItsMarkForARowOverTheWreckGrowingFromASpark()
    {
        var dots = new ScanDots();
        Assert.False(dots.Visible);
        dots.Step(Wreck, Seats, 0);
        Assert.True(dots.Visible);
        Assert.True(dots.Reads);
        // Not left yet: each on its own mark, the drone's spark, not the gauge's.
        for (int j = 0; j < ScanDots.Count; j++) Assert.Equal((Seats[j], 0f), ((dots.Dot(j).X, dots.Dot(j).Y), dots.Dot(j).Away));
        Run(dots, Wreck, 0.2);
        Assert.Contains(All(dots), d => d.Flying && d.Away is > 0 and < 1 && d.Side > I.Spark * I.Px && d.Side < I.Dot * I.WorldPx);
        Assert.All(All(dots).Where(d => d.Flying), d => Assert.Contains((d.FromX, d.FromY), Seats));
        Run(dots, Wreck, I.DotsIn);
        var row = All(dots);
        for (int j = 0; j < ScanDots.Count; j++)
        {
            Assert.False(row[j].Flying);
            Assert.Equal(1, row[j].Away);
            Assert.Equal(I.Dot * I.WorldPx, row[j].Side, 4);
            Assert.Equal(Home(Wreck, j).X, row[j].X, 3);
            Assert.Equal(Home(Wreck, j).Y, row[j].Y, 3);
        }
        // 5 world px over the point, 1.75 apart, centred on it.
        Assert.Equal(5, Wreck.Y * Camera.Cell - row[0].Y, 3);
        Assert.Equal(1.75f, row[1].X - row[0].X, 3);
        Assert.Equal(Wreck.X * Camera.Cell, (row[0].X + row[^1].X) / 2, 3);
        Assert.True(row[16].Heat > row[17].Heat, "a lit dot burns hotter than a dark one");
    }

    [Fact]
    public void GoneEachDotFliesHomeToItsOwnMarkWholeThenShowsNothing()
    {
        var dots = new ScanDots();
        Run(dots, Wreck, 1);
        // The drone moved meanwhile: they go where the marks are now.
        var moved = Seats.Select(s => (s.X + 6, s.Y - 3)).ToArray();
        Run(dots, null, I.DotsOut / 2, moved);
        Assert.False(dots.Reads);
        Assert.Contains(All(dots), d => d.Flying);
        Assert.All(All(dots), d => Assert.Equal(1, d.Alpha));
        for (int j = 0; j < ScanDots.Count; j++)
            if (dots.Dot(j) is { Flying: true } d)
                Assert.True((moved[j].Item1 - d.FromX) * d.Dx + (moved[j].Item2 - d.FromY) * d.Dy > 0, $"dot {j} not heading for its mark");
        Run(dots, null, I.DotsOut / 2 - Frame, moved);
        Assert.Contains(All(dots), d => d.Away == 0);
        for (int j = 0; j < ScanDots.Count; j++)
            if (dots.Dot(j).Away == 0)
            {
                Assert.Equal(moved[j].Item1, dots.Dot(j).X, 3);
                Assert.Equal(moved[j].Item2, dots.Dot(j).Y, 3);
            }
        Run(dots, null, 2 * Frame, moved);
        Assert.False(dots.Visible);
    }

    [Fact]
    public void WithNoDroneTheDotsComeAndGoInPlaceFading()
    {
        var dots = new ScanDots();
        dots.Step(Wreck, null, 0);
        Assert.All(Enumerable.Range(0, ScanDots.Count), j => Assert.Equal((Home(Wreck, j).X, Home(Wreck, j).Y, 1f), (dots.Dot(j).X, dots.Dot(j).Y, dots.Dot(j).Away)));
        for (double t = 0; t < 1; t += Frame) dots.Step(Wreck, null, Frame);
        for (double t = 0; t < I.DotsOut / 2; t += Frame) dots.Step(null, null, Frame);
        Assert.Contains(All(dots), d => d.Alpha is > 0 and < 1);
        Assert.All(All(dots), d => Assert.Equal(1, d.Away));
    }

    [Fact]
    public void AHitShuddersTheRowAndBurstsTheDotsItPutOut()
    {
        var dots = new ScanDots();
        Run(dots, Wreck, 1);
        var before = All(dots);
        var hit = Wreck with { Left = 230 };
        dots.Step(hit, Seats, 0.05);
        var after = All(dots);
        // Dots 11 (half) to 16 went out; their embers burst, none of the rest.
        var burst = Enumerable.Range(0, ScanDots.Count).Where(j => dots.Ember(j) != null).ToArray();
        Assert.Equal(Enumerable.Range(11, 6), burst);
        Assert.NotEqual(before[0].X, after[0].X);
        Assert.Equal(before[0].Y, after[0].Y);
        Assert.True(after[0].Heat > before[0].Heat, "the lit flare as it lands");
        Assert.All(burst, j => Assert.True(dots.Ember(j)!.Value.Heat > 0));
        Run(dots, hit, I.Hit);
        Assert.All(Enumerable.Range(0, ScanDots.Count), j => Assert.Null(dots.Ember(j)));
        Assert.Equal(Home(hit, 0).X, dots.Dot(0).X, 3);
    }

    [Fact]
    public void AHitMidFlightBurstsOnlyTheDotsHomeTheRestArriveDimmer()
    {
        var dots = new ScanDots();
        dots.Step(Wreck, Seats, 0);
        Run(dots, Wreck, 0.3);
        var home = Enumerable.Range(0, ScanDots.Count).Where(j => !dots.Dot(j).Flying).ToHashSet();
        Assert.NotEmpty(home);
        Assert.True(home.Count < ScanDots.Count);
        dots.Step(Wreck with { Left = 0 }, Seats, Frame);
        for (int j = 0; j < 17; j++) Assert.Equal(home.Contains(j), dots.Ember(j) != null);
        Run(dots, Wreck with { Left = 0 }, 1);
        Assert.All(All(dots), d => Assert.Equal(I.DotDark, d.Heat, 4));
    }

    [Fact]
    public void ANewTargetDropsTheBurstsAndTheDotsHopOverWithoutComingHome()
    {
        var dots = new ScanDots();
        Run(dots, Wreck, 1);
        dots.Step(Wreck with { Left = 100 }, Seats, Frame);
        var was = All(dots);
        dots.Step(Other, Seats, 0);
        Assert.All(Enumerable.Range(0, ScanDots.Count), j => Assert.Null(dots.Ember(j)));
        var now = All(dots);
        for (int j = 0; j < ScanDots.Count; j++) Assert.Equal((was[j].X, was[j].Y), (now[j].X, now[j].Y));
        for (double t = 0; t < I.DotsIn + Frame; t += Frame)
        {
            dots.Step(Other, Seats, Frame);
            Assert.All(All(dots), d => Assert.Equal(1, d.Away));
            Assert.All(All(dots), d => Assert.DoesNotContain((d.X, d.Y), Seats));
        }
        Assert.All(Enumerable.Range(0, ScanDots.Count), j => Assert.Equal(Home(Other, j).X, dots.Dot(j).X, 3));
    }

    [Theory]
    [InlineData(0.1)]
    [InlineData(0.2)]
    [InlineData(0.3)]
    public void LeavingAndComingBackMidFlightNoDotJumps(double gone)
    {
        foreach (var start in (bool[])[true, false])
        {
            var dots = new ScanDots();
            Run(dots, Wreck, start ? 1 : gone);
            if (start) Run(dots, null, gone);
            var was = All(dots);
            Readout? next = start ? Wreck : null;
            dots.Step(next, Seats, 0);
            var now = All(dots);
            for (int j = 0; j < ScanDots.Count; j++)
            {
                Assert.Equal(was[j].X, now[j].X, 3);
                Assert.Equal(was[j].Y, now[j].Y, 3);
                Assert.Equal(was[j].Alpha, now[j].Alpha, 3);
                Assert.Equal(was[j].Away, now[j].Away, 3);
            }
        }
    }
}
