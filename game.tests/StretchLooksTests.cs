using System;
using System.Collections.Generic;
using System.Linq;
using Stillwake.Game.Features.Computer;
using Stillwake.Game.Features.Sky;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>A stretch of space seen coming and running without a word (docs/decisions/2026-11-15-stretches-of-space.md): the wash, the radar's band, the frost, the pieces.</summary>
public class StretchLooksTests
{
    private const string Belt = "iceBelt";
    private static readonly int Warning = Content.Stretches.Warning;

    [Fact]
    public void StretchShownGrowsOverTheWarningAndIsWholeWhileItRuns()
    {
        var shown = new StretchShown();
        shown.See(null, Belt, Warning, 0);
        Assert.Equal(Belt, shown.Look?.Id);
        Assert.Equal(0, shown.Grow);
        shown.See(null, Belt, Warning / 2, Warning / 2);
        Assert.Equal(0.5f, shown.Grow, 3);
        shown.See(Belt, null, 0, Warning);
        Assert.Equal(1, shown.Grow);
        Assert.Equal(1, shown.Shown);
    }

    [Fact]
    public void StretchShownEasesBackOverTheThawThenShowsNone()
    {
        var shown = new StretchShown();
        shown.See(Belt, null, 0, 100);
        shown.See(null, null, 0, 100);
        Assert.Equal(0, shown.After);
        shown.See(null, null, 0, 100 + StretchLooks.Thaw / 2.0);
        Assert.Equal(0.5f, shown.Melt, 3);
        Assert.Equal(0.5f, shown.Shown, 3);
        shown.See(null, null, 0, 100 + StretchLooks.Thaw * 1.2);
        Assert.Equal(1, shown.Melt);
        Assert.NotNull(shown.Look);
        shown.See(null, null, 0, 100 + StretchLooks.Thaw * StretchLooks.Dry);
        Assert.Null(shown.Look);
        Assert.Equal(0, shown.Grow);
    }

    /// <summary>All of it is read from the sky now: a peer joining mid-warning sees what the others do; one joining after the end sees none (accepted).</summary>
    [Fact]
    public void APeerJoiningMidWarningSeesWhatTheOthersDo()
    {
        var all = new StretchShown();
        for (int left = Warning; left >= Warning / 3; left -= 60) all.See(null, Belt, left, Warning - left);
        var joiner = new StretchShown();
        joiner.See(null, Belt, Warning / 3, Warning - Warning / 3);
        Assert.Equal((all.Grow, all.Since), (joiner.Grow, joiner.Since));
        Assert.Equal(Warning - Warning / 3, joiner.Since);
        // One loading a world mid-stretch knows when it began: its far picture is the one shown before, where it was.
        var running = new StretchShown();
        running.See(Belt, null, 0, 0, ran: 500);
        Assert.Equal(Warning + 500, running.Since);
        var late = new StretchShown();
        late.See(null, null, 0, 5 * Warning);
        Assert.Null(late.Look);
    }

    [Fact]
    public void EveryStretchHasItsLook()
    {
        foreach (var stretch in Content.Stretches.All) Assert.NotNull(StretchLooks.Of(stretch.Id));
        Assert.True(StretchLooks.Of(Belt)!.Frost);
        Assert.False(StretchLooks.Of(Content.Stretches.ContrabandDrift.Id)!.Frost);
    }

    [Fact]
    public void EveryFarPictureIsDrawnAndAFarRowMixesThemAll()
    {
        string hd = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "game", "art", "hd");
        foreach (var look in StretchLooks.All)
        {
            Assert.True(look.Far.Speed > 0, look.Id);
            Assert.True(look.Far.Pictures.Count > 0 != (look.Far.Scattered != null), $"{look.Id}: pictures or a field, one of them");
            foreach (var picture in look.Far.Pictures) Assert.True(File.Exists(Path.Combine(hd, picture + ".png")), picture);
            int count = look.Far.Pictures.Count;
            if (count == 0) continue;
            var taken = Enumerable.Range(-10, 30).Select(place => StretchLooks.PictureAt(place, 1234, count)).ToList();
            Assert.Equal(count, taken.Distinct().Count());
            Assert.All(taken, i => Assert.InRange(i, 0, count - 1));
            Assert.Equal(taken, Enumerable.Range(-10, 30).Select(place => StretchLooks.PictureAt(place, 1234, count)));
        }
    }

    [Fact]
    public void AFieldScattersItsPiecesSmallWithinItsPlaceAndTheSameEachTime()
    {
        string hd = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "game", "art", "hd");
        foreach (var look in StretchLooks.All)
        {
            if (look.Far.Scattered is not { } field) continue;
            using (var file = File.OpenRead(Path.Combine(hd, field.Atlas + ".png")))
            {
                var header = new byte[24];
                file.ReadExactly(header);
                int width = (header[16] << 24) | (header[17] << 16) | (header[18] << 8) | header[19];
                int height = (header[20] << 24) | (header[21] << 16) | (header[22] << 8) | header[23];
                Assert.All(field.Pieces, p => Assert.True(p.X >= 0 && p.Y >= 0 && p.X + p.W <= width && p.Y + p.H <= height, $"{p}"));
            }
            // At the player's nearest, three times the farthest, a piece's pixel stays within a screen's.
            Assert.InRange(field.Large * 3 * 1.25f, field.Small, 1f);
            List<StretchLooks.Spot> spots = [], again = [];
            for (long place = 0; place < 20; place++)
            {
                StretchLooks.PiecesAt(field, place, 77, look.Far.Width, spots);
                StretchLooks.PiecesAt(field, place, 77, look.Far.Width, again);
            }
            Assert.Equal(spots, again);
            Assert.Equal(20 * field.Each, spots.Count);
            Assert.All(spots, s =>
            {
                Assert.InRange(s.Piece, 0, field.Pieces.Count - 1);
                Assert.InRange(s.X, 0, look.Far.Width);
                Assert.InRange(s.Y, -field.Tall / 2, field.Tall / 2);
                Assert.InRange(s.Scale, field.Small, field.Large);
            });
            Assert.True(spots.Select(s => s.Piece).Distinct().Count() > field.Pieces.Count / 2, "most pieces are used");
        }
    }

    [Fact]
    public void TheRadarsBandSlidesFromTheRightEdgeOntoTheShip()
    {
        const float width = 200, ship = 70;
        var start = RadarBand.Across(0, width, ship);
        Assert.Equal(width - RadarBand.Peeks * width, start.Near, 3);
        var end = RadarBand.Across(1, width, ship);
        Assert.Equal(ship, (end.Near + end.Far) / 2, 3);
        float last = float.MaxValue;
        for (float grow = 0; grow <= 1; grow += 0.1f)
        {
            var (near, far) = RadarBand.Across(grow, width, ship);
            Assert.True(near < last);
            Assert.Equal(RadarBand.Width * width, far - near, 3);
            last = near;
        }
    }

    [Fact]
    public void AWallsSidesFacingSpaceAreItsOpenOnesAndTheGridsEdge()
    {
        // A 4 by 3 grid: walls in its middle row, space above, a room below the left two.
        bool[] wall = [false, false, false, false, true, true, true, true, false, false, false, false];
        bool[] space = [true, true, true, true, false, false, false, false, false, false, true, true];
        int Sides(int x, int y) => Frost.SidesOf(x, y, 4, 3, c => wall[c], c => space[c]);
        Assert.Equal(Frost.Up | Frost.Left, Sides(0, 1));
        Assert.Equal(Frost.Up, Sides(1, 1));
        Assert.Equal(Frost.Up | Frost.Down, Sides(2, 1));
        Assert.Equal(Frost.Up | Frost.Right | Frost.Down, Sides(3, 1));
        Assert.Equal(0, Sides(0, 0));
        Assert.True(Frost.Cornered(Frost.Up | Frost.Left));
        Assert.False(Frost.Cornered(Frost.Up | Frost.Down));
    }

    [Fact]
    public void AWallUnderAPillarsFootTurnsTheFrostRoundItsInnerCorners()
    {
        // A 5 by 4 grid: a roof on row 2, a pillar on (2, 1), space above the roof and round the pillar, a room under the roof.
        const int width = 5, height = 4;
        var wall = new bool[width * height];
        for (int x = 0; x < width; x++) wall[2 * width + x] = true;
        wall[width + 2] = true;
        bool Space(int c) => !wall[c] && c < 2 * width;
        int Hollows(int x, int y) => Frost.HollowsOf(x, y, width, height, c => wall[c], Space);
        // Under the pillar: space up-left (bit 3, left and up) and up-right (bit 0, up and right).
        Assert.Equal(1 << 3 | 1 << 0, Hollows(2, 2));
        Assert.Equal(0, Hollows(1, 2));
        Assert.Equal(0, Hollows(2, 1));
        Assert.Equal(0, Frost.SidesOf(2, 2, width, height, c => wall[c], Space));
        // Kept with the hull's cells, above its sides.
        var hull = new HullCells(width, height, c => wall[c], Space);
        hull.Scan(0, 0, width - 1, height - 1);
        Assert.Equal((1 << 3 | 1 << 0) << Frost.HollowShift, hull.Sides[2 * width + 2]);
    }

    [Fact]
    public void FrostReachesTheOuterCornersFirstAndEveryCellByTheLatest()
    {
        // A long flat roof, its two ends turning down: corners at both ends.
        const int width = 120;
        var hull = new Dictionary<int, int>();
        for (int x = 1; x < width - 1; x++) hull[width + x] = Frost.Up;
        hull[width + 1] = Frost.Up | Frost.Left;
        hull[width + width - 2] = Frost.Up | Frost.Right;
        var delays = Frost.Delays(hull, width);
        Assert.Equal(hull.Keys.Order(), delays.Keys.Order());
        Assert.All(delays.Values, d => Assert.InRange(d, 0, Frost.Latest));
        Assert.True(delays[width + 1] <= 0.06f);
        Assert.True(delays[width + 1] < delays[width + 10]);
        // Away from the corners, cold spots start their own frost, earlier than the corner's would reach.
        Assert.Contains(hull.Keys, c => !Frost.Cornered(hull[c]) && Frost.Hash(c) % Frost.ColdEvery == 0 && delays[c] < Frost.Latest);
    }

    [Fact]
    public void FrostGrowsFromItsTurnAndTheLastToFreezeMeltsFirst()
    {
        Assert.Equal(0, Frost.Amount(0, 0, 0));
        Assert.Equal(1, Frost.Amount(0, Frost.Span, 0), 3);
        Assert.True(Frost.Amount(0.1f, 0.3f, 0) > Frost.Amount(0.3f, 0.3f, 0));
        Assert.Equal(0, Frost.Amount(0.3f, 0.3f, 0));
        // Melting: the latest part goes first; by the thaw's end, all.
        Assert.True(Frost.Amount(Frost.Latest, 1, 0.1f) < Frost.Amount(0, 1, 0.1f));
        Assert.All(new[] { 0f, 0.2f, Frost.Latest }, d => Assert.Equal(0, Frost.Amount(d, 1, 1)));
        Assert.True(Frost.Shows(0.01f, 0));
        Assert.False(Frost.Shows(0, 0));
        Assert.False(Frost.Shows(1, StretchLooks.Dry));
        Assert.Equal(0, Frost.Thawing(0).Wet);
        Assert.True(Frost.Thawing(0.6).Wet > 0);
    }

    [Fact]
    public void TheHullsCellsFollowAChangeAsAWholeScanWouldFindThem()
    {
        const int width = 6, height = 5;
        var wall = new bool[width * height];
        var space = Enumerable.Repeat(true, width * height).ToArray();
        void Build(int x, int y) => (wall[y * width + x], space[y * width + x]) = (true, false);
        for (int x = 1; x <= 4; x++) Build(x, 2);
        var cells = new HullCells(width, height, c => wall[c], c => space[c]);
        cells.Scan(0, 0, width - 1, height - 1);
        Assert.Equal(4, cells.Sides.Count);
        int version = cells.Version;
        Build(2, 1);
        cells.Changed(1 * width + 2);
        Assert.True(cells.Version > version);
        var whole = new HullCells(width, height, c => wall[c], c => space[c]);
        whole.Scan(0, 0, width - 1, height - 1);
        Assert.Equal(whole.Sides.OrderBy(p => p.Key), cells.Sides.OrderBy(p => p.Key));
        Assert.Equal(Frost.Left | Frost.Down, cells.Sides[2 * width + 1] & (Frost.Left | Frost.Down));
        Assert.Equal(0, cells.Sides[2 * width + 2] & Frost.Up);
        version = cells.Version;
        cells.Changed(0);
        Assert.Equal(version, cells.Version);
    }

    [Fact]
    public void EveryCratesLookHasItsLampsAndBlinksItsOwnWay()
    {
        Assert.Equal(Content.Looks.CountOf(StretchLooks.ContrabandCrate), CrateLamps.Of.Count);
        Assert.All(CrateLamps.Of, lamps => Assert.NotEmpty(lamps));
        Assert.All(CrateLamps.Of.SelectMany(l => l), l => Assert.True(Math.Abs(l.X) < 12 && Math.Abs(l.Y) < 12 && l.R > 0));
        double Share(int look, int lamp) => Enumerable.Range(0, 40000).Count(i => CrateLamps.Lit(look, lamp, i / 1000.0)) / 40000.0;
        // The case once in 1.6 s for 0.28 s; the drum's three in turn; the bundle's short strobe.
        Assert.Equal(0.28 / 1.6, Share(0, 0), 2);
        for (int i = 0; i < 3; i++) Assert.Equal(0.25, Share(1, i), 2);
        Assert.All(Enumerable.Range(0, 1000), i => Assert.True(Enumerable.Range(0, 3).Count(lamp => CrateLamps.Lit(1, lamp, i / 100.0)) <= 1));
        Assert.Equal(0.12, Share(3, 0), 2);
        Assert.True(CrateLamps.Lit(2, 0, 0.05) && !CrateLamps.Lit(2, 0, 0.18) && CrateLamps.Lit(2, 0, 0.3));
    }

    [Fact]
    public void TheDruseShattersColdAndEveryOtherWreckAsMetal()
    {
        var ice = BreakLooks.Of(StretchLooks.IceBlock);
        Assert.Same(BreakLooks.Ice, ice);
        Assert.Equal(8, ice.Cuts.Length);
        Assert.True(ice.Cuts.Zip(ice.Cuts.Skip(1)).All(p => p.First < p.Second) && ice.Cuts[^1] < ice.Cuts[0] + MathF.Tau);
        Assert.False(ice.Hot);
        Assert.False(ice.Sparks);
        Assert.Equal(12, ice.Glitter);
        Assert.Equal(0, BreakLooks.Glitter.Gravity);
        Assert.Same(BreakLooks.Wreck, BreakLooks.Of("cargoCrate"));
        Assert.Same(BreakLooks.Wreck, BreakLooks.Of(StretchLooks.ContrabandCrate));
        Assert.True(BreakLooks.Wreck.Hot && BreakLooks.Wreck.Sparks && BreakLooks.Wreck.Glitter == 0);
    }

    [Fact]
    public void TheDruseIsStruckAsIceFloatingAndCrackedAlongPlanes()
    {
        Assert.Equal(Impact.Ice, HitLooks.PieceOf(StretchLooks.IceBlock));
        Assert.Equal(Impact.Wreck, HitLooks.PieceOf(StretchLooks.ContrabandCrate));
        Assert.All(HitLooks.Of[Impact.Ice].Throws, t => Assert.Equal(0, t.Look.Gravity));
        Assert.Same(DamageLooks.Ice, DamageLooks.InkOf(StretchLooks.IceBlock));
        Assert.Equal(0, DamageLooks.Ice.Wander);
        Assert.Same(DamageLooks.Wreck, DamageLooks.InkOf("cargoCrate"));
    }
}
