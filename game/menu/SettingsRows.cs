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
/// The settings kept on this machine, the game menu's and the title's alike: the language (2026-11-02-russian-language), the slots' size, Normal or Large (0058), heat
/// shimmer On or Off (0075), the screen (in a window, full screen, frameless), each applied at once and kept in <c>user://settings.cfg</c>, the one in use lit; and Open logs folder.
/// </summary>
public partial class SettingsRows : VBoxContainer
{
    private readonly Ui _ui;
    private readonly Dictionary<string, Button> _buttons = [];

    public SettingsRows(Ui ui)
    {
        _ui = ui;
        Style.Gap(this, 8);
        AddChild(Line(T("menu.language")));
        AddChild(Pair([("menu.langEn", () => _ui.Language.Set(Language.English)), ("menu.langRu", () => _ui.Language.Set(Language.Russian))]));
        _ui.Language.Subscribe(Paint);
        AddChild(Line(T("menu.slots")));
        AddChild(Pair([("menu.slotsNormal", () => _ui.SlotScale.Set(SlotScale.Normal)), ("menu.slotsLarge", () => _ui.SlotScale.Set(SlotScale.Large))]));
        _ui.SlotScale.Subscribe(Paint);
        AddChild(Line(T("menu.shimmer")));
        AddChild(Pair([("menu.on", () => Shimmer(true)), ("menu.off", () => Shimmer(false))]));
        AddChild(Line(T("menu.screen")));
        AddChild(Pair([("menu.windowed", () => Screen(ScreenMode.Windowed)), ("menu.fullscreen", () => Screen(ScreenMode.Fullscreen)),
            ("menu.borderless", () => Screen(ScreenMode.Borderless))]));
        var logs = Style.Button(T("menu.logs"), Logs.OpenFolder, size: 14, padX: 16, padY: 6);
        AddChild(logs);
        Paint();
    }

    private static void Keep(bool on)
    {
        Render.Shimmer.Enabled = on;
        Settings.KeepShimmer(on);
    }

    private void Shimmer(bool on)
    {
        Keep(on);
        Paint();
    }

    private void Screen(ScreenMode mode)
    {
        Settings.Apply(mode);
        Settings.Keep(mode);
        Paint();
    }

    private HBoxContainer Pair((string Key, Action Pressed)[] choices)
    {
        var row = Style.Gap(new HBoxContainer { Alignment = AlignmentMode.Center }, 8);
        foreach (var (key, pressed) in choices) row.AddChild(_buttons[key] = Style.Button(T(key), pressed, size: 13, padX: 14, padY: 5));
        return row;
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

    private static Label Line(string text) => new()
    {
        Text = text,
        LabelSettings = Style.Text(Style.Mono, 12, Style.Slate300),
        HorizontalAlignment = HorizontalAlignment.Center,
        MouseFilter = MouseFilterEnum.Ignore,
    };
}
