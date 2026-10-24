using System.Reflection;
using Stillwake.Content;
using Stillwake.Game;
using Xunit;
using DebrisSpec = Stillwake.Content.Debris;

namespace Stillwake.Sim.Tests;

/// <summary>Weapons, the harpoon, the grapple and the modules fitted into tools.</summary>
public partial class ContentTests
{
    [Fact]
    public void TheAxeIsAToolCraftedAtThePodKnownFromTheStartThatHitsAndSwingsInWholeUnits()
    {
        Assert.True(Weapons.Axe.Damage > 0);
        Assert.True(Weapons.Axe.Pace > 0);
        Assert.True(Content.Items.TryGet(Weapons.Axe.Item, out var axe));
        Assert.Equal(1, axe!.Stack);
        Assert.True(Crafts.TryGet(Weapons.Axe.Item, out _));
        Assert.Contains(Weapons.Axe.Item, Content.Research.StartKnown);
    }

    [Fact]
    public void EveryWeaponIsAToolOfOneCraftedHitsInWholeUnitsAndTheDrillsOutdamageTheAxeOnTheBelt()
    {
        Assert.Equal(Weapons.All.Count, Weapons.All.Select(w => w.Item).Distinct().Count());
        foreach (var weapon in Weapons.All)
        {
            Assert.True(weapon.Damage > 0 && weapon.Pace > 0, weapon.Item);
            Assert.Equal(1, Content.Items.Get(weapon.Item).Stack);
            // Made at a station: nothing is only found (0098).
            Assert.True(Crafts.TryGet(weapon.Item, out _), weapon.Item);
            Assert.Same(weapon, Weapons.Of(weapon.Item));
        }
        Assert.Null(Weapons.Of(Harpoon.Item));
        // Damage a second is all that sets them apart (0054): the drill 10, the axe 4.
        Assert.Equal(10, Weapons.Drill.Damage * 20 / Weapons.Drill.Pace);
        Assert.Equal(4, Weapons.Axe.Damage * 20 / Weapons.Axe.Pace);
        // Electric: it draws every tick it runs (0055), so a full battery runs it 600 ticks, 30 s.
        int draw = Content.Items.Get(Weapons.Drill.Item).Draw!.Value;
        Assert.Equal(3, draw);
        Assert.Equal(600, Charge.Battery / Charge.Scale / draw);
        // The arc module in it (0098): twice the damage, 20 a second, for twice the draw, 300 ticks of a battery.
        Assert.Equal((20, 6), (Sim.Items.Boost.Gain(Weapons.Drill.Damage, Modules.Arc.Adds(Weapons.Drill.Item, Stat.Damage)) * 20 / Weapons.Drill.Pace, Sim.Items.Boost.Cost(draw, Modules.Arc.Adds(Weapons.Drill.Item, Stat.Draw))));
        Assert.Null(Content.Items.Get(Weapons.Axe.Item).Draw);
        Assert.Equal([("frame", 1), ("rotor", 1), ("coil", 1), ("gear", 2), ("tube", 1), ("circuit", 2)], Crafts.Get(Weapons.Drill.Item).Cost);
        Assert.Equal(160, Crafts.Get(Weapons.Drill.Item).Ticks);
        Assert.DoesNotContain(Weapons.Drill.Item, Content.Research.StartKnown);
    }

    [Fact]
    public void TheHarpoonShootsFasterThanItReelsAndStopsAPieceWithinTheHandsReachInsideItsRange()
    {
        AllPositive(typeof(Harpoon));
        Assert.True(Harpoon.ReelSpeed < Harpoon.RodSpeed);
        Assert.True(Harpoon.RodSpeed < Harpoon.Range);
        Assert.True(Harpoon.Stop < Hero.Reach);
        Assert.True(Harpoon.Stop < Harpoon.Range);
        Assert.True(Harpoon.RodSpeed < Harpoon.ReturnSpeed);
    }

    [Fact]
    public void AReelFollowsAHeroThatMovesFartherThanThePieceIsHeldBeforeTheGunNoMoreOftenThanItTakesToArrive()
    {
        AllPositive(typeof(Harpoon));
        Assert.True(Harpoon.FollowBeyond < Harpoon.Stop);
        Assert.True(Harpoon.FollowTicks < Harpoon.CooldownTicks);
    }

    [Fact]
    public void TheGrapplePullsTheHeroFasterThanItWalksAndStopsItWithinTheHandsReachOfTheHook()
    {
        // Per tick against the hero's per step: 20 ticks a second, 60 steps.
        Assert.True(Harpoon.PullSpeed > Hero.Walk * 3);
        Assert.True(Harpoon.PullStop < Hero.Reach);
        Assert.True(Harpoon.PullStop < Harpoon.Stop);
        Assert.True(Harpoon.PullSpeed < Harpoon.RodSpeed);
    }

    [Fact]
    public void TheHarpoonIsAToolOfOneCraftedAtThePodAndOpenedByAnalysingWhatItIsMadeOf()
    {
        Assert.True(Content.Items.TryGet(Harpoon.Item, out var harpoon));
        Assert.Equal(1, harpoon!.Stack);
        Assert.True(Crafts.TryGet(Harpoon.Item, out var craft));
        Assert.Equal([("tube", 1), ("coil", 1), ("wire", 4)], craft!.Cost);
        Assert.DoesNotContain(Harpoon.Item, Content.Research.StartKnown);
    }

    /// <summary>
    /// A module (0086) is an item of its own, a stack of one kept in death, made at a station and never found: no wreck,
    /// container or growth gives one. It fits only what draws power, for it multiplies that draw: electric stations, the
    /// charger, electric tools; never the furnace's planks nor the pod, nor the drone bay yet. Every host it names exists, in one
    /// of its entries at most, and each trade there means something to it (0100): speed a station's, damage a weapon's, heat a
    /// fed station's. What it adds is never negative, in whole thousandths.
    /// </summary>
    [Fact]
    public void ModulesAreGearMadeNeverLootFittingOnlyWhatDrawsPowerEachTradeMeaningSomethingThere()
    {
        Assert.Equal(Modules.All.Count, Modules.All.Select(m => m.Item).Distinct().Count());
        var given = DebrisSpec.Zones.SelectMany(z => z.Tiers).SelectMany(t => t.Kinds).SelectMany(k => Loot.Of(k.Item) ?? []).Select(e => e.Item)
            .Concat(Flora.Every.SelectMany(k => k.Yields).Select(y => y.Item)).ToHashSet();
        foreach (var module in Modules.All)
        {
            var item = Content.Items.Get(module.Item);
            Assert.True(item is { Stack: 1, Gear: true, Capacity: null, Equip: null }, module.Item);
            Assert.True(Crafts.TryGet(module.Item, out _), module.Item);
            Assert.DoesNotContain(module.Item, given);
            Assert.NotEmpty(module.On);
            Assert.False(string.IsNullOrEmpty(module.Tech), module.Item);
            Assert.Equal(module.Hosts.Count(), module.Hosts.Distinct().Count());
            foreach (var on in module.On)
            {
                Assert.NotEmpty(on.Hosts);
                Assert.NotEmpty(on.Effects);
                foreach (var host in on.Hosts)
                {
                    // Its hosts draw power: electric machines, by kind, or electric weapons, by item (0098).
                    bool weapon = Weapons.Of(host) != null;
                    if (weapon)
                    {
                        Assert.True(Content.Items.Get(host) is { Draw: not null, Stack: 1 }, $"{module.Item} fits {host}");
                        Assert.False(Machines.TryGet(host, out _), host);
                    }
                    else
                    {
                        Assert.True(Machines.TryGet(host, out var machine) && machine is { Draws: not null, Makes: null, Fixed: false, Tunes: false }, $"{module.Item} fits {host}");
                    }
                    bool station = Stations.TryGet(host, out var spec);
                    foreach (var trade in on.Effects.Cast<Trade>())
                    {
                        Assert.True(trade.Add >= 0 && trade.Add % (trade.Stat == Stat.Heat ? 1 : 1000) == 0, $"{module.Item} in {host}: {trade}");
                        Assert.True(trade.Stat switch
                        {
                            Stat.Speed => station,
                            Stat.Damage => weapon,
                            Stat.Heat => station && spec!.Fed,
                            _ => true,
                        }, $"{module.Item} in {host}: {trade.Stat} means nothing there");
                    }
                }
            }
        }
        Assert.DoesNotContain(Modules.All, m => m.Hosts.Contains("furnace") || m.Hosts.Contains("lifepod") || m.Hosts.Contains(Content.Logistics.Bay));
    }

    /// <summary>
    /// The arc module's numbers, per host (designer-approved, 0100): twice the speed for twice the power in the electric stations,
    /// twice the damage for twice the draw in the drill, five times the draw, and so the charging, in the charger. Its charger fills
    /// a battery in 6 s instead of 30, an accumulator in 18 s instead of 90.
    /// </summary>
    [Fact]
    public void TheArcModuleTradesPerHostAsTheDesignerSet()
    {
        (int, int, int, int) Adds(string host) =>
            (Modules.Arc.Adds(host, Stat.Speed), Modules.Arc.Adds(host, Stat.Draw), Modules.Arc.Adds(host, Stat.Damage), Modules.Arc.Adds(host, Stat.Heat));
        Assert.Equal(["smelter", "former", "assembler", "drill", "charger"], Modules.Arc.Hosts);
        foreach (var station in new[] { "smelter", "former", "assembler" }) Assert.Equal((1000, 1000, 0, 0), Adds(station));
        Assert.Equal((0, 1000, 1000, 0), Adds(Weapons.Drill.Item));
        Assert.Equal((0, 4000, 0, 0), Adds("charger"));
        Assert.Equal((0, 0, 0, 0), Adds(Content.Logistics.Bay));
        int draws = Machines.Get("charger").Draws!.Value, fitted = Sim.Items.Boost.Cost(draws, Modules.Arc.Adds("charger", Stat.Draw));
        Assert.Equal((3, 15), (draws, fitted));
        Assert.Equal((30, 6), (Charge.Battery / Charge.Scale / draws / World.TicksPerSecond, Charge.Battery / Charge.Scale / fitted / World.TicksPerSecond));
        Assert.Equal((90, 18), (Charge.Accumulator / Charge.Scale / draws / World.TicksPerSecond, Charge.Accumulator / Charge.Scale / fitted / World.TicksPerSecond));
    }
}
