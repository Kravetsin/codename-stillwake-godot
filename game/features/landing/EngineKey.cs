using System;
using Godot;
using Stillwake.Game.Shared;
using Stillwake.Sim;
using Stillwake.Sim.Landing;
using static Stillwake.Game.I18n.En;

namespace Stillwake.Game.Features.Landing;

/// <summary>
/// The engine's key under its slot (planet-jump): **Fire**, its face backlit while it can fire, and pressable too while only the
/// crew is away, the press being the call to come home; dim with the sim's refusal in its tooltip otherwise
/// (<see cref="LandingModule.FireProblem"/>). A press sends <see cref="FireEngine"/>. Read on a timer.
/// </summary>
public partial class EngineKey : WristKey
{
    private const double RefreshMs = 100;
    private const int FontSize = 13;
    private const string Away = "landing.crewAway";
    private static readonly Color Sky = new("38bdf8"), Calling = new("f5b041");

    private readonly Func<LandingModule?> _landing;
    private readonly int _engine;
    private readonly Action<Command> _send;
    private readonly TipSpots _tips;
    private string? _problem = "landing.noEngine";
    private double _sinceRead = RefreshMs;
    private Vector2 _mouse;

    public EngineKey(int engine, Func<LandingModule?> landing, Action<Command> send, Store<Tip?> tip)
        : base(new Vector2(96, WristLook.KeycapSide))
    {
        (_engine, _landing, _send) = (engine, landing, send);
        SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        _tips = new TipSpots(tip);
        _tips.Add(this, () => _problem == null ? null
            : new Tip(T("landing.fire"), new TipLine(T(_problem, ("fuel", Content.Landing.JumpFuel)), _problem == Away ? Tone.Plain : Tone.Bad)));
        Read();
    }

    protected override void Pressed()
    {
        if (_problem is null or Away) _send(new FireEngine(_engine));
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
        if (_landing() is not { } landing) return;
        string? problem = landing.FireProblem(_engine);
        if (problem == _problem) return;
        _problem = problem;
        QueueRedraw();
    }

    protected override void DrawCap(Rect2 face)
    {
        bool ready = _problem is null or Away;
        Face(face, ready);
        var font = Render.Style.Mono;
        string text = T("landing.fire");
        var size = font.GetStringSize(text, HorizontalAlignment.Left, -1, FontSize);
        var at = new Vector2(face.Position.X + (face.Size.X - size.X) / 2,
            face.Position.Y + (face.Size.Y + font.GetAscent(FontSize) - font.GetDescent(FontSize)) / 2 + (State == KeyState.Down ? 1 : 0));
        var lit = _problem == Away ? Calling : Sky;
        var colour = ready ? (State == KeyState.Hover ? Colors.White : lit) : Colors.White with { A = 0.35f };
        DrawString(font, at, text, HorizontalAlignment.Left, -1, FontSize, colour);
    }
}
