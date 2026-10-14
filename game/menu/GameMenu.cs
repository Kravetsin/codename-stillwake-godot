using System;
using System.Collections.Generic;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using static Stillwake.Game.I18n.En;

namespace Stillwake.Game.Menu;

/// <summary>
/// The game menu on Esc: resume, settings (the suit's colour), save, load and quit, over a darkened
/// screen. Esc is decided here for the whole game (<see cref="Escape"/>): it puts away what is open or
/// in hand first and opens the menu only when nothing is. The world runs on behind it, alone too: in
/// co-op every peer must keep stepping, and alone a pause is not worth a second way to run the world.
/// While it is open it keeps every key and click from the game behind it.
/// </summary>
public partial class GameMenu : CanvasLayer
{
    private readonly Ui _ui;
    private readonly Func<bool> _busy, _canLoad;
    private readonly Action _putAway;
    private readonly Func<string> _save, _load;
    private readonly VBoxContainer _main, _settings;
    private readonly Button _loadButton;
    private readonly Label _aloneOnly, _status;
    private readonly Dictionary<string, Button> _buttons = [];
    private MenuPage _page = MenuPage.Closed;

    /// <param name="ui">Where the menu says it is open, for the hero and the hands to stand still.</param>
    /// <param name="busy">Whether anything is open or in hand that Esc puts away first.</param>
    /// <param name="putAway">Closes the inventory and its crate and puts the plan away.</param>
    /// <param name="suits">The suit colours' row, shown in the settings.</param>
    /// <param name="save">Saves the world, and says how it went as an i18n key.</param>
    /// <param name="load">Loads the saved world, and says how it went as an i18n key.</param>
    /// <param name="canLoad">Whether this machine plays alone, the only way a load works.</param>
    /// <param name="quit">Quits the game.</param>
    public GameMenu(Ui ui, Func<bool> busy, Action putAway, Control suits, Func<string> save, Func<string> load, Func<bool> canLoad, Action quit)
    {
        (_ui, _busy, _putAway, _save, _load, _canLoad) = (ui, busy, putAway, save, load, canLoad);
        Layer = ScreenLayers.Menu;
        Visible = false;

        var backdrop = new ColorRect { Color = new Color(0, 0, 0, 0.5f), MouseFilter = Control.MouseFilterEnum.Stop };
        backdrop.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(backdrop);
        var centre = new CenterContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        centre.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(centre);
        var panel = new PanelContainer { CustomMinimumSize = new Vector2(240, 0) };
        panel.AddThemeStyleboxOverride("panel", Style.Box(Style.Slate900 with { A = 0.95f }, Style.Slate700, 16));
        centre.AddChild(panel);
        var column = Style.Gap(new VBoxContainer(), 12);
        panel.AddChild(column);

        _main = Style.Gap(new VBoxContainer(), 8);
        _main.AddChild(Choice("menu.resume", () => Show(MenuPage.Closed)));
        _main.AddChild(Choice("menu.settings", () => Show(MenuPage.Settings)));
        _main.AddChild(Choice("menu.save", () => Say(Run(_save))));
        _main.AddChild(_loadButton = Choice("menu.load", () => Say(Run(_load))));
        _main.AddChild(_aloneOnly = Line(T("menu.aloneOnly"), Style.Slate400, 11));
        _main.AddChild(Choice("menu.quit", quit));
        column.AddChild(_main);

        _settings = Style.Gap(new VBoxContainer(), 8);
        _settings.AddChild(Line(T("menu.suit"), Style.Slate300, 12));
        suits.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
        _settings.AddChild(suits);
        _settings.AddChild(Choice("menu.back", () => Show(MenuPage.Main)));
        column.AddChild(_settings);

        column.AddChild(_status = Line("", Style.Slate300, 12));
        column.AddChild(Line(T("menu.running"), Style.Slate400, 11));
        _settings.Visible = false;
        Say("");
    }

    /// <summary>The page shown, closed or open.</summary>
    public MenuPage Page => _page;

    /// <summary>The middle of the button saying <paramref name="key"/> on screen, for scripted runs.</summary>
    public Vector2 CenterOf(string key) => _buttons[key].GetGlobalRect().GetCenter();

    private Button Choice(string key, Action pressed)
    {
        var button = Style.Button(T(key), pressed, size: 14, padX: 16, padY: 6);
        _buttons[key] = button;
        return button;
    }

    private static Label Line(string text, Color colour, int size) => new()
    {
        Text = text,
        LabelSettings = Style.Text(Style.Mono, size, colour),
        HorizontalAlignment = HorizontalAlignment.Center,
        MouseFilter = Control.MouseFilterEnum.Ignore,
    };

    private static string Run(Func<string> action)
    {
        try
        {
            return T(action());
        }
        catch (Exception e)
        {
            return T("menu.failed", ("why", e.Message));
        }
    }

    /// <summary>A line under the menu saying how the last save or load went; none when there is nothing to say.</summary>
    private void Say(string text)
    {
        _status.Text = text;
        _status.Visible = text != "";
    }

    private void Show(MenuPage page)
    {
        if (_page == MenuPage.Closed) Say("");
        _page = page;
        Visible = page != MenuPage.Closed;
        _main.Visible = page == MenuPage.Main;
        _settings.Visible = page == MenuPage.Settings;
        bool alone = _canLoad();
        _loadButton.Disabled = !alone;
        _aloneOnly.Visible = !alone;
        _ui.MenuOpen.Set(Visible);
    }

    /// <summary>Esc, first of every node: before the panels, the plan and the hero hear it, and every key while open.</summary>
    public override void _Input(InputEvent e)
    {
        if (e is not InputEventKey key) return;
        if (key is { Pressed: true, Echo: false, PhysicalKeycode: Key.Escape })
        {
            switch (Escape.Of(_page, _busy()))
            {
                case EscapeDoes.PutAway:
                    _putAway();
                    break;
                case EscapeDoes.OpenMenu:
                case EscapeDoes.Back:
                    Show(MenuPage.Main);
                    break;
                case EscapeDoes.CloseMenu:
                    Show(MenuPage.Closed);
                    break;
            }
            GetViewport().SetInputAsHandled();
        }
        else if (_page != MenuPage.Closed) GetViewport().SetInputAsHandled();
    }
}
