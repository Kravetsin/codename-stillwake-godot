using Xunit;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Tests;

/// <summary>Every species is seen and heard struck by what it is (0093, 0094, 0102): a new one fails here until it has both.</summary>
public class CreatureLooksTests
{
    public static TheoryData<string> Species() => [.. Spec.Creatures.All.Select(s => s.Id)];

    [Fact]
    public void EveryWalkerHasARig() =>
        Assert.All(Spec.Creatures.All.Where(s => s.Body is Spec.Creatures.Walker && !CreatureLooks.Istukans.Contains(s.Look)), s => Assert.True(CreatureLooks.Varieties.ContainsKey(s.Id), s.Id));

    [Theory]
    [MemberData(nameof(Species))]
    public void EverySpeciesIsDrawnByItsRigOrAsTheTick(string species) =>
        Assert.True(CreatureLooks.Varieties.ContainsKey(species) ^ (CreatureLooks.Ticks.Contains(Spec.Creatures.Get(species).Look) || CreatureLooks.Istukans.Contains(Spec.Creatures.Get(species).Look)), species);

    [Theory]
    [MemberData(nameof(Species))]
    public void EverySpeciesHasItsHitsLookAndItsHitsAndDeathsSound(string species)
    {
        Assert.True(HitLooks.Creatures.TryGetValue(species, out var impact), $"{species} has no hit look");
        Assert.NotEmpty(HitLooks.Of[impact].Throws);
        if (CreatureLooks.Istukans.Contains(Spec.Creatures.Get(species).Look))
        {
            Assert.True(Sound.Istukans.TryGetValue(species, out var guard), $"{species} is drawn as the Istukan with no sounds of its own");
            Assert.All(new[] { guard.Wake, guard.Alert, guard.WindUp, guard.Pounce, guard.Land, guard.Hit, guard.Killed, guard.ShutDown }, cue => Assert.NotNull(cue.Heard));
            return;
        }
        if (CreatureLooks.Ticks.Contains(Spec.Creatures.Get(species).Look))
        {
            Assert.True(Sound.Ticks.TryGetValue(species, out var tick), $"{species} is drawn as the tick with no tick's sounds");
            Assert.All(new[] { tick.Alert, tick.WindUp, tick.Unclamp, tick.Clamp, tick.Snap, tick.Thrust, tick.Hit, tick.Death }, cue => Assert.NotNull(cue.Heard));
            return;
        }
        Assert.True(Sound.Creatures.TryGetValue(species, out var cues), $"{species} has no sounds");
        Assert.All(new[] { cues.Hit, cues.Thump, cues.Death }, cue => Assert.NotNull(cue.Heard));
    }
}
