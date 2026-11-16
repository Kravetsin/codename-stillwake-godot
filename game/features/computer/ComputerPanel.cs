using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Stillwake.Content;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using Stillwake.Sim;
using Stillwake.Sim.Landing;
using Stillwake.Sim.Places;
using static Stillwake.Game.I18n.En;

namespace Stillwake.Game.Features.Computer;

/// <summary>
/// The ship's computer's window, opened by the crew's E at it through <see cref="Ui.OpenComputer"/>: a monitor in the middle of
/// the screen, its side panel a button a program while any program's module is on its network (<see cref="Readout"/>), its
/// screen the chosen program or a line saying why there is none. A button is lit while its program is ready, dim while its
/// module is short of power; a click on a lit one shows its program. E and Esc put it away; the world runs on behind it.
/// </summary>
public partial class ComputerPanel : CanvasLayer
{
    private static readonly Color Steel = new("7f848f"), Line = new("1a1e25"), Glass = new("10161d"), Socket = new("474c57"),
        Dim = new("3a3f48"), Lit = new("6fe08a"), LitBack = new("0b1f16"), Words = new("7fc4f0"), Warning = new("f0c050"), Off = new("5b6170");

    /// <summary>Seconds between reads of the world: a panel reads on a timer, never every frame.</summary>
    private const double ReadEvery = 0.25;
    private const int Button = 52, Pad = 10, CloseSize = 30;

    private readonly Ui _ui;
    private readonly Func<Registry?> _world;
    private readonly Action<Command> _send;
    private readonly Panel _frame = Boxed(Steel, 3, 4);
    private readonly VBoxContainer _rail = new() { MouseFilter = Control.MouseFilterEnum.Ignore };
    private readonly Panel _screen = Boxed(Glass, 2, 2);
    private readonly Panel _close = Boxed(Socket, 2, 2);
    private readonly Label _text = new()
    {
        HorizontalAlignment = HorizontalAlignment.Center,
        VerticalAlignment = VerticalAlignment.Center,
        AutowrapMode = TextServer.AutowrapMode.WordSmart,
        MouseFilter = Control.MouseFilterEnum.Ignore,
    };
    private readonly RadarScreen _radar;
    private readonly List<(Panel Box, TextureRect Icon, ProgramSpec Program)> _buttons = [];
    private readonly TipSpots _tips;
    private Reading _reading = new(false, []);
    private double _sinceRead;

    /// <param name="ui">Which computer is open, set by the crew's E, and cleared here when it is put away.</param>
    /// <param name="world">The world the computer stands in; null with none played.</param>
    /// <param name="send">Sends a command as this player: the radar's course for the ship and route for the shuttle.</param>
    public ComputerPanel(Ui ui, Func<Registry?> world, Action<Command> send)
    {
        (_ui, _world, _send) = (ui, world, send);
        _radar = new RadarScreen(ui)
        {
            Visible = false,
            PlotCourse = planet => _send(new PlotCourse(planet)),
            SetRoute = contact => _send(new SetCourse(contact)),
            ClearRoute = () => _send(new ClearCourse()),
            Forget = contact => _send(new Forget(contact)),
        };
        _tips = new TipSpots(ui.Tip);
        Layer = ScreenLayers.Handbook;
        var root = new Control { MouseFilter = Control.MouseFilterEnum.Ignore, Theme = new Theme { DefaultFont = Style.Mono, DefaultFontSize = 20 } };
        root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(root);
        root.AddChild(_frame);
        _rail.AddThemeConstantOverride("separation", 8);
        _frame.AddChild(_rail);
        _frame.AddChild(_screen);
        _screen.ClipContents = true;
        _text.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _radar.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _screen.AddChild(_radar);
        _screen.AddChild(_text);
        BuildClose();
        foreach (var program in Programs.All) AddButton(program);
        _frame.Visible = false;
        ui.OpenComputer.Subscribe(() =>
        {
            // Each time it opens, on no program: the player picks again.
            _ui.ComputerProgram.Set(null);
            _radar.Reset();
            _sinceRead = ReadEvery;
        });
    }

    /// <summary>Whether a computer is open: what Esc puts away before it opens the menu.</summary>
    public bool Shown => _ui.OpenComputer.Get() != null;

    /// <summary>Puts it away: E, or Esc through the menu's <c>Escape</c>.</summary>
    public void Close() => _ui.OpenComputer.Set(null);

    /// <summary>Puts away the radar's open callout, Esc's first step before the window: whether there was one.</summary>
    public bool LetGo() => Shown && _radar.IsVisibleInTree() && _radar.LetGo();

    /// <summary>The window's close in its top-right corner, over the screen: the same as E.</summary>
    private void BuildClose()
    {
        _close.Size = new Vector2(CloseSize, CloseSize);
        var cross = new Label
        {
            Text = "✕",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        cross.AddThemeFontSizeOverride("font_size", 16);
        cross.AddThemeColorOverride("font_color", Words);
        cross.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _close.AddChild(cross);
        _tips.Add(_close, () => new Tip(T("computer.close"), [new TipLine(T("computer.closeHow"))]));
        _close.MouseEntered += () => Restyle(_close, Dim, Lit);
        _close.MouseExited += () => Restyle(_close, Socket, Line);
        _close.GuiInput += e =>
        {
            if (e is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left }) return;
            _close.AcceptEvent();
            Close();
        };
        _frame.AddChild(_close);
    }

    private void AddButton(ProgramSpec program)
    {
        var box = Boxed(Socket, 2, 2);
        box.CustomMinimumSize = new Vector2(Button, Button);
        _tips.Add(box, () => new Tip(T($"computer.program.{program.Id}")));
        var icon = new TextureRect
        {
            Texture = PixelArt.Picture($"machine_{program.Module}"),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        icon.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        (icon.OffsetLeft, icon.OffsetTop, icon.OffsetRight, icon.OffsetBottom) = (6, 6, -6, -6);
        box.AddChild(icon);
        box.GuiInput += e =>
        {
            if (e is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left }) return;
            // Only a ready program opens: a dim button's reason is on the screen already.
            if (Readout.Ready(_reading, program.Id)) _ui.ComputerProgram.Set(program.Id);
            Paint();
            box.AcceptEvent();
        };
        _rail.AddChild(box);
        _buttons.Add((box, icon, program));
    }

    public override void _Process(double delta)
    {
        bool open = Shown && _world() != null;
        _frame.Visible = open;
        if (!open)
        {
            _tips.Show(null);
            return;
        }
        LayOut();
        Tips();
        _sinceRead += delta;
        if (_sinceRead < ReadEvery) return;
        _sinceRead = 0;
        _reading = Readout.Of(_world()!.Grid, _world()!.Power, _ui.OpenComputer.Get()!.Value);
        // A program that stops being ready is put away: the screen says why instead.
        if (!Readout.Ready(_reading, _ui.ComputerProgram.Get())) _ui.ComputerProgram.Set(null);
        // The contacts the radar holds, aboard (places'; none on another surface); a selected one gone is let go.
        var contacts = _world()!.Places.Contacts;
        _radar.Contacts = [.. contacts];
        if (_ui.RadarContact.Get() is { } id && !contacts.Any(c => c.Id == id)) _ui.RadarContact.Set(null);
        // The system and where the ship is in it (solar-system).
        var landing = _world()!.Landing;
        _radar.At = landing.At;
        _radar.Course = landing.Course is { } course ? (course.To, course.TicksLeft) : null;
        _radar.Problems = [.. Enumerable.Range(0, Content.Landing.Planets.Count).Select(landing.CourseProblem)];
        _radar.Ores = [.. Enumerable.Range(0, Content.Landing.Planets.Count).Select(landing.OresOf)];
        // The shuttle's route (shuttle-flight), and a stretch of space coming (stretches-of-space).
        _radar.Route = _world()!.Places.Course?.Id;
        ShowStretch(_world()!);
        Paint();
    }

    private StretchShown _stretch = new();
    private Registry? _stretchOf;
    /// <summary>The panel's own clock while it reads, in ticks: what a stretch's band fades out by once it has gone.</summary>
    private double _clock;

    /// <summary>The stretch of space the sky says is coming or running, as the radar's band (docs/decisions/2026-11-15-stretches-of-space.md); read anew in another world.</summary>
    private void ShowStretch(Registry world)
    {
        if (_stretchOf != world) (_stretch, _stretchOf) = (new StretchShown(), world);
        _clock += ReadEvery * Sim.World.TicksPerSecond;
        var now = world.Sky.Now;
        _stretch.See(now.Stretch, now.Coming, now.ComingIn, _clock);
        _radar.Stretch = _stretch.Look is { Band: var (r, g, b) } ? (new Color(r, g, b), _stretch.Grow, _stretch.Shown) : null;
    }

    /// <summary>The one tooltip: what the radar shows under the pointer; else a button's program; none behind the menu.</summary>
    private void Tips()
    {
        if (_ui.MenuOpen.Get())
        {
            _tips.Show(null);
            return;
        }
        var mouse = _frame.GetViewport().GetMousePosition();
        if (_radar.IsVisibleInTree() && _radar.GetGlobalRect().HasPoint(mouse) && !_close.GetGlobalRect().HasPoint(mouse)) _tips.Show(_radar.TipAt(mouse));
        else _tips.Frame(mouse);
    }

    /// <summary>A monitor over the screen as the tablet is, a margin round it; the side panel only while it has buttons; the close in its corner.</summary>
    private void LayOut()
    {
        var view = _frame.GetViewportRect().Size;
        float around = TabletLayout.ArtAround * TabletLayout.ArtScale + 2 * TabletPeek.Margin;
        float width = Mathf.Floor(Mathf.Max(480, view.X - around)), height = Mathf.Floor(Mathf.Max(300, view.Y - around));
        _frame.Size = new Vector2(width, height);
        _frame.Position = ((view - _frame.Size) / 2).Round();
        bool rail = _reading.Buttons.Count > 0;
        _rail.Visible = rail;
        _rail.Position = new Vector2(Pad, Pad);
        _rail.Size = new Vector2(Button, height - 2 * Pad);
        float left = rail ? Pad * 2 + Button : Pad;
        _screen.Position = new Vector2(left, Pad);
        _screen.Size = new Vector2(width - left - Pad, height - 2 * Pad);
        _close.Position = new Vector2(width - Pad - CloseSize - 8, Pad + 8);
    }

    /// <summary>The buttons and the screen as the last read and the choice have them.</summary>
    private void Paint()
    {
        var screen = Readout.ScreenOf(_reading, _ui.ComputerProgram.Get());
        _radar.Visible = screen.Program == Programs.Radar;
        _text.Visible = screen.Text != null;
        if (screen.Text is { } key)
        {
            _text.Text = T(key);
            _text.AddThemeColorOverride("font_color", key.StartsWith("computer.short.", StringComparison.Ordinal) ? Warning
                : key == "computer.noPower" ? Off : Words);
        }
        for (int i = 0; i < _buttons.Count; i++)
        {
            var (box, icon, program) = _buttons[i];
            var state = i < _reading.Buttons.Count ? _reading.Buttons[i].State : ProgramState.Absent;
            bool chosen = program.Id == screen.Program;
            Restyle(box, chosen ? LitBack : state == ProgramState.Short ? Dim : Socket, chosen ? Lit : Line);
            icon.Visible = state != ProgramState.Absent;
            icon.Modulate = state == ProgramState.Short ? new Color(0.45f, 0.45f, 0.5f) : Colors.White;
        }
    }

    public override void _Input(InputEvent e)
    {
        // E puts it away and is spent, so the crew's E does not open it again at once.
        if (e is not InputEventKey { Pressed: true, Echo: false } key || !key.Is(Bind.Use) || !Shown) return;
        Close();
        GetViewport().SetInputAsHandled();
    }

    private static Panel Boxed(Color fill, int border, int corner)
    {
        var box = new Panel { MouseFilter = Control.MouseFilterEnum.Stop };
        Restyle(box, fill, Line, border, corner);
        return box;
    }

    private static void Restyle(Panel box, Color fill, Color edge, int border = 2, int corner = 2)
    {
        var look = new StyleBoxFlat { BgColor = fill, BorderColor = edge };
        look.SetBorderWidthAll(border);
        look.SetCornerRadiusAll(corner);
        box.AddThemeStyleboxOverride("panel", look);
    }
}
