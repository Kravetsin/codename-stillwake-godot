using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using Stillwake.Game.Features.Grid;
using Stillwake.Sim;
using Stillwake.Sim.Grid;
using Xunit;
using V = Stillwake.Content.Vessels;

namespace Stillwake.Game.Tests;

/// <summary>The signs of abandonment inside a derelict (derelict-looks): how many, which, where, and hidden once their cells change.</summary>
public class VesselSignsTests
{
    private static Vessel Of(string cls, uint seed) => Shipyard.Describe(V.Class(cls)!, seed);

    private static IEnumerable<(string Cls, uint Seed)> Many() =>
        from cls in (string[])["freighter", "shuttle"] from seed in Enumerable.Range(1, 60) select (cls, (uint)seed);

    private static string Say(IEnumerable<VesselSign> signs) => string.Join(' ', signs.Select(s => $"{s.Kind}@{s.X},{s.Y}{(s.Dir < 0 ? "<" : "")}"));

    [Fact]
    public void OneSeedIsSignedAsPinned()
    {
        var signs = new VesselSigns(Of("freighter", 1)).Signs;
        Assert.Equal("Crate@127,23< Panel@130,27< Soot@49,20 Gas@34,27", Say(signs));
        var soot = signs.Single(s => s.Kind == SignKind.Soot);
        Assert.Equal(("vesselLifeTank", 50, 22), (soot.Machine, soot.MachineX, soot.MachineY));
        Assert.Equal(15, signs.Single(s => s.Kind == SignKind.Crate).Cells.Count);
    }

    [Fact]
    public void EachClassShowsItsCountOfSignsEachKindOnceOutsideTheAirlockAndBridge()
    {
        foreach (var cls in (string[])["freighter", "shuttle"])
        {
            var (least, most) = VesselLooks.Signs[cls];
            int fewer = 0;
            foreach (uint seed in Enumerable.Range(1, 60).Select(s => (uint)s))
            {
                var v = Of(cls, seed);
                var signs = new VesselSigns(v).Signs;
                Assert.True(signs.Count <= most, $"{cls} {seed}: {Say(signs)}");
                if (signs.Count < least) fewer++;
                Assert.Equal(signs.Count, signs.Select(s => s.Kind).Distinct().Count());
                var (sx, sy) = Shipyard.Door(v);
                bool In(Vessel.Compartment c, int x, int y) => x >= c.Left && x < c.Left + c.Width && y >= c.Top && y < c.Top + V.Open;
                var barred = v.Compartments.Where(c => c.Role == Content.VesselRole.Bridge || In(c, sx, sy)).ToList();
                foreach (var sign in signs)
                {
                    Assert.NotEmpty(sign.Cells);
                    for (int y = sign.Y; y < sign.Y + sign.Height; y++)
                        for (int x = sign.X; x < sign.X + sign.Width; x++)
                            Assert.DoesNotContain(barred, c => In(c, x, y));
                }
            }
            // Fewer only where no room fits one: a cramped shuttle now and then.
            Assert.True(fewer <= 3, $"{cls}: {fewer} of 60 short of {least}");
        }
    }

    [Fact]
    public void TheSameSeedIsSignedAlikeTwice()
    {
        foreach (var (cls, seed) in Many())
        {
            var (a, b) = (new VesselSigns(Of(cls, seed)).Signs, new VesselSigns(Of(cls, seed)).Signs);
            Assert.Equal(Say(a), Say(b));
            Assert.Equal(a.SelectMany(s => s.Cells), b.SelectMany(s => s.Cells));
        }
    }

    [Fact]
    public void ASignIsHiddenOnceOneOfItsCellsChanges()
    {
        var v = Of("freighter", 1);
        var m = VesselDressTests.Laid(v);
        var signs = new VesselSigns(v).Signs;
        Assert.All(signs, s => Assert.True(VesselSigns.Shows(m.Grid, s), s.Kind.ToString()));
        var crate = signs.Single(s => s.Kind == SignKind.Crate);
        var (x, y) = crate.Cells[^1];
        int cell = VesselSigns.CellOf(x, y, m.Grid.Width);
        m.Grid.Place(cell, Placement.Wall());
        Assert.False(VesselSigns.Shows(m.Grid, crate));
        Assert.All(signs.Where(s => s != crate), s => Assert.True(VesselSigns.Shows(m.Grid, s), s.Kind.ToString()));
        m.Grid.Clear(cell, "structure");
        Assert.True(VesselSigns.Shows(m.Grid, crate));
        // Its back wall taken, or a machine built over it: hidden too.
        m.Grid.Clear(cell, "back");
        Assert.False(VesselSigns.Shows(m.Grid, crate));
        var gas = signs.Single(s => s.Kind == SignKind.Gas);
        m.Grid.Place(VesselSigns.CellOf(gas.Cells[0].X, gas.Cells[0].Y, m.Grid.Width), new Placement("machine", "lamp"));
        Assert.False(VesselSigns.Shows(m.Grid, gas));
    }

    private static string Here([CallerFilePath] string path = "") => path;

    [Fact]
    public void EachKindsPictureIsItsBoxDrawnFourTimesOver()
    {
        string hd = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Here())!, "..", "game", "art", "hd"));
        foreach (var kind in Enum.GetValues<SignKind>())
        {
            var png = File.ReadAllBytes(Path.Combine(hd, VesselSigns.PictureOf(kind) + ".png"));
            var (w, h, _) = VesselSigns.Size(kind);
            Assert.Equal((w * 8 * 4, h * 8 * 4), (BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(16)), BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(20))));
        }
    }
}
