using Xunit;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Tests;

/// <summary>Every species is seen and heard struck by what it is (0093, 0094, 0102): a new one fails here until it has both.</summary>
public class FaunaLooksTests
{
    public static TheoryData<string> Species() => [.. Spec.Fauna.All.Select(s => s.Id)];

    [Theory]
    [MemberData(nameof(Species))]
    public void EverySpeciesHasItsHitsLookAndItsHitsAndDeathsSound(string species)
    {
        Assert.True(HitLooks.Creatures.TryGetValue(species, out var impact), $"{species} has no hit look");
        Assert.NotEmpty(HitLooks.Of[impact].Throws);
        Assert.True(Sound.Creatures.TryGetValue(species, out var cues), $"{species} has no sounds");
        Assert.All(new[] { cues.Hit, cues.Thump, cues.Death }, cue => Assert.NotNull(cue.Heard));
    }
}
