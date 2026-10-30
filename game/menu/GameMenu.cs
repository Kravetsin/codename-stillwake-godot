using System;
using System.Collections.Generic;
using Godot;
using Stillwake.Game.Platform;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using static Stillwake.Game.I18n.En;

namespace Stillwake.Game.Menu;

/// <summary>
/// The game menu on Esc (0031): resume, the Handbook (J, 0066), save, settings (the suit's colour, the slots' size, heat shimmer), save and exit to the title,
/// save and quit — a guest leaves and quits without saving — over a darkened screen, the world, when it
/// was saved and the code while hosting at its foot. Only in a world: on the title, Esc does nothing. Esc is decided here for the whole game (<see cref="Escape"/>): it puts away what is open or
/// in hand first and opens the menu only when nothing is. The world runs on behind it, alone too: in
/// co-op every peer must keep stepping, and alone a pause is not worth a second way to run the world.
/// While it is open it keeps every key and click from the game behind it.
/// </summary>
public partial class GameMenu : CanvasLayer
{
    private readonly Ui _ui;
    private readonly Func<bool> _busy, _inWorld, _owns;
    private readonly Action _putAway, _handbook, _tree;
    private readonly Func<string> _save, _foot;
    private readonly VBoxContainer _main, _settings;
    private readonly Func<string?> _updateTo, _updating;
    private readonly Button _update, _saveButton, _exitButton, _quitButton;
    private readonly Label _updateLine;
    private readonly Label _status, _footLine;
    private readonly Dictionary<string, Button> _buttons = [];
    private MenuPage _page = MenuPage.Closed;

    /// <param name="ui">Where the menu says it is open, for the hero and the hands to stand still.</param>
    /// <param name="busy">Whether anything is open or in hand that Esc puts away first.</param>
    /// <param name="putAway">Closes the inventory and its crate and the Handbook, and puts the plan away.</param>
    /// <param name="handbook">Opens the Handbook (0066): a line here says J does, for a player who has not found it.</param>
    /// <param name="tree">Opens the tech tree: a line here says T does.</param>
    /// <param name="look">Opens the look editor.</param>
    /// <param name="suits">The suit colours' row, shown in the settings.</param>
    /// <param name="save">Saves the world, and says how it went as an i18n key.</param>
    /// <param name="inWorld">Whether a world is played: on the title the menu stays shut.</param>
    /// <param name="owns">Whether this machine keeps the world it plays, so saves it; a guest does not.</param>
    /// <param name="exitToTitle">Saves if it owns the world, leaves it and goes to the title.</param>
    /// <param name="quit">Saves if it owns the world, and quits the game.</param>
    /// <param name="foot">The world, when it was saved, and the code while hosting.</param>
    /// <param name="version">This build, as the menu shows it at its foot.</param>
    /// <param name="updateTo">A newer release's tag, once one is known to be out.</param>
    /// <param name="update">Fetches the newer release and restarts into it.</param>
    /// <param name="updating">How an update stands while it runs.</param>
    public GameMenu(Ui ui, Func<bool> busy, Action putAway, Action handbook, Action tree, Action look, Control suits, Func<string> save, Func<bool> inWorld, Func<bool> owns,
        Action exitToTitle, Action quit, Func<string> foot, string version, Func<string?> updateTo, Action update, Func<string?> updating)
    {
        (_ui, _busy, _putAway, _handbook, _tree, _save, _inWorld, _owns, _foot) = (ui, busy, putAway, handbook, tree, save, inWorld, owns, foot);
        (_updateTo, _updating) = (updateTo, updating);
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
        _main.AddChild(_update = Choice("menu.update", update));
        _update.Visible = false;
        _main.AddChild(_updateLine = Line("", Style.Sky200, 11));
        _updateLine.Visible = false;
        _main.AddChild(Choice("menu.resume", () => Show(MenuPage.Closed)));
        _main.AddChild(Choice("menu.handbook", () =>
        {
            Show(MenuPage.Closed);
            _handbook();
        }));
        _main.AddChild(Choice("menu.tree", () =>
        {
            Show(MenuPage.Closed);
            _tree();
        }));
        _main.AddChild(Choice("menu.look", () =>
        {
            Show(MenuPage.Closed);
            look();
        }));
        _main.AddChild(_saveButton = Choice("menu.save", () => Say(Run(_save))));
        _main.AddChild(Choice("menu.settings", () => Show(MenuPage.Settings)));
        _main.AddChild(_exitButton = Choice("menu.saveExit", exitToTitle));
        _main.AddChild(_quitButton = Choice("menu.saveQuit", quit));
        column.AddChild(_main);

        _settings = Style.Gap(new VBoxContainer(), 8);
        _settings.AddChild(Line(T("menu.suit"), Style.Slate300, 12));
        suits.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
        _settings.AddChild(suits);
        _settings.AddChild(new SettingsRows(ui));
        _settings.AddChild(Choice("menu.back", () => Show(MenuPage.Main)));
        column.AddChild(_settings);

        column.AddChild(_status = Line("", Style.Slate300, 12));
        column.AddChild(_footLine = Line("", Style.Slate300, 11));
        column.AddChild(Line(T("menu.running"), Style.Slate400, 11));
        column.AddChild(Line(version, Style.Slate600, 10));
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

    public override void _Process(double delta)
    {
        if (_page != MenuPage.Main) return;
        _footLine.Text = _foot();
        string? tag = _updateTo(), how = _updating();
        _update.Visible = tag != null && how == null;
        if (tag != null) _update.Text = T("menu.update", ("version", tag));
        _updateLine.Text = how ?? "";
        _updateLine.Visible = how != null;
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
        // A guest keeps nothing: it leaves and quits, saving nothing.
        bool owns = _owns();
        _saveButton.Visible = owns;
        _exitButton.Text = T(owns ? "menu.saveExit" : "menu.leave");
        _quitButton.Text = T(owns ? "menu.saveQuit" : "menu.quit");
        _ui.MenuOpen.Set(Visible);
    }

    /// <summary>Esc, first of every node: before the panels, the plan and the hero hear it, and every key while open.</summary>
    public override void _Input(InputEvent e)
    {
        if (e is not InputEventKey key) return;
        if (!_inWorld()) return;
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
        // A field being typed in takes its keys; the game behind takes none.
        else if (_page != MenuPage.Closed && GetViewport().GuiGetFocusOwner() is not LineEdit) GetViewport().SetInputAsHandled();
    }
}
