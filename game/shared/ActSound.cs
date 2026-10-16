using Godot;
using S = Stillwake.Game.Sound;

namespace Stillwake.Game.Shared;

/// <summary>
/// The sound an action makes, on the Suit bus (0033, 0042): the soft click aboard, the glove's low thump while
/// this player's hero floats in space. Crew's actions and items' pick-ups each hold one, so both choose alike.
/// </summary>
public partial class ActSound : Node
{
    private readonly AudioStreamPlayer _aboard = Cues.Player(S.Act, S.Suit, voices: 2);
    private readonly AudioStreamPlayer _inSpace = Cues.Player(S.ActInSpace, S.Suit, voices: 2);

    public override void _Ready()
    {
        AddChild(_aboard);
        AddChild(_inSpace);
    }

    /// <summary>Plays the action's sound, the thump if this player's hero <paramref name="floats"/> in space.</summary>
    public void Play(bool floats) => (floats ? _inSpace : _aboard).Play();
}
