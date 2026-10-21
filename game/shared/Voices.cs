using System.Collections.Generic;
using Godot;
using S = Stillwake.Game.Sound;

namespace Stillwake.Game.Shared;

/// <summary>
/// Players of whichever cues something plays, one a cue, made the first time each is played and kept as its children (0093):
/// a planet's breaks, falls and hits, chosen by what was there, and a hero's boots by what they stand on. Placed in the world, each
/// is heard from where it plays (<see cref="Heard2D"/>); else the same anywhere.
/// </summary>
public partial class Voices(string bus, int voices = 2, bool placed = true) : Node
{
    private readonly Dictionary<S.Cue, Node> _players = new();

    /// <summary>Plays <paramref name="cue"/> at <paramref name="at"/>, in the world's pixels, its pitch times <paramref name="tune"/> and <paramref name="offsetDb"/> over its volume.</summary>
    public void Play(S.Cue cue, Vector2 at, float tune = 1, float offsetDb = 0)
    {
        if (!_players.TryGetValue(cue, out var player))
        {
            player = placed ? Cues.Player2D(cue, bus, voices) : Cues.Player(cue, bus, voices);
            AddChild(player);
            _players[cue] = player;
        }
        if (player is Heard2D heard) heard.PlayAt(at, cue.Tune * tune, offsetDb);
        else if (player is AudioStreamPlayer anywhere)
        {
            (anywhere.PitchScale, anywhere.VolumeDb) = (cue.Tune * tune, cue.VolumeDb + offsetDb);
            anywhere.Play();
        }
    }
}
