using System;
using System.Collections.Generic;
using Godot;
using Stillwake.Game.Platform;
using Stillwake.Game.Render;
using static Stillwake.Game.I18n.En;

namespace Stillwake.Game.Menu;

/// <summary>
/// What the game opens on (0031), over the title's world (<see cref="TitleBackdrop"/>): for a moment the scene alone with
/// its music, then the name fading in and the choices after it, words alone at the left where the screen is darkened for
/// them — Continue the last world played (New game on a first run), Worlds, Play together, Settings, Quit; Testbed in a
/// debug build. Any key or click brings them at once. Worlds, Play together and Settings slide a panel out beside them,
/// closed by choosing again. A newer version shows top right from the first moment, bright, its Update a click away; the
/// build at the foot.
/// </summary>
public partial class TitleScreen : CanvasLayer
{
    /// <summary>When, in seconds into the title, the name begins to fade in and the choices after it, one by one; how long each fade takes.</summary>
    private const double NameAt = 1.6, ChoicesAt = 3.0, ChoiceAfter = 0.12, FadeSeconds = 0.9, Skipped = 6;
    /// <summary>How long a panel takes to slide out, in seconds, and how far it slides, in pixels.</summary>
    private const double PanelSeconds = 0.22;
    private const float PanelSlide = 24, PanelLeft = 400;

    private static readonly Color Amber = new("fbbf24"), AmberDeep = new("3a2a0a"), AmberText = new("fde68a");

    private readonly Func<WorldEntry?> _continue;
    private readonly Func<string?> _updateTo, _updating;
    private readonly TitleItem _continueItem;
    private readonly List<TitleItem> _items = [];
    private readonly Label _name, _problem, _updateLine, _foot;
    private readonly PanelContainer _panel, _update;
    private readonly Button _updateButton;
    private readonly Dictionary<string, (TitleItem Item, Control Page)> _pages = [];
    private string? _open;
    private double _seconds, _panelShown;

    /// <summary>The Worlds panel: what it has picked is what Play together hosts.</summary>
    public WorldsPanel Worlds { get; }

    /// <param name="continueWorld">The world Continue plays, or none on a first run.</param>
    /// <param name="play">Continue: plays that world, or a new one.</param>
    /// <param name="settings">The settings kept on this machine (<see cref="SettingsRows"/>).</param>
    /// <param name="testbed">Plays the dev testbed (<c>Dev.Testbed</c>): given only in a debug build, so a release shows no choice.</param>
    public TitleScreen(Func<WorldEntry?> continueWorld, Action play, WorldsPanel worlds, ITogether together, Control settings, Action quit,
        string version, Func<string?> updateTo, Action update, Func<string?> updating, Action? testbed = null)
    {
        (_continue, _updateTo, _updating) = (continueWorld, updateTo, updating);
        Worlds = worlds;
        Layer = ScreenLayers.Menu;
        var root = new Control { MouseFilter = Control.MouseFilterEnum.Ignore };
        root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(root);
        root.AddChild(Shade());

        var column = new VBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        column.AddThemeConstantOverride("separation", 4);
        (column.AnchorTop, column.AnchorBottom) = (0.5f, 0.5f);
        column.GrowVertical = Control.GrowDirection.Both;
        column.OffsetLeft = 72;
        root.AddChild(column);
        var spaced = new FontVariation { BaseFont = Style.Mono, SpacingGlyph = 6 };
        column.AddChild(_name = new Label { Text = "STILLWAKE", LabelSettings = new LabelSettings { Font = spaced, FontSize = 44, FontColor = Style.Sky200, ShadowColor = new Color(0, 0, 0, 0.7f), ShadowSize = 4 } });
        column.AddChild(new Control { CustomMinimumSize = new Vector2(0, 28), MouseFilter = Control.MouseFilterEnum.Ignore });
        _continueItem = Item(column, "title.continue", play, 26);
        var pages = new (string Key, Control Page)[] { ("title.worlds", worlds), ("title.together", new TogetherPage(together, () => Open(null))), ("title.settings", settings) };
        foreach (var (key, page) in pages) _pages[key] = (Item(column, key, () => Open(_open == key ? null : key), 20), page);
        Item(column, "title.quit", quit, 20);
        if (testbed != null) Item(column, "title.testbed", testbed, 14);
        column.AddChild(_problem = new Label
        {
            LabelSettings = Style.Text(Style.Mono, 12, Style.Red300, shadow: true),
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            CustomMinimumSize = new Vector2(260, 0),
        });

        _panel = new PanelContainer { Visible = false, MouseFilter = Control.MouseFilterEnum.Stop };
        _panel.AddThemeStyleboxOverride("panel", Style.Box(Style.Slate950 with { A = 0.9f }, Style.Slate700 with { A = 0.6f }, 18, radius: 6));
        root.AddChild(_panel);
        foreach (var (_, page) in _pages.Values)
        {
            page.Visible = false;
            _panel.AddChild(page);
        }

        _update = new PanelContainer { Visible = false };
        _update.AddThemeStyleboxOverride("panel", Style.Box(AmberDeep with { A = 0.92f }, Amber, 12, radius: 6));
        _update.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopRight, Control.LayoutPresetMode.KeepSize, 24);
        _update.GrowHorizontal = Control.GrowDirection.Begin;
        var row = Style.Gap(new HBoxContainer(), 14);
        row.AddChild(_updateLine = new Label { LabelSettings = Style.Text(Style.Mono, 14, AmberText), VerticalAlignment = VerticalAlignment.Center });
        row.AddChild(_updateButton = Style.Button(T("title.update"), update, size: 14, padX: 16, padY: 6));
        Style.Paint(_updateButton, Amber, new Color("fcd34d"), 16, 6);
        foreach (var state in (string[])["font_color", "font_hover_color", "font_pressed_color"]) _updateButton.AddThemeColorOverride(state, new Color("412402"));
        _update.AddChild(row);
        root.AddChild(_update);

        root.AddChild(_foot = new Label { Text = version, LabelSettings = Style.Text(Style.Mono, 11, Style.Slate600) });
        _foot.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.BottomRight, Control.LayoutPresetMode.KeepSize, 16);
        _foot.GrowHorizontal = _foot.GrowVertical = Control.GrowDirection.Begin;
        Fade(0);
    }

    private TitleItem Item(VBoxContainer column, string key, Action pressed, int size)
    {
        var item = new TitleItem(T(key), pressed, size);
        column.AddChild(item);
        _items.Add(item);
        return item;
    }

    /// <summary>The screen darkened from the left, so the words read over the scene and the hero's side stays clear.</summary>
    private static TextureRect Shade()
    {
        var gradient = new Gradient { Offsets = [0, 0.38f, 0.62f], Colors = [new Color(0, 0, 0, 0.72f), new Color(0, 0, 0, 0.45f), new Color(0, 0, 0, 0)] };
        var shade = new TextureRect
        {
            Texture = new GradientTexture2D { Gradient = gradient, Width = 256, Height = 4 },
            StretchMode = TextureRect.StretchModeEnum.Scale,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        shade.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        return shade;
    }

    /// <summary>Says what went wrong under the choices: a world that would not load, a host that left.</summary>
    public void Say(string text) => _problem.Text = text;

    /// <summary>Opens the panel of <paramref name="key"/>'s choice, or closes the one open.</summary>
    public void Open(string? key)
    {
        _seconds = Math.Max(_seconds, Skipped);
        _open = key;
        _panelShown = 0;
        _panel.Visible = key != null;
        foreach (var (k, (item, page)) in _pages)
        {
            page.Visible = k == key;
            item.Active = k == key;
        }
        if (key == "title.worlds") Worlds.Refresh();
        if (key == "title.together" && _pages[key].Page is TogetherPage together) together.Opened();
    }

    /// <summary>Any key or click while the name and choices fade in brings them at once; Esc closes the panel open.</summary>
    public override void _Input(InputEvent e)
    {
        if (_seconds < Skipped && e is InputEventKey { Pressed: true } or InputEventMouseButton { Pressed: true }) _seconds = Skipped;
        // A field being typed in hears its own Esc first: New world's name puts itself away.
        if (e is InputEventKey { Pressed: true, Echo: false, PhysicalKeycode: Key.Escape } && _open != null && GetViewport().GuiGetFocusOwner() is not LineEdit)
        {
            Open(null);
            GetViewport().SetInputAsHandled();
        }
    }

    public override void _Process(double delta)
    {
        _seconds += delta;
        Fade(_seconds);
        var world = _continue();
        if (world != null) _continueItem.Say(T("title.continue"), $"{world.Name} · {WorldsPanel.Played(world.PlaySeconds)} · {WorldsPanel.Ago(world.LastPlayed)}");
        else _continueItem.Say(T("title.newGame"));
        string? tag = _updateTo(), how = _updating();
        _update.Visible = tag != null || how != null;
        _updateLine.Text = how ?? (tag != null ? T("title.updateOut", ("version", tag)) : "");
        _updateButton.Visible = how == null;
        // The notice breathes, so the eye finds it.
        _update.SelfModulate = new Color(1, 1, 1, 0.82f + 0.18f * Mathf.Sin((float)_seconds * 2.4f));
        if (_open is null) return;
        _panelShown = Math.Min(1, _panelShown + delta / PanelSeconds);
        float t = (float)(1 - Math.Pow(1 - _panelShown, 3));
        var screen = _panel.GetViewportRect().Size;
        var size = _panel.GetCombinedMinimumSize();
        _panel.Size = size;
        _panel.Position = new Vector2(PanelLeft - PanelSlide * (1 - t), Mathf.Max(16, (screen.Y - size.Y) / 2));
        _panel.Modulate = new Color(1, 1, 1, t);
    }

    /// <summary>The name and the choices as far faded in as <paramref name="seconds"/> into the title says.</summary>
    private void Fade(double seconds)
    {
        static float Eased(double t) => (float)(Math.Clamp(t, 0, 1) is var c ? c * c * (3 - 2 * c) : 0);
        _name.Modulate = new Color(1, 1, 1, Eased((seconds - NameAt) / FadeSeconds));
        for (int i = 0; i < _items.Count; i++)
        {
            float a = Eased((seconds - ChoicesAt - i * ChoiceAfter) / FadeSeconds);
            _items[i].Modulate = new Color(1, 1, 1, a);
            _items[i].MouseFilter = a > 0.5f ? Control.MouseFilterEnum.Stop : Control.MouseFilterEnum.Ignore;
        }
        float rest = Eased((seconds - ChoicesAt) / FadeSeconds);
        _foot.Modulate = _problem.Modulate = new Color(1, 1, 1, rest);
    }
}
