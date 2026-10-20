using System;
using Godot;
using Stillwake.Game.Render;

namespace Stillwake.Game.Dev;

/// <summary>
/// <c>-- --planet --sweep=DEPTH</c>: the camera, not the hero, runs at a sprint's pace along the row <c>DEPTH</c> cells under
/// the spawn, through whatever ground and caves are there, <see cref="Cells"/> to the right and back; to measure what new ground
/// coming into view costs a frame (<c>--frames</c>) where the hero could not run. Processed after <c>Main</c>, so it moves the
/// camera the hero just centred.
/// </summary>
public partial class CameraSweep(Camera2D camera, Func<Vector2?> start, int depth) : Node
{
    /// <summary>How far the sweep runs, in cells, and how fast: a sprint's 0.22 cells a 60 Hz step.</summary>
    private const float Cells = 400, Speed = 0.22f * 60;

    private Vector2? _from;
    private double _seconds;

    public override void _Process(double delta)
    {
        if (_from == null)
        {
            if (start() is not { } at) return;
            _from = at + new Vector2(0, depth);
        }
        _seconds += delta;
        // A second still, then out and back.
        float run = (float)Math.Max(0, _seconds - 1) * Speed, along = run <= Cells ? run : Math.Max(0, 2 * Cells - run);
        camera.Position = (_from.Value + new Vector2(along, 0)) * ShipCamera.Cell;
    }
}
