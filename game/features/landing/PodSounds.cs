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
/// surface hears it, host and guests alike, as loud as their distance says. Its engine burns through the drop, the touchdown and
/// the lift-off (a loop with an envelope); as its flame reaches the ground the engine swells and the blasted grit roars, briefly
/// (<see cref="FlightTimeline.Blast"/>). Its clamps let go as it drops or lifts off; it touches down with a thump and its legs'
/// clunk, the engine cut, again softer at its bounce; docking aboard, back from a planet, it gives a short burst, a thump and its
/// clamps closing. Told each frame what moves by <see cref="PodFlights"/>; a pod no longer moving fades out.
/// </summary>
public partial class PodSounds : Node2D
{
    /// <summary>What a pod is doing: dropping from the ship, coming down on a planet, lifting off it, docking back aboard.</summary>
    public enum Motion { Falling, Coming, Lifting, Docking }

    private sealed class Voice
    {
        public required Heard2D Engine, Blast, Release, Touchdown;
        public LoopEnvelope Envelope;
        public double Last = double.NaN;
        public bool Moving;
    }

    private readonly Dictionary<(Motion, int), Voice> _voices = [];

    /// <summary>
    /// Pod <paramref name="id"/>, doing <paramref name="motion"/> <paramref name="t"/> seconds in, its bottom at <paramref name="at"/>
    /// in the world's pixels, its engine <paramref name="burning"/> or not, its flame blasting the ground by <paramref name="blast"/>
    /// (0 to 1), a frame of <paramref name="delta"/> seconds.
    /// </summary>
    public void Heard(Motion motion, int id, Vector2 at, double t, bool burning, double blast, double delta)
    {
        if (!_voices.TryGetValue((motion, id), out var voice)) _voices[(motion, id)] = voice = Made();
        voice.Moving = true;
        // Cut short as it touches down or docks: the engine stops, it does not wind down.
        double release = motion is Motion.Coming or Motion.Docking ? 0.12 : S.PodThrustReleaseS;
        voice.Envelope.Step(burning, delta, S.PodThrustAttackS, release);
        foreach (var player in new[] { voice.Engine, voice.Blast }) player.GlobalPosition = at;
        voice.Engine.OffsetDb = voice.Envelope.GainDb + S.PodBlastDb * (float)blast;
        voice.Blast.OffsetDb = voice.Envelope.GainDb + Mathf.LinearToDb(Mathf.Max((float)blast, 1e-4f));
        Loop(voice.Engine, voice.Envelope.Playing);
        Loop(voice.Blast, voice.Envelope.Playing && blast > 0.01);
        bool first = double.IsNaN(voice.Last);
        switch (motion)
        {
            case Motion.Falling when Crossed(voice.Last, t, DropFrom):
            // A lift-off is first seen as it begins: its clamps let go then.
            case Motion.Lifting when first && t < 0.1:
                voice.Release.PlayAt(at);
                break;
            case Motion.Coming when Crossed(voice.Last, t, Descend):
                voice.Touchdown.PlayAt(at);
                voice.Release.PlayAt(at, voice.Release.PitchScale * 0.85f, -4);
                break;
            case Motion.Coming when Crossed(voice.Last, t, BounceTo):
                voice.Touchdown.PlayAt(at, voice.Touchdown.PitchScale, -8);
                break;
            case Motion.Docking when first && t < 0.1:
                voice.Touchdown.PlayAt(at, voice.Touchdown.PitchScale * 1.1f, -3);
                voice.Release.PlayAt(at);
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
                Loop(voice.Engine, voice.Envelope.Playing);
                Loop(voice.Blast, false);
                if (!voice.Envelope.Playing && !voice.Release.Playing && !voice.Touchdown.Playing)
                {
                    foreach (var player in Players(voice)) player.QueueFree();
                    _voices.Remove(key);
                    continue;
                }
            }
            voice.Moving = false;
        }
    }

    private static void Loop(Heard2D loop, bool on)
    {
        if (on && !loop.Playing) loop.Play();
        else if (!on && loop.Playing) loop.Stop();
    }

    private static Heard2D[] Players(Voice voice) => [voice.Engine, voice.Blast, voice.Release, voice.Touchdown];

    private Voice Made()
    {
        var voice = new Voice
        {
            Engine = Cues.Player2D(S.PodThrust, S.World),
            Blast = Cues.Player2D(S.PodBlast, S.World),
            Release = Cues.Player2D(S.PodRelease, S.World, voices: 2),
            Touchdown = Cues.Player2D(S.PodTouchdown, S.World, voices: 2),
        };
        foreach (var player in Players(voice)) AddChild(player);
        return voice;
    }
}
