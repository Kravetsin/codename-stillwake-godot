using System;
using Godot;
using Stillwake.Game.Render;

namespace Stillwake.Game.Menu;

/// <summary>
/// One of the title's choices: words alone over the scene, no box; under the pointer, or while its panel is open, they
/// brighten and slide right behind a short line, eased. A second, smaller line under them says more: Continue's world.
/// A button underneath, so it clicks as every button does.
/// </summary>
public partial class TitleItem : Button
{
    /// <summary>How long the light and the slide take to ease in and out, in seconds.</summary>
    private const double EaseSeconds = 0.14;
    /// <summary>How far the words slide right when lit, in pixels; the line drawn in the gap.</summary>
    private const float Slide = 18;

    private static readonly Color Idle = new("8fa3bd"), Lit = new("e0f2fe"), Marker = new("38bdf8"), Under = new("64748b"), UnderLit = new("7dd3fc");

    private readonly Label _words, _under;
    private readonly ColorRect _line = new() { Color = Marker, MouseFilter = MouseFilterEnum.Ignore };
    private double _light;

    /// <summary>Whether its panel is open: it stays lit.</summary>
    public bool Active { get; set; }

    public TitleItem(string text, Action pressed, int size)
    {
        FocusMode = FocusModeEnum.None;
        Flat = true;
        foreach (var state in (string[])["normal", "hover", "pressed", "disabled", "focus"]) AddThemeStyleboxOverride(state, new StyleBoxEmpty());
        MouseDefaultCursorShape = CursorShape.PointingHand;
        var column = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
        column.AddThemeConstantOverride("separation", 0);
        column.AddChild(_words = new Label { Text = text, LabelSettings = Style.Text(Style.Mono, size, Idle, shadow: true), MouseFilter = MouseFilterEnum.Ignore });
        column.AddChild(_under = new Label { LabelSettings = Style.Text(Style.Mono, 13, Under, shadow: true), MouseFilter = MouseFilterEnum.Ignore, Visible = false });
        AddChild(column);
        AddChild(_line);
        Pressed += pressed;
        column.Resized += () => CustomMinimumSize = column.Size + new Vector2(Slide + 8, 4);
    }

    /// <summary>The words, and the smaller line under them; none hides it.</summary>
    public void Say(string words, string? under = null)
    {
        _words.Text = words;
        _under.Text = under ?? "";
        _under.Visible = under != null;
    }

    public override void _Process(double delta)
    {
        bool lit = Active || IsHovered() && !Disabled;
        _light = Math.Clamp(_light + (lit ? 1 : -1) * delta / EaseSeconds, 0, 1);
        float t = (float)(_light * _light * (3 - 2 * _light));
        _words.LabelSettings.FontColor = Idle.Lerp(Lit, t);
        _under.LabelSettings.FontColor = Under.Lerp(UnderLit, t);
        var column = GetChild<Control>(0);
        column.Position = new Vector2(Slide * t, 0);
        float middle = _words.Size.Y / 2;
        _line.Position = new Vector2(0, middle - 1);
        _line.Size = new Vector2((Slide - 6) * t, 2);
    }
}
