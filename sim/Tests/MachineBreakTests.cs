using Stillwake.Content;
using Stillwake.Sim.Building;
using Stillwake.Sim.Crafting;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Items;
using Stillwake.Sim.Power;
using Stillwake.Sim.Water;
using Xunit;
using Move = Stillwake.Sim.Items.Move;
using Slot = Stillwake.Sim.Items.Place;
using WaterContent = Stillwake.Content.Water;

namespace Stillwake.Sim.Tests;

/// <summary>
/// Machines break to bursts (unified-damage, damage-scale): against their <see cref="MachineSpec.HitPoints"/>, a pile its own, as a
/// wall; a player's own burst on their ship at its <see cref="Damage.OwnShipTakes"/> share. Broken, all it holds, its queue's refund and its
/// cost fall at its cell for anyone, its cables stay on a peg, and no module keeps anything of it. The lifepod never breaks.
/// </summary>
public class MachineBreakTests
{
    private static readonly int Charge = Blasting.Charge.Power;

    /// <summary>
    /// A room aboard walled in bedrock, so only its machines break, a cell a character: the machines of <paramref name="kinds"/> side
    /// by side along its floor, four cells apart, the first's left at x 2; its tanks filled if <paramref name="fill"/>.
    /// </summary>
    private static Scenario Room(string[] kinds, bool fill = false)
    {
        var specs = kinds.Select(Machines.Get).ToList();
        int tall = specs.Max(s => s.Height), wide = 2 + specs.Sum(s => s.Width + 4) + 2;
        var rows = new List<char[]> { new string('#', wide).ToCharArray(), ('#' + new string('.', wide - 2) + '#').ToCharArray() };
        for (int y = 0; y < tall; y++) rows.Add(('#' + new string('.', wide - 2) + '#').ToCharArray());
        rows.Add(new string('#', wide).ToCharArray());
        var under = rows.Select(r => new string(' ', r.Length).ToCharArray()).ToList();
        int x = 2;
        foreach (var spec in specs)
        {
            int top = 2 + tall - spec.Height;
            for (int dy = 0; dy < spec.Height; dy++)
                for (int dx = 0; dx < spec.Width; dx++)
                {
                    rows[top + dy][x + dx] = spec.Char!.Value;
                    if (fill && spec.Is(MachineTrait.Gas)) under[top + dy][x + dx] = 'o';
                }
            x += spec.Width + 4;
        }
        string[] Lines(List<char[]> r) => [.. r.Select(l => new string(l))];
        var rock = Lines(rows).Select(r => new string([.. r.Select(c => c == '#' ? 'b' : '.')])).ToList();
        return new Scenario(Layout.Parse(Lines(rows), fill ? Lines(under) : null).WithMaterials(rock));
    }

    private static Machine Of(Scenario w, string kind) => w.M.Grid.MachinesOf(kind).Single();

    /// <summary>The open cell right of <paramref name="m"/>'s bottom-right cell: a step from it.</summary>
    private static int Beside(Scenario w, Machine m)
    {
        var spec = Machines.Get(m.Kind);
        int width = w.M.Grid.Width;
        return (m.Cell / width + spec.Height - 1) * width + m.Cell % width + spec.Width;
    }

    private static bool Burst(Scenario w, int cell, int power) => w.M.Building.Burst(0, cell, power, Blasting.Charge.Reach, Spoils.Drop);

    /// <summary>A burst of no one's, in full aboard as a meteor's, its spoils dropped as a charge's.</summary>
    private static bool Full(Scenario w, int cell, int power) => w.M.Building.Burst(FloorStack.Nobody, cell, power, Blasting.Charge.Reach, Spoils.Drop);

    private static int CrackAt(Scenario w, int cell) => w.M.Building.Cracks.SingleOrDefault(c => c.Cell == cell && !c.Back)?.Damage ?? 0;

    private static int Lying(Scenario w, string item) => w.M.Items.Floor.Where(s => s.Item == item).Sum(s => s.Count);

    private static int Tenth(int left) => left * Damage.OwnShipTakes / 1000;

    private static int Slot(Scenario w, string item) => w.M.Items.Inventory(0).ToList().FindIndex(s => s?.Item == item);

    [Fact]
    public void AChargeAboardCracksABuiltMachineByATenthAFullBurstBreaksItItsCostOreAndModuleFallingThereForAnyone()
    {
        var w = Room(["crusher"]).GiveItems(0, ("arcModule", 1), ("asteroidOre", 4)).Run();
        var crusher = Of(w, "crusher");
        w.Do(new Fit(new Slot(Slot(w, "arcModule")), crusher.Id)).Run();
        w.Do(new Move(new Slot(Slot(w, "asteroidOre")), new Target(0, crusher.Id), 4)).Run();
        Assert.Empty(w.Refusals);
        Assert.True(Burst(w, Beside(w, crusher), Charge));
        // A scrap wall's hit points, so it cracks as one: a tenth of what was left after a step.
        Assert.Equal(Damage.MachineHp, Machines.Get("crusher").HitPoints);
        Assert.NotNull(w.M.Grid.MachineById(crusher.Id));
        Assert.Equal(Tenth(Charge - Damage.Step), CrackAt(w, crusher.Cell));
        Full(w, Beside(w, crusher), Charge);
        Assert.Null(w.M.Grid.MachineById(crusher.Id));
        Assert.Equal(0, CrackAt(w, crusher.Cell));
        var expected = Machines.Get("crusher").Cost.Append((Item: "asteroidOre", Count: 4)).Append((Item: "arcModule", Count: 1))
            .GroupBy(c => c.Item, StringComparer.Ordinal).ToDictionary(g => g.Key, g => g.Sum(c => c.Count));
        Assert.All(expected, e => Assert.Equal(e.Value, Lying(w, e.Key)));
        Assert.Equal(expected.Values.Sum(), w.M.Items.Floor.Sum(s => s.Count));
        Assert.All(w.M.Items.Floor, s => Assert.Equal(FloorStack.Nobody, s.By));
        Assert.All(w.M.Items.Floor, s => Assert.Equal(crusher.Cell, s.Cell));
        Assert.Empty(w.M.Items.Crate(crusher.Id));
        Assert.Empty(w.M.Items.FittedTo(crusher.Id));
    }

    [Fact]
    public void AStationsQueueIsRefundedAtItsCellWhatItMadeSpilledAndTheQueueForgotten()
    {
        var wire = Crafts.Get("wire").Cost.Single();
        var w = Room(["workbench"]).GiveItems(0, (wire.Item, 4 * wire.Count)).Run();
        var bench = Of(w, "workbench");
        // One wire made: its work at the bench's speed.
        w.Do(new Crafting.Craft(bench.Id, "wire", 4)).Run(Crafts.Get("wire").Ticks * 1000 / Stations.Get("workbench").Speed + 1);
        Assert.Empty(w.Refusals);
        int left = w.M.Crafting.Queue(bench.Id).Single().Count;
        int made = w.M.Items.Crate(bench.Id).Sum(s => s?.Item == "wire" ? s.Count : 0);
        Assert.Equal(4, left + made);
        Assert.True(made > 0 && left > 0);
        Full(w, Beside(w, bench), Charge);
        Assert.Null(w.M.Grid.MachineById(bench.Id));
        int cost = Machines.Get("workbench").Cost.Where(c => c.Item == wire.Item).Sum(c => c.Count);
        Assert.Equal(cost + left * wire.Count, Lying(w, wire.Item));
        Assert.Equal(made, Lying(w, "wire"));
        Assert.Empty(w.M.Crafting.Queue(bench.Id));
    }

    [Fact]
    public void ItsCableStaysOnAPegWhereItsPlugWasTheOtherEndStillWired()
    {
        var w = Room(["crusher", "crusher"]).GiveItems(0, ("wire", 10)).Run();
        var (left, right) = (w.M.Grid.MachinesOf("crusher")[0], w.M.Grid.MachinesOf("crusher")[1]);
        w.Do(new Lay(left.Cell, right.Cell)).Run();
        Assert.Empty(w.Refusals);
        Assert.Single(w.M.Power.Cables);
        // From its left, so the wave meets it before the other.
        int width = w.M.Grid.Width, from = left.Cell + 3 * width - 1;
        Full(w, from, Charge);
        Assert.Null(w.M.Grid.MachineById(left.Id));
        Assert.NotNull(w.M.Grid.MachineById(right.Id));
        var cable = Assert.Single(w.M.Power.Cables);
        Assert.True(cable.Paid);
        Assert.Contains(right.Cell, new[] { cable.A, cable.B });
        Assert.DoesNotContain(left.Cell, new[] { cable.A, cable.B });
        Assert.Equal(0, Lying(w, "wire"));
    }

    [Fact]
    public void TheLifepodNeverBreaksNorCracksAndABurstInsideItNeverGoesOff()
    {
        var w = Room(["lifepod"]);
        var pod = Of(w, "lifepod");
        Assert.Null(Machines.Get("lifepod").HitPoints);
        Assert.True(Burst(w, Beside(w, pod), 100_000));
        Assert.NotNull(w.M.Grid.MachineById(pod.Id));
        Assert.Equal(0, CrackAt(w, pod.Cell));
        Assert.False(Burst(w, pod.Cell, Charge));
    }

    [Fact]
    public void APileGoesToOneChargeAtItsOwnHitPointsItsCostFallingThereForAnyone()
    {
        var w = Room(["pileScrap"]);
        var pile = Of(w, "pileScrap");
        Burst(w, Beside(w, pile), Charge);
        Assert.Null(w.M.Grid.MachineById(pile.Id));
        Assert.All(Machines.Get("pileScrap").Cost, c => Assert.Equal(c.Count, Lying(w, c.Item)));
        Assert.All(w.M.Items.Floor, s => Assert.Equal(FloorStack.Nobody, s.By));
        // A faint one only cracks it, by a tenth of what was left: a pile aboard is of the ship too.
        var faint = Room(["pileScrap"]);
        var other = Of(faint, "pileScrap");
        Burst(faint, Beside(faint, other), Damage.Step + 30);
        Assert.Equal(3, CrackAt(faint, other.Cell));
    }

    [Fact]
    public void WhatTheLastBurstPaidForNeverMakesTheNextOnesFirstCellFree()
    {
        // A crusher broken, one built again on its cell: a burst from inside it then cracks it, as in the world loaded fresh.
        var w = Room(["crusher"]);
        var crusher = Of(w, "crusher");
        Full(w, Beside(w, crusher), Charge);
        w.Do(new Place(crusher.Cell, new Placement("machine", "crusher"))).Run();
        Assert.Empty(w.Refusals);
        var again = Of(w, "crusher");
        Assert.Equal(crusher.Cell, again.Cell);
        var loaded = Room(["crusher"]).Reload(w.World.Save());
        foreach (var world in new[] { w, loaded })
        {
            Full(world, again.Cell, 100);
            Assert.NotNull(world.M.Grid.MachineById(again.Id));
            Assert.Equal(100, CrackAt(world, again.Cell));
        }
    }

    [Fact]
    public void NoModuleKeepsAnythingOfABrokenCrateTankOrCistern()
    {
        var w = Room(["crate", "tank", "cistern"], fill: true).GiveItems(0, ("planks", 5), (WaterContent.Item, 1)).Run();
        var (crate, tank, cistern) = (Of(w, "crate"), Of(w, "tank"), Of(w, "cistern"));
        w.Do(new Move(new Slot(Slot(w, "planks")), new Target(0, crate.Id), 5)).Do(new PourWater(cistern.Id)).Run();
        Assert.Empty(w.Refusals);
        Assert.True(w.M.Atmosphere.OxygenIn(tank.Id) > 0);
        Assert.True(w.M.Water.WaterIn(cistern.Id) > 0);
        foreach (var m in new[] { crate, tank, cistern })
        {
            Full(w, Beside(w, m), Charge);
            Assert.Null(w.M.Grid.MachineById(m.Id));
        }
        w.Run();
        Assert.Empty(w.M.Items.Crate(crate.Id));
        Assert.Equal(0, w.M.Atmosphere.OxygenIn(tank.Id));
        Assert.Equal(0, w.M.Water.WaterIn(cistern.Id));
        int planks = Machines.Get("crate").Cost.Single(c => c.Item == "planks").Count;
        Assert.Equal(planks + 5, Lying(w, "planks"));
        // Saved and loaded, nothing of them comes back.
        w = w.Reload().Run();
        Assert.Equal(0, w.M.Atmosphere.OxygenIn(tank.Id) + w.M.Water.WaterIn(cistern.Id));
    }
}
