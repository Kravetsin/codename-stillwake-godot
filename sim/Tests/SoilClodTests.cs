using Stillwake.Sim.Crew;
using Stillwake.Sim.Debris;
using Xunit;
using DebrisSpec = Stillwake.Content.Debris;
using Loot = Stillwake.Content.Loot;
using Weapons = Stillwake.Content.Weapons;
using WreckageSpec = Stillwake.Content.Wreckage;

namespace Stillwake.Sim.Tests;

/// <summary>The soil clod: a frail wreck of the stream by the ship that breaks into seeds and a little soil, always.</summary>
public class SoilClodTests
{
    /// <summary>A soil clod out in space beside the hero, broken with the axe in <paramref name="seed"/>'s world.</summary>
    private static Scenario Broken(uint seed)
    {
        int hits = (WreckageSpec.HpOf("soilClod")!.Value + Weapons.Axe.Damage - 1) / Weapons.Axe.Damage;
        var w = new Scenario(DebrisZonesTests.Open(20, 8, 1, 1, w: 1, h: 1), seed).Run(DebrisSpec.Every);
        w.M.Debris.Pieces[0].Item = "soilClod";
        w.Do(new Enter(1 * 20 + 12)).Run();
        for (int i = 0; i < hits - 1; i++) w.Do(HewTesting.Hit(w, w.M.Debris.Pieces[0].Id, Weapons.Axe.Item));
        w.Run();
        Assert.Single(w.M.Debris.Pieces);
        Assert.Empty(w.M.Items.Floor);
        w.Do(HewTesting.Hit(w, w.M.Debris.Pieces[0].Id, Weapons.Axe.Item));
        return w.Run();
    }

    private static int Floating(Scenario w, string item) => w.M.Items.Floor.Where(s => s.Item == item).Sum(s => s.Count);

    [Fact]
    public void BreaksOnItsLastHitPointIntoOneToThreeSeedsAndOneOrTwoSoilAlikeInTwoWorldsOfOneSeed()
    {
        var seeds = new HashSet<int>();
        var soil = new HashSet<int>();
        for (uint seed = 1; seed <= 40; seed++)
        {
            var (a, b) = (Broken(seed), Broken(seed));
            Assert.Empty(a.Refusals);
            Assert.Empty(a.M.Debris.Pieces);
            Assert.Equal(a.World.Hash(), b.World.Hash());
            Assert.All(a.M.Items.Floor, s => Assert.Contains(s.Item, new[] { "seed", "soil" }));
            Assert.InRange(Floating(a, "seed"), 1, 3);
            Assert.InRange(Floating(a, "soil"), 1, 2);
            seeds.Add(Floating(a, "seed"));
            soil.Add(Floating(a, "soil"));
        }
        // Forty breaks draw every count.
        Assert.Equal([1, 2, 3], seeds.Order());
        Assert.Equal([1, 2], soil.Order());
        Assert.Equal(2, Loot.Of("soilClod")!.Count);
    }

    /// <summary>
    /// On the wreck, with no hero out, the stream (not the asteroid band) spawns some 350 pieces an hour (its beat of 160 to 240 ticks, a little
    /// fewer at its cap), 2 in 137 of them soil clods: about 5 an hour, one every 12 minutes. Four hours of four worlds
    /// stay within 3 to 8 an hour.
    /// </summary>
    [Fact]
    public void DriftsByInTheStreamNowAndThenAboutFiveAnHour()
    {
        const int Hour = 60 * 60 * 20;
        int clods = 0, spawns = 0;
        for (uint seed = 1; seed <= 4; seed++)
        {
            var w = new Scenario(Registry.Wreck, seed);
            int last = -1;
            for (int t = 0; t < Hour; t++)
            {
                w.Run();
                var pieces = w.M.Debris.Pieces;
                foreach (var p in pieces.Where(p => p.Id > last))
                {
                    if (p.Zone != DebrisSpec.Ship.Id) continue;
                    spawns += 1;
                    if (p.Item == "soilClod") clods += 1;
                }
                if (pieces.Count > 0) last = Math.Max(last, pieces[^1].Id);
            }
        }
        Assert.InRange(spawns / 4, 850, 950);
        Assert.InRange(clods, 3 * 4, 8 * 4);
    }
}
