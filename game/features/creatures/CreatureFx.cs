using System;
using System.Collections.Generic;
using Godot;
using Stillwake.Game.Render;

namespace Stillwake.Game.Features.Creatures;

/// <summary>What the creatures' views share of a blow: whom it faces and its sparks.</summary>
internal static class CreatureFx
{
    /// <summary>Which of <paramref name="heroes"/> is nearest <paramref name="at"/>; none with no hero.</summary>
    public static Vector2? NearestOf(IReadOnlyList<(float X, float Y)> heroes, Vector2 at)
    {
        Vector2? nearest = null;
        float best = float.MaxValue;
        foreach (var (x, y) in heroes)
        {
            var hero = new Vector2(x, y);
            float d = hero.DistanceSquaredTo(at);
            if (d >= best) continue;
            (best, nearest) = (d, hero);
        }
        return nearest;
    }

    /// <summary>The page's spark burst, its <c>fine</c> sparks: a spray along <paramref name="heading"/>, dust, a warm flash.</summary>
    public static void Sparks(Stage stage, Vector2 at, double heading, double scale)
    {
        var glims = stage.Glims;
        glims.Spray(Luminous.FineSpark, at.X, at.Y, (int)Math.Round(28 * scale * Fx.Count), heading, (-1.1, 1.1), (70, 230));
        for (int i = 0; i < Math.Round(5 * scale); i++) glims.Add(Luminous.HitDust, at.X, at.Y, glims.Between(-20, 20), glims.Between(-20, 20));
        stage.Flares.Flash(at.X, at.Y, (255, 220, 140), 0.5 * scale * Fx.Glow, 8, CreatureLooks.LightKeep, 0.8);
    }
}
