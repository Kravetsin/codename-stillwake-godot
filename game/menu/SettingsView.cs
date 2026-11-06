using System;
using System.Collections.Generic;
using Godot;
using Stillwake.Game.I18n;
using Stillwake.Game.Platform;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using static Stillwake.Game.I18n.En;

namespace Stillwake.Game.Menu;

/// <summary>
/// Settings (settings-menu), the game menu's and the title's alike: the categories as the tablet's tabs, the chosen one lit, its rows
/// under them, a name at the left and its choices at the right. Sound: Master, Effects and Music, a slider each (<see cref="Volume"/>);
/// Graphics: the screen (in a window, full screen, frameless) and heat shimmer (0075); Game: the language (2026-11-02-russian-language),
/// the slots' size (0058) and Open logs folder; Controls: the keys (<see cref="BindRows"/>); Hero, in a world only: the suit's colour
/// and Edit look. Each applied at once and kept in <c>user://settings.cfg</c>, the choice in use lit.
/// </summary>
public partial class SettingsView : VBoxContainer
{
    /// <summary>The body's size, every category's: the tablet keeps one size as the tabs change.</summary>
    public static readonly Vector2 BodySize = new(560, 300);
    private const int LabelWidth = 180, SliderWidth = 220;

    private readonly Ui _ui;
    private readonly IReadOnlyList<SettingsCategory> _categories;
    private readonly List<PanelContainer> _tabs = [];
    private readonly List<Control> _pages = [];
    private readonly Dictionary<string, Button> _buttons = [];
    private int _chosen, _hovered = -1;

    /// <param name="ui">The stores the settings set: the language, the slots' size, the keys.</param>
    /// <param name="suits">The suit colours' row, in a world; none on the title, which has no Hero.</param>
    /// <param name="look">Opens the look editor, in a world.</param>
    public SettingsView(Ui ui, Control? suits = null, Action? look = null)
    {
        _ui = ui;
        _categories = SettingsCategories.Of(inWorld: suits != null);
        Style.Gap(this, TabletLayout.Pad);
        var strip = Style.Gap(new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore }, 4);
        AddChild(strip);
        var body = new Control { CustomMinimumSize = BodySize, MouseFilter = MouseFilterEnum.Ignore };
        AddChild(body);
        for (int i = 0; i < _categories.Count; i++)
        {
            var category = _categories[i];
            var tab = Tablet.Tab(T(SettingsCategories.Key(category)), null, 0);
            int index = i;
            tab.GuiInput += e =>
            {
                if (e is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left }) return;
                Choose(index);
                tab.AcceptEvent();
            };
            tab.MouseEntered += () => Hover(index);
            tab.MouseExited += () => Hover(-1);
            strip.AddChild(tab);
            _tabs.Add(tab);
            var page = Page(category, suits, look);
            page.SetAnchorsPreset(LayoutPreset.FullRect);
            body.AddChild(page);
            _pages.Add(page);
        }
        this.Watch(_ui.Language, Paint);
        this.Watch(_ui.SlotScale, Paint);
        Choose(0);
        Paint();
    }

    /// <summary>Shows the first category again, as Settings opens.</summary>
    public void Reset() => Choose(0);

    /// <summary>The middle of <paramref name="category"/>'s tab on screen, for scripted runs.</summary>
    public Vector2 CenterOf(SettingsCategory category) => _tabs[IndexOf(category)].GetGlobalRect().GetCenter();

    /// <summary>Shows <paramref name="category"/>, for scripted runs and recordings.</summary>
    public void Show(SettingsCategory category) => Choose(IndexOf(category));

    private int IndexOf(SettingsCategory category)
    {
        for (int i = 0; i < _categories.Count; i++) if (_categories[i] == category) return i;
        throw new ArgumentException($"no {category} here", nameof(category));
    }

    private Control Page(SettingsCategory category, Control? suits, Action? look)
    {
        var page = Style.Gap(new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore }, 14);
        switch (category)
        {
            case SettingsCategory.Sound:
                foreach (var bus in Sound.Volumed) page.AddChild(Row(VolumeKey(bus), Slider(bus)));
                break;
            case SettingsCategory.Graphics:
                page.AddChild(Row("menu.screen", Choices([("menu.windowed", () => Screen(ScreenMode.Windowed)), ("menu.fullscreen", () => Screen(ScreenMode.Fullscreen)),
                    ("menu.borderless", () => Screen(ScreenMode.Borderless))])));
                page.AddChild(Row("menu.shimmer", Choices([("menu.on", () => Shimmer(true)), ("menu.off", () => Shimmer(false))])));
                break;
            case SettingsCategory.Game:
                page.AddChild(Row("menu.language", Choices([("menu.langEn", () => _ui.Language.Set(Language.English)), ("menu.langRu", () => _ui.Language.Set(Language.Russian))])));
                page.AddChild(Row("menu.slots", Choices([("menu.slotsNormal", () => _ui.SlotScale.Set(SlotScale.Normal)), ("menu.slotsLarge", () => _ui.SlotScale.Set(SlotScale.Large))])));
                page.AddChild(Row(null, Style.Button(T("menu.logs"), Logs.OpenFolder, size: 13, padX: 14, padY: 5)));
                break;
            case SettingsCategory.Controls:
                page.AddChild(new BindRows(_ui));
                break;
            case SettingsCategory.Hero:
                suits!.SizeFlagsHorizontal = SizeFlags.ShrinkBegin;
                page.AddChild(Row("menu.suit", suits));
                page.AddChild(Row(null, Style.Button(T("menu.look"), look!, size: 13, padX: 14, padY: 5)));
                break;
        }
        return page;
    }

    private static string VolumeKey(string bus) => bus == Sound.Master ? "menu.volMaster" : bus == Sound.Effects ? "menu.volEffects" : "menu.volMusic";

    /// <summary>A row: its name at the left, in a column as wide as every row's, what it sets at the right.</summary>
    private static HBoxContainer Row(string? key, Control setting)
    {
        var row = Style.Gap(new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore }, 0);
        var name = Tablet.Text(key == null ? "" : T(key), Style.Slate300, 13);
        name.CustomMinimumSize = new Vector2(LabelWidth, 28);
        row.AddChild(name);
        setting.SizeFlagsVertical = SizeFlags.ShrinkCenter;
        row.AddChild(setting);
        return row;
    }

    private HBoxContainer Choices((string Key, Action Pressed)[] choices)
    {
        var row = Style.Gap(new HBoxContainer(), 8);
        foreach (var (key, pressed) in choices)
            row.AddChild(_buttons[key] = Style.Button(T(key), () =>
            {
                pressed();
                Paint();
            }, size: 13, padX: 14, padY: 5));
        return row;
    }

    /// <summary>A bus's slider, set as it moves and kept at each step, its step or "muted" beside it.</summary>
    private static HBoxContainer Slider(string bus)
    {
        var row = Style.Gap(new HBoxContainer(), 14);
        var slider = new HSlider
        {
            MinValue = 0,
            MaxValue = Sound.VolumeSteps,
            Step = 1,
            Value = Settings.Volume(bus),
            TickCount = Sound.VolumeSteps + 1,
            TicksOnBorders = true,
            FocusMode = FocusModeEnum.None,
            CustomMinimumSize = new Vector2(SliderWidth, 22),
            SizeFlagsVertical = SizeFlags.ShrinkCenter,
        };
        Paint(slider);
        var shown = Tablet.Text("", Style.Slate100, 13);
        void Say(int step) => shown.Text = Volume.Muted(step) ? T("menu.muted") : step.ToString(System.Globalization.CultureInfo.InvariantCulture);
        Say((int)slider.Value);
        slider.ValueChanged += value =>
        {
            int step = (int)value;
            Settings.ApplyVolume(bus, step);
            Settings.KeepVolume(bus, step);
            Say(step);
        };
        row.AddChild(slider);
        row.AddChild(shown);
        return row;
    }

    /// <summary>The slider as the tablet's screen draws it: a dark track, its filled part sky, notches at each step, a pale grabber.</summary>
    private static void Paint(HSlider slider)
    {
        var track = Style.Box(Style.Slate800, Style.Slate800, radius: 1);
        (track.ContentMarginTop, track.ContentMarginBottom) = (2, 2);
        slider.AddThemeStyleboxOverride("slider", track);
        slider.AddThemeStyleboxOverride("grabber_area", Style.Box(Style.Sky700, Style.Sky700, radius: 1));
        slider.AddThemeStyleboxOverride("grabber_area_highlight", Style.Box(Style.Sky600, Style.Sky600, radius: 1));
        slider.AddThemeIconOverride("grabber", Block(8, 16, Style.Sky200));
        slider.AddThemeIconOverride("grabber_highlight", Block(8, 16, Colors.White));
        slider.AddThemeIconOverride("tick", Block(1, 6, Style.Slate600));
    }

    private static ImageTexture Block(int width, int height, Color colour)
    {
        var image = Image.CreateEmpty(width, height, false, Image.Format.Rgba8);
        image.Fill(colour);
        return ImageTexture.CreateFromImage(image);
    }

    private static void Shimmer(bool on)
    {
        Render.Shimmer.Enabled = on;
        Settings.KeepShimmer(on);
    }

    private static void Screen(ScreenMode mode)
    {
        Settings.Apply(mode);
        Settings.Keep(mode);
    }

    private void Choose(int index)
    {
        _chosen = index;
        for (int i = 0; i < _pages.Count; i++) _pages[i].Visible = i == index;
        PaintTabs();
    }

    private void Hover(int index)
    {
        _hovered = index;
        PaintTabs();
    }

    private void PaintTabs()
    {
        for (int i = 0; i < _tabs.Count; i++) Tablet.PaintTab(_tabs[i], i == _chosen, i == _hovered);
    }

    /// <summary>The choices in use lit, the others as any button.</summary>
    private void Paint()
    {
        bool large = _ui.SlotScale.Get() == SlotScale.Large, on = Render.Shimmer.Enabled;
        Light("menu.langEn", _ui.Language.Get() == Language.English);
        Light("menu.langRu", _ui.Language.Get() == Language.Russian);
        Light("menu.slotsNormal", !large);
        Light("menu.slotsLarge", large);
        Light("menu.on", on);
        Light("menu.off", !on);
        var screen = Settings.Shown();
        Light("menu.windowed", screen == ScreenMode.Windowed);
        Light("menu.fullscreen", screen == ScreenMode.Fullscreen);
        Light("menu.borderless", screen == ScreenMode.Borderless);
    }

    private void Light(string key, bool lit) =>
        Style.Paint(_buttons[key], lit ? Style.Sky700 : Style.Slate700, lit ? Style.Sky700 : Style.Slate600, 14, 5);
}
