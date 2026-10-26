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

    /// <summary>A creature of <paramref name="s"/> let out where its body lives, a hero beside it: on a planet's floor, or on a wreck in space.</summary>
    private static (Scenario W, Creature C) LetOut(Tune.Species s)
    {
        if (s.Body is Tune.Walker)
        {
            var ground = CreaturesTesting.Planet("""
                ####################
                #..................#
                #..................#
                ####################
                """, CreaturesTesting.Lifeless).HeroAt(2, 2);
            return (ground, ground.Let(s, 6, 2).Still());
        }
        var w = new Scenario(string.Join('\n', Enumerable.Repeat(new string('.', 120), 60)), cells: true).Do(new Crew.Enter(30 * 120 + 50)).Run();
        var debris = (Debris.DebrisState)w.M.Debris.Slice;
        debris.Pieces.Add(new Debris.Piece { Id = debris.NextId++, Zone = Content.Debris.Far.Id, Item = "engineBlock", X = 60_000, Y = 30_000 });
        w.Do(new Release(s.Id, 28 * 120 + 60)).Run(3);
        return (w, w.M.Creatures.Creatures.Single());
    }

    [Fact]
    public void EverySpeciesStruckTurnsToSomethingAndAClampedSentryGuards()
    {
        // A hit some drive claims, or a cut act would only begin again (2026-10-26-creatures-are-composed).
        Assert.All(Tune.All, s =>
        {
            var (w, c) = LetOut(s);
            int drive = c.Drive;
            w.Do(new Strike(c.Id, Content.Weapons.Axe.Item)).Run();
            Assert.True(c.Act != null || c.Drive != drive, $"{s.Id} struck changed nothing");
        });
        var (_, sentry) = LetOut(Tune.Sentry);
        Assert.True(CreaturesModule.Clamped(sentry));
        Assert.IsType<Tune.Guard>(Tune.Sentry.Brain[CreaturesModule.Fallback(Tune.Sentry, sentry)]);
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
