using FsCheck.Xunit;
using Stillwake.Game.Features.Scanner;
using Xunit;
using I = Stillwake.Game.Shared.DroneInk;

namespace Stillwake.Game.Tests;

/// <summary>The scanner's gauge (drone-threads, the pick «Нити», gauge dots): ember dots lit by the hit points left, flown from the spyglass.</summary>
public class ScanDotsTests
{
    private const double Frame = 1 / 60.0;
    private static readonly (float X, float Y) Mouth = (40, 70), Eye = (32, 72);
    private static readonly Readout Wreck = new(7, 10, 5, 340, 520), Other = new(9, 30, 6, 100, 200);

    private static void Run(ScanDots dots, Readout? now, double seconds)
    {
        for (double t = 0; t < seconds - 1e-9; t += Frame) dots.Step(now, Mouth, Eye, Frame);
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
    public void TheDotsFlyFromTheSpyglassIntoARowOverTheWreck()
    {
        var dots = new ScanDots();
        Assert.False(dots.Visible);
        dots.Step(Wreck, Mouth, Eye, 0);
        Assert.True(dots.Visible);
        Assert.All(All(dots), d => Assert.Equal(Mouth, (d.X, d.Y)));
        Run(dots, Wreck, 0.2);
        Assert.Contains(All(dots), d => d.Flying);
        Run(dots, Wreck, I.DotsIn);
        var row = All(dots);
        for (int j = 0; j < ScanDots.Count; j++)
        {
            Assert.False(row[j].Flying);
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
    public void GoneItFliesHomeToTheEyeFadingThenShowsNothing()
    {
        var dots = new ScanDots();
        Run(dots, Wreck, 1);
        Run(dots, null, I.DotsOut / 2);
        Assert.Contains(All(dots), d => d.Flying && d.Alpha < 1);
        Assert.All(All(dots).Where(d => d.Flying), d => Assert.True((Eye.X - d.FromX) * d.Dx + (Eye.Y - d.FromY) * d.Dy > 0));
        Run(dots, null, I.DotsOut / 2 + Frame);
        Assert.False(dots.Visible);
    }

    [Fact]
    public void AHitShuddersTheRowAndBurstsTheDotsItPutOut()
    {
        var dots = new ScanDots();
        Run(dots, Wreck, 1);
        var before = All(dots);
        var hit = Wreck with { Left = 230 };
        dots.Step(hit, Mouth, Eye, 0.05);
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
        dots.Step(Wreck, Mouth, Eye, 0);
        Run(dots, Wreck, 0.3);
        var home = Enumerable.Range(0, ScanDots.Count).Where(j => !dots.Dot(j).Flying).ToHashSet();
        Assert.NotEmpty(home);
        Assert.True(home.Count < ScanDots.Count);
        dots.Step(Wreck with { Left = 0 }, Mouth, Eye, Frame);
        for (int j = 0; j < 17; j++) Assert.Equal(home.Contains(j), dots.Ember(j) != null);
        Run(dots, Wreck with { Left = 0 }, 1);
        Assert.All(All(dots), d => Assert.Equal(I.DotDark, d.Heat, 4));
    }

    [Fact]
    public void ANewTargetDropsTheBurstsAndTheDotsFlyOverFromWhereTheyAre()
    {
        var dots = new ScanDots();
        Run(dots, Wreck, 1);
        dots.Step(Wreck with { Left = 100 }, Mouth, Eye, Frame);
        var was = All(dots);
        dots.Step(Other, Mouth, Eye, 0);
        Assert.All(Enumerable.Range(0, ScanDots.Count), j => Assert.Null(dots.Ember(j)));
        var now = All(dots);
        for (int j = 0; j < ScanDots.Count; j++) Assert.Equal((was[j].X, was[j].Y), (now[j].X, now[j].Y));
        Run(dots, Other, I.DotsIn + Frame);
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
            dots.Step(next, Mouth, Eye, 0);
            var now = All(dots);
            for (int j = 0; j < ScanDots.Count; j++)
            {
                Assert.Equal(was[j].X, now[j].X, 3);
                Assert.Equal(was[j].Y, now[j].Y, 3);
                Assert.Equal(was[j].Alpha, now[j].Alpha, 3);
            }
        }
    }
}
