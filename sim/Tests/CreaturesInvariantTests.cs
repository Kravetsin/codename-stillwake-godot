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
            // A catcher leaving its hunt (tracker-catchers) flies off through anything.
            bool leaving = spec.Lifetime == Tune.Lifetime.Hunter && CreaturesModule.Hunting(c) == null;
            if (!leaving && w.M.Creatures.Overlaps(spec.Body, c.X, c.Y)) return $"creature {c.Id} in a wall at ({c.X}, {c.Y})";
            // A lurker on guard (cave-hostiles) watches with no count, alerted with its alert's. A catcher hunts with none.
            int least = c.Act == null && spec.Brain[c.Drive] is Tune.Guard or Tune.Hunt ? 0 : 1;
            if (c.Left < least || c.Left > LongestOf(c, spec)) return $"creature {c.Id} {c.Posture} with {c.Left} ticks left";
        }
        foreach (var hero in w.M.Crew.Heroes)
            if (hero.Air < 0 || hero.Air > Scenario.Full) return $"hero {hero.Id}'s suit at {hero.Air}";
        // A patch (lurker-attacks) lies on the grid, not empty, and no longer than its kind lasts.
        foreach (var p in w.M.Creatures.Patches)
            if (p.Ticks < 1 || p.Ticks > Tune.PatchOf(p.Kind).Lasts || p.Width < 1 || p.Height < 1 || p.Left < 0 || p.Top < 0
                || p.Left + p.Width > Width || p.Top + p.Height > Height)
                return $"patch {p.Id} of {p.Kind} at ({p.Left}, {p.Top}) {p.Width}×{p.Height} with {p.Ticks} ticks";
        return null;
    }

    /// <summary>The damage of each creature whose shell is shut (lurker-attacks), by id: none of it may grow while it is.</summary>
    private static Dictionary<int, int> Shut(Scenario w) =>
        w.M.Creatures.Creatures.Where(CreaturesModule.Shut).ToDictionary(c => c.Id, c => c.Damage);

    /// <summary>The most ticks <paramref name="c"/>'s posture lasts for a creature of <paramref name="spec"/>: what is left of it never exceeds it.</summary>
    private static int LongestOf(Creature c, Tune.Species spec) => c.Posture switch
    {
        Posture.Idle when c.Act == null && spec.Brain[c.Drive] is Tune.Guard guard => guard.AlertTicks,
        Posture.Idle => Tune.IdleMost,
        Posture.Wander => Tune.WanderMost,
        Posture.Flee => spec.Brain.OfType<Tune.Flee>().Single().Ticks,
        Posture.WindUp => spec.Moves[c.Act!.Move] switch { Tune.Lunge l => l.WindUp, Tune.Pounce p => p.WindUp, Tune.Shoot s => s.WindUp, _ => 0 },
        Posture.Charge => spec.Moves[c.Act!.Move] switch { Tune.Lunge l => l.Ticks, Tune.Pounce p => p.Ticks, Tune.Shoot s => s.Recover, Tune.Vent v => v.Ticks, _ => 0 },
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
                var shut = Shut(w);
                w.Run();
                broken = Broken(w);
                foreach (var c in w.M.Creatures.Creatures)
                    if (shut.TryGetValue(c.Id, out int was) && c.Damage > was) broken ??= $"creature {c.Id} hurt shut, {was} to {c.Damage}";
                if (NextId(w) < next || all.Count > 0 && all[^1].Id >= NextId(w)) broken ??= $"the next id {NextId(w)} after {next}";
                next = NextId(w);
            }
            return (broken == null).Label(broken ?? "");
        });

    private const int HillsWidth = 440, HillsHeight = 48;

    /// <summary>
    /// Hills a character a cell, wide enough for a herd on either side of a hero: each column's top a step off the last now and then,
    /// a cliff sometimes, so some herds find every place and some miss one. The tops, for heroes to stand on.
    /// </summary>
    private static (Layout Map, int[] Tops) Hills(int seed, char open = ' ')
    {
        var rng = new Rng(new RngState { S = (uint)seed });
        var tops = new int[HillsWidth];
        int top = 30;
        for (int x = 0; x < HillsWidth; x++)
        {
            if (rng.Below(5) == 0) top = Math.Clamp(top + (rng.Below(8) == 0 ? (int)rng.Below(17) - 8 : (int)rng.Below(5) - 2), 12, HillsHeight - 4);
            tops[x] = top;
        }
        var rows = Enumerable.Range(0, HillsHeight).Select(y => new string([.. tops.Select(t => y >= t ? '#' : open)]));
        return (Layout.Parse([.. rows]), tops);
    }

    /// <summary>
    /// Herds (creatures-in-herds), over hills with a hero who moves about and strikes now and then: whatever spawns on a tick is a
    /// herd, on the beat, of two to four of one kind, each a spacing further out than the last, and the surface never past its cap.
    /// </summary>
    [Property(MaxTest = 40)]
    public Property EverySpawnIsAHerdOfOneKindSpacedOutUnderTheCap() =>
        Prop.ForAll(Gen.Choose(0, int.MaxValue).ToArbitrary(), Gen.Choose(0, int.MaxValue).ToArbitrary(), (ground, seed) =>
        {
            var (map, tops) = Hills(ground);
            var w = CreaturesTesting.Planet(map, cells: true).Breathing();
            var rng = new Rng(new RngState { S = (uint)seed });
            void Move()
            {
                int x = (int)rng.Below(HillsWidth);
                w.HeroAt(x, tops[x] - 1);
            }
            Move();
            string? broken = null;
            int seen = 0;
            for (int t = 0; t < 60 * Tune.Beat && broken == null; t++)
            {
                var all = w.M.Creatures.Creatures;
                if (rng.Below(300) == 0) Move();
                if (all.Count > 0 && rng.Below(20) == 0) w.Do(new Strike(all[(int)rng.Below((uint)all.Count)].Id, Content.Weapons.Axe.Item));
                w.Run();
                var herd = all.Where(c => c.Id > seen).ToList();
                if (all.Count > Tune.Max) broken = $"{all.Count} creatures";
                if (herd.Count == 0) continue;
                seen = herd[^1].Id;
                string kinds = string.Join(",", herd.Select(c => c.Species));
                if (w.World.Tick % Tune.Beat != 0) broken ??= $"a herd off the beat at {w.World.Tick}";
                if (herd.Count < Tune.HerdLeast || herd.Count > Tune.HerdMost) broken ??= $"a herd of {herd.Count}: {kinds}";
                if (herd.Any(c => c.Species != herd[0].Species)) broken ??= $"a herd of kinds {kinds}";
                for (int i = 1; i < herd.Count; i++)
                    if (Math.Abs(herd[i].X - herd[i - 1].X) != Tune.Spacing * Unit) broken ??= $"a herd's members {Math.Abs(herd[i].X - herd[i - 1].X)} apart";
            }
            return (broken == null).Label(broken ?? "");
        });

    /// <summary>The first planet with a lurker in each of two cave bands, the first's bottom at row <see cref="BandEdge"/> of a test's map.</summary>
    private static readonly Content.PlanetType Banded = Content.PlanetTypes.First with
    {
        Bands = [Down(BandEdge), 600, 850],
        Creatures = [new(Tune.Capback.Id, Tune.Place.Band, 1, Band: 0), new(Tune.GeodeSnail.Id, Tune.Place.Band, 1, Band: 1)],
    };

    private const int BandEdge = 30;

    /// <summary>How far down the first planet's ground row <paramref name="row"/> is, in thousandths, as its bands are measured.</summary>
    private static int Down(int row)
    {
        var g = Content.PlanetTypes.First.Ground;
        return (int)((long)(row - g.Surface) * 1000 / Math.Max(1, g.Height - g.CoreRows - g.Surface));
    }

    /// <summary>
    /// Herds by depth band (cave-hostiles), over cave hills a band's bottom crosses, a hero moving about across it: every herd is of
    /// a kind of the hero's own band.
    /// </summary>
    [Property(MaxTest = 30)]
    public Property EveryHerdIsOfItsHerosBand() =>
        Prop.ForAll(Gen.Choose(0, int.MaxValue).ToArbitrary(), Gen.Choose(0, int.MaxValue).ToArbitrary(), (ground, seed) =>
        {
            var (map, tops) = Hills(ground, '.');
            var w = CreaturesTesting.Planet(map, Banded, cells: true).Breathing();
            var rng = new Rng(new RngState { S = (uint)seed });
            int row = 0;
            void Move()
            {
                int x = (int)rng.Below(HillsWidth);
                row = tops[x] - 1;
                w.HeroAt(x, row);
            }
            Move();
            string? broken = Terrain.BandOf(Banded, BandEdge - 1) == 0 && Terrain.BandOf(Banded, BandEdge) == 1 ? null : "the band's bottom is off";
            int seen = 0;
            for (int t = 0; t < 60 * Tune.Beat && broken == null; t++)
            {
                if (rng.Below(150) == 0) Move();
                w.Run();
                var herd = w.M.Creatures.Creatures.Where(c => c.Id > seen).ToList();
                if (herd.Count == 0) continue;
                seen = herd[^1].Id;
                int band = Banded.Creatures.First(h => h.Species == herd[0].Species).Band;
                if (band != Terrain.BandOf(Banded, row)) broken = $"a herd of {herd[0].Species} round a hero at row {row}";
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
