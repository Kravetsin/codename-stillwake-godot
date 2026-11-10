using Xunit;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Tests;

/// <summary>
/// The arc bench's look (0099): any tool with a held picture sits on it by its own clamp, within the bench or overhanging
/// it no more than <see cref="BenchLook.Overhang"/> and <see cref="BenchLook.Hang"/> say; the drill as the picked design drew it.
/// </summary>
public class BenchLookTests
{
    /// <summary>Every item held as a picture: every tool a hero wields, and any to come.</summary>
    public static TheoryData<string> Held => new(Spec.Items.All.Select(i => i.Id).Where(id => HeldLook.Of(id) != null));

    [Theory]
    [MemberData(nameof(Held))]
    public void EveryToolWithAHeldPictureIsClampedWhereItSaysAndLiesOnTheBenchOrOverhangsItGracefully(string item)
    {
        var look = HeldLook.Of(item)!;
        Assert.True(look.Clamp is { } clamp && clamp.X >= 0 && clamp.Y >= 0, $"{item} declares no clamp");
        var (w, h) = Png.SizeOf(look.Picture);
        var (cx, cy) = look.Clamp!.Value;
        Assert.True(cx < w && cy < h, $"{item}'s clamp lies outside its picture");
        var at = BenchLook.ToolAt(look);
        // Drawn at its size, its clamp on the jaws to the nearest pixel.
        var (dw, dh) = ((int)Math.Ceiling(w * look.Size), (int)Math.Ceiling(h * look.Size));
        Assert.True(BenchLook.Fits(at, dw, dh), $"{item} at {at}, {dw}×{dh}, sticks out past the bench");
        Assert.InRange(at.X + cx * look.Size - BenchLook.Clamp.X, -0.5, 0.5);
        Assert.InRange(at.Y + cy * look.Size - BenchLook.Clamp.Y, -0.5, 0.5);
        // Every frame it is drawn in is its picture's size, so a spun or loaded one lies alike (only the still one is drawn there).
        foreach (var frame in look.Spin ?? []) Assert.Equal((w, h), Png.SizeOf(frame));
    }

    [Fact]
    public void AnOverhangIsBoundedOnEverySideAndNeverAboveThePicture()
    {
        Assert.True(BenchLook.Fits((0, 4), 33, 15));
        Assert.True(BenchLook.Fits((-BenchLook.Overhang, 0), BenchLook.Width + 2 * BenchLook.Overhang, BenchLook.TableTop + BenchLook.Hang));
        Assert.False(BenchLook.Fits((-BenchLook.Overhang - 1, 0), 8, 8));
        Assert.False(BenchLook.Fits((BenchLook.Width + BenchLook.Overhang - 7, 0), 8, 8));
        Assert.False(BenchLook.Fits((0, -1), 8, 8));
        Assert.False(BenchLook.Fits((0, BenchLook.TableTop + BenchLook.Hang - 7), 8, 8));
    }

    [Fact]
    public void TheDrillLiesAtItsSizeByItsMotorItsCoilOverTheTable()
    {
        // The vise's manifest drew it whole at (0, 4); at three quarters (tool-size) it lies at (4, 6), its coil from (6.25, 8.6) to (12.25, 10.9).
        var drill = HeldLook.Drill;
        Assert.Equal((4, 6), BenchLook.ToolAt(drill));
        var box = drill.Mounts![0].Box;
        var (x0, y0, x1, y1) = (4 + box.X0 * drill.Size, 6 + box.Y0 * drill.Size, 4 + box.X1 * drill.Size, 6 + box.Y1 * drill.Size);
        Assert.True(x0 >= 0 && x1 <= BenchLook.Width && y1 <= BenchLook.TableTop, $"its coil at ({x0}, {y0})–({x1}, {y1}) is off the table");
    }

    [Fact]
    public void ItsPicturesAreThereTheJawsCloseWhereToolsAreClampedAndItsArcsLeaveTheShardAlight()
    {
        foreach (var name in (string[])[BenchLook.Body, BenchLook.Alight, BenchLook.Front])
            Assert.Equal((BenchLook.Width, BenchLook.Height), Png.SizeOf(name));
        foreach (var name in (string[])[BenchLook.Glow, BenchLook.FitGlow])
            Assert.Equal((BenchLook.Width + 2 * BenchLook.GlowMargin, BenchLook.Height + 2 * BenchLook.GlowMargin), Png.SizeOf(name));
        Assert.Equal($"machine_{BenchLook.Kind}", BenchLook.Body);
        // Four cells across, four tall: its footprint is its whole picture, the vise in it (2026-11-04-footprint-is-the-picture).
        var spec = Spec.Machines.Get(BenchLook.Kind);
        Assert.Equal((4, 4), (spec.Width, spec.Height));
        Assert.Equal((BenchLook.Width, BenchLook.Height), (spec.Width * Camera.Cell, spec.Height * Camera.Cell));

        var front = Pixels(BenchLook.Front);
        var jaws = Enumerable.Range(0, 32 * 32).Where(i => front[i] >> 24 != 0).ToArray();
        Assert.Equal(BenchLook.Clamp.X, (jaws.Min(i => i % 32) + jaws.Max(i => i % 32) + 1) / 2.0, 3);
        Assert.Equal(BenchLook.Clamp.Y, (jaws.Min(i => i / 32) + jaws.Max(i => i / 32) + 1) / 2.0, 3);

        // The shard alight is a wash: an arc leaves from it within a pixel of its paint.
        var (_, _, alight) = Png.Logical(BenchLook.Alight, soft: true);
        bool Lit(float x, float y) =>
            Enumerable.Range(-1, 3).Any(dy => Enumerable.Range(-1, 3).Any(dx => alight[((int)y + dy) * 32 + (int)x + dx] >> 24 > 20));
        Assert.True(Lit(BenchLook.Idle.X, BenchLook.Idle.Y));
        Assert.All(BenchLook.Sources, s => Assert.True(Lit(s.X, s.Y), $"{s}"));
        // Nothing of the body but the shard is drawn alight.
        var body = Pixels(BenchLook.Body);
        for (int i = 0; i < 32 * 32; i++)
            if (alight[i] >> 24 > 20) Assert.True(body[i] >> 24 != 0, $"alight over nothing at {i % 32}, {i / 32}");
    }

    /// <summary>A square picture <paramref name="size"/> wide at the world's pixels: each pixel's ARGB.</summary>
    private static uint[] Pixels(string name, int size = 32)
    {
        var (w, h, argb) = Png.Logical(name);
        Assert.Equal((size, size), (w, h));
        return argb;
    }
}
