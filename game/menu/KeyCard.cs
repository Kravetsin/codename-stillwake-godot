using System;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using static Stillwake.Game.I18n.En;

namespace Stillwake.Game.Menu;

/// <summary>
/// Shown as a world opens to friends: the code they join by, large and a click from the clipboard (<see cref="KeyBox"/>),
/// and a line saying the Esc menu keeps it. Play, Enter or Esc puts it away; the world runs on behind it, as behind the
/// menu, and the hero stands still. Added before the game menu, so the menu hears Esc first and puts it away.
/// </summary>
public partial class KeyCard : CanvasLayer
{
    private readonly Ui _ui;
    private readonly Func<TogetherState> _state;
    private readonly Label _title, _kept;
    private readonly Button _play;

    public KeyCard(Ui ui, Func<TogetherState> state)
    {
        (_ui, _state) = (ui, state);
        Layer = ScreenLayers.Menu;
        Visible = false;
        var backdrop = new ColorRect { Color = new Color(0, 0, 0, 0.5f), MouseFilter = Control.MouseFilterEnum.Stop };
        backdrop.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(backdrop);
        var centre = new CenterContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        centre.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(centre);
        var panel = new PanelContainer { CustomMinimumSize = new Vector2(320, 0) };
        panel.AddThemeStyleboxOverride("panel", Style.Box(Style.Slate900 with { A = 0.95f }, Style.Slate700, 20));
        centre.AddChild(panel);
        var column = Style.Gap(new VBoxContainer(), 12);
        panel.AddChild(column);
        column.AddChild(_title = Line(T("menu.keyTitle"), Style.Slate100, 16));
        column.AddChild(new KeyBox(state, 40));
        column.AddChild(_kept = Line(T("menu.keyKept"), Style.Slate500, 11));
        _play = Style.Button(T("menu.keyPlay"), Close, size: 14, padX: 20, padY: 6);
        _play.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
        Style.Paint(_play, Style.Sky600, Style.Sky400, 20, 6);
        column.AddChild(_play);
    }

    /// <summary>Whether the card is up: Esc puts it away first.</summary>
    public bool Shown => Visible;

    public void Open()
    {
        Visible = true;
        _ui.MenuOpen.Set(true);
    }

    public void Close()
    {
        if (!Visible) return;
        Visible = false;
        _ui.MenuOpen.Set(false);
    }

    /// <summary>A session the relay never opened: the card says so, the world playing on alone.</summary>
    public override void _Process(double delta)
    {
        if (!Visible) return;
        bool open = _state().Mode != TogetherMode.Alone;
        _title.Text = T(open ? "menu.keyTitle" : "menu.keyFailed");
        _kept.Visible = open;
    }

    /// <summary>While it is up the game behind takes no key: Enter puts it away, Esc is the game menu's.</summary>
    public override void _Input(InputEvent e)
    {
        if (!Visible || e is not InputEventKey key) return;
        if (key is { Pressed: true, Echo: false, PhysicalKeycode: Key.Enter or Key.KpEnter }) Close();
        GetViewport().SetInputAsHandled();
    }

    private static Label Line(string text, Color colour, int size) => new()
    {
        Text = text,
        LabelSettings = Style.Text(Style.Mono, size, colour),
        HorizontalAlignment = HorizontalAlignment.Center,
        MouseFilter = Control.MouseFilterEnum.Ignore,
    };
}
