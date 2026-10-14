using System;
using Godot;

namespace Stillwake.Game.Render;

/// <summary>The Tailwind colours and boxes the React panels used, as Godot style boxes and label settings.</summary>
public static class Style
{
    public static readonly Color Slate950 = new("020617"), Slate900 = new("0f172a"), Slate800 = new("1e293b"),
        Slate700 = new("334155"), Slate600 = new("475569"), Slate400 = new("94a3b8"), Slate300 = new("cbd5e1"),
        Slate100 = new("f1f5f9"), Amber300 = new("fcd34d"), Amber400 = new("fbbf24"), Amber700 = new("b45309"),
        Orange400 = new("fb923c"), Green400 = new("4ade80"), Orange500 = new("f97316"), Orange700 = new("c2410c"), Red300 = new("fca5a5"),
        Sky200 = new("bae6fd"), Sky400 = new("38bdf8"), Sky600 = new("0284c7"), Sky700 = new("0369a1");

    // Stems widened a touch, as Chrome's DirectWrite drew light text on dark; mono on whole pixels (render/fonts).
    // sans.tres is the project's default font (gui/theme/custom_font), so every Control falls back to it.
    public static readonly Font Mono = GD.Load<Font>("res://render/fonts/mono.tres");
    public static readonly Font Sans = GD.Load<Font>("res://render/fonts/sans.tres");
    public static readonly Font Bold = GD.Load<Font>("res://render/fonts/semibold.tres");

    /// <summary><c>rounded border bg-… border-… p-…</c>.</summary>
    public static StyleBoxFlat Box(Color bg, Color border, int padding = 0, int radius = 4)
    {
        var box = new StyleBoxFlat { BgColor = bg, BorderColor = border };
        box.SetBorderWidthAll(1);
        box.SetCornerRadiusAll(radius);
        box.SetContentMarginAll(padding);
        return box;
    }

    /// <summary>A panel: <c>rounded border bg-slate-900/90 p-1</c> with the given edge.</summary>
    public static PanelContainer Panel(Color border, Control child)
    {
        var panel = new PanelContainer { MouseFilter = Control.MouseFilterEnum.Stop };
        panel.AddThemeStyleboxOverride("panel", Box(Slate900 with { A = 0.9f }, border, 4));
        panel.AddChild(child);
        return panel;
    }

    public static LabelSettings Text(Font font, int size, Color color, bool shadow = false) => new()
    {
        Font = font,
        FontSize = size,
        FontColor = color,
        ShadowColor = shadow ? new Color(0, 0, 0, 0.6f) : Colors.Transparent,
        ShadowSize = shadow ? 1 : 0,
        ShadowOffset = new Vector2(0, 1),
    };

    /// <summary>A button, <c>rounded bg-slate-700 px-… py-… text-slate-300 hover:bg-slate-600</c> in mono, never taking the keyboard's focus.</summary>
    public static Button Button(string text, Action pressed, int size = 12, int padX = 8, int padY = 4)
    {
        var button = new Button { Text = text, FocusMode = Control.FocusModeEnum.None };
        Paint(button, Slate700, Slate600, padX, padY);
        button.AddThemeFontOverride("font", Mono);
        button.AddThemeFontSizeOverride("font_size", size);
        foreach (var state in (string[])["font_color", "font_hover_color", "font_pressed_color"]) button.AddThemeColorOverride(state, Slate300);
        button.AddThemeColorOverride("font_disabled_color", Slate600);
        button.Pressed += pressed;
        return button;
    }

    /// <summary>A button's background, <paramref name="colour"/> and <paramref name="hover"/> under the pointer; disabled, dark.</summary>
    public static void Paint(Button button, Color colour, Color hover, int padX = 8, int padY = 4)
    {
        StyleBoxFlat Of(Color c)
        {
            var box = Box(c, c);
            (box.ContentMarginLeft, box.ContentMarginRight, box.ContentMarginTop, box.ContentMarginBottom) = (padX, padX, padY, padY);
            return box;
        }
        button.AddThemeStyleboxOverride("normal", Of(colour));
        button.AddThemeStyleboxOverride("hover", Of(hover));
        button.AddThemeStyleboxOverride("pressed", Of(hover));
        button.AddThemeStyleboxOverride("disabled", Of(Slate800));
    }

    /// <summary>The gap between a box's children, <c>gap-…</c>.</summary>
    public static T Gap<T>(T box, int gap) where T : Control
    {
        box.AddThemeConstantOverride("separation", gap);
        box.AddThemeConstantOverride("h_separation", gap);
        box.AddThemeConstantOverride("v_separation", gap);
        return box;
    }

    /// <summary>A bar filled from the left, <c>h-1.5 rounded bg-slate-700</c> with its fill colour.</summary>
    public static ProgressBar Bar(Color fill, float width)
    {
        var bar = new ProgressBar
        {
            CustomMinimumSize = new Vector2(width, 6),
            ShowPercentage = false,
            MaxValue = 1,
            SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        bar.AddThemeStyleboxOverride("background", Box(Slate700, Slate700));
        bar.AddThemeStyleboxOverride("fill", Box(fill, fill));
        return bar;
    }
}
