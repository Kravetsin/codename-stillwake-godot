using Stillwake.Sim.Crew;
using Xunit;
using Xunit.Abstractions;
using DebrisSpec = Stillwake.Content.Debris;

namespace Stillwake.Sim.Tests;

/// <summary>
/// How full the far field looks (0052), measured in the sim: a hero hovering, and one flying straight at the
/// jetpack's speed, the far pieces counted in the default zoom's view (40×22 cells) and the farthest's
/// (120×68) round where its body is. The numbers are printed; the asserts hold the targets loosely.
/// </summary>
public class DebrisDensityTests(ITestOutputHelper output)
{
    /// <summary>The half-sizes of the default zoom's view and the farthest's, in cells.</summary>
    private static readonly (double X, double Y) Near = (20, 11), Wide = (60, 34);
    /// <summary>The jetpack's top speed, in cells a tick: 15 cells a second.</summary>
    private const double Jetpack = 0.75;
    /// <summary>Flying slowly, looking about: 3 cells a second.</summary>
    private const double Slowly = 0.15;

    private sealed record Seen(double NearAvg, int NearMin, double WideAvg, int WideMin, double FirstNear, double FirstWide, double Between, double LongestEmpty, int Peak, int NearMax);

    /// <summary>
    /// Runs <paramref name="ticks"/> ticks with peer 0's body at <paramref name="body"/>(tick), reported to the sim
    /// once a second as its client does, its suit kept up with canisters. Averages and minimums count from
    /// <paramref name="settle"/> ticks on; the first arrivals, in seconds, from the start.
    /// </summary>
    private static Seen Measure(Layout map, Func<int, (double X, double Y)> body, int ticks, int settle)
    {
        var w = new Scenario(map).GiveItems(0, ("canister", 60));
        long near = 0, wide = 0;
        int nearMin = int.MaxValue, wideMin = int.MaxValue, firstNear = -1, firstWide = -1, arrivals = 0, empty = 0, longest = 0, peak = 0, nearMax = 0;
        var seen = new HashSet<int>();
        for (int t = 0; t < ticks; t++)
        {
            var (x, y) = body(t);
            if (t % Content.Crew.ReportEvery == 0) w.Do(new Enter((int)y * map.Width + (int)x));
            if (t % 400 == 399) w.Do(new Refill());
            w.Run();
            int inNear = 0, inWide = 0, far = 0;
            foreach (var p in w.M.Debris.Pieces)
            {
                if (p.Zone != DebrisSpec.Far.Id) continue;
                far++;
                double dx = Math.Abs(p.X / 1000.0 - x), dy = Math.Abs(p.Y / 1000.0 - y);
                if (dx <= Wide.X && dy <= Wide.Y) inWide++;
                if (dx > Near.X || dy > Near.Y) continue;
                inNear++;
                if (seen.Add(p.Id) && t >= settle) arrivals++;
            }
            peak = Math.Max(peak, far);
            if (inNear > 0 && firstNear < 0) firstNear = t;
            if (inWide > 0 && firstWide < 0) firstWide = t;
            if (t < settle) continue;
            near += inNear;
            wide += inWide;
            nearMin = Math.Min(nearMin, inNear);
            nearMax = Math.Max(nearMax, inNear);
            wideMin = Math.Min(wideMin, inWide);
            empty = inNear == 0 ? empty + 1 : 0;
            longest = Math.Max(longest, empty);
        }
        int counted = ticks - settle;
        double S(int t) => t / 20.0;
        return new(near / (double)counted, nearMin, wide / (double)counted, wideMin, S(firstNear), S(firstWide),
            arrivals == 0 ? double.PositiveInfinity : S(counted) / arrivals, S(longest), peak, nearMax);
    }

    private void Print(string what, Seen s) => output.WriteLine(
        $"{what}: default zoom avg {s.NearAvg:F1} min {s.NearMin} max {s.NearMax}, farthest avg {s.WideAvg:F1} min {s.WideMin}; " +
        $"first in default view {s.FirstNear:F0} s, farthest {s.FirstWide:F0} s; a new one in default view every {s.Between:F1} s; " +
        $"longest empty default view {s.LongestEmpty:F1} s; at most {s.Peak} far pieces out");

    /// <summary>2048×400 of space, a small hull in its bottom-left corner: its stream's box far from the heroes.</summary>
    private static readonly Layout Field = DebrisZonesTests.Open(2048, 400, 10, 390);

    [Fact]
    public void FillsTheViewOfAHeroHoveringStill()
    {
        // Fifteen minutes, the first two settling: pieces cross the view slowly, so a short count is luck.
        var s = Measure(Field, _ => (1000.5, 200.5), 18000, 2400);
        Print("hovering", s);
        Assert.InRange(s.NearAvg, 3.5, 7);
        Assert.InRange(s.WideAvg, 25, 45);
        Assert.True(s.Between <= 12, $"a new piece every {s.Between:F1} s");
    }

    [Theory]
    [InlineData(1, Jetpack)]
    [InlineData(-1, Jetpack)]
    [InlineData(1, Slowly)]
    public void NeverLeavesTheViewEmptyLongForAHeroFlying(int way, double speed)
    {
        // Straight across the field: two minutes at the jetpack's speed, the first ten seconds settling;
        // four slowly, the first minute settling.
        double from = way > 0 ? 100 : 1950;
        bool slow = speed < Jetpack;
        var s = Measure(Field, t => (from + way * speed * t, 200.5), slow ? 4800 : 2400, slow ? 1200 : 200);
        Print($"flying {(way > 0 ? "right" : "left")} at {speed * 20:F0} cells a second", s);
        Assert.True(s.LongestEmpty <= 5, $"the view empty for {s.LongestEmpty:F1} s");
        Assert.InRange(s.NearAvg, 2, 10);
    }

    [Fact]
    public void ShowsAHeroClimbingThroughWhatItsRingFilledWhileHoveringACrowdAtMost()
    {
        // A minute hovering, then a minute climbing slowly through the rows its ring filled meanwhile: the
        // ring's top band, a share's quarter in a few rows, is a crowd some five times the field's density.
        // Accepted (0052); this holds it from growing.
        var s = Measure(Field, t => (1000.5, t < 1200 ? 300.5 : 300.5 - (t - 1200) * Slowly), 2400, 1200);
        Print("hovering, then climbing at 3 cells a second", s);
        Assert.True(s.NearMax <= 25, $"{s.NearMax} pieces at once in the default view");
    }
}
