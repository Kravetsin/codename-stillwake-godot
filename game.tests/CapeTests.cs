using System;
using System.Linq;
using Stillwake.Game.Features.Crew;
using Stillwake.Game.Render;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>A sheet of cloth (<see cref="Cloth"/>) and the cape made of one (<see cref="Cape"/>).</summary>
public class CapeTests
{
    [Fact]
    public void AClothHungFromItsTopRowHangsBelowItKeepingItsWidth()
    {
        var start = Enumerable.Range(0, 12).Select(i => ((double)(i % 3) * 4, (double)(i / 3) * 4)).ToArray();
        var cloth = new Cloth(new ClothStyle(Keep: 0.98, Gravity: 0.4, Shear: 0.5, Passes: 10), 3, 4, start);
        for (int i = 0; i < 600; i++) cloth.Update([0, 1, 2], p => start[p], Rope.StepMs);
        var (left, right) = (cloth.At(9), cloth.At(11));
        Assert.InRange(right.X - left.X, 7.5, 8.5);
        Assert.InRange(left.Y, 11, 13.5);
    }

    /// <summary>The figure's frame is the world's, its feet at <c>Feet</c>, turned by <c>Facing</c>.</summary>
    private sealed class Frame
    {
        public double Feet, Facing = 1;
        public (double X, double Y) ToWorld((double X, double Y) p) => (Feet + p.X * Facing, p.Y);
        public (double X, double Y) ToFrame((double X, double Y) p) => ((p.X - Feet) * Facing, p.Y);
    }

    /// <summary>A back like the hero's: the air pack behind the shoulders to the hips, the legs below.</summary>
    private static double Back(double y) => y < -27 ? 6.5 : y < -14 ? 9.9 : 5;

    private static double HemOffRest(Cape cape) => cape.Points[^1].X - cape.Rest(Cape.Cols * Cape.Rows - 1).X;

    [Fact]
    public void ACapeHangsAsItWasCutAndOnARunTrailsBack()
    {
        var f = new Frame();
        var cape = new Cape(inside: false);
        for (int i = 0; i < 300; i++) cape.Step(Rope.StepMs, f.ToWorld, f.ToFrame, Back);
        Assert.InRange(HemOffRest(cape), -2.5, 2.5);
        // Its back edge lies over the pack, not standing off it: no hump; below the pack it flares as it was cut.
        for (int r = 1; r < 4; r++)
        {
            var p = cape.Points[r * Cape.Cols + Cape.Cols - 1];
            Assert.InRange(p.X, -Back(p.Y) - 1.5, -Back(p.Y) + 0.01);
        }
        Assert.DoesNotContain(true, Enumerable.Range(0, Cape.Triangles.Length / 3).Select(cape.Flipped));
        for (int i = 0; i < 90; i++)
        {
            f.Feet += 211.0 / 60;
            cape.Step(Rope.StepMs, f.ToWorld, f.ToFrame, Back);
        }
        Assert.True(HemOffRest(cape) < -4, $"the hem only {HemOffRest(cape)} back of where it hangs");
    }

    [Fact]
    public void TurningSwingsItRoundTheBackShowingItsLiningNeverThroughTheChest()
    {
        var f = new Frame();
        var cape = new Cape(inside: false);
        for (int i = 0; i < 300; i++) cape.Step(Rope.StepMs, f.ToWorld, f.ToFrame, Back);
        f.Facing = -1;
        bool lined = false;
        double shoulders = cape.Rest(Cape.Cols).Y, most = double.MinValue;
        for (int i = 0; i < 120; i++)
        {
            cape.Step(Rope.StepMs, f.ToWorld, f.ToFrame, Back);
            lined |= Enumerable.Range(0, Cape.Triangles.Length / 3).Any(cape.Flipped);
            foreach (var p in cape.Points.ToArray().Where(p => p.Y > shoulders)) most = Math.Max(most, p.X);
        }
        Assert.True(lined, "it never turned its lining to the eye");
        Assert.True(most <= 1.01, $"a point came to {most}, in front of the chest");
    }
}
