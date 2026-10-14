using System;
using System.Numerics;
using Stillwake.Game.Features.Light;
using Stillwake.Game.Render;
using Xunit;
using L = Stillwake.Game.Light;

namespace Stillwake.Game.Tests;

/// <summary>
/// The cells mode's light (0026): emitters' glowing pixels given to the cells they fall in, spread by the
/// TypeScript game's sweeps (cells.test.ts ported), and laid out for the lit shader.
/// </summary>
public class CellLightTests
{
    private const int Cell = 16;

    /// <summary>
    /// Cells from rows: '.' air, '#' a wall, 'D' a closed door, 'M' a machine, '*' air giving off a whole
    /// unit of white light; the light and the kinds, row by row.
    /// </summary>
    private static (Vector4[] Light, byte[] Kind, int W, int H) CellsOf(params string[] rows)
    {
        int w = rows[0].Length, h = rows.Length;
        var light = new Vector4[w * h];
        var kind = new byte[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                char c = rows[y][x];
                kind[y * w + x] = c switch { '#' => Sweeps.Wall, 'D' => Sweeps.Door, 'M' => Sweeps.Machine, _ => Sweeps.Air };
                if (c == '*') light[y * w + x] = new Vector4(1, 1, 1, 0);
            }
        return (light, kind, w, h);
    }

    private static Vector4[] Spread(params string[] rows)
    {
        var (light, kind, w, h) = CellsOf(rows);
        Sweeps.Spread(light, kind, w, h);
        return light;
    }

    [Fact]
    public void LightFadesByTheAirItCrossesRoundACornerToo()
    {
        var light = Spread("*....", "####.", ".....");
        Assert.Equal(1, light[0].X);
        Assert.Equal(MathF.Pow(L.Air, 2), light[2].X, 4);
        // Down past the wall's end and back along under it.
        Assert.Equal(MathF.Pow(L.Air, 6), light[2 * 5 + 4].X, 4);
        Assert.Equal(MathF.Pow(L.Air, 10), light[2 * 5].X, 4);
    }

    [Fact]
    public void AWallIsLitWhereLightFallsOnItDarkerDeeperInAndLetsNextToNoneThrough()
    {
        var light = Spread("*.##..");
        Assert.Equal(L.Air * L.WallIn, light[2].X, 4);
        Assert.Equal(L.Air * L.WallIn * L.WallDeep, light[3].X, 4);
        Assert.True(light[4].X < 0.02f);
        Assert.True(Spread("*.#.")[3].X < 0.1f);
    }

    [Fact]
    public void AClosedDoorTakesLightAsItsShareAndLetsNextToNoneThrough()
    {
        var light = Spread("*.D.");
        Assert.Equal(L.Air * L.DoorIn, light[2].X, 4);
        Assert.True(light[3].X < 0.1f);
    }

    [Fact]
    public void LightDimsThroughAMachineMoreThanThroughAir()
    {
        var light = Spread("*M.", "*..");
        Assert.True(light[2].X < light[3 + 2].X);
    }

    [Fact]
    public void EachColourSpreadsApartTheBrightestWayWinningForEach()
    {
        var (light, kind, w, h) = CellsOf(".....");
        light[0] = new Vector4(1, 0, 0, 0);
        light[4] = new Vector4(0, 0, 1, 0);
        Sweeps.Spread(light, kind, w, h);
        Assert.Equal(MathF.Pow(L.Air, 2), light[2].X, 4);
        Assert.Equal(MathF.Pow(L.Air, 2), light[2].Z, 4);
        Assert.Equal(MathF.Pow(L.Air, 4), light[0].Z, 4);
    }

    /// <summary>A picture <paramref name="w"/> by <paramref name="h"/>, its pixels at <paramref name="glowing"/> white, the rest clear.</summary>
    private static EmitterMask MaskOf(int w, int h, params (int X, int Y)[] glowing)
    {
        var rgba = new byte[w * h * 4];
        foreach (var (x, y) in glowing) rgba.AsSpan((y * w + x) * 4, 4).Fill(255);
        return EmitterMask.Of(rgba, w, h, 0);
    }

    [Fact]
    public void APictureGlowsInItsOpaquePixelsAtLeastAsBrightAsItsKindSays()
    {
        // Clear, dark opaque (a grate's bar), bright opaque (a flame).
        byte[] rgba = [0, 0, 0, 0, 34, 38, 44, 255, 255, 224, 138, 255];
        Assert.Equal(2, EmitterMask.Of(rgba, 3, 1, 0).Count);
        var flames = EmitterMask.Of(rgba, 3, 1, L.Emits.Fire.Glowing);
        Assert.Equal(new[] { 2.5f, 0.5f }, flames.Pixels);
    }

    [Fact]
    public void ALampsBulbGlowsAndItsShadeAndBracketDoNot()
    {
        // The lamp's palette (ship-art.ts): K its outline, e its shade, u and w its bulb.
        byte[] rgba = [0x1a, 0x1e, 0x25, 255, 0x5f, 0x86, 0xa8, 255, 0xff, 0xf1, 0xc8, 255, 0xe8, 0xf2, 0xff, 255];
        Assert.Equal(new[] { 2.5f, 0.5f, 3.5f, 0.5f }, EmitterMask.Of(rgba, 4, 1, L.Emits.Lamp.Glowing).Pixels);
    }

    private static void Near(Vector4 expected, Vector4 actual) => Assert.True(Vector4.Distance(expected, actual) < 1e-5f, $"{expected} != {actual}");

    private static CellLight Empty(int w, int h)
    {
        var cells = new CellLight(Cell);
        // A world of space with no stars in it: the stretch lies inside a grid that is all walls.
        var world = new TestWorld(string.Join("\n", Rows(w + 2, h + 2)));
        cells.Begin(world.M.Grid, 1, 1, w, h);
        cells.Light.Clear();
        return cells;
    }

    private static string[] Rows(int w, int h)
    {
        var rows = new string[h];
        for (int y = 0; y < h; y++) rows[y] = new string('#', w);
        return rows;
    }

    [Fact]
    public void AnEmittersLightGoesWholeToTheCellMostOfItsGlowingPixelsFallInAndByShareToTheRest()
    {
        var cells = Empty(4, 4);
        // Twelve glowing pixels in one cell, four in the next.
        var pixels = new (int, int)[16];
        for (int i = 0; i < 16; i++) pixels[i] = (i < 12 ? 4 + i : 16 + i - 12, 5);
        var light = new Vector4(1.5f, 1.3f, 0.95f, 0);
        // The picture laid from the stretch's corner cell (1, 1).
        cells.Give(MaskOf(32, 8, pixels), Matrix3x2.CreateTranslation(Cell, Cell), light);
        Near(light, cells.Light[0]);
        Near(light / 3, cells.Light[1]);
        Assert.Equal(Vector4.Zero, cells.Light[2]);
    }

    [Fact]
    public void AnEmitterGivesTheSameLightWhateverHowManyOfItsPixelsGlow()
    {
        var one = Empty(3, 3);
        one.Give(MaskOf(16, 16, (8, 8)), Matrix3x2.CreateTranslation(Cell * 2, Cell * 2), Vector4.One);
        var many = Empty(3, 3);
        many.Give(MaskOf(16, 16, (2, 2), (3, 2), (2, 3), (3, 3), (9, 9)), Matrix3x2.CreateTranslation(Cell * 2, Cell * 2), Vector4.One);
        Assert.Equal(one.Light[1 * 3 + 1], many.Light[1 * 3 + 1]);
    }

    [Fact]
    public void AnEmitterMovedOrFlippedLightsWhereItsPixelsAreDrawn()
    {
        var cells = Empty(4, 2);
        // One pixel at the picture's left; the picture flipped about its middle and moved two cells right.
        var flipped = Matrix3x2.CreateScale(-1, 1) * Matrix3x2.CreateTranslation(Cell * 3 + 16, Cell);
        cells.Give(MaskOf(16, 16, (1, 1)), flipped, Vector4.One);
        Assert.Equal(Vector4.One, cells.Light[2]);
        Assert.Equal(1, cells.Light.ToArray().Count(v => v != Vector4.Zero));
    }

    [Fact]
    public void PixelsOutsideTheStretchGiveNothingAndTheCountsAreClearedForTheNextEmitter()
    {
        var cells = Empty(2, 2);
        cells.Give(MaskOf(16, 16, (1, 1)), Matrix3x2.CreateTranslation(-100, -100), Vector4.One);
        Assert.All(cells.Light.ToArray(), v => Assert.Equal(Vector4.Zero, v));
        var mask = MaskOf(16, 16, (1, 1));
        var white = new Vector4(1, 1, 1, 0);
        cells.Give(mask, Matrix3x2.CreateTranslation(Cell, Cell), white);
        cells.Give(mask, Matrix3x2.CreateTranslation(Cell, Cell), white);
        Assert.Equal(new Vector4(2, 2, 2, 0), cells.Light[0]);
    }

    [Fact]
    public void AParticleLightsTheCellItIsIn()
    {
        var cells = Empty(3, 3);
        cells.Give(Cell * 2.5f, Cell * 3.9f, new Vector4(0.5f, 0.24f, 0.06f, 0));
        Assert.Equal(new Vector4(0.5f, 0.24f, 0.06f, 0), cells.Light[2 * 3 + 1]);
        cells.Give(-50, 0, Vector4.One);
        Assert.Equal(1, cells.Light.ToArray().Count(v => v != Vector4.Zero));
    }

    [Fact]
    public void TheStarsShineInWhereNoBackWallIsAndWallsAndDoorsAndMachinesAreReadAsSuch()
    {
        var world = new TestWorld("""
            #####
            #.LD.
            #####
            """);
        var cells = new CellLight(Cell);
        cells.Begin(world.M.Grid, -1, 0, 7, 3);
        var star = new Vector4(L.Star.R, L.Star.G, L.Star.B, 0);
        Assert.Equal(star, cells.Light[1 * 7]);
        Assert.Equal(Vector4.Zero, cells.Light[1 * 7 + 2]);
        Assert.Equal(Sweeps.Wall, cells.Kind[1]);
        Assert.Equal(Sweeps.Machine, cells.Kind[1 * 7 + 3]);
        Assert.Equal(Sweeps.Door, cells.Kind[1 * 7 + 4]);
        Assert.Equal(Sweeps.Air, cells.Kind[1 * 7 + 2]);
    }

    [Fact]
    public void ALampsBulbLightsItsRoomAndAWallShadesTheNext()
    {
        var world = new TestWorld("""
            #########
            #...#...#
            #.L.#...#
            #...#...#
            #########
            """);
        var cells = new CellLight(Cell);
        cells.Begin(world.M.Grid, 0, 0, 9, 5);
        var bulb = MaskOf(16, 16, (7, 8), (8, 8), (7, 9), (8, 9));
        cells.Give(bulb, Matrix3x2.CreateTranslation(Cell * 2, Cell * 2), new Vector4(1.5f, 1.3f, 0.95f, 0));
        cells.Spread();
        cells.Finish();
        var light = cells.Light;
        Assert.Equal(1.5f * L.Air, light[2 * 9 + 3].X, 4);
        Assert.True(light[2 * 9 + 6].X < 0.05f);
        // The wall holds the light on its face, as much as the cell before it, its picture taking WallIn of it; it blocks.
        Assert.Equal(light[2 * 9 + 3].X, light[2 * 9 + 4].X, 4);
        Assert.Equal(1, light[2 * 9 + 4].W);
        Assert.Equal(0, light[2 * 9 + 3].W);
    }
}
