using Stillwake.Game.Features.Crew;
using Xunit;
using Xy = (float X, float Y);

namespace Stillwake.Game.Tests;

/// <summary>The thermometer's crust where Godot's triangulation fails: its pieces as the clipping gave them, filled whole.</summary>
public class CrustFillTests
{
    public static TheoryData<string, float[]> GodotGaveUpOn => new()
    {
        { "36.94°, a crest's tip just inside the rim", [71.7514343f, 34.2937164f, 71.7448502f, 34.2871323f, 71.7454529f, 34.2862091f] },
        {
            "36.86°, the whole crust pinched by a trough on the rim",
            [
                72.5727234f, 35.1016541f, 73.4000015f, 34.6082077f, 74.2272720f, 35.2618637f, 75.0545425f, 34.9566422f, 75.8818207f, 35.2272034f,
                76.7090912f, 35.5138702f, 77.5363617f, 34.1792297f, 78.3636398f, 35.5067520f, 79.1909103f, 34.5726929f, 79.4830551f, 35.0592270f,
                79.4632034f, 35.0790863f, 78.4100266f, 35.7408371f, 77.2360001f, 36.1516418f, 76.0000000f, 36.2909088f, 74.7639999f, 36.1516418f,
                73.5899734f, 35.7408371f, 72.5367966f, 35.0790787f, 71.6579208f, 34.2002029f, 71.7454529f, 34.0633621f,
            ]
        },
        { "36.54°, the next crest's tip", [80.8460999f, 33.3984299f, 80.8408203f, 33.4068298f, 80.8454514f, 33.3972549f] },
    };

    [Theory]
    [MemberData(nameof(GodotGaveUpOn))]
    public void APieceGodotCannotTriangulateIsFilledWhole(string at, float[] flat)
    {
        var piece = flat.Chunk(2).Select(p => (X: p[0], Y: p[1])).ToArray();
        var (points, indices) = CrustFill.Upright(piece);
        Assert.True(indices.Length >= 3 && indices.Length % 3 == 0, $"{at}: no triangles");
        double filled = 0;
        for (int i = 0; i < indices.Length; i += 3)
            filled += Area([points[indices[i]], points[indices[i + 1]], points[indices[i + 2]]]);
        Assert.InRange(filled - Area(piece), -5e-5, 5e-5);
        Assert.All(points, p => Assert.InRange(p.X, piece.Min(q => q.X), piece.Max(q => q.X)));
        Assert.All(points, p => Assert.InRange(p.Y, piece.Min(q => q.Y) - 1e-4f, piece.Max(q => q.Y) + 1e-4f));
    }

    [Fact]
    public void APieceOfNoWidthHasNothingToFill() => Assert.Empty(CrustFill.Upright([(2, 1), (2, 3), (2, 2)]).Indices);

    private static double Area(Xy[] p)
    {
        double twice = 0;
        for (int i = 0; i < p.Length; i++)
        {
            var next = p[(i + 1) % p.Length];
            twice += (double)p[i].X * next.Y - (double)next.X * p[i].Y;
        }
        return Math.Abs(twice / 2);
    }
}
