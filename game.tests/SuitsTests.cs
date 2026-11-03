using Stillwake.Game.Features.Crew;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>features/crew/suits.test.ts, ported.</summary>
public class SuitsTests
{
    [Fact]
    public void HasOneColourForEachTheSimHandsOutAllDifferent()
    {
        Assert.Equal(Content.Crew.Colours, Suits.All.Length);
        Assert.Equal(Suits.All.Length, Suits.All.Distinct().Count());
    }
}
