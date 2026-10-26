using Xunit;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Tests;

/// <summary>Every species is seen and heard struck by what it is (0093, 0094, 0102): a new one fails here until it has both.</summary>
public class CreatureLooksTests
{
    /// <summary>Species still silent, their sounds picked on an audition page to come: the sentry's with its look (C-4).</summary>
    private static readonly string[] Silent = [Spec.Creatures.Sentry.Id];

    public static TheoryData<string> Species() => [.. Spec.Creatures.All.Select(s => s.Id)];

    [Fact]
    public void EveryWalkerHasARigOnlyACreatureWithNoneIsDrawnAsABox() =>
        Assert.All(Spec.Creatures.All.Where(s => s.Body is Spec.Creatures.Walker), s => Assert.True(CreatureLooks.Varieties.ContainsKey(s.Id), s.Id));

    [Theory]
    [MemberData(nameof(Species))]
    public void EverySpeciesHasItsHitsLookAndItsHitsAndDeathsSound(string species)
    {
        Assert.True(HitLooks.Creatures.TryGetValue(species, out var impact), $"{species} has no hit look");
        Assert.NotEmpty(HitLooks.Of[impact].Throws);
        if (Silent.Contains(species))
        {
            Assert.False(Sound.Creatures.ContainsKey(species), $"{species} has its sounds now: take it off the silent list");
            return;
        }
        Assert.True(Sound.Creatures.TryGetValue(species, out var cues), $"{species} has no sounds");
        Assert.All(new[] { cues.Hit, cues.Thump, cues.Death }, cue => Assert.NotNull(cue.Heard));
    }
}
