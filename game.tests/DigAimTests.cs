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
        Assert.Null(Aiming.DigAt(m, 0, hero, new Point(31 * Unit + 500, 500), Axe));
    }

    [Fact]
    public void AStrikeDigsTheCellItIsAimedAtAndAPressDigsNothing()
    {
        var m = Shaft().M;
        var hero = InShaft;
        Assert.Equal(new Dig(At(3, 3), Axe), Aiming.DigAt(m, 0, hero, new Point(3 * Unit + 300, 3 * Unit + 700), Axe));
        // Open cells, hull and bedrock give nothing to send.
        Assert.Null(Aiming.DigAt(m, 0, hero, new Point(4 * Unit + 500, 2 * Unit + 500), Axe));
        Assert.Null(Aiming.DigAt(m, 0, hero with { Y = 5 * Unit }, new Point(4 * Unit + 500, 5 * Unit + 500), Axe));
        var aim = Aiming.AimAt(m, 0, hero, Centre(4, 3), axe: true);
        Assert.Empty(Aiming.UsesOf(aim, pressed: true, held: false, tick: 1, workedOnTick: -1).Commands);
        // On a ladder too: the hands dig as anywhere.
        Assert.Equal(new Dig(At(3, 3), Axe), Aiming.DigAt(m, 0, hero with { Climbing = true }, new Point(3 * Unit + 300, 3 * Unit + 700), Axe));
    }
}
