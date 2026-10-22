using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Stillwake.Sim.Fauna;
using Tune = Stillwake.Content.Fauna;

namespace Stillwake.Sim.Tests;

/// <summary>
/// What creatures must always keep (0102), over random ground — hills, steps, cliffs, overhangs, pits — with up to more than the
/// cap let out, struck with every weapon from heroes moving anywhere, for long runs: every body on the grid and out of every wall,
/// its damage under its hit points, ids growing, never more than the cap.
/// </summary>
public class FaunaInvariantTests
{
    private const int Unit = FaunaModule.Unit, Width = 48, Height = 16;

    /// <summary>A run: the seed of its ground, the releases, then that many ticks of strikes and heroes' moves.</summary>
    public sealed record Run(int Ground, int Releases, int Ticks, int Seed);

    private static Gen<Run> Runs() =>
        from ground in Gen.Choose(0, int.MaxValue)
        from releases in Gen.Choose(0, Tune.Max + 6)
        from ticks in Gen.Choose(50, 900)
        from seed in Gen.Choose(0, int.MaxValue)
        select new Run(ground, releases, ticks, seed);

    /// <summary>Ground of columns rising and falling by up to three rows, a ledge hung here and there, walled at the sides and the bottom.</summary>
    private static Layout Ground(int seed)
    {
        var rng = new Rng(new RngState { S = (uint)seed });
        var rows = Enumerable.Range(0, Height).Select(_ => new char[Width]).ToArray();
        int top = 10;
        for (int x = 0; x < Width; x++)
        {
            top = Math.Clamp(top + (int)rng.Below(7) - 3, 3, Height - 2);
            for (int y = 0; y < Height; y++)
                rows[y][x] = x == 0 || x == Width - 1 || y == Height - 1 || y >= top ? '#' : ' ';
            if (rng.Below(6) == 0 && top > 5) rows[top - 3 - (int)rng.Below(2)][x] = '#';
        }
        return Layout.Parse([.. rows.Select(r => new string(r))]);
    }

    private static int NextId(Scenario w) => ((FaunaState)w.M.Fauna.Slice).NextId;

    private static string? Broken(Scenario w)
    {
        var all = w.M.Fauna.Creatures;
        if (all.Count > Tune.Max) return $"{all.Count} creatures";
        for (int i = 0; i < all.Count; i++)
        {
            var c = all[i];
            var spec = Tune.Get(c.Species);
            if (i > 0 && all[i - 1].Id >= c.Id) return $"ids {all[i - 1].Id} then {c.Id}";
            if (c.Damage >= spec.Hp) return $"creature {c.Id} alive at {c.Damage} damage";
            int half = spec.Width * Unit / 2;
            if (c.X - half < 0 || c.X + half > Width * Unit || c.Y - spec.Height * Unit < 0 || c.Y > Height * Unit)
                return $"creature {c.Id} off the grid at ({c.X}, {c.Y})";
            if (w.M.Fauna.Overlaps(spec, c.X, c.Y)) return $"creature {c.Id} in a wall at ({c.X}, {c.Y})";
        }
        return null;
    }

    [Property(MaxTest = 60)]
    public Property CreaturesStayOnTheGridOutOfWallsUnderTheirHitPointsAndTheCap() =>
        Prop.ForAll(Runs().ToArbitrary(), run =>
        {
            var w = FaunaTesting.Planet(Ground(run.Ground)).Breathing();
            var rng = new Rng(new RngState { S = (uint)run.Seed });
            for (int h = 0; h < 2; h++) w.HeroAt(1 + (int)rng.Below(Width - 2), 2, peer: h);
            w.Run();
            for (int i = 0; i < run.Releases; i++) w.Do(new Release(Tune.Loper.Id, (int)rng.Below(Width * Height)));
            w.Run();
            string? broken = Broken(w);
            int next = NextId(w);
            for (int t = 0; t < run.Ticks && broken == null; t++)
            {
                var all = w.M.Fauna.Creatures;
                // A strike now and then, so most live long enough to wander, fall and flee into walls.
                if (all.Count > 0 && rng.Below(25) == 0)
                {
                    var weapons = Content.Weapons.All;
                    w.Do(new Strike(all[(int)rng.Below((uint)all.Count)].Id, weapons[(int)rng.Below((uint)weapons.Count)].Item) { From = (int)rng.Below(2) });
                }
                if (rng.Below(40) == 0) w.HeroAt((int)rng.Below(Width), (int)rng.Below(Height), peer: (int)rng.Below(2));
                w.Run();
                broken = Broken(w);
                if (NextId(w) < next || all.Count > 0 && all[^1].Id >= NextId(w)) broken ??= $"the next id {NextId(w)} after {next}";
                next = NextId(w);
            }
            return (broken == null).Label(broken ?? "");
        });
}
