using Stillwake.Content;
using Stillwake.Game.Dev;
using Stillwake.Game.Features.Wrecked;
using Stillwake.Sim;
using Xunit;
using V = Stillwake.Content.Vessels;

namespace Stillwake.Game.Tests;

/// <summary>What still moves on a wreck (vessel-wrecks W-3): motes by event and age, drifters, sparks, vents, glows, culling.</summary>
public class WreckLifeTests
{
    private static WreckLife Life(string cls, uint seed, string chain, IReadOnlyList<(double, double, double)>? lights = null)
    {
        var built = Shipyard.Build(V.Class(cls)!, seed, WreckChain.Parse(chain));
        var scene = new WreckScene(built);
        var ends = new List<WreckLooks.End>();
        WreckLooks.Of(scene, 0xaeb6b7, _ => null, ends);
        return WreckLife.Of(scene, Shipyard.Build(V.Class(cls)!, seed, []), ends, lights ?? []);
    }

    private static int Count(WreckLife l, Mote k) => l.Motes[k].Count;

    [Fact]
    public void AnExplosionLeavesAshEmbersAndATrailAFireAshAndEmbersInItsRoomsAnAsteroidDustIceAndStones()
    {
        var blast = Life("liner", 7, "blast");
        Assert.True(Count(blast, Mote.Ash) > 500, $"{Count(blast, Mote.Ash)} ash");
        Assert.True(Count(blast, Mote.AshLight) > 100);
        Assert.True(Count(blast, Mote.Ember) > 20);
        Assert.Contains(blast.Drifters, d => d.Kind is DriftKind.Shard or DriftKind.Bit);
        Assert.NotEmpty(blast.Sparkers);
        var fire = Life("liner", 7, "fire");
        Assert.True(Count(fire, Mote.Ash) > 50);
        Assert.Contains(fire.Glows, g => g.Light);
        Assert.Empty(fire.Drifters);
        var rock = Life("liner", 7, "rock");
        Assert.True(Count(rock, Mote.Dust) + Count(rock, Mote.DustLit) > 500);
        Assert.True(Count(rock, Mote.RockDust) > 100);
        Assert.True(Count(rock, Mote.Ice) > 200, $"{Count(rock, Mote.Ice)} ice: a strike freezes its breach");
        Assert.Contains(rock.Drifters, d => d.Kind == DriftKind.Rock);
        var overload = Life("tanker", 7, "break");
        Assert.True(Count(overload, Mote.Rivet) > 20);
        Assert.True(Count(overload, Mote.Fuel) > 200);
        Assert.Contains(overload.Vents, v => v.Kind == Mote.Fuel);
        var power = Life("liner", 7, "ice");
        Assert.True(Count(power, Mote.Ice) > 100);
        Assert.Contains(power.Vents, v => v.Kind == Mote.Ice);
    }

    [Fact]
    public void AnOldGroupKeepsASixthOfItsMotesNoEmbersItsIceDullAThirdOfItsDriftersAndNoSparksVentsOrGlows()
    {
        var fresh = Life("liner", 7, "blast,rock");
        var old = Life("liner", 7, "blast:old,rock:old");
        double ash = (double)Count(old, Mote.Ash) / Count(fresh, Mote.Ash);
        Assert.InRange(ash, WreckLife.OldMotes - 0.06, WreckLife.OldMotes + 0.06);
        Assert.Equal(0, Count(old, Mote.Ember));
        Assert.Equal(0, Count(old, Mote.Ice));
        Assert.True(Count(old, Mote.IceDull) > 0);
        double drifters = (double)old.Drifters.Count / fresh.Drifters.Count;
        Assert.InRange(drifters, WreckLife.OldDrifters - 0.12, WreckLife.OldDrifters + 0.12);
        Assert.Empty(old.Sparkers);
        Assert.Empty(old.Vents);
        Assert.Empty(old.Glows);
    }

    [Fact]
    public void TheSameWreckMovesTheSameEachRun()
    {
        var a = Life("liner", 7, "blast,fire,rock");
        var b = Life("liner", 7, "blast,fire,rock");
        foreach (var k in Enum.GetValues<Mote>()) Assert.Equal(a.Motes[k], b.Motes[k]);
        Assert.Equal(a.Drifters.Select(d => (d.Kind, d.X, d.Y, d.Turn)), b.Drifters.Select(d => (d.Kind, d.X, d.Y, d.Turn)));
        Assert.Equal(a.Sparkers.Select(s => (s.X, s.Y, s.Id)), b.Sparkers.Select(s => (s.X, s.Y, s.Id)));
        var s0 = a.Sparkers[0];
        Assert.Equal(s0.At(1.3).Streaks.Count, b.Sparkers[0].At(1.3).Streaks.Count);
        Assert.Equal(Vent.Crystals, a.Vents.Concat(Life("liner", 7, "ice").Vents).First().At(2).Count());
    }

    [Fact]
    public void DustALightFallsOnShowsWarm()
    {
        var dark = Life("liner", 7, "rock");
        var p = dark.Motes[Mote.Dust][0];
        var lit = Life("liner", 7, "rock", [(p.X, p.Y, 6.0)]);
        Assert.True(Count(lit, Mote.DustLit) > 0);
        Assert.Equal(Count(dark, Mote.Dust) + Count(dark, Mote.DustLit), Count(lit, Mote.Dust) + Count(lit, Mote.DustLit));
        Assert.All(lit.Motes[Mote.DustLit], d => Assert.True(Math.Sqrt((d.X - p.X) * (d.X - p.X) + (d.Y - p.Y) * (d.Y - p.Y)) < 6 * 0.9 + 1e-3));
    }

    [Fact]
    public void MotesAreChunkedWhereTheyHangAndOnlyWhatIsInViewIsPicked()
    {
        var life = Life("liner", 7, "blast,fire,rock");
        foreach (var k in Enum.GetValues<Mote>())
            foreach (var (chunk, specks) in life.Chunks(k))
                Assert.All(specks, s => Assert.Equal(chunk, WreckPaint.ChunkOf((int)Math.Floor(s.X), (int)Math.Floor(s.Y))));
        var view = (X0: 0.0, Y0: 0.0, X1: 40.0, Y1: 30.0);
        var picked = WreckLife.InView(life.Drifters, d => (d.X, d.Y), view, 2).ToList();
        Assert.All(picked, d => Assert.True(d.X >= -2 && d.X <= 42 && d.Y >= -2 && d.Y <= 32));
        Assert.Equal(life.Drifters.Count(d => d.X >= -2 && d.X <= 42 && d.Y >= -2 && d.Y <= 32), picked.Count);
        Assert.True(picked.Count < life.Drifters.Count);
    }

    [Fact]
    public void AWrecksOwnSootAndFrostStandForTheDerelictsRandomOnes()
    {
        int wrecked = 0, intact = 0;
        for (uint seed = 1; seed <= 30; seed++)
        {
            var b = Shipyard.Build(V.Class("liner")!, seed, WreckChain.Parse("blast,fire"));
            wrecked += new Features.Grid.VesselSigns(b).Signs.Count(x => x.Kind is Features.Grid.SignKind.Soot or Features.Grid.SignKind.Frost);
            intact += new Features.Grid.VesselSigns(Shipyard.Build(V.Class("liner")!, seed)).Signs.Count(x => x.Kind is Features.Grid.SignKind.Soot or Features.Grid.SignKind.Frost);
        }
        Assert.Equal(0, wrecked);
        Assert.True(intact > 0);
    }
}
