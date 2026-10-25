using Stillwake.Game.Features.Atmosphere;
using Stillwake.Sim;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>A room turned to space at once drawn venting as through a door: what goes, toward where, and when it stops.</summary>
public class DecompressionTests
{
    private const int Space = -1, Width = 6;

    /// <summary>A room of eight cells, id 7, over a floor with a hole two cells wide: <c>.</c> room, <c>#</c> solid, space else.</summary>
    private static readonly string[] Shut =
    [
        "######",
        "#....#",
        "#....#",
        "######",
        "      ",
    ];

    private static readonly string[] Open =
    [
        "######",
        "#    #",
        "#    #",
        "##  ##",
        "      ",
    ];

    private static System.Func<int, int?> RoomsOf(string[] map) => cell =>
    {
        if (cell < 0 || cell >= Width * map.Length) return null;
        char c = map[cell / Width][cell % Width];
        return c == '#' ? null : c == '.' ? 7 : Space;
    };

    private static int At(int x, int y) => y * Width + x;

    private static readonly int[] RoomCells = [At(1, 1), At(2, 1), At(3, 1), At(4, 1), At(1, 2), At(2, 2), At(3, 2), At(4, 2)];

    [Fact]
    public void LetsARoomsAirGoOutThroughTheHoleItWentToSpaceThroughFarthestFirst()
    {
        var d = new Decompression();
        d.Survey(RoomCells, RoomsOf(Shut), Space, Width, 250);
        Assert.False(d.Any);
        d.Survey([], RoomsOf(Open), Space, Width, 250);
        Assert.Equal(RoomCells, d.Cells);
        Assert.Equal(7, d.RoomOf(At(2, 1)));
        Assert.Equal([At(2, 3), At(3, 3)], d.Breaches[7]);
        // Out of the hole, downward, into the space past it.
        Assert.All(d.Streams, s => Assert.Equal(System.Math.PI / 2, s.Heading!.Value, 6));
        Assert.Equal([(2.5, 3.5), (3.5, 3.5)], d.Streams.Select(s => (s.X, s.Y)));
        // As fast as a room open to space loses its air in the sim: the top corners, farthest from the hole, first; the cells
        // over the hole last.
        Assert.Equal(1000, Decompression.MsPerCell * Content.Air.VentLoss * World.TicksPerSecond, 6);
        double drain = RoomCells.Length * Decompression.MsPerCell;
        d.Survey([], RoomsOf(Open), Space, Width, drain * 0.6);
        Assert.DoesNotContain(At(1, 1), d.Cells);
        Assert.Contains(At(2, 2), d.Cells);
        d.Survey([], RoomsOf(Open), Space, Width, drain);
        Assert.False(d.Any);
        Assert.Empty(d.Streams);
    }

    [Fact]
    public void LetsNothingGoOfAirLostInARoomOrOfARoomShutAgain()
    {
        var d = new Decompression();
        d.Survey(RoomCells, RoomsOf(Shut), Space, Width, 250);
        // Air lost while the room stands, venting through a door as the sim drains it: nothing of this.
        d.Survey(RoomCells[..4], RoomsOf(Shut), Space, Width, 250);
        Assert.False(d.Any);
        d.Survey(RoomCells, RoomsOf(Shut), Space, Width, 250);
        d.Survey([], RoomsOf(Open), Space, Width, 250);
        Assert.True(d.Any);
        // Shut again before it is gone: the room stands, airless, and nothing goes on going.
        d.Survey([], RoomsOf(Shut), Space, Width, 250);
        Assert.False(d.Any);
    }
}
