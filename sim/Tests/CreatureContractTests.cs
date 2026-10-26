using Stillwake.Sim.Creatures;
using Xunit;
using Tune = Stillwake.Content.Creatures;

namespace Stillwake.Sim.Tests;

/// <summary>
/// The composed species keep their contract (2026-10-26-creatures-are-composed): every record a species may name has a rule,
/// and every species names what its rules need.
/// </summary>
public class CreatureContractTests
{
    [Fact]
    public void EveryDriveShapeAndMoveRecordHasARule()
    {
        var records = typeof(Tune.Drive).Assembly.GetTypes()
            .Where(t => !t.IsAbstract && (t.IsSubclassOf(typeof(Tune.Drive)) || t.IsSubclassOf(typeof(Tune.Shape)) || t.IsSubclassOf(typeof(Tune.Move))))
            .OrderBy(t => t.Name, StringComparer.Ordinal).ToList();
        Assert.NotEmpty(records);
        Assert.All(records, t => Assert.True(RuleBook.Serves(t), $"no rule serves {t.Name}"));
    }

    [Fact]
    public void EverySpeciesHasABodyALifeAFactionAndADriveThatAlwaysWantsIt()
    {
        Assert.All(Tune.All, s =>
        {
            Assert.True(s.Body.Width > 0 && s.Body.Height > 0 && s.Life.Hp > 0, s.Id);
            Assert.Contains(s.Faction, Tune.Factions);
            // What it does when nothing else wants it: some drive always does.
            Assert.True(CreaturesModule.Fallback(s, new Creature()) >= 0);
            // A hit is claimed by some drive, or a cut act would only begin again: the flight, the guard.
            Assert.Contains(s.Brain, d => RuleBook.Drive(d).ClaimsHits);
            // A drive that begins an act hands the creature over after it: the ram to the escape, the pounce to the return.
            if (s.Brain.Any(d => d is Tune.Retaliate || d is Tune.Flee { CorneredChance: > 0 }))
            {
                Assert.NotEmpty(s.Moves);
                Assert.Single(s.Brain.OfType<Tune.Escape>());
            }
            if (s.Brain.Any(d => d is Tune.Guard))
            {
                Assert.NotEmpty(s.Moves);
                Assert.Single(s.Brain.OfType<Tune.Return>());
            }
        });
        Assert.All(Tune.Relations, r => Assert.True(Tune.Factions.Contains(r.From) && Tune.Factions.Contains(r.To), $"{r.From} → {r.To}"));
    }

    /// <summary>Each species' drives and moves in order: a creature saves its drive and act by their places in them.</summary>
    public static TheoryData<string, string> Saved => new()
    {
        { "loper", "Retaliate Flee Escape Graze | Lunge" },
        { "tapir", "Retaliate Flee Escape Graze | Lunge" },
        { "sixleg", "Retaliate Flee Escape Graze | Lunge" },
        { "fluff", "Flee Graze | " },
        { "sentry", "CutTether Return Guard | Pounce" },
    };

    [Fact]
    public void EverySpeciesHasItsOrderPinned() =>
        Assert.Equal(Tune.All.Select(s => s.Id).Order(StringComparer.Ordinal), Saved.Select(row => (string)row[0]).Order(StringComparer.Ordinal));

    [Theory]
    [MemberData(nameof(Saved))]
    public void ABrainsOrderIsWhatSavesHold(string species, string order)
    {
        var s = Tune.Get(species);
        string now = $"{string.Join(' ', s.Brain.Select(d => d.GetType().Name))} | {string.Join(' ', s.Moves.Select(m => m.GetType().Name))}";
        Assert.True(now == order, $"{species}'s brain or moves are now '{now}': bump World.SaveFormat with a step that drops or remaps its creatures, then update this");
    }
}
