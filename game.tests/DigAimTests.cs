using Stillwake.Game.Features.Crew;
using Stillwake.Sim;
using Stillwake.Sim.Building;
using Xunit;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Tests;

/// <summary>Digging aimed (0077): the axe or the drill at natural ground under the pointer, in reach, wherever the hero stands.</summary>
public class DigAimTests
{
    private const int Unit = Hero.Unit;
    private static readonly string Axe = Spec.Weapons.Axe.Item, Drill = Spec.Weapons.Drill.Item;

    /// <summary>A shaft down through soil over stone, bedrock at its foot: a room, not space, so no wreck could be aimed at here. A character a cell: digging is per cell; wide past the reach.</summary>
    private static TestWorld Shaft() => new(ShaftLayout);

    private static Layout ShaftLayout => Layout.Parse("""
        ################################
        ###...##########################
        ###...##########################
        ################################
        ################################
        ################################
        """).WithMaterials("""
        ssssssssssssssssssssssssssssssss
        sss...ssssssssssssssssssssssssss
        sss...ssssssssssssssssssssssssss
        ssssssssssssssssssssssssssssssss
        tttttttttttttttttttttttttttttttt
        bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb
        """);

    private static int At(int x, int y) => y * 32 + x;

    /// <summary>A hero standing on the shaft's floor, its feet on row 3.</summary>
    private static Hero InShaft => new(4 * Unit + Unit / 2, 3 * Unit, 0, 0, false, 1);

    private static (double X, double Y) Centre(int x, int y) => (x + 0.5, y + 0.5);

    [Fact]
    public void TheAxeOrTheDrillAimsAtTheGroundUnderThePointerInAShaftAndSaysDig()
    {
        var m = Shaft().M;
        var aim = Aiming.AimAt(m, 0, InShaft, Centre(4, 3), axe: true);
        Assert.Equal(new Aim(At(4, 3), true, new Use(new Dig(At(4, 3), Axe), null)), aim);
        Assert.Equal(new Prompt("use.dig", null), Prompts.Of(m, aim));
        // A drill with no battery is refused flat before its run is asked.
        Assert.Equal(new Use(new Dig(At(5, 3), Drill), "items.flat"), Aiming.AimAt(m, 0, InShaft, Centre(5, 3), drill: Drill)?.Press);
        // Bare hands dig nothing.
        Assert.Null(Aiming.AimAt(m, 0, InShaft, Centre(4, 3))?.Press);
    }

    [Fact]
    public void TheAxeAimsAtAPlantStandingInTheOpenToFellItAndAtNothingWhereNoneStands()
    {
        // A flower on the shaft's floor (0079).
        var m = new TestWorld(ShaftLayout with { Flora = [(At(4, 2), "bellTeal")] }).M;
        Assert.Equal(new Use(new Dig(At(4, 2), Axe), null), Aiming.AimAt(m, 0, InShaft, Centre(4, 2), axe: true)?.Press);
        Assert.Null(Aiming.AimAt(Shaft().M, 0, InShaft, Centre(4, 2), axe: true)?.Press);
    }

    [Fact]
    public void BedrockSaysTooHardAndGroundOutOfReachIsAimedAtButNotDug()
    {
        var m = Shaft().M;
        var hero = InShaft;
        Assert.Equal(new Prompt("use.dig", "building.bedrock"), Prompts.Of(m, Aiming.AimAt(m, 0, hero with { Y = 5 * Unit }, Centre(4, 5), axe: true)));
        var far = Aiming.AimAt(m, 0, hero, Centre(31, 0), axe: true);
        Assert.False(far!.InReach);
        Assert.Null(Prompts.Of(m, far));
        Assert.Null(One(m, 0, hero, new Point(31 * Unit + 500, 500)));
    }

    [Fact]
    public void AStrikeDigsTheCellItIsAimedAtAndAPressDigsNothing()
    {
        var m = Shaft().M;
        var hero = InShaft;
        Assert.Equal(new Dig(At(3, 3), Axe), One(m, 0, hero, new Point(3 * Unit + 300, 3 * Unit + 700)));
        // Open cells, hull and bedrock give nothing to send.
        Assert.Null(One(m, 0, hero, new Point(4 * Unit + 500, 2 * Unit + 500)));
        Assert.Null(One(m, 0, hero with { Y = 5 * Unit }, new Point(4 * Unit + 500, 5 * Unit + 500)));
        var aim = Aiming.AimAt(m, 0, hero, Centre(4, 3), axe: true);
        Assert.Empty(Aiming.UsesOf(aim, pressed: true, held: false, tick: 1, workedOnTick: -1).Commands);
        // On a ladder too: the hands dig as anywhere.
        Assert.Equal(new Dig(At(3, 3), Axe), One(m, 0, hero with { Climbing = true }, new Point(3 * Unit + 300, 3 * Unit + 700)));
    }

    /// <summary>The one dig a precise strike at <paramref name="at"/> sends, if any.</summary>
    private static Dig? One(Registry m, int peer, Hero hero, Point at) => Aiming.DigsAt(m, peer, hero, at, Axe, brush: 1).SingleOrDefault();

    private static Point P(double x, double y) => Aiming.PointOf((x, y));

    [Fact]
    public void AStrikeDigsEveryCellOfTheBrushRoundTheNearestCornerAscendingEachAlone()
    {
        var m = Shaft().M;
        // The corner (6, 4): two soil cells over two of stone, each its own dig.
        Assert.Equal([new Dig(At(5, 3), Axe), new Dig(At(6, 3), Axe), new Dig(At(5, 4), Axe), new Dig(At(6, 4), Axe)],
            Aiming.DigsAt(m, 0, InShaft, P(5.9, 3.9), Axe, brush: 2));
        // The corner (5, 3): the shaft's floor under two open cells; (4, 3): the floor's other two.
        Assert.Equal([new Dig(At(4, 3), Axe), new Dig(At(5, 3), Axe)], Aiming.DigsAt(m, 0, InShaft, P(4.9, 3.1), Axe, brush: 2));
        Assert.Equal([new Dig(At(3, 3), Axe), new Dig(At(4, 3), Axe)], Aiming.DigsAt(m, 0, InShaft, P(4.2, 3.4), Axe, brush: 2));
        // The shaft's side, two rows of it.
        Assert.Equal([new Dig(At(6, 1), Axe), new Dig(At(6, 2), Axe)], Aiming.DigsAt(m, 0, InShaft, P(5.8, 2.2), Axe, brush: 2));
    }

    [Fact]
    public void ACellTheSimWouldRefuseOrOutOfReachIsLeftOutOfTheBrush()
    {
        var m = Shaft().M;
        // Stone over bedrock: the bedrock is not sent.
        Assert.Equal([new Dig(At(4, 4), Axe), new Dig(At(5, 4), Axe)], Aiming.DigsAt(m, 0, InShaft with { Y = 4 * Unit }, P(4.9, 4.9), Axe, brush: 2));
        // At the edge of reach, only the near cells.
        var far = InShaft with { X = 9 * Unit + Unit / 2 };
        var cut = Aiming.DigsAt(m, 0, far, P(15, 1), Axe, brush: 2);
        Assert.All(cut, d => Assert.True(Aiming.Reaches(far, d.Cell, 16)));
        Assert.True(cut.Count < 4);
    }

    [Fact]
    public void PreciseStrikesTheCellUnderThePointerOnly() =>
        Assert.Equal([new Dig(At(5, 3), Axe)], Aiming.DigsAt(Shaft().M, 0, InShaft, P(5.9, 3.9), Axe, brush: 1));

    [Fact]
    public void APlantInSeveralCellsOfTheBrushTakesOneDigSoItFallsAsFastAsBefore()
    {
        // Threads hanging two cells from the shaft's ceiling, down column 4.
        var m = new TestWorld(ShaftLayout with { Flora = [(At(4, 1), "threadTeal")] }).M;
        Assert.Equal(m.Building.PlantAt(At(4, 1)), m.Building.PlantAt(At(4, 2)));
        Assert.Equal([new Dig(At(4, 1), Axe)], Aiming.DigsAt(m, 0, InShaft, P(4.9, 1.9), Axe, brush: 2));
    }

    [Fact]
    public void TheAimTakesTheBrushsSquareAndGroundAnywhereInIt()
    {
        var m = Shaft().M;
        // The pointer's own cell open, the floor under it in the brush: aimed at the floor, the square outlined.
        var aim = Aiming.AimAt(m, 0, InShaft, (4.9, 2.9), axe: true, brush: 2);
        Assert.Equal(new Aim(At(4, 3), true, new Use(new Dig(At(4, 3), Axe), null), Square: (4, 2, 2)), aim);
        // Precise, the same point aims at the open cell and digs nothing.
        Assert.Null(Aiming.AimAt(m, 0, InShaft, (4.9, 2.9), axe: true, brush: 1)?.Press);
        // The pointer's cell first when it is ground.
        Assert.Equal(At(5, 3), Aiming.AimAt(m, 0, InShaft, (5.1, 3.1), axe: true, brush: 2)?.Cell);
        // No ground in the square: no dig.
        Assert.Null(Aiming.AimAt(m, 0, InShaft, (4.9, 1.9), axe: true, brush: 2)?.Press);
    }

    [Fact]
    public void AStrikeIsHeardAtTheDigInThePointersCellElseTheFirst()
    {
        Dig a = new(At(4, 3), Axe), b = new(At(5, 3), Axe);
        Assert.Equal(b, Aiming.LeadOf([a, b], P(5.2, 3.5), 16));
        Assert.Equal(a, Aiming.LeadOf([a, b], P(5.2, 2.5), 16));
        Assert.Null(Aiming.LeadOf([], P(5.2, 2.5), 16));
    }
}
