using Stillwake.Content;
using Stillwake.Sim.Building;
using Stillwake.Sim.Creatures;
using Stillwake.Sim.Grid;
using Xunit;

namespace Stillwake.Sim.Tests;

/// <summary>
/// One damage rule (hit-targets): every hitter strikes what it reaches, a station and a door as a wall; a player's hit on their own
/// ship takes <see cref="Damage.OwnShipTakes"/>, everything else in full; the lifepod never breaks.
/// </summary>
public class OneDamageRuleTests
{
    private static readonly Weapon Pick = Weapons.Pickaxe;
    private const int Unit = CreaturesModule.Unit;
    private const int Right = 0;

    private static int Tenth(int hit) => hit * Damage.OwnShipTakes / 1000;

    private static int At(Scenario w, int x, int y) => y * w.M.Grid.Width + x;

    private static int CrackAt(Scenario w, int cell) => w.M.Building.Cracks.SingleOrDefault(c => c.Cell == cell && !c.Back)?.Damage ?? 0;

    private static int Held(Scenario w, string item) => w.M.Items.Totals(0).Where(t => t.Item == item).Sum(t => t.Count);

    private const string Room = """
        ..........
        .FFFF.DD.#
        .FFFF.DD.#
        .FFFF....#
        .FFFF....#
        ##########
        """;

    private static Machine Furnace(Scenario w) => w.M.Grid.MachinesOf("furnace").Single();

    [Fact]
    public void AboardAPickaxeTakesATenthOffAStationADoorAndAWall()
    {
        var w = new Scenario(Layout.Parse(Room));
        var furnace = Furnace(w);
        w.Do(new HewCells(Pick.Item, new List<int> { At(w, 3, 3), At(w, 7, 2), At(w, 9, 2) })).Run();
        Assert.Empty(w.Refusals);
        Assert.Equal(Tenth(Pick.BlockDamage), CrackAt(w, furnace.Cell));
        Assert.Equal(Tenth(Pick.BlockDamage), CrackAt(w, At(w, 6, 1)));
        Assert.Equal(Tenth(Pick.BlockDamage), CrackAt(w, At(w, 9, 2)));
    }

    [Fact]
    public void OffTheShipAStationTakesInFullBreakingInSixPickaxeBlowsItsCostToTheHitter()
    {
        var w = new Scenario(Layout.Parse(Room).WithMaterials(Room.Replace('#', 's').Replace('F', '.').Replace('D', '.')).OnPlanet());
        var furnace = Furnace(w);
        int blows = (Damage.MachineHp + Pick.BlockDamage - 1) / Pick.BlockDamage;
        Assert.Equal(6, blows);
        for (int i = 1; i < blows; i++)
        {
            w.Do(new HewCells(Pick.Item, new List<int> { furnace.Cell })).Run();
            Assert.Equal(i * Pick.BlockDamage, CrackAt(w, furnace.Cell));
        }
        w.Do(new HewCells(Pick.Item, new List<int> { furnace.Cell })).Run();
        Assert.Empty(w.Refusals);
        Assert.Null(w.M.Grid.MachineById(furnace.Id));
        Assert.Equal(0, CrackAt(w, furnace.Cell));
        Assert.All(Machines.Get("furnace").Cost, c => Assert.Equal(c.Count, Held(w, c.Item)));
    }

    [Fact]
    public void AboardOneSwingStrikesTheStationAndTheWallBehindItOnceEach()
    {
        var w = new Scenario(Layout.Parse("""
            ........
            .FFFF#..
            .FFFF#..
            .FFFF#..
            .FFFF#..
            ########
            """));
        w.Do(new Hew(Pick.Item, 1500, 2500, Right)).Run();
        Assert.Empty(w.Refusals);
        Assert.Equal(Tenth(Pick.BlockDamage), CrackAt(w, Furnace(w).Cell));
        Assert.Equal(Tenth(Pick.BlockDamage), CrackAt(w, At(w, 5, 2)));
    }

    [Fact]
    public void TheLifepodTakesNothing()
    {
        var w = new Scenario(Layout.Parse("""
            ........
            .OOOOOO.
            .OOOOOO.
            .OOOOOO.
            .OOOOOO.
            .OOOOOO.
            .OOOOOO.
            ########
            """));
        var pod = w.M.Grid.MachinesOf("lifepod").Single();
        w.Do(new HewCells(Pick.Item, new List<int> { pod.Cell })).Run();
        Assert.Equal("building.nothingToDig", Assert.Single(w.Refusals).Reason);
        Assert.NotNull(w.M.Grid.MachineById(pod.Id));
        Assert.Empty(w.M.Building.Cracks);
    }

    /// <summary>The first planet, a cell a character, the hero at column 2 with <paramref name="gun"/>, nails and a full battery; a furnace standing at columns 30 to 33.</summary>
    private static Scenario Range(Gun gun)
    {
        var rows = Enumerable.Range(0, 20).Select(y => string.Concat(Enumerable.Range(0, 60).Select(x => y >= 16 && x is >= 30 and <= 33 ? 'F' : '.')))
            .Concat(Enumerable.Repeat(new string('#', 60), 3)).ToList();
        var materials = rows.Select(r => string.Concat(r.Select(c => c == '#' ? 's' : '.')));
        var layout = Layout.Parse(string.Join('\n', rows)).WithMaterials(string.Join('\n', materials)) with { Kind = PlanetTypes.First.Id };
        return CreaturesTesting.Planet(layout, CreaturesTesting.Lifeless, cells: true).Do(new Crew.Enter(19 * 60 + 2))
            .GiveItems(0, (gun.Item, 1), ("nail", 10)).Put(Inventory.Slots + 2, new Items.Stack("battery", 1, Charge.Battery)).Run();
    }

    private static Fire Along(Gun gun) => new(gun.Item, 25 * Unit, 17 * Unit + Unit / 2, Right);

    [Fact]
    public void ARailgunBreaksAStationInThreeShotsTheLastCarryingOnTheNailerEndingInIt()
    {
        var w = Range(Shots.Railgun);
        var furnace = Furnace(w);
        int block = Shots.Railgun.BlockDamage + Shots.Nail.BlockDamage;
        for (int shot = 1; shot <= 2; shot++)
        {
            var (hits, end) = PierceTests.Flown(w, Along(Shots.Railgun));
            Assert.Equal([(ShotTarget.Machine, block, false)], hits.Select(h => (h.What, h.Damage, h.Broke)));
            Assert.True(end.Met);
            Assert.Equal(shot * block, CrackAt(w, furnace.Cell));
            w.Run(Shots.Railgun.Pace);
        }
        var (last, _) = PierceTests.Flown(w, Along(Shots.Railgun));
        Assert.Equal((ShotTarget.Machine, block, true), (last[0].What, last[0].Damage, last[0].Broke));
        Assert.Null(w.M.Grid.MachineById(furnace.Id));

        var nailed = Range(Shots.Nailer);
        var (nails, stop) = PierceTests.Flown(nailed, Along(Shots.Nailer));
        Assert.Equal([(ShotTarget.Machine, Shots.Nailer.BlockDamage, false)], nails.Select(h => (h.What, h.Damage, h.Broke)));
        Assert.Equal((30, true), (stop.Cell % 60, stop.Met));
        Assert.Equal(Shots.Nailer.BlockDamage, CrackAt(nailed, Furnace(nailed).Cell));
    }
}
