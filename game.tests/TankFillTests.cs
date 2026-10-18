using Stillwake.Game.Shared;
using Xunit;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Tests;

/// <summary>A tank's sight glass: empty only at none, any oxygen at least the lowest step, full only when full.</summary>
public class TankFillTests
{
    private const int Full = Spec.Air.TankCapacity, Fifth = Full / 5;

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 1)]
    [InlineData(Fifth, 1)]
    [InlineData(Fifth + 1, 2)]
    [InlineData(2 * Fifth, 2)]
    [InlineData(Full / 2, 3)]
    [InlineData(4 * Fifth, 4)]
    [InlineData(4 * Fifth + 1, 5)]
    [InlineData(Full - 1, 5)]
    [InlineData(Full, 5)]
    public void TheGlassShowsTheOxygenByFifthsRoundedUp(int oxygen, int frame) => Assert.Equal(frame, TankFill.Frame(oxygen));
}
