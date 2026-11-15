using System;
using System.Linq;
using Godot;
using Stillwake.Game.Shared;
using Stillwake.Sim.Landing;
using static Stillwake.Game.I18n.En;

namespace Stillwake.Game.Features.Landing;

/// <summary>
/// The crew shuttle's key, the body of its panel (<see cref="Panel"/>): over it a mark for each of its seats, warm where a rider
/// sits and ringed where this player does, and where it would fly. With no course yet it is always dim, <c>places.noCourse</c> in
/// its tooltip; nothing is sent. Read on a timer.
/// </summary>
public partial class ShuttleKey : WristKey
{
    private const double RefreshMs = 100;
    private const int FontSize = 13;
    private const float Mark = 14, MarkGap = 6;
    private static readonly Color Warm = new("ffbd61"), Sky = new("38bdf8"), Empty = new("3a404d");

    private readonly int _shuttle, _riders;
    private readonly Func<(LandingModule Landing, int Peer)?> _world;
    private readonly TipSpots _tips;
    private readonly Control _marks = new();
    private (bool[] Taken, int? Own) _seen = ([], null);
    private double _sinceRead = RefreshMs;
    private Vector2 _mouse;

    private ShuttleKey(int shuttle, int riders, Func<(LandingModule Landing, int Peer)?> world, Store<Tip?> tip)
        : base(new Vector2(96, WristLook.KeycapSide))
    {
        (_shuttle, _riders, _world) = (shuttle, riders, world);
        SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        _tips = new TipSpots(tip);
        _tips.Add(this, () => new Tip(T("landing.setOff"), new TipLine(T("places.noCourse"), Tone.Bad)));
        _marks.CustomMinimumSize = new Vector2(riders * Mark + (riders - 1) * MarkGap, Mark);
        _marks.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        _marks.MouseFilter = MouseFilterEnum.Ignore;
        _marks.Draw += DrawMarks;
        Read();
    }

    /// <summary>
    /// The panel of shuttle <paramref name="shuttle"/>: its <paramref name="riders"/> seats' marks, the line of where it would fly
    /// (<c>landing.noRoute</c> with no course), and its Set off key.
    /// </summary>
    public static Control Panel(int shuttle, int riders, Func<(LandingModule Landing, int Peer)?> world, Store<Tip?> tip)
    {
        var key = new ShuttleKey(shuttle, riders, world, tip);
        var column = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ShrinkCenter, MouseFilter = MouseFilterEnum.Ignore };
        column.AddThemeConstantOverride("separation", 6);
        column.AddChild(key._marks);
        column.AddChild(new Label
        {
            Text = T("landing.noRoute"),
            HorizontalAlignment = HorizontalAlignment.Center,
            LabelSettings = Render.Style.Text(Render.Style.Mono, FontSize, Colors.White with { A = 0.6f }),
            MouseFilter = MouseFilterEnum.Ignore,
        });
        column.AddChild(key);
        return column;
    }

    /// <summary>No course can be set yet (slice 2 of the shuttle's flight): a press does nothing.</summary>
    protected override void Pressed() { }

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
        if (_world() is not var (landing, peer)) return;
        var seats = landing.RidersOf(_shuttle);
        bool[] taken = [.. Enumerable.Range(0, _riders).Select(p => seats.Any(s => s.Place == p))];
        int? own = seats.FirstOrDefault(s => s.Peer == peer)?.Place;
        if (taken.SequenceEqual(_seen.Taken) && own == _seen.Own) return;
        _seen = (taken, own);
        _marks.QueueRedraw();
    }

    /// <summary>A square a seat, left to right by place: warm where a rider sits, ringed in the screen's blue where this player does.</summary>
    private void DrawMarks()
    {
        for (int place = 0; place < _seen.Taken.Length; place++)
        {
            var box = new Rect2(place * (Mark + MarkGap), 0, Mark, Mark);
            _marks.DrawRect(box, _seen.Taken[place] ? Warm : Empty);
            if (place == _seen.Own) _marks.DrawRect(box.Grow(1), Sky, filled: false, width: 2);
        }
    }

    protected override void DrawCap(Rect2 face)
    {
        Face(face, false);
        var font = Render.Style.Mono;
        string text = T("landing.setOff");
        var size = font.GetStringSize(text, HorizontalAlignment.Left, -1, FontSize);
        var at = new Vector2(face.Position.X + (face.Size.X - size.X) / 2,
            face.Position.Y + (face.Size.Y + font.GetAscent(FontSize) - font.GetDescent(FontSize)) / 2 + (State == KeyState.Down ? 1 : 0));
        DrawString(font, at, text, HorizontalAlignment.Left, -1, FontSize, Colors.White with { A = 0.35f });
    }
}
