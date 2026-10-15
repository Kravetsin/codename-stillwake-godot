using Stillwake.Game.Features.Light;
using Stillwake.Sim.Grid;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>What the lightmap's shader reads: where light stops, and each light it draws.</summary>
public class LightSourcesTests
{
    private const int Cell = 16;

    private static LightSource Lamp(double x, double y, float strength = 1) => new(x, y, (1.5f, 1.3f, 0.95f), strength, 16, 0.88f, 1);

    [Fact]
    public void StopsLightAtWallsAndClosedDoorsAndLetsItThroughOpenDoorsAndTheOpen()
    {
        int[] structure = [Structure.Wall, Structure.DoorClosed, Structure.DoorOpen, Structure.Floor];
        var cells = new byte[4];
        Sources.Occupy(structure, cells);
        Assert.Equal(new[] { Sources.Wall, Sources.Door, Sources.Open, Sources.Open }, cells);
    }

    [Fact]
    public void DrawsALightWhoseReachTouchesTheViewThoughItStandsOffItAndNoneBeyond()
    {
        var view = (0.0, 0.0, 320.0, 160.0);
        float[] buffer = [];
        // Ten cells right of the view, a sixteen-cell reach still lights its edge; twenty cells off, not.
        int n = Sources.Pack([Lamp(320 + 10 * Cell, 80), Lamp(320 + 20 * Cell, 80), Lamp(100, 80, strength: 0)], view, Cell, ref buffer);
        Assert.Equal(1, n);
        Assert.Equal(16f * Cell, buffer[0]);
        Assert.Equal(16f * Cell, buffer[5]);
        Assert.Equal((480f, 80f), (buffer[3], buffer[7]));
        Assert.Equal((1.5f, 1f), (buffer[8], buffer[11]));
        Assert.Equal((16f, 0.88f, 1f), (buffer[12], buffer[13], buffer[14]));
    }

    [Fact]
    public void GrowsItsBufferForAsManyLightsAsTouchTheView()
    {
        var lights = Enumerable.Range(0, 300).Select(i => Lamp(i, 80)).ToList();
        float[] buffer = [];
        Assert.Equal(300, Sources.Pack(lights, (0, 0, 320, 160), Cell, ref buffer));
        Assert.True(buffer.Length >= 300 * Sources.Stride);
        Assert.Equal(0, buffer.Length % Sources.Stride);
    }
}
