using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Stillwake.Content;
using Xunit;

namespace Stillwake.Sim.Tests;

/// <summary>
/// The wave a burst spreads by (round-blasts) and the cells it comes to but cannot pay to enter (unified-damage): what a burst
/// cracks. Costs here in a straight step's units, as callers count them: a step costs <see cref="Damage.Step"/> and the cell's own.
/// </summary>
public class WaveTests
{
    /// <summary>
    /// Spreads over <paramref name="rows"/> (a digit a cell's cost to enter, <c>.</c> nothing, <c>#</c> a stop) from
    /// <paramref name="start"/>: the cells reached, and those unpaid with what was left for each, in the order heard.
    /// </summary>
    private static (List<int> Reached, List<(int Cell, int Left)> Unpaid) Spread(string[] rows, int start, int first, int charge, int most = 8)
    {
        int width = rows[0].Length;
        int? Enter(int cell) => rows[cell / width][cell % width] switch { '#' => null, '.' => 0, var c => c - '0' };
        var (reached, unpaid) = (new List<int>(), new List<(int, int)>());
        new Wave().Spread(width, rows.Length, start, first, charge, most, Enter, (cell, _) => reached.Add(cell), (cell, left) => unpaid.Add((cell, left)));
        return (reached, unpaid);
    }

    [Fact]
    public void AnUnpaidCellIsHeardOnceWithTheMostLeftOnItsCheapestWayThere()
    {
        // The 9 is come to straight from the cell beside the start, aslant from the one under that, and straight from below:
        // straight from beside, 10 fifths spent, is the cheapest, 50 − 10 − 10 = 30 fifths, 6 left; the others 3 each.
        var (reached, unpaid) = Spread(["..9", "..."], start: 0, first: 0, charge: 10);
        Assert.Equal([0, 1, 3, 4, 5], reached.Order());
        Assert.Equal([(2, 6)], unpaid);
        Assert.True(6 < 9);
    }

    [Fact]
    public void ACellEnteredLaterIsNeverHeardUnpaid()
    {
        // Aslant from the start the 6 costs 7 × 8 = 56 fifths, past the 50; straight from either side 10 + 5 × 8 = 50, paid.
        var (reached, unpaid) = Spread(["..", ".6"], start: 0, first: 0, charge: 10);
        Assert.Contains(3, reached);
        Assert.Empty(unpaid);
    }

    [Fact]
    public void NothingIsHeardWhenTheStepAteItAll()
    {
        // 10 fifths to spend: the step to the 9 takes them all; with 5, more than all.
        Assert.Empty(Spread([".9"], start: 0, first: 0, charge: 2).Unpaid);
        Assert.Empty(Spread([".9"], start: 0, first: 0, charge: 1).Unpaid);
        // A stop is no cell to crack.
        Assert.Empty(Spread([".#"], start: 0, first: 0, charge: 40).Unpaid);
    }

    [Fact]
    public void AStartTooDearGetsTheWholeCharge()
    {
        var (reached, unpaid) = Spread(["9."], start: 0, first: 9, charge: 5);
        Assert.Empty(reached);
        Assert.Equal([(0, 5)], unpaid);
        Assert.Empty(Spread(["9."], start: 0, first: 9, charge: 0).Unpaid);
    }

    [Fact]
    public void UnpaidCellsAreHeardAscending()
    {
        // 15 fifths: a straight step leaves 5, 1 for each side's 9; an aslant one 1 fifth, nothing.
        var (reached, unpaid) = Spread(["999", "9.9", "999"], start: 4, first: 0, charge: 3);
        Assert.Equal([4], reached);
        Assert.Equal([(1, 1), (3, 1), (5, 1), (7, 1)], unpaid);
    }

    [Property(MaxTest = 300)]
    public Property EveryUnpaidCellIsUnenteredBesideAnEnteredOneAndLeftLessThanItsCost()
    {
        const int Width = 6, Height = 5;
        var gen =
            from costs in Gen.Choose(-1, 12).ArrayOf(Width * Height)
            from start in Gen.Choose(0, Width * Height - 1)
            from first in Gen.Choose(0, 8)
            from charge in Gen.Choose(0, 40)
            from most in Gen.Choose(1, 6)
            select (costs, start, first, charge, most);
        return Prop.ForAll(gen.ToArbitrary(), t =>
        {
            var rows = Enumerable.Range(0, Height)
                .Select(y => new string([.. Enumerable.Range(0, Width).Select(x => t.costs[y * Width + x] switch { < 0 => '#', 0 => '.', var c => (char)('0' + Math.Min(c, 9)) })]))
                .ToArray();
            var (reached, unpaid) = Spread(rows, t.start, t.first, t.charge, t.most);
            if (reached.Count == 0)
                return t.first > t.charge && (t.charge >= 1 ? unpaid.SequenceEqual([(t.start, t.charge)]) : unpaid.Count == 0);
            bool Beside(int a, int b) => a != b && Math.Abs(a % Width - b % Width) <= 1 && Math.Abs(a / Width - b / Width) <= 1;
            int CostOf(int cell) => rows[cell / Width][cell % Width] - '0';
            for (int i = 0; i < unpaid.Count; i++)
            {
                var (cell, left) = unpaid[i];
                if (i > 0 && unpaid[i - 1].Cell >= cell) return false;
                if (reached.Contains(cell) || !reached.Any(r => Beside(r, cell))) return false;
                if (left < 1 || left >= CostOf(cell)) return false;
            }
            return true;
        });
    }
}
