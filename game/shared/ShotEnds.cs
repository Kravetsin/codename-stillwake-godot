using Godot;
using Stillwake.Game.Features.Effects;
using Stillwake.Game.Render;
using Stillwake.Sim;
using Stillwake.Sim.Creatures;
using S = Stillwake.Game.Sound;

namespace Stillwake.Game.Shared;

/// <summary>
/// A shot's hit (shots-are-the-sims, 2026-10-27-shots-pierce), heard and shown as what it met: the ground's chips, a wreck's; a
/// creature's is its own.
/// </summary>
public static class ShotEnds
{
    /// <summary>
    /// <paramref name="hit"/>, heard on <paramref name="voices"/>: on a wall or a built piece thrown off the face turned toward
    /// where the shot came from, chips only while something stands there to throw them, what broke showing its own breaking; on a
    /// wreck, off its side. Drawn by what the sim met, not the grid now, where what broke is gone.
    /// </summary>
    public static void Show(Registry m, Voices voices, ShotHit hit)
    {
        var grid = m.Grid;
        int cell = hit.Cell;
        (double X, double Y) from = (hit.From.X / 1000.0, hit.From.Y / 1000.0);
        switch (hit.What)
        {
            case ShotTarget.Solid:
                var at = Impacts.On(grid, cell, from.X, from.Y);
                var point = new Vector2((float)(at.X * Camera.Cell), (float)(at.Y * Camera.Cell));
                voices.Play(S.NailHit, point);
                if (Impacts.At(grid, m.Building, cell) is { } impact)
                    Stages.Of(Layers.Effects).Hits.Throw(HitLooks.Of[impact], point.X, point.Y, at.Nx, at.Ny,
                        Impacts.ShareAt(grid, m.Building, cell, hit.Projectile, Content.Shots.BlockDamageOf(hit.Gun, hit.Projectile)),
                        Impacts.FloorUnder(grid, at) * Camera.Cell);
                return;
            case ShotTarget.Wreck:
                var middle = new Vector2((float)((cell % grid.Width + 0.5) * Camera.Cell), (float)((cell / grid.Width + 0.5) * Camera.Cell));
                voices.Play(S.NailHit, middle);
                Stages.Of(Layers.Effects).Hits.Throw(HitLooks.Of[Impact.Wreck], middle.X, middle.Y, from.X < cell % grid.Width ? -1 : 1, 0, 1);
                return;
        }
    }
}
