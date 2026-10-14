using System;
using Stillwake.Game.Features.Light;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>The light Godot's lamps pour on each cell together, which the lit shader shares out.</summary>
public class LightFieldTests
{
    // Light.Air, Light.Lamp, Light.LampMost, Light.Fire and Light.FireKeep as content has them.
    private const float Air = 0.88f, LampMost = 1f, FireKeep = 0.72f;
    private static readonly (float R, float G, float B) Lamp = (1.5f, 1.3f, 0.95f), Fire = (1f, 0.48f, 0.14f);

    private static (float R, float G, float B) At(LightField field, int x, int y)
    {
        int i = ((y - field.Y) * field.Width + x - field.X) * 4;
        return (field.Light[i], field.Light[i + 1], field.Light[i + 2]);
    }

    [Fact]
    public void ALampIsNearWhiteByItsBulbEachColourAtMostWholeAndFallsOffByTheAirItCrosses()
    {
        var field = new LightField(0, 0, 40, 40);
        field.Add(20.5f, 20.5f, Lamp, LampMost, (1, 1, 1), 16, Air);
        Assert.Equal((LampMost, LampMost, Lamp.B), At(field, 20, 20));
        float f = MathF.Pow(Air, 10);
        var (r, g, b) = At(field, 30, 20);
        Assert.Equal(Lamp.R * f, r, 4);
        Assert.Equal(Lamp.G * f, g, 4);
        Assert.Equal(Lamp.B * f, b, 4);
        Assert.Equal((0f, 0f, 0f), At(field, 37, 20));
    }

    [Fact]
    public void AFirePoolsAtItsFurnaceWhereALampLightsItsRoom()
    {
        var lamp = new LightField(0, 0, 40, 40);
        lamp.Add(20.5f, 20.5f, Lamp, LampMost, (1, 1, 1), 16, Air);
        var fire = new LightField(0, 0, 40, 40);
        fire.Add(20.5f, 20.5f, Fire, 1, (1, 1, 1), 8, FireKeep);
        Assert.Equal(Fire.R, At(fire, 20, 20).R, 4);
        // Six cells off a fire gives under a seventh of its light, a lamp still more than half of whole.
        Assert.True(At(fire, 26, 20).R < Fire.R / 7);
        Assert.True(At(lamp, 26, 20).R > 0.5f);
        Assert.Equal((0f, 0f, 0f), At(fire, 29, 20));
    }

    [Fact]
    public void AnEmberLightsTheCellsItPassesAndNoFurther()
    {
        var field = new LightField(0, 0, 20, 20);
        field.Add(10.5f, 10.5f, (1, 1, 1), 1, (0.5f, 0.24f, 0.06f), 3, 0.5f);
        Assert.Equal(0.5f, At(field, 10, 10).R, 4);
        Assert.True(At(field, 11, 10).R < 0.3f);
        Assert.Equal((0f, 0f, 0f), At(field, 13, 10));
    }

    [Fact]
    public void LightsTogetherAddUpPastWhole()
    {
        var lamp = new LightField(0, 0, 20, 20);
        lamp.Add(10.5f, 13.5f, Lamp, LampMost, (1, 1, 1), 16, Air);
        var fire = new LightField(0, 0, 20, 20);
        fire.Add(10f, 10.5f, Fire, 1, (1, 1, 1), 16, FireKeep);
        var both = new LightField(0, 0, 20, 20);
        both.Add(10.5f, 13.5f, Lamp, LampMost, (1, 1, 1), 16, Air);
        both.Add(10f, 10.5f, Fire, 1, (1, 1, 1), 16, FireKeep);
        both.Add(10.2f, 9.7f, (1, 1, 1), 1, (0.5f, 0.25f, 0.1f), 3, 0.5f);
        var (r, g, b) = At(both, 10, 10);
        // The fire's warmth over the lamp's light, the glow's over both: the lit shader scales the sum to fit.
        Assert.True(r > At(lamp, 10, 10).R + At(fire, 10, 10).R);
        Assert.True(r > 1);
        Assert.True(g > At(lamp, 10, 10).G + At(fire, 10, 10).G);
        Assert.True(b > At(lamp, 10, 10).B + At(fire, 10, 10).B);
    }

    [Fact]
    public void ALightAddsTheSameEveryTimeItsStrengthScaled()
    {
        var once = new LightField(0, 0, 20, 20);
        once.Add(5f, 7.5f, Fire, 1, (1, 1, 1), 16, FireKeep);
        var twice = new LightField(0, 0, 20, 20);
        twice.Add(5f, 7.5f, Fire, 1, (0.5f, 0.5f, 0.5f), 16, FireKeep);
        twice.Add(5f, 7.5f, Fire, 1, (0.5f, 0.5f, 0.5f), 16, FireKeep);
        for (int i = 0; i < once.Light.Length; i++) Assert.Equal(once.Light[i], twice.Light[i], 5);
    }

    [Fact]
    public void ALightOffTheFieldLightsOnlyTheCellsItHolds()
    {
        var field = new LightField(10, 10, 5, 5);
        field.Add(9.5f, 9.5f, Lamp, 1, (1, 1, 1), 16, Air);
        Assert.Equal(Lamp.B * MathF.Pow(Air, MathF.Sqrt(2)), At(field, 10, 10).B, 4);
        Assert.Equal(Lamp.B * MathF.Pow(Air, MathF.Sqrt(50)), At(field, 14, 14).B, 4);
    }
}
