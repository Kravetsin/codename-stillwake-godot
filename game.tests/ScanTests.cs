using System.Text.Json;
using Stillwake.Game.Features.Scanner;
using Stillwake.Sim.Debris;
using Xunit;

namespace Stillwake.Game.Tests;

// The scanner's readout (2026-11-07-the-scanner): the hit points left of the wreck under the pointer, only while worn.
public class ScanTests
{
    private static readonly JsonSerializerOptions Loose = new() { PropertyNameCaseInsensitive = true };

    /// <summary>A piece of <paramref name="item"/> at (<paramref name="x"/>, <paramref name="y"/>) cells, <paramref name="damage"/> taken.</summary>
    private static Piece Of(int id, string item, int x, int y, int damage = 0, int speed = 0) =>
        JsonSerializer.Deserialize<Piece>($$"""{"id":{{id}},"item":"{{item}}","x":{{x * 1000}},"y":{{y * 1000}},"damage":{{damage}},"speed":{{speed}},"end":-100000}""", Loose)!;

    [Fact]
    public void ReadsTheWreckUnderThePointerAtItsTopEdge()
    {
        // A large asteroid is 6 by 6 cells and holds 200.
        var read = Scan.Of([Of(1, "asteroidLarge", 40, 20)], (41.5, 21), worn: true, progress: 0);
        Assert.Equal(new Readout(1, 40, 17, 200, 200), read);
    }

    [Fact]
    public void ReadsWhatIsLeftAfterHits()
    {
        var read = Scan.Of([Of(1, "asteroidLarge", 40, 20, damage: 46)], (40, 20), worn: true, progress: 0);
        Assert.Equal((154, 200), (read?.Left, read?.Max));
    }

    [Fact]
    public void ReadsNothingUnworn()
    {
        Assert.Null(Scan.Of([Of(1, "asteroidLarge", 40, 20)], (40, 20), worn: false, progress: 0));
    }

    [Fact]
    public void ReadsNothingOverASmallPieceOrEmptySpace()
    {
        Assert.Null(Scan.Of([Of(1, "scrapChunk", 40, 20)], (40, 20), worn: true, progress: 0));
        Assert.Null(Scan.Of([Of(1, "asteroidLarge", 40, 20)], (50, 20), worn: true, progress: 0));
    }

    [Fact]
    public void ReadsTheWreckOverASmallPieceAndFollowsItWhereItIsDrawn()
    {
        var read = Scan.Of([Of(1, "scrapChunk", 40, 20), Of(2, "asteroidLarge", 40, 20, speed: 100)], (40, 20), worn: true, progress: 0.5);
        Assert.Equal((2, 39.95), (read?.Id, read?.X));
    }
}
