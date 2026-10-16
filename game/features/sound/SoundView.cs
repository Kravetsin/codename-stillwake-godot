using System;
using Godot;
using Stillwake.Game.Shared;
using S = Stillwake.Game.Sound;

namespace Stillwake.Game.Features.Sound;

/// <summary>
/// The hearing (ADR 0033): a click for every button, and floating in space silence — the World bus's
/// low-pass falling as it fades out; only the Suit bus is left. Reads whether this player's hero floats
/// ten times a second and eases every frame.
/// </summary>
public partial class SoundView : Node
{
    private const double ReadS = 0.1;

    private readonly Func<bool> _muffled, _title;
    private readonly AudioStreamPlayer _music = Cues.Player(S.Title, S.Music);
    private double _musicShare;
    private readonly AudioStreamPlayer _click = Cues.Player(S.Click, S.Ui, voices: 2);
    private Callable _onPressed;
    private AudioEffectLowPassFilter _lowPass = null!;
    private int _world;
    private bool _heard;
    private double _sinceRead = ReadS, _muffle;

    /// <param name="muffled">Whether this player's placed hero floats out in space.</param>
    /// <param name="title">Whether the title is shown: its music plays while it is (0035).</param>
    public SoundView(Func<bool> muffled, Func<bool> title)
    {
        (_muffled, _title) = (muffled, title);
        // Heard behind the game menu as well.
        ProcessMode = ProcessModeEnum.Always;
        AddChild(_click);
        AddChild(_music);
    }

    public override void _Ready()
    {
        _world = AudioServer.GetBusIndex(S.World);
        _lowPass = (AudioEffectLowPassFilter)AudioServer.GetBusEffect(_world, 0);
        // The AudioServer outlives a scene reload: the title must not stay dulled from a world left out of air.
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
            _heard = _muffled();
        }
        _muffle = Toward(_muffle, _heard ? 1 : 0, delta * 1000 / S.MuffleMs);
        Hear();
        _musicShare = Toward(_musicShare, _title() ? 1 : 0, delta / S.MusicFadeS);
        Play(_music, _musicShare, S.Title.VolumeDb);
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

    /// <summary>The World bus as dulled and faded as the hearing is now; the cutoff eased in log space, so it dulls evenly across the octaves.</summary>
    private void Hear()
    {
        double hz = Math.Exp(Math.Log(S.ClearHz) + (Math.Log(S.MuffledHz) - Math.Log(S.ClearHz)) * _muffle);
        _lowPass.CutoffHz = (float)hz;
        AudioServer.SetBusVolumeDb(_world, Mathf.LinearToDb((float)Math.Max(1 - _muffle, 1e-4)));
    }

    private static double Toward(double from, double to, double step) =>
        from < to ? Math.Min(to, from + step) : Math.Max(to, from - step);

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
