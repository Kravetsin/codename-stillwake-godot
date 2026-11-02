using System.Linq;
using Xunit;

namespace Stillwake.Game.Tests;

public class FloraLooksTests
{
    [Fact]
    public void In_pixels_a_tuft_is_only_ever_its_own_picture()
    {
        foreach (uint hash in Enumerable.Range(0, 100).Select(i => (uint)i))
            Assert.Equal("planet/tuft_2", FloraLooks.Drawn("tuft", 2, hash, ink: false));
    }

    [Fact]
    public void In_ink_tufts_of_one_variant_come_as_every_plant()
    {
        // Hashes that all pick variant 1, as FloraView's do: the plant must not follow the variant.
        var drawn = Enumerable.Range(0, 30).Select(i => FloraLooks.Drawn("tuft", 1, (uint)(i * 3 + 1), ink: true)).Distinct().OrderBy(n => n, System.StringComparer.Ordinal);
        Assert.Equal(["planet/tuftFungus_1", "planet/tuftReed_1", "planet/tuft_1"], drawn);
    }

    [Fact]
    public void A_kind_with_no_ink_plants_keeps_its_picture()
    {
        Assert.Equal("planet/bush_1", FloraLooks.Drawn("bush", 1, 12345, ink: true));
    }
}
