using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Stillwake.Sim.Creatures;
using Stillwake.Sim.Items;
using Spec = Stillwake.Content;

namespace Stillwake.Sim.Tests;

/// <summary>
/// What shots must always keep (shots-are-the-sims), over random walls on the test strip's gravity and in open space, fired any
/// way from anywhere a few at a tick: every shot on the grid, its flight within its reach, ids growing, and none flown through
/// a solid cell.
/// </summary>
public class ShotsInvariantTests
{
    private const int Unit = CreaturesModule.Unit, Width = 64, Height = 32;

    /// <summary>A run: the seed of its walls and shots, whether on the strip or in space, and its ticks.</summary>
    public sealed record Run(int Seed, bool Strip, int Ticks);

    private static Gen<Run> Runs() =>
        from seed in Gen.Choose(0, int.MaxValue)
        from strip in Gen.Elements(true, false)
        from ticks in Gen.Choose(20, 200)
        select new Run(seed, strip, ticks);

    /// <summary>Walls a cell each, about one cell in six, and a border.</summary>
    private static Layout Walls(Rng rng, bool strip)
    {
        var rows = Enumerable.Range(0, Height).Select(y => new string([.. Enumerable.Range(0, Width).Select(x =>
            x == 0 || y == 0 || x == Width - 1 || y == Height - 1 || rng.Below(6) == 0 ? '#' : '.')])).ToArray();
        var layout = Layout.Parse(string.Join('\n', rows));
        return strip ? layout with { Kind = Spec.Surfaces.TestStrip.Id } : layout;
    }

    private static string? Broken(Scenario w, Dictionary<int, (int X, int Y)> was)
    {
        var shots = w.M.Creatures.Shots;
        int most = Spec.Shots.Nailer.Reach * Unit / Spec.Shots.Nailer.Speed;
        for (int i = 0; i < shots.Count; i++)
        {
            var s = shots[i];
            if (i > 0 && shots[i - 1].Id >= s.Id) return $"ids {shots[i - 1].Id} then {s.Id}";
            if (s.X < 0 || s.Y < 0 || s.X >= Width * Unit || s.Y >= Height * Unit) return $"shot {s.Id} off the grid at ({s.X}, {s.Y})";
            if (s.Left <= 0 || s.Left > most) return $"shot {s.Id} with {s.Left} ticks left";
            if (!was.TryGetValue(s.Id, out var from)) continue;
            int solid = -1;
            Rays.Walk(Width, Height, from.X, from.Y, s.X - from.X, s.Y - from.Y, cell =>
            {
                if (w.M.Grid.Solid(cell)) solid = cell;
                return solid >= 0;
            });
            if (solid >= 0) return $"shot {s.Id} flew through solid cell {solid}";
        }
        return null;
    }

    [Property(MaxTest = 60)]
    public Property NoShotLeavesTheGridOutrunsItsReachOrFliesThroughASolidCell() => Prop.ForAll(Runs().ToArbitrary(), run =>
    {
        var rng = new Rng(new RngState { S = (uint)run.Seed });
        var w = new Scenario(Walls(rng, run.Strip)).GiveItems(0, (Spec.Shots.Nailer.Item, 1));
        w.Do(new SetCreative(true)).Run();
        for (int t = 0; t < run.Ticks; t++)
        {
            var was = w.M.Creatures.Shots.ToDictionary(s => s.Id, s => (s.X, s.Y));
            for (int k = (int)rng.Below(3); k > 0; k--)
                w.Do(new Fire(Spec.Shots.Nailer.Item, (int)rng.Below(Width * Unit), (int)rng.Below(Height * Unit), (int)rng.Below(Spec.Headings.Turn)));
            w.Run();
            if (Broken(w, was) is { } broken) return false.Label($"tick {t}: {broken}");
        }
        return true.ToProperty();
    });
}
