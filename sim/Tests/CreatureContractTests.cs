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
            // A drive that begins an act hands the creature over after it: the ram to the escape, a clinger's pounce to the return;
            // a lurker on the ground (cave-hostiles) stays on guard where it lands.
            if (s.Brain.Any(d => d is Tune.Retaliate || d is Tune.Flee { CorneredChance: > 0 }))
            {
                Assert.NotEmpty(s.Moves);
                Assert.Single(s.Brain.OfType<Tune.Escape>());
            }
            if (s.Brain.Any(d => d is Tune.Guard))
            {
                Assert.NotEmpty(s.Moves);
                if (s.Body is Tune.Clinger) Assert.Single(s.Brain.OfType<Tune.Return>());
            }
        });
        Assert.All(Tune.Relations, r => Assert.True(Tune.Factions.Contains(r.From) && Tune.Factions.Contains(r.To), $"{r.From} → {r.To}"));
    }

    /// <summary>A creature of <paramref name="s"/> let out where its body lives, a hero beside it: on a planet's floor, or on a wreck in space.</summary>
    private static (Scenario W, Creature C) LetOut(Tune.Species s)
    {
        if (s.Body is Tune.Flyer)
        {
            // A catcher let out by hero 1 hunts it: struck by hero 0, it answers.
            var sky = CreaturesTesting.Planet("""
                ####################
                #..................#
                #..................#
                #..................#
                #..................#
                ####################
                """, CreaturesTesting.Lifeless).HeroAt(2, 4).HeroAt(16, 4, peer: 1);
            sky.Do(new Release(s.Id, sky.Feet(8, 4)) { From = 1 }).Run();
            return (sky, sky.M.Creatures.Creatures[^1]);
        }
        if (s.Body is Tune.Walker)
        {
            var ground = CreaturesTesting.Planet("""
                ####################
                #..................#
                #..................#
                ####################
                """, CreaturesTesting.Lifeless).HeroAt(2, 2);
            var c = ground.Let(s, 6, 2);
            // A grazer set idling; a lurker (cave-hostiles) is on guard from birth.
            return (ground, s.Brain.Any(d => d is Tune.Graze) ? c.Still() : c);
        }
        var w = new Scenario(string.Join('\n', Enumerable.Repeat(new string('.', 120), 60)), cells: true).Do(new Crew.Enter(30 * 120 + 50)).Run();
        var debris = (Debris.DebrisState)w.M.Debris.Slice;
        debris.Pieces.Add(new Debris.Piece { Id = debris.NextId++, Zone = Content.Debris.Far.Id, Item = "engineBlock", X = 60_000, Y = 30_000 });
        SentryTests.Guarded(w);
        w.Do(new Release(s.Id, 28 * 120 + 60)).Run(3);
        return (w, w.M.Creatures.Creatures.Single());
    }

    [Fact]
    public void EverySpeciesStruckTurnsToSomethingAndAClampedSentryGuards()
    {
        // A hit some drive claims, or a cut act would only begin again (2026-10-26-creatures-are-composed). A posted one is never let out: SpireGuardTests.
        Assert.All(Tune.All.Where(s => s.Lifetime != Tune.Lifetime.Posted), s =>
        {
            var (w, c) = LetOut(s);
            int drive = c.Drive;
            w.Do(new Strike(c.Id, Content.Weapons.Axe.Item)).Run();
            // A lurker out of its strike answers by being provoked (lurker-attacks); a shut snail's shell turns the blow aside.
            bool answered = c.Act != null || c.Drive != drive || GuardDrive.Provoked(w.M.Creatures, c) || CreaturesModule.Shut(c);
            Assert.True(answered, $"{s.Id} struck changed nothing");
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
        // The shot added after the pounce (shots-are-the-sims): every place a save holds keeps its meaning, so no bump.
        { "sentry", "CutTether Return Guard | Pounce Shoot" },
        { "warden", "Ward | Pounce" },
        { "capback", "Guard | Vent Shoot" },
        { "geodeSnail", "Guard Shell | Lunge" },
        { "catcher", "Hunt | Pounce Shoot" },
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
