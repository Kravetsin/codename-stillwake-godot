using System;
using System.Collections.Generic;
using Godot;
using Stillwake.Game.Platform;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using static Stillwake.Game.I18n.En;

namespace Stillwake.Game.Menu;

/// <summary>
/// The game menu on Esc (0031, settings-menu), on the rugged tablet's screen (<see cref="Tablet"/>), raised from the screen's foot
/// over a darkened world: resume, save, Settings (<see cref="SettingsView"/>, its categories as the tablet's tabs), save and exit to
/// the title, save and quit — a guest leaves and quits without saving; while hosting, the code friends join by with Copy
/// (<see cref="KeyBox"/>) over them; the world, when it was saved at its foot. What the tablet's edge keys open (the Handbook, the
/// tablet) has no line here. Only in a world: on the title, Esc does nothing. Esc is decided here for the whole game
/// (<see cref="Escape"/>): it puts away what is open or in hand first and opens the menu only when nothing is. The world runs on
/// behind it, alone too: in co-op every peer must keep stepping, and alone a pause is not worth a second way to run the world.
/// While it is open it keeps every key and click from the game behind it.
/// </summary>
public partial class GameMenu : CanvasLayer
{
    private const float Dark = 0.5f;

    private readonly Ui _ui;
    private readonly Func<bool> _busy, _inWorld, _owns;
    private readonly Action _putAway;
    private readonly Func<string> _save, _foot;
    private readonly Func<TogetherState> _together;
    private readonly Tablet _tablet = new();
    private readonly ColorRect _backdrop;
    private readonly MarginContainer _content;
    private readonly KeyBox _key;
    private readonly VBoxContainer _main, _settingsPage;
    private readonly SettingsView _settings;
    private readonly Func<string?> _updateTo, _updating;
    private readonly Button _update, _saveButton, _exitButton, _quitButton;
    private readonly Label _updateLine;
    private readonly Label _status, _footLine;
    private readonly Dictionary<string, Button> _buttons = [];
    private MenuPage _page = MenuPage.Closed;

    /// <param name="ui">Where the menu says it is open, for the hero and the hands to stand still.</param>
    /// <param name="busy">Whether anything is open or in hand that Esc puts away first.</param>
    /// <param name="putAway">Closes the inventory and its crate and the Handbook, and puts the plan away.</param>
    /// <param name="look">Opens the look editor, from Settings' Hero.</param>
    /// <param name="suits">The suit colours' row, in Settings' Hero.</param>
    /// <param name="save">Saves the world, and says how it went as an i18n key.</param>
    /// <param name="inWorld">Whether a world is played: on the title the menu stays shut.</param>
    /// <param name="owns">Whether this machine keeps the world it plays, so saves it; a guest does not.</param>
    /// <param name="exitToTitle">Saves if it owns the world, leaves it and goes to the title.</param>
    /// <param name="quit">Saves if it owns the world, and quits the game.</param>
    /// <param name="foot">The world, when it was saved, and why hosting stopped.</param>
    /// <param name="together">How playing together stands: while hosting, the code is shown with Copy.</param>
    /// <param name="version">This build, as the menu shows it at its foot.</param>
    /// <param name="updateTo">A newer release's tag, once one is known to be out.</param>
    /// <param name="update">Fetches the newer release and restarts into it.</param>
    /// <param name="updating">How an update stands while it runs.</param>
    public GameMenu(Ui ui, Func<bool> busy, Action putAway, Action look, Control suits, Func<string> save, Func<bool> inWorld, Func<bool> owns,
        Action exitToTitle, Action quit, Func<string> foot, Func<TogetherState> together, string version, Func<string?> updateTo, Action update, Func<string?> updating)
    {
        (_ui, _busy, _putAway, _save, _inWorld, _owns, _foot) = (ui, busy, putAway, save, inWorld, owns, foot);
        (_updateTo, _updating, _together) = (updateTo, updating, together);
        Layer = ScreenLayers.Menu;
        Visible = false;

        var root = new Control { MouseFilter = Control.MouseFilterEnum.Ignore, Theme = new Theme { DefaultFont = Style.Mono, DefaultFontSize = Tablet.FontSize } };
        root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(root);
        _backdrop = new ColorRect { Color = Colors.Black with { A = 0 }, MouseFilter = Control.MouseFilterEnum.Stop };
        _backdrop.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        root.AddChild(_backdrop);
        root.AddChild(_tablet);

        _content = new MarginContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        foreach (var side in (string[])["margin_left", "margin_right", "margin_top", "margin_bottom"]) _content.AddThemeConstantOverride(side, 2 * TabletLayout.Pad);
        _tablet.OnScreen(_content);
        var column = Style.Gap(new VBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore }, 12);
        _content.AddChild(column);

        _main = Style.Gap(new VBoxContainer { CustomMinimumSize = new Vector2(280, 0) }, 8);
        _main.AddChild(_key = new KeyBox(together, 28) { Visible = false });
        _main.AddChild(_update = Choice("menu.update", update));
        _update.Visible = false;
        _main.AddChild(_updateLine = Line("", Style.Sky200, 11));
        _updateLine.Visible = false;
        _main.AddChild(Choice("menu.resume", () => Show(MenuPage.Closed)));
        _main.AddChild(_saveButton = Choice("menu.save", () => Say(Run(_save))));
        _main.AddChild(Choice("menu.settings", () => Show(MenuPage.Settings)));
        _main.AddChild(_exitButton = Choice("menu.saveExit", exitToTitle));
        _main.AddChild(_quitButton = Choice("menu.saveQuit", quit));
        column.AddChild(_main);

        _settingsPage = Style.Gap(new VBoxContainer(), 12);
        _settingsPage.AddChild(_settings = new SettingsView(ui, suits, () =>
        {
            Show(MenuPage.Closed);
            look();
        }));
        var back = Choice("menu.back", () => Show(MenuPage.Main));
        back.SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin;
        _settingsPage.AddChild(back);
        column.AddChild(_settingsPage);

        // The world and the build, under the first page only: Settings has the tablet to itself.
        _main.AddChild(_status = Line("", Style.Slate300, 12));
        _main.AddChild(_footLine = Line("", Style.Slate300, 11));
        _main.AddChild(Line(T("menu.running"), Style.Slate400, 11));
        _main.AddChild(Line(version, Style.Slate600, 10));
        _settingsPage.Visible = false;
        Say("");
    }

    /// <summary>The page shown, closed or open.</summary>
    public MenuPage Page => _page;

    /// <summary>Settings, for scripted runs to show a category.</summary>
    public SettingsView SettingsPage => _settings;

    /// <summary>The middle of the button saying <paramref name="key"/> on screen, for scripted runs.</summary>
    public Vector2 CenterOf(string key) => _buttons[key].GetGlobalRect().GetCenter();

    private Button Choice(string key, Action pressed)
    {
        var button = Style.Button(T(key), pressed, size: 14, padX: 16, padY: 6);
        _buttons[key] = button;
        return button;
    }

    /// <summary>
    /// The tablet raised or put away for this frame (<see cref="Tablet.Step"/>), resting in the middle of the screen as large as
    /// the page shown, the world behind darkened as far as it is up; the first page's lines read while it is shown.
    /// </summary>
    public override void _Process(double delta)
    {
        bool open = _page != MenuPage.Closed;
        _tablet.Step(open, delta);
        Visible = open || _tablet.Shown;
        if (!Visible) return;
        _backdrop.Color = Colors.Black with { A = Dark * (float)_tablet.Up };
        var size = _content.GetCombinedMinimumSize().Ceil();
        _content.Size = size;
        var screen = _tablet.GetViewportRect().Size;
        // Centred with its bezel, which reaches further above and to the right of the screen than below and to the left.
        const int s = TabletLayout.ArtScale;
        var art = size + new Vector2(TabletLayout.ArtAround * s, TabletLayout.ArtAround * s);
        var rest = ((screen - art) / 2 + new Vector2(TabletLayout.ScreenLeft * s, TabletLayout.ScreenTop * s)).Round();
        _tablet.Lay(rest.X, rest.Y, size.X, size.Y);
        if (_page != MenuPage.Main) return;
        _footLine.Text = _foot();
        _key.Visible = HostKey.Code(_together()) != null;
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
        if (page == MenuPage.Settings && _page != MenuPage.Settings) _settings.Reset();
        _page = page;
        bool open = page != MenuPage.Closed;
        if (open) Visible = true;
        _backdrop.MouseFilter = open ? Control.MouseFilterEnum.Stop : Control.MouseFilterEnum.Ignore;
        // Put away, it slides down showing the page it was on.
        if (open) (_main.Visible, _settingsPage.Visible) = (page == MenuPage.Main, page == MenuPage.Settings);
        // A guest keeps nothing: it leaves and quits, saving nothing.
        bool owns = _owns();
        _saveButton.Visible = owns;
        _exitButton.Text = T(owns ? "menu.saveExit" : "menu.leave");
        _quitButton.Text = T(owns ? "menu.saveQuit" : "menu.quit");
        _ui.MenuOpen.Set(open);
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
