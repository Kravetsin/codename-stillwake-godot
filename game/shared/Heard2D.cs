using System;
using System.Collections.Generic;
using Godot;
using Stillwake.Game.Features.Sound;
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
    /// <summary>Who takes a sound played again before Godot starts the last (<see cref="VoiceStarts"/>): else it would replace it.</summary>
    private VoiceStarts _starts;
    /// <summary>The voices past this one, made the first time a physics tick asks for them, each placed on its own.</summary>
    private readonly List<Heard2D> _spares = [];
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

    /// <summary>
    /// Plays it at <paramref name="at"/> at <paramref name="pitch"/>, <paramref name="offsetDb"/> louder or quieter than its cue:
    /// played again within the same physics tick, on a spare voice, up to its <c>MaxPolyphony</c>.
    /// </summary>
    public void PlayAt(Vector2 at, float pitch, float offsetDb)
    {
        int voice = _starts.Take(Engine.GetPhysicsFrames(), MaxPolyphony);
        (voice == 0 ? this : Spare(voice - 1)).Start(at, pitch, offsetDb);
    }

    private void Start(Vector2 at, float pitch, float offsetDb)
    {
        (PitchScale, OffsetDb) = (pitch, offsetDb);
        GlobalPosition = at;
        Play();
    }

    /// <summary>Spare voice <paramref name="index"/>: the same cue on the same bus, one sound at a time, placed apart from this one.</summary>
    private Heard2D Spare(int index)
    {
        while (_spares.Count <= index)
        {
            var spare = new Heard2D(_reach, _volumeDb) { Stream = Stream, Bus = Bus, TopLevel = true };
            AddChild(spare);
            _spares.Add(spare);
        }
        return _spares[index];
    }

    /// <summary>Plays it where it stands from <paramref name="fromPosition"/> seconds in, at once as loud as it is heard from there.</summary>
    public new void Play(float fromPosition = 0)
    {
        Hear();
        base.Play(fromPosition);
    }

    // Only a voice playing is heard each frame, one starting as it starts (Play): a planet holds some 70, 0.1 ms a frame (planet-frame-rate).
    public override void _Process(double delta)
    {
        if (Playing) Hear();
    }

    private void Hear()
    {
        if (!IsInsideTree()) return;
        var ear = GetViewport().GetCamera2D()?.GetScreenCenterPosition() ?? GlobalPosition;
        VolumeDb = _volumeDb + OffsetDb + Mathf.LinearToDb(Math.Max(_reach.Share(GlobalPosition.DistanceTo(ear) / ShipCamera.Cell), 1e-4f));
    }
}
