using System;
using Godot;
using Stillwake.Game.Shared;

namespace Stillwake.Game.Features.Sound;

/// <summary>
/// Every <see cref="EveryS"/> seconds, a line in the log counting the sound players in the tree and those playing, with the
/// frame rate and the hearing (heard-every-other): to see what grows from one descent to the next in a co-op session.
/// </summary>
public partial class AudioCensus(Action<string> log, SoundView sound, Func<int?> surface) : Node
{
    /// <summary>How often a line is written.</summary>
    public const double EveryS = 10;

    private double _since;

    public override void _Process(double delta)
    {
        if ((_since += delta) < EveryS) return;
        _since = 0;
        var tally = new AudioTally();
        Walk(GetTree().Root, tally);
        string on = surface() is int s ? $"surface {s}" : "no world";
        log($"sound on {on}: {tally.Line()}; {Engine.GetFramesPerSecond():F0} fps, physics {Engine.PhysicsTicksPerSecond}/s, {sound.Census()}");
    }

    private static void Walk(Node node, AudioTally tally)
    {
        if (node is AudioStreamPlayer anywhere) tally.Add(KeyOf(node, anywhere.Stream), anywhere.Playing);
        else if (node is AudioStreamPlayer2D placed) tally.Add(KeyOf(node, placed.Stream), placed.Playing);
        foreach (var child in node.GetChildren()) Walk(child, tally);
    }

    /// <summary>Where a player hangs, past the pools it may sit in, and the first file it plays.</summary>
    private static string KeyOf(Node player, AudioStream? stream)
    {
        var owner = player.GetParent();
        while (owner is Voices or Heard2D) owner = owner.GetParent();
        return $"{owner?.GetType().Name ?? "-"}/{NameOf(stream)}";
    }

    private static string NameOf(AudioStream? stream) => stream switch
    {
        AudioStreamRandomizer shots when shots.StreamsCount > 0 => NameOf(shots.GetStream(0)),
        { ResourcePath.Length: > 0 } file => System.IO.Path.GetFileNameWithoutExtension(file.ResourcePath),
        _ => "?",
    };
}
