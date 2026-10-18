using Stillwake.Game.Features.Light;
using Stillwake.Sim;
using Stillwake.Sim.Grid;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>What the lightmap's shader reads: where light stops, and each light it draws.</summary>
public class LightSourcesTests
{
    private const int Cell = 16;

    private static LightSource Lamp(double x, double y, float strength = 1) => new(x, y, (1.5f, 1.3f, 0.95f), strength, 16, 0.88f, 1, 6);

    [Fact]
    public void StopsLightAtWallsAndClosedDoorsAndLetsItThroughOpenDoorsAndTheOpen()
    {
        int[] structure = [Structure.Empty, Structure.Wall, Structure.DoorClosed, Structure.Empty, Structure.Empty, Structure.DoorOpen, Structure.Floor, Structure.Wall];
        var cells = new byte[6];
        // Two rows four wide, the box from the second column on: row by row, what is built only.
        Sources.Occupy(structure, 4, new Box(1, 0, 3, 1), cells);
        // The closed door alone is a hatch (0067).
        Assert.Equal(new[] { Sources.Wall, Sources.Hatch, Sources.Open, Sources.Open, Sources.Open, Sources.Wall }, cells);
    }

    [Fact]
    public void TellsAClosedDoorsPanelFromAClosedHatchsByTheDoorwaysShape()
    {
        // A door two tall, shut over open, beside a hatch two wide.
        int[] structure = [Structure.DoorClosed, Structure.Empty, Structure.DoorClosed, Structure.DoorClosed, Structure.DoorOpen, Structure.Empty, Structure.Empty, Structure.Empty];
        var cells = new byte[8];
        Sources.Occupy(structure, 4, new Box(0, 0, 3, 1), cells);
        Assert.Equal(new[] { Sources.Door, Sources.Open, Sources.Hatch, Sources.Hatch, Sources.Open, Sources.Open, Sources.Open, Sources.Open }, cells);
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
        Assert.Equal((16f, 0.88f, 1f, 6f), (buffer[12], buffer[13], buffer[14], buffer[15]));
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
