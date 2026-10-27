using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Stillwake.Sim.Creatures;
using Tune = Stillwake.Content.Creatures;

namespace Stillwake.Sim.Tests;

/// <summary>
/// What creatures must always keep (0102), over random ground — hills, steps, cliffs, overhangs, pits — with up to more than the
/// cap let out, struck with every weapon from heroes moving anywhere, for long runs: every body on the grid and out of every wall,
/// its damage under its hit points, ids growing, never more than the cap.
/// </summary>
public class CreaturesInvariantTests
{
    private const int Unit = CreaturesModule.Unit, Width = 96, Height = 32;

    /// <summary>A run: the seed of its ground, the releases, then that many ticks of strikes and heroes' moves.</summary>
    public sealed record Run(int Ground, int Releases, int Ticks, int Seed);

    private static Gen<Run> Runs() =>
        from ground in Gen.Choose(0, int.MaxValue)
        from releases in Gen.Choose(0, Tune.Max + 6)
        from ticks in Gen.Choose(50, 900)
        from seed in Gen.Choose(0, int.MaxValue)
        select new Run(ground, releases, ticks, seed);

    /// <summary>
    /// Ground drawn a character a cell, its columns rising and falling by up to six rows, a ledge a cell thick hung here and there,
    /// walled at the sides and the bottom: steps a walker climbs (0104), steps it does not, and thin walls a fast one meets.
    /// </summary>
    private static Layout Ground(int seed)
    {
        var rng = new Rng(new RngState { S = (uint)seed });
        var rows = Enumerable.Range(0, Height).Select(_ => new char[Width]).ToArray();
        int top = 20;
        for (int x = 0; x < Width; x++)
        {
            top = Math.Clamp(top + (int)rng.Below(13) - 6, 6, Height - 4);
            for (int y = 0; y < Height; y++)
                rows[y][x] = x == 0 || x == Width - 1 || y == Height - 1 || y >= top ? '#' : ' ';
            if (rng.Below(6) == 0 && top > 10) rows[top - 6 - (int)rng.Below(4)][x] = '#';
        }
        return Layout.Parse([.. rows.Select(r => new string(r))]);
    }

    private static int NextId(Scenario w) => ((CreaturesState)w.M.Creatures.Slice).NextId;

    private static string? Broken(Scenario w)
    {
        var all = w.M.Creatures.Creatures;
        if (all.Count > Tune.Max) return $"{all.Count} creatures";
        for (int i = 0; i < all.Count; i++)
        {
            var c = all[i];
            var spec = Tune.Get(c.Species);
            if (i > 0 && all[i - 1].Id >= c.Id) return $"ids {all[i - 1].Id} then {c.Id}";
            if (c.Damage >= spec.Life.Hp) return $"creature {c.Id} alive at {c.Damage} damage";
            int half = spec.Body.Width * Unit / 2;
            if (c.X - half < 0 || c.X + half > Width * Unit || c.Y - spec.Body.Height * Unit < 0 || c.Y > Height * Unit)
                return $"creature {c.Id} off the grid at ({c.X}, {c.Y})";
            if (w.M.Creatures.Overlaps(spec.Body, c.X, c.Y)) return $"creature {c.Id} in a wall at ({c.X}, {c.Y})";
            if (c.Left < 1 || c.Left > LongestOf(c.Posture, spec)) return $"creature {c.Id} {c.Posture} with {c.Left} ticks left";
        }
        foreach (var hero in w.M.Crew.Heroes)
            if (hero.Air < 0 || hero.Air > Scenario.Full) return $"hero {hero.Id}'s suit at {hero.Air}";
        return null;
    }

    /// <summary>The most ticks <paramref name="posture"/> lasts for a creature of <paramref name="spec"/>: what is left of it never exceeds it.</summary>
    private static int LongestOf(Posture posture, Tune.Species spec) => posture switch
    {
        Posture.Idle => Tune.IdleMost,
        Posture.Wander => Tune.WanderMost,
        Posture.Flee => spec.Brain.OfType<Tune.Flee>().Single().Ticks,
        Posture.WindUp => ((Tune.Lunge)spec.Moves[0]).WindUp,
        Posture.Charge => ((Tune.Lunge)spec.Moves[0]).Ticks,
        Posture.Retreat => spec.Brain.OfType<Tune.Retaliate>().Single().FleeMost,
        Posture.Escape => spec.Brain.OfType<Tune.Escape>().Single().Ticks,
        _ => 0,
    };

    [Property(MaxTest = 60)]
    public Property CreaturesStayOnTheGridOutOfWallsUnderTheirHitPointsAndTheCap() =>
        Prop.ForAll(Runs().ToArbitrary(), run =>
        {
            var w = CreaturesTesting.Planet(Ground(run.Ground), cells: true).Breathing();
            var rng = new Rng(new RngState { S = (uint)run.Seed });
            for (int h = 0; h < 2; h++) w.HeroAt(1 + (int)rng.Below(Width - 2), 4, peer: h);
            w.Run();
            for (int i = 0; i < run.Releases; i++) w.Do(new Release(Tune.All[(int)rng.Below((uint)Tune.All.Count)].Id, (int)rng.Below(Width * Height)));
            w.Run();
            string? broken = Broken(w);
            int next = NextId(w);
            for (int t = 0; t < run.Ticks && broken == null; t++)
            {
                var all = w.M.Creatures.Creatures;
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

    /// <summary>
    /// Rams (0103), over the same ground: rammers let out by heroes who breathe their suits, struck often so they flee into
    /// corners, and every hero sending a hit by every charging creature most ticks, by others too: no charge hurts more than once,
    /// every suit stays within nothing and full, and the creatures keep all the above.
    /// </summary>
    [Property(MaxTest = 60)]
    public Property AChargeHurtsAtMostOnceAndEverySuitStaysWithinItsRange() =>
        Prop.ForAll(Runs().ToArbitrary(), run =>
        {
            var w = CreaturesTesting.Planet(Ground(run.Ground), cells: true);
            var rng = new Rng(new RngState { S = (uint)run.Seed });
            int heroes = 3;
            for (int h = 0; h < heroes; h++) w.HeroAt(1 + (int)rng.Below(Width - 2), 4, peer: h);
            w.Run();
            Tune.Species[] rammers = [Tune.Tapir, Tune.Loper, Tune.Sixleg];
            for (int i = 0; i < run.Releases; i++) w.Do(new Release(rammers[(int)rng.Below(3)].Id, (int)rng.Below(Width * Height)));
            w.Run();
            string? broken = Broken(w);
            // Each creature's hits on its charge, counted from the tick it began charging.
            var hits = new Dictionary<int, int>();
            var charging = new HashSet<int>();
            // Each creature's wind-ups since it was last struck: one ram a hit, never a chain.
            var windUps = new Dictionary<int, int>();
            var winding = new HashSet<int>();
            for (int t = 0; t < run.Ticks && broken == null; t++)
            {
                var all = w.M.Creatures.Creatures;
                Strike? strike = null;
                if (all.Count > 0 && rng.Below(6) == 0)
                    w.Do(strike = new Strike(all[(int)rng.Below((uint)all.Count)].Id, Content.Weapons.Axe.Item) { From = (int)rng.Below((uint)heroes) });
                var sent = new List<HitBy>();
                foreach (var c in all)
                    for (int h = 0; h < heroes; h++)
                        if (c.Posture == Posture.Charge ? rng.Below(2) == 0 : rng.Below(50) == 0) sent.Add(new HitBy(c.Id) { From = h });
                foreach (var hit in sent) w.Do(hit);
                if (rng.Below(30) == 0) w.HeroAt(1 + (int)rng.Below(Width - 2), (int)rng.Below(Height), peer: (int)rng.Below((uint)heroes));
                int refused = w.Refusals.Count;
                w.Run();
                var turnedDown = w.Refusals.Skip(refused).Select(r => r.Command).ToHashSet(ReferenceEqualityComparer.Instance);
                foreach (var hit in sent)
                    if (!turnedDown.Contains(hit)) hits[hit.Creature] = hits.GetValueOrDefault(hit.Creature) + 1;
                if (strike != null && !turnedDown.Contains(strike)) windUps[strike.Creature] = 0;
                foreach (var c in w.M.Creatures.Creatures)
                {
                    if (c.Posture == Posture.WindUp && winding.Add(c.Id)) windUps[c.Id] = windUps.GetValueOrDefault(c.Id) + 1;
                    else if (c.Posture != Posture.WindUp) winding.Remove(c.Id);
                    if (windUps.GetValueOrDefault(c.Id) > 1) broken ??= $"creature {c.Id} wound up {windUps[c.Id]} times on one hit";
                    if (c.Posture == Posture.Charge && charging.Add(c.Id)) hits[c.Id] = 0;
                    else if (c.Posture != Posture.Charge) charging.Remove(c.Id);
                    if (hits.GetValueOrDefault(c.Id) > 1) broken ??= $"creature {c.Id}'s charge hurt {hits[c.Id]} times";
                }
                broken ??= Broken(w);
            }
            return (broken == null).Label(broken ?? "");
        });
}
