using Stillwake.Game.Platform;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>The smoothing kept in the settings file and named on the command line.</summary>
public class SmoothingsTests
{
    [Fact]
    public void EverySmoothingIsKeptAndReadBackAsItself()
    {
        foreach (var smoothing in Enum.GetValues<Smoothing>()) Assert.Equal(smoothing, Smoothings.Named(Smoothings.NameOf(smoothing)));
    }

    [Fact]
    public void NoneOrAnUnknownWordIsOff()
    {
        Assert.Equal(Smoothing.Off, Smoothings.Named(null));
        Assert.Equal(Smoothing.Off, Smoothings.Named("16"));
        Assert.Equal(Smoothing.Four, Smoothings.Named("4"));
    }
}
