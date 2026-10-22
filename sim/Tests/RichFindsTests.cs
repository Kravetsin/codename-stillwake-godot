using Stillwake.Sim.Crew;
using Stillwake.Sim.Debris;
using Xunit;
using DebrisSpec = Stillwake.Content.Debris;
using HullBand = Stillwake.Content.HullBand;
using Loot = Stillwake.Content.Loot;
using Weapons = Stillwake.Content.Weapons;
using WreckageSpec = Stillwake.Content.Wreckage;

namespace Stillwake.Sim.Tests;

/// <summary>Rich finds far out (0056): the asteroid's kinds, and the derelicts only in the far field's outer tiers.</summary>
public class RichFindsTests
{
    /// <summary>The big wrecks found only far out.</summary>
    public static readonly HashSet<string> Derelicts = ["engineBlock", "cargoHold", "podCluster"];
    private static readonly HashSet<string> Rocks = ["asteroidSmall", "asteroid", "asteroidLarge", "asteroidRich"];
    private static readonly HullBand Band = (HullBand)DebrisSpec.Asteroids.Anchor;

    private static IReadOnlyList<Piece> Pieces(Scenario w) => w.M.Debris.Pieces;

    /// <summary>Every piece spawned over <paramref name="ticks"/> ticks, as it was when it spawned: its zone, kind and cell.</summary>
    private static List<(string Zone, string Item, int X, int Y)> Spawned(Scenario w, int ticks)
    {
        var seen = new List<(string, string, int, int)>();
        int last = -1;
        for (int i = 0; i < ticks; i++)
        {
            w.Run();
            foreach (var p in Pieces(w))
                if (p.Id > last) seen.Add((p.Zone, p.Item, p.X / 1000, p.Y / 1000));
            if (Pieces(w).Count > 0) last = Math.Max(last, Pieces(w)[^1].Id);
        }
        return seen;
    }

    [Fact]
    public void SpawnsDerelictsOnlyInTheFarFieldsOuterTiersThePodsAndTheRichRockOnlyPast512()
    {
        // 2800×1200, the hull at columns 100 to 111 and rows 600 to 605; two heroes whose rings lie mostly
        // in the 256 tier, two far past 512. Lengths twice the 16 px cell's (0104), as the tiers' are.
        var map = DebrisZonesTests.Open(2800, 1200, 100, 600, 12, 6);
        var hull = (Left: 100, Right: 111, Top: 600, Bottom: 605);
        var spawned = new List<(string Zone, string Item, int X, int Y)>();
        for (uint seed = 1; seed <= 6; seed++)
        {
            var w = new Scenario(map, seed);
            (int X, int Y)[] heroes = [(480, 600), (480, 760), (1400, 600), (1400, 240)];
            for (int i = 0; i < heroes.Length; i++) w.Do(new Enter(heroes[i].Y * map.Width + heroes[i].X) { From = i });
            spawned.AddRange(Spawned(w, 600));
        }
        int Tier((string Zone, string Item, int X, int Y) s) => DebrisModule.TierOf(DebrisSpec.Far, s.X, s.Y, hull).From;
        var far = spawned.Where(s => s.Zone == DebrisSpec.Far.Id).ToList();
        Assert.True(far.Count > 2000, $"{far.Count} far pieces spawned");
        var derelicts = far.Where(s => Derelicts.Contains(s.Item)).ToList();
        Assert.All(derelicts, s => Assert.True(Tier(s) >= 256, $"{s.Item} at ({s.X}, {s.Y}), tier {Tier(s)}"));
        Assert.All(far.Where(s => s.Item is "podCluster" || Rocks.Contains(s.Item)), s => Assert.True(Tier(s) >= 512, $"{s.Item} in tier {Tier(s)}"));
        Assert.Contains(derelicts, s => Tier(s) == 256);
        Assert.Contains(derelicts, s => Tier(s) == 512);
        Assert.Contains(far, s => s.Item == "asteroidRich");
        // Rare: a few in a hundred even out past 512.
        double share = derelicts.Count / (double)far.Count;
        Assert.InRange(share, 0.005, 0.05);
        // Nothing but the stream's own kinds by the ship.
        Assert.All(spawned.Where(s => s.Zone == DebrisSpec.Ship.Id), s => Assert.DoesNotContain(s.Item, Derelicts.Concat(Rocks)));
    }

    [Fact]
    public void NeverSpawnsADerelictInTheStreamNorAnyRockButAsteroidsInTheBand()
    {
        // 400×240, the hull at columns 180 to 191 and rows 100 to 105, no hero out: the stream and the band only.
        var spawned = new List<(string Zone, string Item, int X, int Y)>();
        for (uint seed = 1; seed <= 4; seed++) spawned.AddRange(Spawned(new Scenario(DebrisZonesTests.Open(400, 240, 180, 100, 12, 6), seed), 60_000));
        var stream = spawned.Where(s => s.Zone == DebrisSpec.Ship.Id).ToList();
        var band = spawned.Where(s => s.Zone == DebrisSpec.Asteroids.Id).ToList();
        Assert.True(stream.Count > 1000, $"{stream.Count} stream pieces");
        Assert.True(band.Count > 30, $"{band.Count} asteroids");
        Assert.DoesNotContain(stream, s => Derelicts.Contains(s.Item) || Rocks.Contains(s.Item));
        Assert.All(band, s => Assert.Contains(s.Item, Rocks));
        Assert.DoesNotContain(spawned, s => s.Zone == DebrisSpec.Far.Id);
    }

    [Fact]
    public void PutsTheRichRockOnlyOnTheBandsFarEdgeRows72To88Out()
    {
        var hull = (Left: 180, Right: 191, Top: 100, Bottom: 105);
        int column = hull.Right + Band.Lead;
        // The band's tier by row, every piece of it spawning Lead columns right of the hull.
        for (int out_ = Band.From; out_ <= Band.To; out_++)
        {
            int expected = out_ >= 72 ? 144 : 0;
            Assert.Equal(expected, DebrisModule.TierOf(DebrisSpec.Asteroids, column, hull.Top - out_, hull).From);
            Assert.Equal(expected, DebrisModule.TierOf(DebrisSpec.Asteroids, column, hull.Bottom + out_, hull).From);
        }
        var near = DebrisSpec.Asteroids.Tiers[0].Kinds.Select(k => k.Item).ToList();
        Assert.DoesNotContain("asteroidRich", near);
        // Over many asteroids, the rich ones all came from the far edge.
        var band = new List<(string Zone, string Item, int X, int Y)>();
        for (uint seed = 1; seed <= 4; seed++)
            band.AddRange(Spawned(new Scenario(DebrisZonesTests.Open(400, 240, 180, 100, 12, 6), seed), 60_000).Where(s => s.Zone == DebrisSpec.Asteroids.Id));
        int Out(int row) => row < hull.Top ? hull.Top - row : row - hull.Bottom;
        Assert.All(band.Where(s => s.Item == "asteroidRich"), s => Assert.InRange(Out(s.Y), 72, 88));
        Assert.Contains(band, s => Out(s.Y) < 72);
        Assert.Contains(band, s => Out(s.Y) >= 72);
    }

    /// <summary>
    /// Every new kind broken with the axe in many worlds: what falls is only its loot's items, each within its
    /// range, and alike in two worlds of one seed.
    /// </summary>
    [Theory]
    [InlineData("asteroidSmall")]
    [InlineData("asteroidLarge")]
    [InlineData("asteroidRich")]
    [InlineData("engineBlock")]
    [InlineData("cargoHold")]
    [InlineData("podCluster")]
    public void BreaksEachNewKindOnItsLastHitPointIntoItsLootWithinItsRanges(string kind)
    {
        int hp = WreckageSpec.HpOf(kind)!.Value, damage = Weapons.Axe.Damage, hits = (hp + damage - 1) / damage;
        var loot = Loot.Of(kind)!;
        Scenario Broken(uint seed)
        {
            var w = new Scenario(DebrisZonesTests.Open(20, 8, 1, 1, w: 1, h: 1), seed).Run(DebrisSpec.Every);
            Pieces(w)[0].Item = kind;
            w.Do(new Enter(1 * 20 + 12)).Run();
            for (int i = 0; i < hits - 1; i++) w.Do(new Hit(Pieces(w)[0].Id, Weapons.Axe.Item));
            w.Run();
            Assert.Single(Pieces(w));
            Assert.Empty(w.M.Items.Floor);
            w.Do(new Hit(Pieces(w)[0].Id, Weapons.Axe.Item));
            return w.Run();
        }
        var seen = new HashSet<string>();
        for (uint seed = 1; seed <= 40; seed++)
        {
            var (a, b) = (Broken(seed), Broken(seed));
            Assert.Empty(a.Refusals);
            Assert.Empty(Pieces(a));
            Assert.Equal(a.World.Hash(), b.World.Hash());
            Assert.All(a.M.Items.Floor, s => Assert.Contains(s.Item, loot.Select(e => e.Item)));
            foreach (var entry in loot)
            {
                int count = a.M.Items.Floor.Where(s => s.Item == entry.Item).Sum(s => s.Count);
                Assert.InRange(count, entry.OneIn == null ? entry.Min : 0, entry.Max);
                if (count > 0) seen.Add(entry.Item);
            }
        }
        // Forty breaks show every entry, the one-in rolls too.
        Assert.Equal(loot.Select(e => e.Item).ToHashSet(), seen);
    }
}
