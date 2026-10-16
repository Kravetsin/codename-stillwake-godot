using System;
using Godot;
using Stillwake.Game.Render;
using S = Stillwake.Game.Sound;

namespace Stillwake.Game.Shared;

/// <summary>
/// A sound placed in the world, as loud as its distance from the listener — the middle of the screen,
/// where the hero is — says (<see cref="S.Reach"/>): full up to <c>Full</c> cells, then fading as the
/// square of what is left of the way to <c>Silent</c>. Godot's own fall-off has no full-volume stretch,
/// so it is turned off and this sets the volume every frame; Godot still pans it.
/// </summary>
public partial class Heard2D : AudioStreamPlayer2D
{
    private readonly S.Reach _reach;
    private readonly float _volumeDb;

    public Heard2D(S.Reach reach, float volumeDb)
    {
        (_reach, _volumeDb) = (reach, volumeDb);
        Attenuation = 0;
        MaxDistance = (reach.Silent + 1) * ShipCamera.Cell;
        VolumeDb = volumeDb;
    }

    /// <summary>Moves it to <paramref name="at"/>, in the world's pixels, and plays it there, at once as loud as it is heard from there.</summary>
    public void PlayAt(Vector2 at)
    {
        GlobalPosition = at;
        Hear();
        Play();
    }

    public override void _Process(double delta) => Hear();

    private void Hear()
    {
        if (!IsInsideTree()) return;
        var ear = GetViewport().GetCamera2D()?.GetScreenCenterPosition() ?? GlobalPosition;
        VolumeDb = _volumeDb + Mathf.LinearToDb(Math.Max(_reach.Share(GlobalPosition.DistanceTo(ear) / ShipCamera.Cell), 1e-4f));
    }
}
