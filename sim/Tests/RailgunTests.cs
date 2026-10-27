using Stillwake.Sim.Creatures;
using Stillwake.Sim.Items;
using Xunit;
using Spec = Stillwake.Content;

namespace Stillwake.Sim.Tests;

/// <summary>
/// The railgun (2026-10-27-the-railgun): a nail at 300 cells a second, a tenth of its pull, through everything its hit takes, keeping
/// 650‰ at each, 24, 15, 9, 5, 3, 1; never hitting a thing it passed again; 120 of the belt's charge a shot.
/// </summary>
public class RailgunTests
{
    private const int Unit = CreaturesModule.Unit;
    private static readonly Spec.Gun Gun = Spec.Shots.Railgun;
    private static readonly int Belt = Spec.Inventory.Slots + 2;
    private const int Right = 0;

    /// <summary><see cref="ShotsTests.Space"/> with the railgun in hand and a full battery on the belt.</summary>
    private static Scenario Space() => ShotsTests.Space().GiveItems(0, (Gun.Item, 1)).Put(Belt, new Stack("battery", 1, Spec.Charge.Battery));

    private static Fire Along(int row, int column = 10) => new(Gun.Item, column * Unit + Unit / 2, row * Unit + Unit / 2, Right);

    /// <summary>
    /// The first planet, a cell a character: 60 open columns by 20 over three rows of soil, and soil standing in columns
    /// <paramref name="from"/> to <paramref name="to"/>; the hero at column 2 with the railgun, nails and a full battery.
    /// </summary>
    private static Scenario Ground(int from, int to)
    {
        var rows = Enumerable.Range(0, 20).Select(_ => string.Concat(Enumerable.Range(0, 60).Select(x => x >= from && x <= to ? '#' : '.')))
            .Concat(Enumerable.Repeat(new string('#', 60), 3));
        var layout = Layout.Parse(string.Join('\n', rows)).WithMaterials(string.Join('\n', Enumerable.Repeat(new string('s', 60), 23)))
            with
        { Kind = Spec.PlanetTypes.First.Id };
        return CreaturesTesting.Planet(layout, CreaturesTesting.Lifeless, cells: true).Do(new Crew.Enter(19 * 60 + 2))
            .GiveItems(0, (Gun.Item, 1), ("nail", 10)).Put(Belt, new Stack("battery", 1, Spec.Charge.Battery)).Run();
    }

    private static int Charge(Scenario w) => w.M.Items.Inventory(0)[Belt]!.Charge / Spec.Charge.Scale;

    [Fact]
    public void ItPassesAWreckItCannotBreakHittingItOnceAcrossItsCellsAndTheNextForWhatIsKept()
    {
        var w = Space();
        var (first, second) = (PierceTests.Wreck(w, 40, 180), PierceTests.Wreck(w, 70, 180));
        var (hits, end) = PierceTests.Flown(w, Along(10));
        Assert.Equal([(ShotTarget.Wreck, 24, false), (ShotTarget.Wreck, 15, false)], hits.Take(2).Select(h => (h.What, h.Damage, h.Broke)));
        Assert.Equal((24, 15), (first.Damage, second.Damage));
        Assert.False(end.Met);
        Assert.Equal(1800 - Gun.Energy, Charge(w));
    }

    [Fact]
    public void ItBreaksTheFirstCellOfSoilScarsTheNextTwoAndHurtsTheLoperBehindForFive()
    {
        var w = Ground(30, 32);
        var loper = w.Let(Spec.Creatures.Loper, 40, 19);
        int row = 17 * 60;
        var (hits, _) = PierceTests.Flown(w, new Fire(Gun.Item, 25 * Unit, 17 * Unit + Unit / 2, Right));
        Assert.Equal([(ShotTarget.Solid, 4, true), (ShotTarget.Solid, 2, false), (ShotTarget.Solid, 1, false), (ShotTarget.Creature, 5, false)],
            hits.Take(4).Select(h => (h.What, h.Damage, h.Broke)));
        Assert.Null(w.M.Grid.WallAt(row + 30));
        Assert.Equal((2, 1), (w.M.Building.Cracks.Single(c => c.Cell == row + 31).Damage, w.M.Building.Cracks.Single(c => c.Cell == row + 32).Damage));
        Assert.Equal(5, loper.Damage);
    }

    [Fact]
    public void AFourthCellOfSoilEndsItWithNothingLeftForABlockTheLoperBehindUnhurt()
    {
        var w = Ground(30, 33);
        var loper = w.Let(Spec.Creatures.Loper, 40, 19);
        var (hits, end) = PierceTests.Flown(w, new Fire(Gun.Item, 25 * Unit, 17 * Unit + Unit / 2, Right));
        Assert.Equal((ShotTarget.Solid, 0, false), (hits[^1].What, hits[^1].Damage, hits[^1].Broke));
        Assert.Equal((17 * 60 + 33, true), (end.Cell, end.Met));
        Assert.Equal(0, loper.Damage);
    }

    [Fact]
    public void ItStopsWhereItsHitTakesNothingAtBedrockAndAboard()
    {
        var rows = Enumerable.Repeat(new string('.', 30) + "#" + new string('.', 29), 20).Concat(Enumerable.Repeat(new string('#', 60), 3));
        var looks = Enumerable.Repeat(new string('.', 30) + "b" + new string('.', 29), 20).Concat(Enumerable.Repeat(new string('s', 60), 3));
        var layout = Layout.Parse(string.Join('\n', rows)).WithMaterials(string.Join('\n', looks)) with { Kind = Spec.PlanetTypes.First.Id };
        var w = CreaturesTesting.Planet(layout, CreaturesTesting.Lifeless, cells: true).Do(new Crew.Enter(19 * 60 + 2))
            .GiveItems(0, (Gun.Item, 1), ("nail", 10)).Put(Belt, new Stack("battery", 1, Spec.Charge.Battery)).Run();
        var (_, end) = PierceTests.Flown(w, new Fire(Gun.Item, 25 * Unit, 17 * Unit + Unit / 2, Right));
        Assert.Equal((17 * 60 + 30, true), (end.Cell, end.Met));

        var aboard = new Scenario(".....#....\n..........", cells: true).Do(new Crew.Enter(2)).GiveItems(0, (Gun.Item, 1), ("nail", 1))
            .Put(Belt, new Stack("battery", 1, Spec.Charge.Battery)).Run();
        var (_, at) = PierceTests.Flown(aboard, new Fire(Gun.Item, Unit + Unit / 2, Unit / 2, Right));
        Assert.Equal((5, true), (at.Cell, at.Met));
        Assert.NotNull(aboard.M.Grid.WallAt(5));
    }

    [Fact]
    public void AShotCostsItsChargeRefusedAsFlatBelowItSpendingNothingAndFreeInCreative()
    {
        var w = ShotsTests.Space().GiveItems(0, (Gun.Item, 1)).Put(Belt, new Stack("battery", 1, (Gun.Energy - 1) * Spec.Charge.Scale));
        int nails = w.M.Items.Totals(0).Where(t => t.Item == "nail").Sum(t => t.Count);
        w.Do(Along(10)).Run();
        Assert.Equal("items.flat", w.Refusals.Single().Reason);
        Assert.Equal((Gun.Energy - 1, nails), (Charge(w), w.M.Items.Totals(0).Where(t => t.Item == "nail").Sum(t => t.Count)));
        Assert.Empty(w.M.Creatures.Shots);

        var creative = Space().Do(new SetCreative(true)).Run();
        creative.Do(Along(10)).Run();
        Assert.Equal(1800, Charge(creative));
        Assert.Single(creative.M.Creatures.Shots);
    }

    [Fact]
    public void ItsNailFallsATenthOfTheNailersOnAPlanetAndNotAtAllAboard()
    {
        var planet = Spec.PlanetTypes.First;
        int nail = Spec.Shots.Nail.Weight * Spec.Creatures.Gravity;
        Assert.Equal(planet.Fall(nail) * Gun.Fall / 1000, ShotFlight.Gravity(planet, Spec.Shots.Nail, Gun));
        Assert.Equal(29, ShotFlight.Gravity(planet, Spec.Shots.Nail, Gun));
        Assert.Equal(planet.Fall(nail), ShotFlight.Gravity(planet, Spec.Shots.Nail, Spec.Shots.Nailer));
        Assert.Equal(0, ShotFlight.Gravity(Spec.Surfaces.Ship, Spec.Shots.Nail, Gun));
    }

    [Fact]
    public void ASaveMadeJustAfterItPassedAWreckLoadsAndNeverHitsItAgain()
    {
        Scenario Passed()
        {
            var w = Space();
            PierceTests.Wreck(w, 40, 180);
            // Ten and a half columns, then 15 a tick: inside the wreck's box (35..44) at the end of the second tick.
            return w.Do(Along(10)).Run(2);
        }
        var kept = Passed();
        var shot = kept.M.Creatures.Shots.Single();
        Assert.Single(shot.Passed);
        var loaded = Passed().Reload();
        Assert.Single(loaded.M.Creatures.Shots.Single().Passed);
        kept.Run(30);
        loaded.Run(30);
        Assert.Equal(24, kept.M.Debris.Pieces.Single().Damage);
        Assert.Equal(kept.World.Hash(), loaded.World.Hash());
    }

    [Fact]
    public void AClientsCopyFliesThroughTheWreckAndTheSoilAsTheSimDoes()
    {
        foreach (var (w, fire) in new[] { (Space(), Along(10)), (Ground(30, 32), new Fire(Gun.Item, 25 * Unit, 17 * Unit + Unit / 2, Right)) })
        {
            if (w.M.Debris.Pieces.Count == 0 && w.M.Grid.Width == 320) PierceTests.Wreck(w, 40, 180);
            var copy = ShotFlight.Launch(Gun, fire.X, fire.Y, fire.Heading);
            IReadOnlyList<ShotPass> passed = [];
            var predicted = new List<(ShotTarget, bool)>();
            int? copyEnd = null;
            for (int t = 0; t < 60 && copyEnd == null; t++)
            {
                var step = w.M.Creatures.StepOf(copy, Spec.Shots.Nail, Gun.Item, passed);
                predicted.AddRange(step.Hits.Select(h => (h.What, h.Damage > 0)));
                (copy, passed) = (step.Next, step.Passed);
                if (step.Met >= 0 || step.Next.Left <= 0) copyEnd = step.Met;
            }
            var (hits, end) = PierceTests.Flown(w, fire);
            Assert.Equal(hits.Select(h => (h.What, h.Damage > 0)), predicted);
            Assert.Equal(end.Met ? end.Cell : -1, copyEnd);
        }
    }
}
