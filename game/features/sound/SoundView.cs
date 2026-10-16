using System;
using Godot;
using Stillwake.Game.Shared;
using S = Stillwake.Game.Sound;

namespace Stillwake.Game.Features.Sound;

/// <summary>
/// What this player hears by, as <see cref="SoundView"/> reads it: <see cref="Aboard"/> a world,
/// <see cref="Muffled"/> while its placed hero's body is out of air, <see cref="Out"/> while it is passed out.
/// </summary>
public readonly record struct Ears(bool Aboard, bool Muffled, bool Out);

/// <summary>
/// The hearing (ADR 0033): a click for every button, the room tone aboard, and out of air the world
/// dulled — the World bus's low-pass falling, the bus quieter, the room tone fading — and the hero's own
/// breath rising, but not while passed out. Reads the ears ten times a second and eases every frame.
/// </summary>
public partial class SoundView : Node
{
    private const double ReadS = 0.1;

    private readonly Func<Ears> _ears;
    private readonly AudioStreamPlayer _click = Cues.Player(S.Click, S.Ui, voices: 2);
    private readonly AudioStreamPlayer _roomTone = Cues.Player(S.RoomTone, S.Ambient);
    private readonly AudioStreamPlayer _breath = Cues.Player(S.Breath, S.Suit);
    private Callable _onPressed;
    private AudioEffectLowPassFilter _lowPass = null!;
    private int _world;
    private Ears _heard;
    private double _sinceRead = ReadS, _muffle, _breathing;

    /// <param name="ears">What this player hears by, read from the session and its hero.</param>
    public SoundView(Func<Ears> ears)
    {
        _ears = ears;
        // Heard behind the game menu as well.
        ProcessMode = ProcessModeEnum.Always;
        AddChild(_click);
        AddChild(_roomTone);
        AddChild(_breath);
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
            _heard = _ears();
        }
        double step = delta * 1000 / S.MuffleMs;
        _muffle = Toward(_muffle, _heard.Muffled ? 1 : 0, step);
        _breathing = Toward(_breathing, _heard.Muffled && !_heard.Out ? 1 : 0, step);
        Hear();
        Keep(_roomTone, _heard.Aboard ? 1 - _muffle : 0, S.RoomTone.VolumeDb);
        Keep(_breath, _breathing, S.Breath.VolumeDb);
    }

    /// <summary>The World bus as dulled as the hearing is now; eased in log space, so it dulls evenly across the octaves.</summary>
    private void Hear()
    {
        double hz = Math.Exp(Math.Log(S.ClearHz) + (Math.Log(S.MuffledHz) - Math.Log(S.ClearHz)) * _muffle);
        _lowPass.CutoffHz = (float)hz;
        AudioServer.SetBusVolumeDb(_world, (float)(S.MuffledDb * _muffle));
    }

    /// <summary>A loop at <paramref name="share"/> of its volume: playing while heard at all, stopped once silent.</summary>
    private static void Keep(AudioStreamPlayer loop, double share, float volumeDb)
    {
        if (share <= 0)
        {
            if (loop.Playing) loop.Stop();
            return;
        }
        loop.VolumeDb = volumeDb + Mathf.LinearToDb((float)share);
        if (!loop.Playing) loop.Play();
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
