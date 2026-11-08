using Stillwake.Game.Platform;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>How the window takes the screen, as the settings file keeps it.</summary>
public class ScreensTests
{
    [Theory]
    [InlineData(ScreenMode.Windowed)]
    [InlineData(ScreenMode.Fullscreen)]
    [InlineData(ScreenMode.Borderless)]
    public void EveryModeIsReadBackAsKept(ScreenMode mode) => Assert.Equal(mode, Screens.Named(Screens.NameOf(mode)));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("exclusive")]
    public void NothingKeptOrAWordUnknownIsAWindow(string? name) => Assert.Equal(ScreenMode.Windowed, Screens.Named(name));
}
