using System;
using Godot;
using Stillwake.Game.Shared;
using Stillwake.Sim;
using Stillwake.Sim.Landing;
using static Stillwake.Game.I18n.En;

namespace Stillwake.Game.Features.Landing;

/// <summary>
/// A drop pod's key under its slot (the descent design): **Descend** aboard, **Return** off the ship, its face backlit while its
/// rider may launch, dim with the sim's refusal in its tooltip while not (<see cref="LandingModule.LaunchProblem"/>). A press
/// sends <see cref="Launch"/>. Read on a timer.
/// </summary>
public partial class LaunchKey : WristKey
{
    private const double RefreshMs = 100;
    private const int FontSize = 13;
    private static readonly Color Sky = new("38bdf8");

    private readonly Func<(LandingModule Landing, int Peer, bool Aboard)?> _world;
    private readonly Action<Command> _send;
    private readonly TipSpots _tips;
    private string? _problem = "landing.notSeated";
    private string _word = "landing.descend";
    private double _sinceRead = RefreshMs;
    private Vector2 _mouse;

    public LaunchKey(Func<(LandingModule Landing, int Peer, bool Aboard)?> world, Action<Command> send, Store<Tip?> tip)
        : base(new Vector2(96, WristLook.KeycapSide))
    {
        (_world, _send) = (world, send);
        SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        _tips = new TipSpots(tip);
        _tips.Add(this, () => _problem == null ? null
            : new Tip(T(_word), new TipLine(T(_problem, ("fuel", Content.Landing.DescentFuel)), Tone.Bad)));
        Read();
    }

    protected override void Pressed()
    {
        if (_problem == null) _send(new Launch());
        Read();
    }

    public override void _Input(InputEvent e)
    {
        if (e is InputEventMouse mouse) _mouse = mouse.Position;
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        _sinceRead += delta * 1000;
        if (_sinceRead >= RefreshMs) Read();
        _tips.Frame(IsVisibleInTree() ? _mouse : null);
    }

    public override void _ExitTree() => _tips.Show(null);

    private void Read()
    {
        _sinceRead = 0;
        if (_world() is not var (landing, peer, aboard)) return;
        var (problem, word) = (landing.LaunchProblem(peer), aboard ? "landing.descend" : "landing.return");
        if ((problem, word) == (_problem, _word)) return;
        (_problem, _word) = (problem, word);
        QueueRedraw();
    }

    protected override void DrawCap(Rect2 face)
    {
        bool ready = _problem == null;
        Face(face, ready);
        var font = Render.Style.Mono;
        string text = T(_word);
        var size = font.GetStringSize(text, HorizontalAlignment.Left, -1, FontSize);
        var at = new Vector2(face.Position.X + (face.Size.X - size.X) / 2,
            face.Position.Y + (face.Size.Y + font.GetAscent(FontSize) - font.GetDescent(FontSize)) / 2 + (State == KeyState.Down ? 1 : 0));
        var colour = ready ? (State == KeyState.Hover ? Colors.White : Sky) : Colors.White with { A = 0.35f };
        DrawString(font, at, text, HorizontalAlignment.Left, -1, FontSize, colour);
    }
}
