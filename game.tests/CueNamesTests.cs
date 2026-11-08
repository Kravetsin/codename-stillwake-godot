using System.Linq;
using Stillwake.Game.Features.Dev;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>The dev panel's sound board names each cue by its first file.</summary>
public class CueNamesTests
{
    [Fact]
    public void NamesACueByItsFirstFileWithoutTheFolderNumberOrExtension()
    {
        Assert.Equal("crew/step", CueNames.Of(Sound.Step));
        Assert.Equal("atmosphere/vent-hum", CueNames.Of(Sound.VentHum));
        Assert.Equal("grid/door-open", CueNames.Of(Sound.DoorOpen));
        Assert.Equal("crew/step ×0.85", CueNames.Of(Sound.Land));
        Assert.Equal("crew/thrust", CueNames.Of(Sound.Thrust));
        Assert.Equal("crew/thrust -14 dB", CueNames.Of(Sound.SalvageThrust));
    }

    [Fact]
    public void GivesEveryCueANameOfItsOwn()
    {
        var names = Sound.All.Select(CueNames.Of).ToList();
        Assert.Equal(names.Count, names.Distinct().Count());
    }
}
