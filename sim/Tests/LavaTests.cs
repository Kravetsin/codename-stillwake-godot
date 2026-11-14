using Stillwake.Sim.Building;
using Stillwake.Sim.Crew;
using Xunit;
using LavaSpec = Stillwake.Content.Lava;

namespace Stillwake.Sim.Tests;

/// <summary>The planet's core (Content.Lava): lava over the bedrock burns a hero touching it and is never dug.</summary>
public class LavaTests
{
    /// <summary>A pocket of airless ground: a stone ledge at the left, lava along the rest of row 4, bedrock under it all.</summary>
    private const string Map = """
        S......
        .......
        .......
        .......
        #######
        #######
        """;

    private const string Ground = """
        .......
        .......
        .......
        .......
        tllllll
        bbbbbbb
        """;

    /// <summary>The map is 7 characters wide, doubled (0104): a character is 2×2 cells.</summary>
    private const int Width = 2 * 7;
    /// <summary>The top-left cell of the character at (<paramref name="x"/>, <paramref name="y"/>).</summary>
    private static int At(int x, int y) => 2 * y * Width + 2 * x;

    /// <summary>Where a hero standing on the lava at column 4 is reported: its body's middle, Lava.Reach rows over the lava's top.</summary>
    private static readonly int OnLava = At(4, 2);

    private static Scenario World(bool planet = false, bool bare = false) =>
        new(planet ? Layout.Parse(Map).WithMaterials(Ground).OnPlanet() : Layout.Parse(Map).WithMaterials(Ground), doubled: true) { Bare = bare };
    private static Hero HeroOf(Scenario w) => w.M.Crew.HeroOf(0)!;

    /// <summary>A world with hero 0 brought in at <paramref name="cell"/> with a full suit on a breath's tick, run through it.</summary>
    private static Scenario BroughtIn(int cell, bool bare = false)
    {
        var w = World(bare: bare);
        while (w.Crew().Clock % Content.Crew.BreathEvery != Content.Crew.BreathEvery - 1) w.Run();
        // Brought in on a breath's tick, before the crew's system runs: that breath is the first it pays, and a whole breath later the next.
        w.Do(new Enter(cell) { From = 0 }).Run();
        return w;
    }

    [Fact]
    public void BurnsAHeroInItOrStandingOnItNotOneAboveItOrBeside()
    {
        var crew = World().M.Crew;
        // In the lava, a row over it, standing on it: the feet two rows under the reported middle.
        Assert.True(crew.Burns(At(4, 4)));
        Assert.True(crew.Burns(At(4, 3)));
        Assert.True(crew.Burns(OnLava));
        // A jump's height over it, on the stone ledge beside it, under it in the bedrock, off the grid: nothing.
        Assert.False(crew.Burns(At(4, 1)));
        Assert.False(crew.Burns(At(0, 2)));
        Assert.False(crew.Burns(At(4, 5)));
        Assert.False(crew.Burns(-1));
        Assert.False(crew.Burns(At(0, 6)));
    }

    [Fact]
    public void BareTheLavaBurnsTheSameAndTheColdBesidesSoTheLungsAndTheLastBreathGoOnTheSecondBreath()
    {
        var w = BroughtIn(OnLava, bare: true);
        int cold = TemperatureTests.ColdOn(w.Crew().Clock, w.M.Crew.Felt(0, OnLava));
        Assert.Equal(Content.Crew.Lungs - 1 - LavaSpec.Burn - cold, HeroOf(w).Air);
        w.Run(Content.Crew.BreathEvery);
        // What the burn takes past the lungs cuts the last breath, more units than it has breaths (2026-11-05-the-last-breath).
        Assert.True((LavaSpec.Burn - 1) * Content.Crew.LastBreathPerUnit > Content.Crew.LastBreath);
        Assert.Equal((0, 0, Content.Crew.PassOut), (HeroOf(w).Air, HeroOf(w).Gasp, HeroOf(w).Out));
    }

    [Fact]
    public void EachBreathOnLavaIsCountedAsABurn()
    {
        var w = World();
        while (w.Crew().Clock % Content.Crew.BreathEvery != Content.Crew.BreathEvery - 1) w.Run();
        w.Do(new Enter(OnLava) { From = 0 }).Run();
        Assert.Equal((1, Harm.Burn), (HeroOf(w).Hurts, HeroOf(w).Hurt));
        w.Run(Content.Crew.BreathEvery);
        Assert.Equal(2, HeroOf(w).Hurts);
    }

    [Fact]
    public void ACapeSparesNoneOfTheLavasBurnItIsHeatNotCold()
    {
        var w = World().Wear(0, Content.Cape.Item);
        while (w.Crew().Clock % Content.Crew.BreathEvery != Content.Crew.BreathEvery - 1) w.Run();
        w.Do(new Enter(OnLava) { From = 0 }).Run();
        Assert.Equal((Scenario.Full - 1 - LavaSpec.Burn, 0), (HeroOf(w).Air, HeroOf(w).Spared));
    }

    [Fact]
    public void StandingOnLavaEmptiesAFullSuitOnTheThirdBreathBurnsTheLastOnTheFourthAndTheHeroWakesAtTheSpawn()
    {
        // A third of a suit a breath, the breath itself besides: 45, 29, 13, gone within three seconds of ticks, and
        // the last breath burnt at the next.
        Assert.Equal(3 * LavaSpec.Burn, Scenario.Full);
        var w = BroughtIn(OnLava);
        Assert.Equal(Scenario.Full - 1 - LavaSpec.Burn, HeroOf(w).Air);
        w.Run(Content.Crew.BreathEvery);
        Assert.Equal(Scenario.Full - 2 * (1 + LavaSpec.Burn), HeroOf(w).Air);
        Assert.Equal(0, HeroOf(w).Out);
        w.Run(Content.Crew.BreathEvery);
        Assert.Equal(0, HeroOf(w).Air);
        // 13 less the breath leaves 12 for a burn of 15: the 3 past it cut the last breath.
        Assert.Equal((Content.Crew.LastBreath - 3 * Content.Crew.LastBreathPerUnit, 0), (HeroOf(w).Gasp, HeroOf(w).Out));
        w.Run(Content.Crew.BreathEvery);
        Assert.Equal(Content.Crew.PassOut, HeroOf(w).Out);
        // Passing out is death as anywhere: it wakes at the spawn with a full suit.
        w.Run(Content.Crew.PassOut);
        Assert.Equal((w.M.Crew.Spawn, Scenario.Full, 0), (HeroOf(w).Cell, HeroOf(w).Air, HeroOf(w).Out));
    }

    [Fact]
    public void AFullSuitAndItsLastBreathAreGoneWithinFourSecondsOfTheHerosReportWheneverItLands()
    {
        // Reported on any tick of a breath's beat, it passes out after three whole breaths and at most one more beat: the fourth
        // burns its last breath.
        for (int lag = 0; lag < Content.Crew.BreathEvery; lag++)
        {
            var w = World().Run(lag);
            w.Do(new Enter(OnLava) { From = 0 });
            int ticks = 0;
            while (HeroOf(w)?.Out is null or 0 && ticks < 10 * Content.Crew.BreathEvery)
            {
                w.Run();
                ticks++;
            }
            Assert.InRange(ticks, 3 * Content.Crew.BreathEvery + 1, 4 * Content.Crew.BreathEvery);
        }
    }

    [Fact]
    public void BesideOrAboveTheLavaAHeroBreathesItsSuitAsAnywhere()
    {
        foreach (int cell in new[] { At(0, 2), At(4, 1) })
        {
            var w = BroughtIn(cell);
            w.Run(4 * Content.Crew.BreathEvery);
            Assert.Equal((Scenario.Full - 5, 0), (HeroOf(w).Air, HeroOf(w).Out));
        }
    }

    [Fact]
    public void CreativeModeKeepsTheSuitFullOnLava()
    {
        var w = World();
        w.Do(new Items.SetCreative(true)).Do(new Enter(OnLava) { From = 0 }).Run(5 * Content.Crew.BreathEvery);
        Assert.Equal((Scenario.Full, 0), (HeroOf(w).Air, HeroOf(w).Out));
    }

    [Fact]
    public void LavaIsTooHardToDigAndStays()
    {
        var w = World(planet: true);
        w.Do(HewTesting.Dig(w, At(3, 4), Content.Weapons.Axe.Item)).Run();
        // It stops every ray unhurt, as bedrock does: the fan strikes nothing.
        Assert.Equal(["building.nothingToDig"], w.Refusals.Select(r => r.Reason));
        Assert.Equal(LavaSpec.Material, w.M.Grid.WallAt(At(3, 4))?.Id);
        Assert.Empty(w.M.Building.Cracks);
    }
}
