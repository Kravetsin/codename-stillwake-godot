using System;
using System.Linq;
using Godot;
using Stillwake.Game.Shared;
using S = Stillwake.Game.Sound;

namespace Stillwake.Game.Features.Sound;

/// <summary>
/// The hearing (ADR 0033, 0042): a click for every button; floating in space, the World bus's low-pass
/// falling as it fades out, the Suit bus dulled to a thud and raised, the Helmet bus left clear, and a quiet bed of
/// music fading in; the title's music while the title is up; the ship's music now and then off a planet (<see cref="ShipMusic"/>), the bed of space only in its silences.
/// Reads whether this player's hero floats ten
/// times a second and eases every frame.
/// </summary>
public partial class SoundView : Node
{
    private const double ReadS = 0.1;

    private readonly Func<bool> _floats, _title, _offPlanet;
    private readonly Func<double> _fogged;
    private readonly AudioStreamPlayer _music = Cues.Player(S.Title, S.Music), _space = Cues.Player(S.Space, S.Music);
    private double _musicShare, _spaceShare, _shipShare;
    private readonly AudioStreamPlayer[] _ship = [.. S.ShipTracks.Select(t => Cues.Player(t, S.Music))];
    private readonly ShipMusic _shipMusic = new(new Random(), S.ShipTracks.Count);
    private int? _shipTrack;
    private bool _offPlanetNow;
    private readonly AudioStreamPlayer _click = Cues.Player(S.Click, S.Ui, voices: 2);
    private readonly AudioStreamPlayer _trash = Cues.Player(S.Trash, S.Ui);
    private Callable _onPressed;
    private AudioEffectLowPassFilter _worldLowPass = null!, _suitLowPass = null!;
    private AudioEffectLowShelfFilter _suitLows = null!;
    private int _world, _suit;
    private bool _heard;
    private int _flips;
    private ulong _clickedOn = ulong.MaxValue;
    private double _sinceRead = ReadS, _muffle, _fog, _fogTo;

    /// <param name="floats">Whether this player's placed hero floats out in space: the world falls silent and the bed of space plays.</param>
    /// <param name="title">Whether the title is shown: its music plays while it is (0035).</param>
    /// <param name="offPlanet">Whether this player's placed hero is off a planet, aboard or out in space: the ship's music is heard.</param>
    /// <param name="fogged">How deep in a planet's fog this player's hero is, 0 to 1 (fog-edges): the world dulls as in space.</param>
    public SoundView(Func<bool> floats, Func<bool> title, Func<bool> offPlanet, Func<double> fogged)
    {
        (_floats, _title, _offPlanet, _fogged) = (floats, title, offPlanet, fogged);
        // Heard behind the game menu as well.
        ProcessMode = ProcessModeEnum.Always;
        AddChild(_click);
        AddChild(_trash);
        AddChild(_music);
        AddChild(_space);
        foreach (var track in _ship) AddChild(track);
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
            bool heard = _floats();
            if (heard != _heard) _flips++;
            _heard = heard;
            _fogTo = _fogged();
            _offPlanetNow = _offPlanet();
        }
        double muffleS = S.MuffleMs / 1000.0;
        _muffle = Hearing.Ease(_muffle, _heard, delta, muffleS, muffleS);
        _fog = _fog < _fogTo ? Math.Min(_fogTo, _fog + delta / muffleS) : Math.Max(_fogTo, _fog - delta / muffleS);
        Hear();
        _musicShare = Hearing.Ease(_musicShare, _title(), delta, S.MusicFadeS, S.MusicFadeS);
        Play(_music, _musicShare, S.Title.VolumeDb);
        bool track = Ship(delta);
        _spaceShare = Hearing.Ease(_spaceShare, _heard && !track, delta, S.SpaceInS, S.SpaceOutS);
        Play(_space, _spaceShare, S.Space.VolumeDb);
    }

    /// <summary>
    /// The ship's music: a track begun when <see cref="ShipMusic"/> says, faded out on a planet and back in off it, played to its
    /// end; whether one is heard now, so the bed of space keeps quiet under it.
    /// </summary>
    private bool Ship(double delta)
    {
        bool playing = _shipTrack is int on && _ship[on].Playing;
        if (_shipMusic.Step(delta, _offPlanetNow, playing) is int next)
        {
            _shipTrack = next;
            _ship[next].Play();
            playing = true;
        }
        _shipShare = Hearing.Ease(_shipShare, _offPlanetNow, delta, S.ShipInS, S.ShipOutS);
        if (_shipTrack is int track)
            _ship[track].VolumeDb = S.ShipTracks[track].VolumeDb + Mathf.LinearToDb((float)Math.Max(_shipShare, 0.0001));
        return playing && _offPlanetNow;
    }

    /// <summary>The hearing for the log (<see cref="AudioCensus"/>): how muffled and fogged now, and how often floating flipped since asked last.</summary>
    public string Census()
    {
        var line = $"muffle {_muffle:F2}, fog {_fog:F2}, floating flipped {_flips}×";
        _flips = 0;
        return line;
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
        var mix = Hearing.At(_muffle, _fog);
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

    /// <summary>
    /// The UI's click: a button pressed, and every inventory action and a canister breathed (<see cref="Ui.Clicked"/>,
    /// 0081). Once a frame at most, however many ask; its pitch varies by the cue's ±6 %.
    /// </summary>
    public void Click()
    {
        ulong frame = Engine.GetProcessFrames();
        if (frame == _clickedOn) return;
        _clickedOn = frame;
        _click.Play();
    }

    /// <summary>A stack the trash held destroyed (<see cref="Ui.Trashed"/>, trash-sound C).</summary>
    public void Trash() => _trash.Play();
}
