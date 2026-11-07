using System.Linq;
using Stillwake.Game.Features.Flora;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>Which vein faces glint (2026-11-07-ore-pockets): one cell in <see cref="CaveLooks.VeinGlintEvery"/>, stable by seed and cell.</summary>
public class VeinGlintsTests
{
    [Fact]
    public void AboutOneVeinCellInEveryGlints()
    {
        const int Cells = 30000;
        int picked = Enumerable.Range(0, Cells).Count(cell => VeinGlints.Picks(7, cell));
        int expected = Cells / CaveLooks.VeinGlintEvery;
        Assert.InRange(picked, expected * 9 / 10, expected * 11 / 10);
        Assert.Equal(Cells, Enumerable.Range(0, Cells).Count(cell => VeinGlints.Picks(7, cell, every: 1)));
    }

    [Fact]
    public void TheSameSeedAndCellAlwaysPickTheSameAndAnotherSeedOthers()
    {
        var first = Enumerable.Range(0, 3000).Where(cell => VeinGlints.Picks(7, cell)).ToArray();
        Assert.Equal(first, Enumerable.Range(0, 3000).Where(cell => VeinGlints.Picks(7, cell)).ToArray());
        var other = Enumerable.Range(0, 3000).Where(cell => VeinGlints.Picks(8, cell)).ToArray();
        Assert.NotEqual(first, other);
        // Not a stripe: neighbouring cells picked independently, so a run of veins glints here and there.
        Assert.Contains(first, cell => first.Contains(cell + 1));
        Assert.Contains(Enumerable.Range(0, 3000), cell => !first.Contains(cell) && !first.Contains(cell + 1) && !first.Contains(cell + 2));
    }
}
