using Stillwake.Game.Features.Logistics;
using Xunit;
using SI = Stillwake.Game.Features.Logistics.SalvageInk;
using Tune = Stillwake.Content.Logistics;

namespace Stillwake.Game.Tests;

/// <summary>What a salvage drone's net holds, laid in it (salvage-trips): one to four pictures on the net's bottom, inside its shape.</summary>
public class NetPileTests
{
    private static (float X, float Y, float W, float H)[] Lay(out float tall, params (float W, float H)[] sizes)
    {
        var laid = new (float X, float Y, float W, float H)[sizes.Length];
        tall = NetPile.Lay(sizes, laid);
        return laid;
    }

    [Fact]
    public void OneLiesWholeAndCentredOnTheNetsBottom()
    {
        var laid = Lay(out float tall, (10, 8));
        Assert.Equal((-5f, -8f, 10f, 8f), laid[0]);
        Assert.Equal(8, tall);
    }

    [Fact]
    public void TwoLieSideBySideInCatchOrderThenTheNextOverThem()
    {
        var two = Lay(out _, (10, 8), (10, 8));
        Assert.True(two[0].X < two[1].X);
        Assert.Equal(two[0].Y + two[0].H, two[1].Y + two[1].H);
        Assert.Equal(0, two[0].Y + two[0].H, 3);

        var four = Lay(out float tall, (10, 8), (10, 8), (10, 8), (10, 8));
        // The second row over the first, sunk part way in, each row's pair overlapping a little across the middle.
        Assert.True(four[2].Y < four[0].Y && four[2].Y + four[2].H > four[0].Y);
        Assert.True(four[0].X + four[0].W > 0 && four[1].X < 0);
        Assert.Equal(-four[2].Y, tall, 3);
        // Three: the third alone, centred.
        var three = Lay(out _, (10, 8), (10, 8), (12, 6));
        Assert.Equal(0, three[2].X + three[2].W / 2, 3);
    }

    [Fact]
    public void AFullNetOfPiecesAsBigAsTheBiggestStaysInsideTheNetsShape()
    {
        // The net's inside, by the picture's numbers: from its bottom up to under the body, as wide as the picture less its sides.
        float wide = SI.FlyWidth - 2 * SI.Margin, high = SI.HoldBottom - SI.AnchorY - 4;
        for (int n = 1; n <= Tune.SalvageHolds; n++)
        {
            var sizes = new (float W, float H)[n];
            System.Array.Fill(sizes, (n == 1 ? 12f : 10f, n == 1 ? 12f : 10f));
            var laid = Lay(out float tall, sizes);
            Assert.True(tall <= high, $"{n} pile {tall} high in {high}");
            Assert.All(laid, r => Assert.True(r.X >= -wide / 2 && r.X + r.W <= wide / 2, $"{n}: {r} across {wide}"));
        }
    }
}
