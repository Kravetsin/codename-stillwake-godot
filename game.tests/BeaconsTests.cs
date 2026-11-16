using Stillwake.Game.Features.Light;
using Stillwake.Sim;
using Xunit;
using L = Stillwake.Game.Light;
using V = Stillwake.Content.Vessels;

namespace Stillwake.Game.Tests;

/// <summary>A derelict's beacons (derelict-looks): a red one on each mast, a white strobe on the boom or the bow's tip.</summary>
public class BeaconsTests
{
    [Theory]
    [InlineData(1u)]
    [InlineData(7u)]
    public void AFreighterBlinksRedOnItsMastsTopAndStrobesOnItsBoomsTip(uint seed)
    {
        var vessel = Shipyard.Describe(V.Class("freighter")!, seed);
        var beacons = Beacons.Of(vessel);
        var mast = vessel.Outside.Single(f => f.Kind == V.MastTall);
        var boom = vessel.Outside.Single(f => f.Kind == V.Boom);
        Assert.Equal(2, beacons.Count);
        var red = Assert.Single(beacons, b => b.Look == L.RedBeacon);
        Assert.Equal((V.MastTall, (mast.X, mast.Y)), (red.Kind, red.Cell));
        // On the mast's top: within its two columns, in its top cell.
        Assert.InRange(red.X, mast.X * Camera.Cell, (mast.X + 2) * Camera.Cell);
        Assert.InRange(red.Y, mast.Y * Camera.Cell, (mast.Y + 1) * Camera.Cell);
        var strobe = Assert.Single(beacons, b => b.Look == L.Strobe);
        Assert.Equal((V.Boom, (boom.X, boom.Y)), (strobe.Kind, strobe.Cell));
        // At the boom's far end.
        Assert.InRange(strobe.X, (boom.X + 7) * Camera.Cell, (boom.X + 8) * Camera.Cell);
    }

    [Fact]
    public void AShuttleBlinksRedOnItsMastAndStrobesFromItsBowsTipCell()
    {
        var vessel = Shipyard.Describe(V.Class("shuttle")!, 3);
        var beacons = Beacons.Of(vessel);
        var mast = vessel.Outside.Single(f => f.Kind == V.Mast);
        Assert.Equal([(L.RedBeacon, V.Mast), (L.Strobe, (string?)null)], beacons.Select(b => (b.Look, b.Kind)));
        Assert.Equal((mast.X, mast.Y), beacons[0].Cell);
        // The strobe on the tip's middle row, kept by the hull's wall there.
        var bow = vessel.Sections.Single(s => s.Kind == Vessel.SectionKind.Bow);
        int x = bow.Left + bow.Width - 1;
        var (top, bottom) = bow.RowsAt(x);
        Assert.Equal((x, (top + bottom) / 2), beacons[1].Cell);
        Assert.Contains(Shipyard.Stamp(vessel), p => p.Kind == "wall" && (p.X, p.Y) == beacons[1].Cell);
        Assert.InRange(beacons[1].X, x * Camera.Cell, (x + 1) * Camera.Cell);
    }

    [Fact]
    public void ABeaconFlashesForItsDutyOfEveryPeriodAndTwoOfAVesselNeverTogether()
    {
        foreach (var look in (L.BeaconLight[])[L.RedBeacon, L.Strobe])
        {
            int lit = Enumerable.Range(0, look.PeriodMs).Count(ms => Beacons.Shines(look, ms, 0) > 0);
            Assert.Equal((int)Math.Ceiling(look.Duty * look.PeriodMs), lit);
            Assert.Equal(Beacons.Shines(look, 123, 0), Beacons.Shines(look, 123 + look.PeriodMs, 0));
        }
        Assert.Equal(1, Beacons.Shines(L.Strobe, 0, Beacons.PhaseOf(0)));
        Assert.Equal(0, Beacons.Shines(L.Strobe, 0, Beacons.PhaseOf(1)));
    }
}
