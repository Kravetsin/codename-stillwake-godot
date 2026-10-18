using System;
using Godot;
using Stillwake.Game.Shared;
using S = Stillwake.Game.Sound;

namespace Stillwake.Game.Features.Sound;

/// <summary>
/// The hearing (ADR 0033, 0042): a click for every button; floating in space, the World bus's low-pass
/// falling as it fades out, the Suit bus dulled to a thud and raised, the Helmet bus left clear, and a quiet bed of
/// music fading in; the title's music while the title is up. Reads whether this player's hero floats ten
/// times a second and eases every frame.
/// </summary>
public partial class SoundView : Node
{
    private const double ReadS = 0.1;

    private readonly Func<bool> _floats, _title;
    private readonly AudioStreamPlayer _music = Cues.Player(S.Title, S.Music), _space = Cues.Player(S.Space, S.Music);
    private double _musicShare, _spaceShare;
    private readonly AudioStreamPlayer _click = Cues.Player(S.Click, S.Ui, voices: 2);
    private Callable _onPressed;
    private AudioEffectLowPassFilter _worldLowPass = null!, _suitLowPass = null!;
    private AudioEffectLowShelfFilter _suitLows = null!;
    private int _world, _suit;
    private bool _heard;
    private double _sinceRead = ReadS, _muffle;

    /// <param name="floats">Whether this player's placed hero floats out in space: the world falls silent and the bed of space plays.</param>
    /// <param name="title">Whether the title is shown: its music plays while it is (0035).</param>
    public SoundView(Func<bool> floats, Func<bool> title)
    {
        (_floats, _title) = (floats, title);
        // Heard behind the game menu as well.
        ProcessMode = ProcessModeEnum.Always;
        AddChild(_click);
        AddChild(_music);
        AddChild(_space);
    }

    public override void _Ready()
    {
        _world = AudioServer.GetBusIndex(S.World);
        _worldLowPass = (AudioEffectLowPassFilter)AudioServer.GetBusEffect(_world, 0);
        _suit = AudioServer.GetBusIndex(S.Suit);
        _suitLowPass = (AudioEffectLowPassFilter)AudioServer.GetBusEffect(_suit, 0);
        _suitLows = (AudioEffectLowShelfFilter)AudioServer.GetBusEffect(_suit, 1);
        // The AudioServer outlives a scene reload: the title must not stay dulled from a world left floating.
        Hear();
        _onPressed = new Callable(this, MethodName.Click);
        GetTree().NodeAdded += Added;
        Scan(GetTree().Root);
    }

    public override void _ExitTree() => GetTree().NodeAdded -= Added;

    public override void _Process(double delta)
    {
        if ((_sinceRead += delta) >= ReadS)
        {
            _sinceRead = 0;
            _heard = _floats();
        }
        double muffleS = S.MuffleMs / 1000.0;
        _muffle = Hearing.Ease(_muffle, _heard, delta, muffleS, muffleS);
        Hear();
        _musicShare = Hearing.Ease(_musicShare, _title(), delta, S.MusicFadeS, S.MusicFadeS);
        Play(_music, _musicShare, S.Title.VolumeDb);
        _spaceShare = Hearing.Ease(_spaceShare, _heard, delta, S.SpaceInS, S.SpaceOutS);
        Play(_space, _spaceShare, S.Space.VolumeDb);
    }

    /// <summary>A loop at <paramref name="share"/> of its volume: playing while heard at all, stopped once silent.</summary>
    private static void Play(AudioStreamPlayer loop, double share, float volumeDb)
    {
        if (share <= 0)
        {
            if (loop.Playing) loop.Stop();
            return;
        }
        loop.VolumeDb = volumeDb + Mathf.LinearToDb((float)share);
        if (!loop.Playing) loop.Play();
    }

    /// <summary>The World and Suit buses as dulled (and the Suit as raised) as the hearing is now (<see cref="Hearing.At"/>).</summary>
    private void Hear()
    {
        var mix = Hearing.At(_muffle);
        _worldLowPass.CutoffHz = (float)mix.WorldHz;
        AudioServer.SetBusVolumeDb(_world, Mathf.LinearToDb((float)mix.WorldGain));
        _suitLowPass.CutoffHz = (float)mix.SuitHz;
        _suitLows.Gain = (float)mix.SuitLows;
        AudioServer.SetBusVolumeDb(_suit, (float)mix.SuitDb);
    }

    private void Scan(Node node)
    {
        Added(node);
        foreach (var child in node.GetChildren()) Scan(child);
    }

    private void Added(Node node)
    {
        // A button moved to another parent enters the tree again: it clicks once still.
        if (node is BaseButton button && !button.IsConnected(BaseButton.SignalName.Pressed, _onPressed))
            button.Connect(BaseButton.SignalName.Pressed, _onPressed);
    }

    private void Click() => _click.Play();
}
