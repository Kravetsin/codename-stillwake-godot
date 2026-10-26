using System.Collections.Generic;
using Godot;
using Stillwake.Game.Features.Effects;
using Stillwake.Game.Features.Sound;
using Stillwake.Game.Render;
using Stillwake.Sim;
using S = Stillwake.Game.Sound;

namespace Stillwake.Game.Shared;

/// <summary>
/// Shots in flight drawn (shots-are-the-sims): each a streak behind its point along its way (<see cref="ShotLooks"/>), handed in
/// every frame; the sim's by <c>ShotsView</c>, this player's own by its hero's hands, which fly them ahead of the sim's.
/// </summary>
public partial class ShotStreaks : Node2D
{
    private readonly List<(Vector2 At, Vector2 Way, ShotLook Look)> _streaks = [];

    /// <summary>The streaks this frame: each shot's point and way, in pixels of the world, and its look.</summary>
    public void Show(IEnumerable<(Vector2 At, Vector2 Way, ShotLook Look)> streaks)
    {
        _streaks.Clear();
        _streaks.AddRange(streaks);
        QueueRedraw();
    }

    public override void _Draw()
    {
        foreach (var (at, way, look) in _streaks)
        {
            var tail = at - way.Normalized() * (float)look.Length;
            var head = new Color(look.Head);
            DrawLine(tail, at, head with { A = 0 }, 1);
            DrawLine(at.Lerp(tail, 0.35f), at, head, 1);
        }
    }

    /// <summary>A shot's point <paramref name="progress"/> of the way from <paramref name="was"/> to <paramref name="now"/>, and its way, in pixels of the world.</summary>
    public static (Vector2 At, Vector2 Way) Between(ShotMotion was, ShotMotion now, double progress)
    {
        float k = (float)(Camera.Cell / 1000);
        var from = new Vector2(was.X, was.Y) * k;
        var to = new Vector2(now.X, now.Y) * k;
        var way = to - from;
        return (from.Lerp(to, (float)progress), way == Vector2.Zero ? new Vector2(now.VX, now.VY) : way);
    }
}

/// <summary>Where a shot ends (shots-are-the-sims), shown and heard as what it met: the ground's hit, a wreck's chop; a creature's is its own.</summary>
public static class ShotEnds
{
    /// <summary>
    /// A shot of <paramref name="projectile"/> ended in <paramref name="cell"/>, coming from <paramref name="from"/> (cells): on a wall or
    /// a built piece, its hit heard on <paramref name="voices"/> and thrown off the face turned toward it; on a wreck, its chop.
    /// </summary>
    public static void Show(Registry m, Voices voices, string projectile, int cell, (double X, double Y) from)
    {
        var grid = m.Grid;
        int damage = Content.Shots.Get(projectile).BlockDamage;
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
