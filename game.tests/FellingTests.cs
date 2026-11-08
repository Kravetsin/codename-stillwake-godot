using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Stillwake.Game.Features.Flora;
using Stillwake.Sim.Building;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>A cave view finds the plants felled since it looked from the cells it kept and the plants standing, both ascending.</summary>
public class FellingTests
{
    [Property(MaxTest = 500)]
    public Property TheCellsKeptButNoLongerStandingAreTheFelledAndOnlyThose() =>
        Prop.ForAll(Gen.Choose(0, int.MaxValue).ToArbitrary(), seed =>
        {
            // Plants on distinct cells, none to all of them felled since, in runs or alone.
            var random = new System.Random(seed);
            var before = Enumerable.Range(0, random.Next(0, 300)).Select(_ => random.Next(5000)).Distinct().Order()
                .Select(c => new Plant(c, "moss")).ToList();
            int odds = random.Next(1, 12);
            var standing = before.Where(_ => random.Next(odds) != 0).ToList();
            var kept = before.Select(p => p.Cell).ToList();
            var felled = new List<int>();
            Felling.Take(kept, standing, felled);
            Assert.Equal(standing.Select(p => p.Cell), kept);
            Assert.Equal(before.Except(standing).Select(p => p.Cell), felled);
        });
}
