using Stillwake.Game.Features.Research;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>Blueprints just learned: apps/game/src/features/research/news.test.ts.</summary>
public class NewsTests
{
    [Fact]
    public void AreWhatIsKnownNowAndWasNotAtTheLastRead()
    {
        Assert.Equal(["door", "wallIron"], News.LearnedSince(["crate", "furnace"], ["crate", "door", "furnace", "wallIron"]));
        Assert.Empty(News.LearnedSince(["crate"], ["crate"]));
    }

    [Fact]
    public void AreNothingAtTheFirstReadNorAfterALoadThatKnowsLess()
    {
        Assert.Empty(News.LearnedSince(null, ["crate", "furnace"]));
        Assert.Empty(News.LearnedSince(["crate", "door"], ["crate", "furnace"]));
    }
}
