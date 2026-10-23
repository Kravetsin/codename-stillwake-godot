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
    /// <summary>What is added to the cue's volume, in decibels: a strike's own spread, a loop's envelope.</summary>
    public float OffsetDb { get; set; }

    public Heard2D(S.Reach reach, float volumeDb)
    {
        (_reach, _volumeDb) = (reach, volumeDb);
        Attenuation = 0;
        MaxDistance = (reach.Silent + 2) * ShipCamera.Cell;
        VolumeDb = volumeDb;
    }

    /// <summary>Moves it to <paramref name="at"/>, in the world's pixels, and plays it there, at once as loud as it is heard from there.</summary>
    public void PlayAt(Vector2 at) => PlayAt(at, PitchScale, 0);

    /// <summary>Plays it at <paramref name="at"/> at <paramref name="pitch"/>, <paramref name="offsetDb"/> louder or quieter than its cue.</summary>
    public void PlayAt(Vector2 at, float pitch, float offsetDb)
    {
        (PitchScale, OffsetDb) = (pitch, offsetDb);
        GlobalPosition = at;
        Hear();
        Play();
    }

    public override void _Process(double delta) => Hear();

    private void Hear()
    {
        if (!IsInsideTree()) return;
        var ear = GetViewport().GetCamera2D()?.GetScreenCenterPosition() ?? GlobalPosition;
        VolumeDb = _volumeDb + OffsetDb + Mathf.LinearToDb(Math.Max(_reach.Share(GlobalPosition.DistanceTo(ear) / ShipCamera.Cell), 1e-4f));
    }
}
