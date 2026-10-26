using Godot;
using Stillwake.Game.Features.Effects;
using Stillwake.Game.Features.Sound;
using Stillwake.Game.Render;
using Stillwake.Sim;
using S = Stillwake.Game.Sound;

namespace Stillwake.Game.Shared;

/// <summary>Where a shot ends (shots-are-the-sims), shown and heard as what it met: the ground's hit, a wreck's chop; a creature's is its own.</summary>
public static class ShotEnds
{
    /// <summary>
    /// A shot of <paramref name="projectile"/> from <paramref name="gun"/> ended in <paramref name="cell"/>, coming from
    /// <paramref name="from"/> (cells): on a wall or a built piece, its hit heard on <paramref name="voices"/> and thrown off the face
    /// turned toward it; on a wreck, its chop.
    /// </summary>
    public static void Show(Registry m, Voices voices, string? gun, string projectile, int cell, (double X, double Y) from)
    {
        var grid = m.Grid;
        int damage = Content.Shots.BlockDamageOf(gun, projectile);
        if (grid.Solid(cell))
        {
            var at = Impacts.On(grid, cell, from.X, from.Y);
            var point = new Vector2((float)(at.X * Camera.Cell), (float)(at.Y * Camera.Cell));
            if (GroundSounds.HitAt(grid, m.Building, cell, damage) is { } hit) voices.Play(hit, point);
            if (Impacts.At(grid, m.Building, cell) is { } impact)
                HitBurst.Shared.Throw(HitLooks.Of[impact], point.X, point.Y, at.Nx, at.Ny, Impacts.ShareAt(grid, m.Building, cell, projectile, damage),
                    Impacts.FloorUnder(grid, at) * Camera.Cell);
            return;
        }
        var middle = new Vector2((float)((cell % grid.Width + 0.5) * Camera.Cell), (float)((cell / grid.Width + 0.5) * Camera.Cell));
        if (IsWreck(m, cell))
        {
            voices.Play(S.Chop, middle);
            HitBurst.Shared.Throw(HitLooks.Of[Impact.Wreck], middle.X, middle.Y, from.X < cell % grid.Width ? -1 : 1, 0, 1);
        }
    }

    private static bool IsWreck(Registry m, int cell)
    {
        foreach (var w in m.Debris.Wrecks())
            if (w.Covers(cell, m.Grid.Width)) return true;
        return false;
    }
}
