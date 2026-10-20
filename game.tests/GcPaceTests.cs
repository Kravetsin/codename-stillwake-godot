using Stillwake.Game.Platform;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>When the game collects its youngest garbage itself: every <see cref="GcPace.Every"/> bytes, counted from any collection.</summary>
public class GcPaceTests
{
    [Fact]
    public void DueOnceEveryBytesAreAllocatedSinceTheLastCollection()
    {
        var pace = new GcPace();
        Assert.False(pace.Due(1000, 0));
        Assert.False(pace.Due(1000 + GcPace.Every - 1, 0));
        Assert.True(pace.Due(1000 + GcPace.Every, 0));
        // The collection it asked for ran: counted from there.
        Assert.False(pace.Due(1000 + GcPace.Every + 10, 1));
        Assert.True(pace.Due(1000 + 2 * GcPace.Every, 1));
    }

    [Fact]
    public void ACollectionTheRuntimeRanItselfStartsTheCountAgain()
    {
        var pace = new GcPace();
        pace.Due(0, 0);
        Assert.False(pace.Due(GcPace.Every - 100, 3));
        Assert.False(pace.Due(GcPace.Every + 100, 3));
        Assert.True(pace.Due(2 * GcPace.Every - 100, 3));
    }
}
