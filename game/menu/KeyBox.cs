using System;
using Godot;
using Stillwake.Game.Render;
using static Stillwake.Game.I18n.En;

namespace Stillwake.Game.Menu;

/// <summary>
/// The code a host's friends join by (<see cref="HostKey"/>), large, with Copy beside it, the suits aboard and how hosting
/// stands under it: the card shown as a hosted world opens (<see cref="KeyCard"/>) and the Esc menu's while hosting.
/// </summary>
public partial class KeyBox : VBoxContainer
{
    private readonly Func<TogetherState> _state;
    private readonly Label _tell, _code, _line;
    private readonly Button _copy;
    private readonly HBoxContainer _suits;
    private string? _shownSuits, _copied;

    /// <param name="state">How playing together stands, read each frame the box is shown.</param>
    /// <param name="size">The code's font size.</param>
    public KeyBox(Func<TogetherState> state, int size)
    {
        _state = state;
        Style.Gap(this, 6);
        AddChild(_tell = Line(T("menu.tellCode"), Style.Slate400, 11));
        var row = Style.Gap(new HBoxContainer { Alignment = AlignmentMode.Center }, 12);
        row.AddChild(_code = Line("", Style.Sky200, size));
        _code.VerticalAlignment = VerticalAlignment.Center;
        _copy = Style.Button(T("menu.copy"), Copy, size: 13, padX: 14, padY: 5);
        _copy.SizeFlagsVertical = SizeFlags.ShrinkCenter;
        row.AddChild(_copy);
        AddChild(row);
        AddChild(_suits = Style.Gap(new HBoxContainer { Alignment = AlignmentMode.Center }, 6));
        AddChild(_line = Line("", Style.Green400, 12));
    }

    /// <summary>Copy, the label saying so till the code changes.</summary>
    private void Copy()
    {
        var state = _state();
        if (!HostKey.Copyable(state)) return;
        DisplayServer.ClipboardSet(state.Code);
        _copied = state.Code;
    }

    public override void _Process(double delta)
    {
        if (!IsVisibleInTree()) return;
        var state = _state();
        _code.Text = HostKey.Code(state) ?? "";
        _tell.Visible = _code.Visible = _copy.Visible = _code.Text != "";
        _copy.Disabled = !HostKey.Copyable(state);
        _copy.Text = T(_copied != null && _copied == state.Code ? "menu.copied" : "menu.copy");
        _line.Text = HostKey.Line(state);
        _line.LabelSettings.FontColor = state.Mode == TogetherMode.Alone ? Style.Red300 : Style.Green400;
        ShowSuits(state);
    }

    /// <summary>A small figure in each suit aboard, this player's first.</summary>
    private void ShowSuits(TogetherState state)
    {
        string key = string.Join(",", state.Suits);
        if (key == _shownSuits) return;
        _shownSuits = key;
        foreach (var child in _suits.GetChildren()) child.QueueFree();
        foreach (int suit in state.Suits)
            _suits.AddChild(new ColorRect { Color = new Color((uint)(suit << 8 | 0xff)), CustomMinimumSize = new Vector2(12, 16) });
    }

    private static Label Line(string text, Color colour, int size) => new()
    {
        Text = text,
        LabelSettings = Style.Text(Style.Mono, size, colour),
        HorizontalAlignment = HorizontalAlignment.Center,
        MouseFilter = MouseFilterEnum.Ignore,
    };
}
