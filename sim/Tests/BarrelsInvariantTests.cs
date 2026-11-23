using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Stillwake.Sim.Creatures;
using Stillwake.Sim.Crew;
using Stillwake.Sim.Debris;
using Stillwake.Sim.Items;
using Spec = Stillwake.Content;

namespace Stillwake.Sim.Tests;

/// <summary>
/// What fuel drums must always keep (barrels), over drums put out at random among wrecks, axed and blown up at random, their bursts
/// breaking each other: every drum broken goes off once, where it was, and no other.
/// </summary>
public class BarrelsInvariantTests
{
    private const int Unit = CreaturesModule.Unit, W = 320;
    private const string Drum = "fuelDrum";

    /// <summary>A run: the seed of its drums and blows, how many drums, and its ticks of blows.</summary>
    public sealed record Run(int Seed, int Drums, int Ticks);

    private static Gen<Run> Runs() =>
        from seed in Gen.Choose(0, int.MaxValue)
        from drums in Gen.Choose(1, 12)
        from ticks in Gen.Choose(10, 120)
        select new Run(seed, drums, ticks);

    [Property(MaxTest = 40)]
    public Property EveryDrumBrokenBurstsExactlyOnceWhereItWas() => Prop.ForAll(Runs().ToArbitrary(), run =>
    {
        var rng = new Rng(new RngState { S = (uint)run.Seed });
        string charge = Spec.Shots.Thrown.Item, axe = Spec.Weapons.Axe.Item;
        // Hero 0 throws charges, hero 1 swings the axe, both in creative: never out of charges nor passed out.
        var w = new Scenario(string.Join('\n', Enumerable.Repeat(new string('.', W), 20)), cells: true)
            .Do(new Enter(10 * W + 2)).Do(new Enter(3 * W + 2) { From = 1 })
            .Do(new SetCreative(true)).Do(new SetCreative(true) { From = 1 })
            .GiveItems(0, (charge, 1)).Wields(charge).Run();
        var drums = new List<Piece>();
        for (int i = 0; i < run.Drums; i++)
            drums.Add(BarrelsTests.Put(w, 10 + (int)rng.Below(50), 2 + (int)rng.Below(16), damage: (int)rng.Below(4)));
        for (int i = (int)rng.Below(4); i > 0; i--) BarrelsTests.Put(w, 10 + (int)rng.Below(50), 2 + (int)rng.Below(16), item: "hullSection");
        var bursts = new List<int>();
        for (int t = 0; t < run.Ticks + 300; t++)
        {
            if (t < run.Ticks && rng.Below(4) == 0)
                w.Do(new Fire(charge, (10 + (int)rng.Below(50)) * Unit, (2 + (int)rng.Below(16)) * Unit, (int)rng.Below(Spec.Headings.Turn)));
            if (t < run.Ticks && rng.Below(3) == 0) w.Do(new HewPieces(axe, [drums[(int)rng.Below((uint)drums.Count)].Id]) { From = 1 });
            w.Run();
            // The far field's own pieces spawn 120 cells off and more, past where these fly.
            bursts.AddRange(w.M.Creatures.DrumBursts.Where(b => b.Cell % W < 115).Select(b => b.Cell));
            if (t >= run.Ticks && w.M.Creatures.Shots.Count == 0 && ((DebrisState)w.M.Debris.Slice).Bursting.Count == 0) break;
        }
        var broken = drums.Where(d => w.M.Debris.PieceById(d.Id) == null).Select(d => DebrisModule.CellOf(d.X, d.Y, W)).Order().ToList();
        if (drums.Any(d => w.M.Debris.PieceById(d.Id) == null != d.Damage >= Spec.Wreckage.HpOf(Drum))) return false.Label("a drum gone unbroken");
        return broken.SequenceEqual(bursts.Order()).Label($"broken at [{string.Join(", ", broken)}], burst at [{string.Join(", ", bursts.Order())}]");
    });
}
