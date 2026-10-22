using Stillwake.Sim.Furnace;
using Stillwake.Sim.Items;
using Xunit;
using FurnaceContent = Stillwake.Content.Furnace;
using Machines = Stillwake.Content.Machines;

namespace Stillwake.Sim.Tests;

/// <summary>The burner generator: the furnace's fire, turning a dynamo (the crafting plan, task 2).</summary>
public class BurnerGeneratorTests
{
    /// <summary>
    /// Fourteen wide: a sealed room aired by a tank piped to a vent, as the furnace's tests; a burner
    /// generator cabled to a lamp, and with <paramref name="two"/> a second generator cabled to the first.
    /// </summary>
    private static Layout Map(bool two = false)
    {
        string second = two ? "BB" : "..";
        return Layout.Parse(
            ["##############", $"#BB.TT..{second}.L.#", $"#BB.TTV.{second}...#", "##############"],
            ["", "    o", "    ---"],
            two ? [((1, 1), (11, 1)), ((1, 1), (8, 1))] : [((1, 1), (11, 1))]);
    }

    private static int At(int x, int y) => y * 14 + x;
    private static readonly int Generator = At(1, 1), Second = At(8, 1), Lamp = At(11, 1);
    private static readonly int Makes = Machines.Get("burnerGenerator").Makes!.Value;
    private static readonly int Fuel = Machines.Get("burnerGenerator").FuelSlot!.Value;
    private static readonly int PlankTicks = Content.Items.Get("planks").BurnTicks!.Value;

    /// <summary>The world, its room aired, a peer 0 carrying <paramref name="planks"/> planks.</summary>
    private static Scenario SetUp(bool two = false, int planks = 4)
    {
        var w = new Scenario(Map(two)).GiveItems(0, ("planks", planks));
        for (int i = 0; i < 2000 && !w.M.Atmosphere.AiredAt(Generator); i++) w.Run();
        Assert.True(w.M.Atmosphere.AiredAt(Generator));
        return w;
    }

    private static int IdAt(Scenario w, int cell) => w.M.Grid.MachineAt(cell)?.Id ?? 0;

    /// <summary>Moves <paramref name="count"/> planks from peer 0 into the fuel slot of the generator at <paramref name="cell"/>.</summary>
    private static Scenario Feed(Scenario w, int count, int? cell = null)
    {
        int from = w.M.Items.Inventory(0).ToList().FindIndex(s => s?.Item == "planks");
        return w.Do(new Move(new Items.Place(from), new Target(Fuel, IdAt(w, cell ?? Generator)), count)).Run();
    }

    private static int PlanksIn(Scenario w, int cell) => w.M.Items.Crate(IdAt(w, cell))[Fuel]?.Count ?? 0;
    private static int LampPower(Scenario w) => w.M.Power.Satisfaction(IdAt(w, Lamp));
    private static int Supply(Scenario w) => w.M.Power.NetworkOf(Generator)!.Supply;

    [Fact]
    public void BurnsBiofuelFourTimesAsLongAsAPlank()
    {
        Assert.Equal(4 * PlankTicks, Content.Items.Get("biofuel").BurnTicks);
        var w = new Scenario(Map()).GiveItems(0, ("biofuel", 1));
        for (int i = 0; i < 2000 && !w.M.Atmosphere.AiredAt(Generator); i++) w.Run();
        int from = w.M.Items.Inventory(0).ToList().FindIndex(s => s?.Item == "biofuel");
        w.Do(new Move(new Items.Place(from), new Target(Fuel, IdAt(w, Generator)), 1)).Run(2);
        Assert.Equal(Makes, Supply(w));
        w.Run(4 * PlankTicks - 4);
        Assert.Equal(Makes, Supply(w));
        w.Run(10);
        Assert.Equal(0, Supply(w));
    }

    [Fact]
    public void MakesNothingWithAnEmptySlotAndSaysItHasNoFuel()
    {
        var w = SetUp();
        w.Run(20);
        Assert.Equal(new Smelting(false, Problem: "furnace.noFuel"), w.M.Furnace.At(IdAt(w, Generator)));
        Assert.Equal(0, Supply(w));
        Assert.Equal(0, LampPower(w));
    }

    [Fact]
    public void BurnsAPlankWithAirAndPowersALampFromTheNextTick()
    {
        var w = SetUp();
        Feed(w, 2);
        // Lit on the tick of the move, one plank used up; power reads it a tick late.
        Assert.Equal(new Smelting(true), w.M.Furnace.At(IdAt(w, Generator)));
        Assert.Equal(1, PlanksIn(w, Generator));
        Assert.Equal(0, LampPower(w));
        w.Run();
        Assert.Equal(Makes, Supply(w));
        Assert.Equal(1000, LampPower(w));
    }

    [Fact]
    public void BurnsAPlankForAsLongAsTheFurnaceDoesLightingTheNextAtOnce()
    {
        var w = SetUp();
        Feed(w, 2);
        // The move's tick was the first of the plank's fire.
        w.Run(PlankTicks - 1);
        Assert.Equal(0, PlanksIn(w, Generator));
        for (int i = 0; i < PlankTicks; i++)
        {
            Assert.True(w.M.Furnace.At(IdAt(w, Generator)).Burning);
            Assert.Equal(1000, LampPower(w));
            w.Run();
        }
        Assert.False(w.M.Furnace.At(IdAt(w, Generator)).Burning);
        w.Run();
        Assert.Equal(0, LampPower(w));
    }

    [Fact]
    public void BreathesItsRoomsAirAsTheFurnaceDoes()
    {
        var w = SetUp();
        int tank = w.M.Grid.MachineAt(At(4, 1))!.Id;
        var cold = SetUp();
        Feed(w, 1);
        cold.Run();
        w.Run(4 * FurnaceContent.BreathEvery - 1);
        cold.Run(4 * FurnaceContent.BreathEvery - 1);
        Assert.Equal(4 * FurnaceContent.Breath, cold.M.Atmosphere.OxygenIn(tank) - w.M.Atmosphere.OxygenIn(tank));
    }

    [Fact]
    public void GoesOutWithoutAirItsFuelLostAndTheLampDark()
    {
        var w = SetUp();
        Feed(w, 2).Run();
        Assert.Equal(1000, LampPower(w));
        // A hole in the hull: the room is space, and the fire's next breath finds none.
        w.Do(new ClearAt(At(13, 1), "structure")).Run();
        w.Run(FurnaceContent.BreathEvery);
        Assert.Equal(new Smelting(false, Problem: "furnace.noAir"), w.M.Furnace.At(IdAt(w, Generator)));
        w.Run();
        Assert.Equal(0, Supply(w));
        Assert.Equal(0, LampPower(w));
        // Unlit for want of air, the plank left stays in its slot.
        w.Run(100);
        Assert.Equal(1, PlanksIn(w, Generator));
    }

    [Fact]
    public void TwoGeneratorsOnOneNetworkAddUp()
    {
        // 0101: a fire weighs only what panels make against the draw, so two burn together and never hand over a tick dark.
        var w = SetUp(two: true);
        Feed(w, 1);
        Feed(w, 1, Second).Run();
        Assert.Equal(2 * Makes, Supply(w));
        Assert.Equal(1000, LampPower(w));
    }

    [Fact]
    public void MakesPowerBeforeABackWallNeedingNoStars()
    {
        // In a room with its back wall, where a solar panel would make nothing.
        var w = SetUp();
        Assert.NotNull(w.M.Grid.BackAt(Generator));
        Feed(w, 1).Run();
        Assert.Equal(Makes, Supply(w));
    }

    [Fact]
    public void KeepsBurningAcrossASaveAndALoad()
    {
        var w = SetUp();
        Feed(w, 1).Run(10);
        w.Reload().Run();
        Assert.True(w.M.Furnace.At(IdAt(w, Generator)).Burning);
        Assert.Equal(1000, LampPower(w));
    }
}
