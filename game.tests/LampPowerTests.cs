using Stillwake.Game.Features.Light;
using Xunit;

namespace Stillwake.Game.Tests;

public class LampPowerTests
{
    [Fact]
    public void ALampDrawingNothingIsFullyLitOnNoNetwork() => Assert.Equal(1f, LampPower.ShareOf(null, 0));

    [Theory]
    [InlineData(0, 0f)]
    [InlineData(500, 0.5f)]
    [InlineData(1000, 1f)]
    public void ALampDrawingPowerShinesWithItsSatisfaction(int satisfaction, float share) => Assert.Equal(share, LampPower.ShareOf(1, satisfaction));
}
