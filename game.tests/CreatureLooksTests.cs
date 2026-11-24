using Xunit;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Tests;

/// <summary>Every species is seen and heard struck by what it is (0093, 0094, 0102): a new one fails here until it has both.</summary>
public class CreatureLooksTests
{
    public static TheoryData<string> Species() => [.. Spec.Creatures.All.Select(s => s.Id)];

    [Fact]
    public void APacksBurnersDoNotLoop() => Assert.Null(Sound.Ticks[Spec.Creatures.Breaker.Id].Thrust);

    [Fact]
    public void EveryWalkerHasARig() =>
        Assert.All(Spec.Creatures.All.Where(s => s.Body is Spec.Creatures.Walker && !CreatureLooks.Istukans.Contains(s.Look) && !CreatureLooks.Lurkers.Contains(s.Look) && !CreatureLooks.Escorts.Contains(s.Look)
            && !CreatureLooks.Ticks.ContainsKey(s.Look)), s => Assert.True(CreatureLooks.Varieties.ContainsKey(s.Id), s.Id));

    [Theory]
    [MemberData(nameof(Species))]
    public void EverySpeciesIsDrawnByItsRigOrAsTheTickOrAHero(string species) =>
        Assert.True(CreatureLooks.Varieties.ContainsKey(species) ^ (CreatureLooks.Ticks.ContainsKey(Spec.Creatures.Get(species).Look) || CreatureLooks.Breakers.ContainsKey(Spec.Creatures.Get(species).Look) || CreatureLooks.Istukans.Contains(Spec.Creatures.Get(species).Look) || CreatureLooks.Escorts.Contains(Spec.Creatures.Get(species).Look) || CreatureLooks.Lurkers.Contains(Spec.Creatures.Get(species).Look) || CreatureLooks.Heroes.ContainsKey(Spec.Creatures.Get(species).Look)), species);

    [Fact]
    public void EveryOutfitOfAHeroLookHasEveryPictureAndEachCatcherOneByItsId()
    {
        string hd = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "game", "art", "hd");
        foreach (var look in CreatureLooks.Heroes.Values)
            foreach (var gear in look.Gear)
            {
                foreach (var part in new[] { "torso", "pack", "arm", "leg", "arm_lit" })
                    Assert.True(File.Exists(Path.Combine(hd, $"hero_{gear.Body}_{part}.png")), $"hero_{gear.Body}_{part}");
                foreach (var part in new[] { "shell", "visor", "lit" })
                    Assert.True(File.Exists(Path.Combine(hd, $"hero_{gear.Helmet}_{part}.png")), $"hero_{gear.Helmet}_{part}");
            }
        var catcher = CreatureLooks.Heroes.Values.Single();
        Assert.Equal(catcher.Gear.Count, Enumerable.Range(0, catcher.Gear.Count).Select(catcher.Of).Distinct().Count());
        Assert.Same(catcher.Of(1), catcher.Of(1 + catcher.Gear.Count));
    }

    [Fact]
    public void TheGuardsHitAHeroWithTheSentrysPunch()
    {
        var punch = Sound.RamHits[Spec.Creatures.Sentry.Id];
        Assert.Same(punch, Sound.RamHits.GetValueOrDefault(Spec.Creatures.Spider.Id));
        Assert.Same(punch, Sound.RamHits.GetValueOrDefault(Spec.Creatures.Escort.Id));
    }

    [Theory]
    [MemberData(nameof(Species))]
    public void EverySpeciesHasItsHitsLookAndItsHitsAndDeathsSound(string species)
    {
        Assert.True(HitLooks.Creatures.TryGetValue(species, out var impact), $"{species} has no hit look");
        Assert.NotEmpty(HitLooks.Of[impact].Throws);
        if (CreatureLooks.Escorts.Contains(Spec.Creatures.Get(species).Look))
        {
            Assert.True(Sound.Escorts.TryGetValue(species, out var escort), $"{species} is drawn as the escort with no sounds of its own");
            Assert.All(new[] { escort.Wake, escort.Step, escort.Servo, escort.Pry, escort.Charge, escort.Shot, escort.Stop, escort.Lunge, escort.Hit, escort.Killed },
                cue => Assert.NotNull(cue.Heard));
            return;
        }
        if (CreatureLooks.Istukans.Contains(Spec.Creatures.Get(species).Look))
        {
            Assert.True(Sound.Istukans.TryGetValue(species, out var guard), $"{species} is drawn as the Istukan with no sounds of its own");
            Assert.All(new[] { guard.Wake, guard.Alert, guard.WindUp, guard.Pounce, guard.Land, guard.Hit, guard.Killed, guard.ShutDown }, cue => Assert.NotNull(cue.Heard));
            return;
        }
        if (CreatureLooks.Ticks.ContainsKey(Spec.Creatures.Get(species).Look) || CreatureLooks.Breakers.ContainsKey(Spec.Creatures.Get(species).Look))
        {
            Assert.True(Sound.Ticks.TryGetValue(species, out var tick), $"{species} is drawn as the tick with no tick's sounds");
            Assert.All(new[] { tick.Alert, tick.WindUp, tick.Unclamp, tick.Clamp, tick.Snap, tick.Hit, tick.Death }, cue => Assert.NotNull(cue.Heard));
            Assert.All(new[] { tick.Alert, tick.WindUp, tick.Charge, tick.Unclamp, tick.Clamp, tick.Snap, tick.Hit, tick.Death }, cue => Assert.False(cue.Loops));
            if (tick.Thrust is { } thrust) Assert.NotNull(thrust.Heard);
            // One walking aboard (the spider) is heard stepping and prying too.
            if (Spec.Creatures.Get(species).Body is Spec.Creatures.Walker)
                Assert.All(new[] { tick.Step, tick.Pry }, cue => Assert.NotNull(cue?.Heard));
            return;
        }
        Assert.True(Sound.Creatures.TryGetValue(species, out var cues), $"{species} has no sounds");
        Assert.All(new[] { cues.Hit, cues.Thump, cues.Death }, cue => Assert.NotNull(cue.Heard));
    }
}
