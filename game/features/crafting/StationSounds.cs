using System;
using System.Collections.Generic;
using Godot;
using Stillwake.Game.Features.Sound;
using Stillwake.Game.Shared;
using S = Stillwake.Game.Sound;

namespace Stillwake.Game.Features.Crafting;

/// <summary>
/// A station heard at work where it stands, on the World bus (0059), told by <see cref="StationView"/> every frame what it shows:
/// the Former's press strikes each time its head comes down in its pictures, over its motor spinning up, dipping at each stroke
/// and running down after; the electric smelter's coils hum, risen and fallen, and its melt pops at random moments. Each
/// machine has its own players, its loop begun somewhere along it, so two never sound in step.
/// </summary>
public partial class StationSounds : Node2D
{
    private sealed class Heard
    {
        public Heard2D? Loop, Shots;
        public MotorSound Motor;
        public LoopEnvelope Envelope;
        public FrameBeat Beat;
        public RandomTrain Pops;
        public bool Seen;
    }

    private readonly Dictionary<int, Heard> _heard = new();
    private readonly List<int> _gone = [];
    private readonly Random _random = new();

    /// <summary>
    /// Machine <paramref name="id"/> of <paramref name="kind"/> this frame of <paramref name="elapsedS"/> seconds, its middle at
    /// <paramref name="at"/>, working or not; its pictures at step <paramref name="step"/> of <paramref name="frames"/>, its press
    /// head down on picture <paramref name="down"/>. A kind with no sound of its own is passed over.
    /// </summary>
    public void Hear(int id, string kind, Vector2 at, bool works, long step, int frames, int? down, double elapsedS)
    {
        if (kind is not ("former" or "smelter")) return;
        if (!_heard.TryGetValue(id, out var heard)) _heard[id] = heard = Made(kind);
        heard.Seen = true;
        heard.Loop!.Position = heard.Shots!.Position = at;
        if (kind == "former") Press(heard, works, step, frames, down ?? 0, elapsedS);
        else Smelt(heard, works, elapsedS);
    }

    /// <summary>Every machine not heard this frame, gone from the grid, silenced at once.</summary>
    public void Forget()
    {
        _gone.Clear();
        foreach (var (id, heard) in _heard)
            if (!heard.Seen) _gone.Add(id);
            else heard.Seen = false;
        foreach (int id in _gone)
        {
            _heard[id].Loop!.QueueFree();
            _heard[id].Shots!.QueueFree();
            _heard.Remove(id);
        }
    }

    private Heard Made(string kind)
    {
        var heard = kind == "former"
            ? new Heard { Loop = Cues.Player2D(S.FormerMotor, S.World), Shots = Cues.Player2D(S.FormerStroke, S.World, voices: 2) }
            : new Heard { Loop = Cues.Player2D(S.SmelterHum, S.World), Shots = Cues.Player2D(S.SmelterCrackle, S.World, voices: 3) };
        AddChild(heard.Loop);
        AddChild(heard.Shots);
        return heard;
    }

    /// <summary>The Former: a stroke as its head comes down, the motor dipping with it; spun up while it works, run down after.</summary>
    private void Press(Heard heard, bool works, long step, int frames, int down, double elapsedS)
    {
        bool struck = heard.Beat.Struck(works, step, frames, down);
        heard.Motor.Step(works, false, elapsedS, S.FormerSpin);
        if (struck)
        {
            heard.Motor.Kick();
            var (pitch, offsetDb) = StrikeTrain.Vary(_random, S.FormerStrokePitch, S.FormerStrokeVolumeDb);
            heard.Shots!.PlayAt(heard.Shots.GlobalPosition, pitch, offsetDb);
        }
        Loop(heard.Loop!, heard.Motor.Playing);
        heard.Loop!.PitchScale = (float)heard.Motor.Speed(S.FormerSpin);
        heard.Loop.OffsetDb = heard.Motor.GainDb(S.FormerSpin);
    }

    /// <summary>The smelter: its hum risen while it works and fallen after; its melt popping at random moments while it works.</summary>
    private void Smelt(Heard heard, bool works, double elapsedS)
    {
        heard.Envelope.Step(works, elapsedS, S.SmelterHumInS, S.SmelterHumOutS);
        Loop(heard.Loop!, heard.Envelope.Playing);
        heard.Loop!.OffsetDb = heard.Envelope.GainDb;
        int due = heard.Pops.Due(elapsedS, works, S.SmelterCracklesPerSecond, _random);
        for (int i = 0; i < due; i++)
        {
            var (pitch, _) = StrikeTrain.Vary(_random, S.SmelterCracklePitch, 0);
            heard.Shots!.PlayAt(heard.Shots.GlobalPosition, pitch, (float)(_random.NextDouble() * S.SmelterCrackleLowDb));
        }
    }

    /// <summary>A loop started somewhere along it while heard, stopped once not.</summary>
    private void Loop(Heard2D loop, bool playing)
    {
        if (playing && !loop.Playing) loop.Play((float)(_random.NextDouble() * loop.Stream.GetLength()));
        else if (!playing && loop.Playing) loop.Stop();
    }
}
