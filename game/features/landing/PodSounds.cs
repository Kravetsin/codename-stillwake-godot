using System.Collections.Generic;
using System.Linq;
using Godot;
using Stillwake.Game.Features.Sound;
using Stillwake.Game.Shared;
using static Stillwake.Game.Features.Landing.FlightTimeline;
using S = Stillwake.Game.Sound;

namespace Stillwake.Game.Features.Landing;

/// <summary>
/// What a drop pod moving in the world sounds like (the descent's sounds), placed on it on the World bus, so everyone on its
/// surface hears it, host and guests alike, as loud as their distance says: its engine burning through the drop, the touchdown
/// and the lift-off (a loop with an envelope); its clamps letting go as it drops or lifts off; the thump as it touches down, and
/// again, softer, at its bounce. Told each frame what moves by <see cref="PodFlights"/>; a pod no longer moving fades out.
/// </summary>
public partial class PodSounds : Node2D
{
    /// <summary>What a pod is doing: dropping from the ship, coming down on a planet, lifting off it.</summary>
    public enum Motion { Falling, Coming, Lifting }

    private sealed class Voice
    {
        public required Heard2D Engine, Release, Touchdown;
        public LoopEnvelope Envelope;
        public double Last = double.NaN;
        public bool Moving;
    }

    private readonly Dictionary<(Motion, int), Voice> _voices = [];

    /// <summary>
    /// Pod <paramref name="id"/>, doing <paramref name="motion"/> <paramref name="t"/> seconds in, its bottom at <paramref name="at"/>
    /// in the world's pixels, its engine <paramref name="burning"/> or not, a frame of <paramref name="delta"/> seconds.
    /// </summary>
    public void Heard(Motion motion, int id, Vector2 at, double t, bool burning, double delta)
    {
        if (!_voices.TryGetValue((motion, id), out var voice)) _voices[(motion, id)] = voice = Made();
        voice.Moving = true;
        voice.Envelope.Step(burning, delta, S.PodThrustAttackS, S.PodThrustReleaseS);
        voice.Engine.GlobalPosition = at;
        voice.Engine.OffsetDb = voice.Envelope.GainDb;
        if (voice.Envelope.Playing && !voice.Engine.Playing) voice.Engine.Play();
        else if (!voice.Envelope.Playing && voice.Engine.Playing) voice.Engine.Stop();
        switch (motion)
        {
            case Motion.Falling when Crossed(voice.Last, t, DropFrom):
            // A lift-off is first seen as it begins: its clamps let go then.
            case Motion.Lifting when double.IsNaN(voice.Last) && t < 0.1:
                voice.Release.PlayAt(at);
                break;
            case Motion.Coming when Crossed(voice.Last, t, Descend):
                voice.Touchdown.PlayAt(at);
                break;
            case Motion.Coming when Crossed(voice.Last, t, BounceTo):
                voice.Touchdown.PlayAt(at, voice.Touchdown.PitchScale, -8);
                break;
        }
        voice.Last = t;
    }

    /// <summary>The frame's end: a pod not heard this frame burns out and, once silent, its voice goes.</summary>
    public void Settle(double delta)
    {
        foreach (var (key, voice) in _voices.ToList())
        {
            if (!voice.Moving)
            {
                voice.Envelope.Step(false, delta, S.PodThrustAttackS, S.PodThrustReleaseS);
                voice.Engine.OffsetDb = voice.Envelope.GainDb;
                if (!voice.Envelope.Playing && !voice.Release.Playing && !voice.Touchdown.Playing)
                {
                    foreach (var player in new[] { voice.Engine, voice.Release, voice.Touchdown }) player.QueueFree();
                    _voices.Remove(key);
                    continue;
                }
                if (!voice.Envelope.Playing) voice.Engine.Stop();
            }
            voice.Moving = false;
        }
    }

    private Voice Made()
    {
        var voice = new Voice
        {
            Engine = Cues.Player2D(S.PodThrust, S.World),
            Release = Cues.Player2D(S.PodRelease, S.World),
            Touchdown = Cues.Player2D(S.PodTouchdown, S.World, voices: 2),
        };
        foreach (var player in new[] { voice.Engine, voice.Release, voice.Touchdown }) AddChild(player);
        return voice;
    }
}
