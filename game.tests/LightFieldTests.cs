using System;
using Stillwake.Game.Features.Light;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>The light Godot's lamps pour on each cell together, which the lit shader shares out.</summary>
public class LightFieldTests
{
    // Light.Air, Light.Lamp and Light.Fire as content has them.
    private const float Air = 0.88f;
    private static readonly (float R, float G, float B) Lamp = (1.5f, 1.3f, 0.95f), Fire = (1f, 0.48f, 0.14f);

    private static (float R, float G, float B) At(LightField field, int x, int y)
    {
        int i = ((y - field.Y) * field.Width + x - field.X) * 4;
        return (field.Light[i], field.Light[i + 1], field.Light[i + 2]);
    }

    [Fact]
    public void ALampIsWhiteByItsBulbAndFallsOffByTheAirItCrosses()
    {
        var field = new LightField(0, 0, 40, 40, Air);
        field.Add(20.5f, 20.5f, Lamp, (1, 1, 1), 16);
        Assert.Equal((1f, 1f, Lamp.B), At(field, 20, 20));
        float f = MathF.Pow(Air, 10);
        var (r, g, b) = At(field, 30, 20);
        Assert.Equal(Lamp.R * f, r, 4);
        Assert.Equal(Lamp.G * f, g, 4);
        Assert.Equal(Lamp.B * f, b, 4);
        Assert.Equal((0f, 0f, 0f), At(field, 37, 20));
    }

    [Fact]
    public void LightsTogetherAddUpPastWholeAndTheBrightestOfThemIsKept()
    {
        var field = new LightField(0, 0, 20, 20, Air);
        field.Add(10.5f, 10.5f, Lamp, (1, 1, 1), 16);
        field.Add(10f, 10.5f, Fire, (1, 1, 1), 16);
        field.Add(10.2f, 9.7f, (1, 1, 1), (0.5f, 0.25f, 0.1f), 8);
        Assert.True(At(field, 10, 10).R > 2);
        int i = (10 * field.Width + 10) * 4;
        // The lamp, clamped at whole, is the brightest in red and green; in blue too, the fire and the glow far below it.
        Assert.Equal(1f, field.Brightest[i]);
        Assert.Equal(1f, field.Brightest[i + 1]);
        Assert.Equal(Lamp.B, field.Brightest[i + 2]);
    }

    [Fact]
    public void ALightAddsTheSameEveryTimeItsStrengthScaled()
    {
        var once = new LightField(0, 0, 20, 20, Air);
        once.Add(5f, 7.5f, Fire, (1, 1, 1), 16);
        var twice = new LightField(0, 0, 20, 20, Air);
        twice.Add(5f, 7.5f, Fire, (0.5f, 0.5f, 0.5f), 16);
        twice.Add(5f, 7.5f, Fire, (0.5f, 0.5f, 0.5f), 16);
        for (int i = 0; i < once.Light.Length; i++) Assert.Equal(once.Light[i], twice.Light[i], 5);
    }

    [Fact]
    public void ALightOffTheFieldLightsOnlyTheCellsItHolds()
    {
        var field = new LightField(10, 10, 5, 5, Air);
        field.Add(9.5f, 9.5f, Lamp, (1, 1, 1), 16);
        Assert.Equal(Lamp.B * MathF.Pow(Air, MathF.Sqrt(2)), At(field, 10, 10).B, 4);
        Assert.Equal(Lamp.B * MathF.Pow(Air, MathF.Sqrt(50)), At(field, 14, 14).B, 4);
    }
}
