using System.Linq;
using Stillwake.Game.Features.Effects;
using Stillwake.Game.Render;
using Stillwake.Sim;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>The air an action pushes stirs its surroundings, not the tool (air-gusts): the floor's dust, a strong push's grit.</summary>
public class GustsTests
{
    /// <summary>A room five cells high under a stone ceiling, its floor soil to the left, stone to the right.</summary>
    private static readonly Layout Room = Layout.Parse("""
        ################
        ................
        ................
        ................
        ................
        ################
        """).WithMaterials("""
        tttttttttttttttt
        ................
        ................
        ................
        ................
        sssssssstttttttt
        """);

    private static readonly Registry M = new TestWorld(Room).M;

    private static (Stage Stage, int Thrown) Blow(Gust gust, bool airless = false)
    {
        var stage = new Stage(0);
        return (stage, Gusts.Throw(stage, M.Grid, _ => airless, _ => false, gust));
    }

    private static Glim[] Dust(Stage stage) => stage.Glims.Live.Where(g => g.Look.Kind == Glimmer.Mote).ToArray();

    /// <summary>What it blew off the floor: dust that settles on it.</summary>
    private static Glim[] FloorDust(Stage stage) => Dust(stage).Where(g => g.Floor != null).ToArray();

    [Fact]
    public void AStrokeLowOverTheFloorRaisesItsDustAndOneHighInTheAirNothing()
    {
        Assert.NotEmpty(FloorDust(Blow(new Gust(4, 4.5, 0, 1, 0.3, 3.5)).Stage));
        // Felt down to the floor as if from half as high, 1.9 cells: under the ceiling a gust felt 1.5 cells round raises nothing off it.
        Assert.Empty(FloorDust(Blow(new Gust(4, 1.2, 0, -1, 0.3, 1.5)).Stage));
    }

    [Fact]
    public void ItsDustIsTheColourOfTheGroundItLiesOn()
    {
        static (int R, int G, int B) RampOf(Stage stage) => Dust(stage)[0].Look.Ramp[0];
        var soil = RampOf(Blow(new Gust(2, 4.5, 0, 1, 0.5, 2)).Stage);
        var stone = RampOf(Blow(new Gust(13, 4.5, 0, 1, 0.5, 2)).Stage);
        Assert.NotEqual(soil, stone);
        // Soil is brown: more red in it than blue; stone the other way.
        Assert.True(soil.R > soil.B);
        Assert.True(stone.B > stone.R);
    }

    [Fact]
    public void ItsDustBlowsOutAwayFromWhereItStruck()
    {
        var dust = Dust(Blow(new Gust(8, 4.6, 0, 0, 1, 5)).Stage);
        double Mean(System.Func<Glim, bool> where) => dust.Where(where).Average(g => g.Vx);
        Assert.True(Mean(g => g.X < 8 * Camera.Cell - Camera.Cell) < 0);
        Assert.True(Mean(g => g.X > 8 * Camera.Cell + Camera.Cell) > 0);
    }

    [Fact]
    public void AGustWithAWayIsFeltMostAlongIt()
    {
        var toward = new Gust(8, 4, 1, 0, 1, 4);
        Assert.True(Gusts.Felt(toward, 10, 4) > Gusts.Felt(toward, 6, 4));
        Assert.Equal(0, Gusts.Felt(toward, 12.5, 4));
    }

    [Fact]
    public void NoAirCarriesNoDust()
    {
        Assert.Empty(Dust(Blow(new Gust(4, 4.5, 0, 1, 1, 4), airless: true).Stage));
    }

    [Fact]
    public void OnlyAStrongGustShakesGritOffTheCeiling()
    {
        static int Grit(double strength)
        {
            int most = 0;
            // Grit falls by chance where it is felt enough: the most of a few tries.
            for (int i = 0; i < 6; i++)
                most = System.Math.Max(most, Blow(new Gust(8, 2, 0, 0, strength, 7)).Stage.Glims.Live.Count(g => g.Look.Kind == Glimmer.Chip));
            return most;
        }
        Assert.Equal(0, Grit(Gusts.Shakes - 0.05));
        Assert.True(Grit(1) > 0);
    }
}
