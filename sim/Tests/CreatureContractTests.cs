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
            // The last word of a brain: what it does when nothing else wants it.
            Assert.IsType<Tune.Graze>(s.Brain[^1]);
            // A species that acts escapes after it: its acts' drives hand it over.
            if (s.Moves.Count > 0) Assert.Single(s.Brain.OfType<Tune.Escape>());
            if (s.Brain.Any(d => d is Tune.Retaliate || d is Tune.Flee { CorneredChance: > 0 })) Assert.NotEmpty(s.Moves);
        });
        Assert.All(Tune.Relations, r => Assert.True(Tune.Factions.Contains(r.From) && Tune.Factions.Contains(r.To), $"{r.From} → {r.To}"));
    }
}
