using Stillwake.Sim.Creatures;
using Stillwake.Sim.Debris;
using Xunit;
using Spec = Stillwake.Content;

namespace Stillwake.Sim.Tests;

/// <summary>
/// Shots pierce (2026-10-27-shots-pierce): a hit that breaks or kills what it meets lets the shot fly on, both its damages kept by
/// its gun, half for the nailer's, rounded down: 6, 3, 1, then nothing.
/// </summary>
public class PierceTests
{
    private const int Unit = CreaturesModule.Unit;
    private static readonly string Nailer = Spec.Shots.Nailer.Item;
    private const int Right = 0, Down = 64;

    /// <summary>An engine block at <paramref name="column"/> of row 10 of <see cref="ShotsTests.Space"/>, <paramref name="left"/> hit points left of its 180.</summary>
    internal static Piece Wreck(Scenario w, int column, int left)
    {
        var debris = (DebrisState)w.M.Debris.Slice;
        var piece = new Piece
        {
            Id = debris.NextId++,
            Zone = Spec.Debris.Far.Id,
            Item = "engineBlock",
            X = column * Unit,
            Y = 10_000,
            Start = column * Unit,
            SeenAt = debris.Clock,
            Damage = Spec.Wreckage.HpOf("engineBlock")!.Value - left,
        };
        debris.Pieces.Add(piece);
        SentryTests.Guarded(w);
        return piece;
    }

    /// <summary>Four wrecks along row 10, 6, 3, 1 and 50 hit points left: what the nailer's chain breaks and where it stops.</summary>
    private static (Scenario W, Piece[] Wrecks) Chain()
    {
        var w = ShotsTests.Space();
        return (w, [Wreck(w, 40, 6), Wreck(w, 60, 3), Wreck(w, 80, 1), Wreck(w, 100, 50)]);
    }

    private static Fire Along(int row) => new(Nailer, 10 * Unit + Unit / 2, row * Unit + Unit / 2, Right);

    /// <summary>The shot's flight after <paramref name="fire"/>, tick by tick: every hit it took, and the cell it ended in.</summary>
    internal static (List<ShotHit> Hits, ShotEnd End) Flown(Scenario w, Fire fire)
    {
        w.Do(fire);
        var hits = new List<ShotHit>();
        for (int i = 0; i < 200; i++)
        {
            w.Run();
            hits.AddRange(w.M.Creatures.Hits);
            if (w.M.Creatures.Ended.Count > 0) return (hits, w.M.Creatures.Ended.Single());
        }
        throw new InvalidOperationException("the nail never ended");
    }

    [Fact]
    public void ANailBreaksThreeWrecksOnItsHalvedDamageAndStopsAtTheFourthWithNothingLeftHittingNothing()
    {
        var (w, wrecks) = Chain();
        int fourth = wrecks[3].Damage;
        var (hits, end) = Flown(w, Along(10));
        Assert.Equal([(ShotTarget.Wreck, true), (ShotTarget.Wreck, true), (ShotTarget.Wreck, true), (ShotTarget.Wreck, false)], hits.Select(h => (h.What, h.Broke)));
        Assert.All(wrecks[..3], piece => Assert.DoesNotContain(piece, w.M.Debris.Pieces));
        Assert.True(end.Met);
        Assert.Equal((fourth, null), (wrecks[3].Damage, wrecks[3].Struck));
        Assert.Equal(10, end.Cell / w.M.Grid.Width);
    }

    [Fact]
    public void ANailThatKillsALoperHitsTheOneBehindItForHalf()
    {
        var w = CreaturesTesting.Planet(string.Join('\n', Enumerable.Repeat(new string('.', 80), 20).Append(new string('#', 80))), CreaturesTesting.Lifeless, cells: true)
            .Do(new Crew.Enter(19 * 80 + 2)).GiveItems(0, (Nailer, 1), ("nail", 5)).Run();
        var first = w.Let(Spec.Creatures.Loper, 40, 19);
        var second = w.Let(Spec.Creatures.Loper, 47, 19);
        first.Damage = Spec.Creatures.Loper.Life.Hp - 6;
        var (hits, _) = Flown(w, new Fire(Nailer, 36 * Unit, 17 * Unit, Right));
        Assert.Equal([(ShotTarget.Creature, true), (ShotTarget.Creature, false)], hits.Select(h => (h.What, h.Broke)));
        Assert.DoesNotContain(first, w.M.Creatures.Creatures);
        Assert.Equal(3, second.Damage);
    }

    [Fact]
    public void ANailThatKillsASentryGoesOnIntoItsWreckInTheSameFlight()
    {
        var w = ShotsTests.Space();
        var wreck = Wreck(w, 60, 180);
        w.Do(new Release(Spec.Creatures.Sentry.Id, 8 * 320 + 60)).Run();
        var sentry = w.M.Creatures.Creatures.Single();
        sentry.Damage = Spec.Creatures.Sentry.Life.Hp - 6;
        var (hits, _) = Flown(w, new Fire(Nailer, sentry.X, sentry.Y - 4 * Unit, Down));
        Assert.Equal([(ShotTarget.Creature, true), (ShotTarget.Wreck, false)], hits.Select(h => (h.What, h.Broke)));
        Assert.Equal(3, wreck.Damage);
    }

    [Fact]
    public void ANailAtSoilInAPlanetsFogStopsThereBreakingNothing()
    {
        var rows = Enumerable.Repeat(new string('.', 90) + new string('#', 10), 20).Concat(Enumerable.Repeat(new string('#', 100), 3));
        var looks = Enumerable.Repeat(new string('s', 100), 23);
        var layout = Layout.Parse(string.Join('\n', rows)).WithMaterials(string.Join('\n', looks)) with { Zone = new Box(0, 0, 89, 22), Kind = Spec.PlanetTypes.First.Id };
        var w = CreaturesTesting.Planet(layout, CreaturesTesting.Lifeless, cells: true).Do(new Crew.Enter(19 * 100 + 2)).GiveItems(0, (Nailer, 1), ("nail", 9)).Run();
        int wall = 18 * 100 + 90;
        for (int i = 0; i < 8; i++)
        {
            var (_, end) = Flown(w, new Fire(Nailer, 87 * Unit, 18 * Unit + Unit / 4, Right));
            Assert.Equal((wall, true), (end.Cell, end.Met));
        }
        Assert.NotNull(w.M.Grid.WallAt(wall));
        Assert.Empty(w.M.Building.Cracks);
    }

    [Fact]
    public void AKillAndTheCreatureBehindInTheSameCellAreBothHitThatTick()
    {
        var w = CreaturesTesting.Planet(string.Join('\n', Enumerable.Repeat(new string('.', 80), 20).Append(new string('#', 80))), CreaturesTesting.Lifeless, cells: true)
            .Do(new Crew.Enter(19 * 80 + 2)).GiveItems(0, (Nailer, 1), ("nail", 5)).Run();
        var first = w.Let(Spec.Creatures.Loper, 40, 19);
        var second = w.Let(Spec.Creatures.Loper, 40, 19);
        first.Damage = Spec.Creatures.Loper.Life.Hp - 6;
        // From beside them, so the nail meets them on the tick it flies.
        w.Do(new Fire(Nailer, first.X - 3 * Unit, first.Y - 2 * Unit, Right)).Run();
        Assert.Equal([(ShotTarget.Creature, true), (ShotTarget.Creature, false)], w.M.Creatures.Hits.Select(h => (h.What, h.Broke)));
        Assert.Single(w.M.Creatures.Hits.Select(h => h.Cell).Distinct());
        Assert.Equal(3, second.Damage);
        w.Run();
        Assert.Empty(w.M.Creatures.Hits);
    }

    [Fact]
    public void AShotThatPiercedFliesOnFromWhereAnUnhinderedOneWould()
    {
        var pierced = ShotsTests.Space();
        Wreck(pierced, 40, 6);
        var open = ShotsTests.Space();
        pierced.Do(Along(10)).Run(15);
        open.Do(Along(10)).Run(15);
        var (a, b) = (pierced.M.Creatures.Shots.Single(), open.M.Creatures.Shots.Single());
        Assert.Equal(1, a.Pierced);
        Assert.Equal((b.X, b.Y, b.VX, b.VY, b.Left), (a.X, a.Y, a.VX, a.VY, a.Left));
    }

    [Fact]
    public void ASaveMadeAfterAPiercePlaysOnAsOneNeverSaved()
    {
        Scenario Pierced()
        {
            var (w, _) = Chain();
            return w.Do(Along(10)).Run(12);
        }
        var kept = Pierced();
        Assert.Equal(1, kept.M.Creatures.Shots.Single().Pierced);
        var loaded = Pierced().Reload();
        Assert.Equal(1, loaded.M.Creatures.Shots.Single().Pierced);
        kept.Run(40);
        loaded.Run(40);
        Assert.Equal(kept.World.Hash(), loaded.World.Hash());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    public void AClientsCopyPredictsTheChainAsTheSimFliesItEachWreckOnceAcrossItsCellsAndChangesNothing(int ahead)
    {
        var (w, _) = Chain();
        var gun = Spec.Shots.Nailer;
        var fire = Along(10);
        var copy = ShotFlight.Launch(gun, fire.X, fire.Y, fire.Heading);
        IReadOnlyList<ShotPass> passed = [];
        var predicted = new List<(ShotTarget, bool)>();
        int? copyEnd = null;
        w.Do(fire);
        // As a guest's copy flies, ahead of the sim's by <ahead> ticks of its round trip: the wreck it breaks still whole there.
        var simHits = new List<(ShotTarget, bool)>();
        int? simEnd = null;
        for (int t = 0; t < 200 && (copyEnd == null || simEnd == null); t++)
        {
            if (copyEnd == null)
            {
                ulong hash = w.World.Hash();
                var step = w.M.Creatures.StepOf(copy, Spec.Shots.Nail, gun.Item, passed);
                Assert.Equal(hash, w.World.Hash());
                predicted.AddRange(step.Hits.Select(h => (h.What, h.Broke)));
                (copy, passed) = (step.Next, step.Passed);
                if (step.Met >= 0) copyEnd = step.Met;
            }
            if (t < ahead) continue;
            w.Run();
            simHits.AddRange(w.M.Creatures.Hits.Select(h => (h.What, h.Broke)));
            if (w.M.Creatures.Ended.Count > 0) simEnd = w.M.Creatures.Ended.Single().Cell;
        }
        Assert.Equal(simHits, predicted);
        Assert.Equal(3, passed.Count);
        Assert.Equal(simEnd, copyEnd);
    }
}
