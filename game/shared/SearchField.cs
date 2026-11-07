using Godot;
using Stillwake.Game.Render;

namespace Stillwake.Game.Shared;

/// <summary>
/// A tablet's search field, the build menu's and the board's: dark, outlined sky while typed in, a lens at its left and
/// the Search key's cap (/) at its right while it is empty and idle. Its owner lays it out with <see cref="Lay"/>.
/// </summary>
public partial class SearchField : LineEdit
{
    private static readonly Color Sky500 = new("0ea5e9");
    private readonly Control _lens = new() { MouseFilter = MouseFilterEnum.Ignore };
    private readonly PanelContainer _slash = new() { MouseFilter = MouseFilterEnum.Ignore };

    public SearchField() : this("") { }

    /// <param name="placeholder">What it says while empty.</param>
    public SearchField(string placeholder)
    {
        (ContextMenuEnabled, CaretBlink, MouseFilter, PlaceholderText) = (false, true, MouseFilterEnum.Stop, placeholder);
        AddThemeFontOverride("font", Style.Mono);
        AddThemeFontSizeOverride("font_size", Tablet.FontSize);
        AddThemeColorOverride("font_color", Style.Slate100);
        AddThemeColorOverride("font_placeholder_color", Style.Slate500);
        AddThemeColorOverride("caret_color", Style.Slate100);
        AddThemeStyleboxOverride("normal", Field(Style.Slate700));
        var focus = Field(Sky500);
        focus.DrawCenter = false;
        AddThemeStyleboxOverride("focus", focus);
        var keyBox = Style.Box(Colors.Transparent, Style.Slate700, radius: 3);
        (keyBox.ContentMarginLeft, keyBox.ContentMarginRight) = (4, 4);
        _slash.AddThemeStyleboxOverride("panel", keyBox);
        _slash.AddChild(Tablet.Text("/", Style.Slate500, Tablet.HintSize));
        _lens.Draw += () =>
        {
            _lens.DrawArc(new Vector2(12, 11), 4, 0, Mathf.Tau, 16, Style.Slate500, 1.5f, true);
            _lens.DrawLine(new Vector2(15, 14), new Vector2(18, 17), Style.Slate500, 1.5f, true);
        };
        AddChild(_lens);
        AddChild(_slash);
    }

    // Text set from code emits no signal, so the key cap is checked each frame.
    public override void _Process(double delta) => _slash.Visible = Text == "" && !HasFocus();

    private static StyleBoxFlat Field(Color border)
    {
        var box = Style.Box(Style.Slate950, border);
        (box.ContentMarginLeft, box.ContentMarginRight, box.ContentMarginTop, box.ContentMarginBottom) = (24, 8, 0, 0);
        return box;
    }

    /// <summary>Sets it at <paramref name="at"/>, <paramref name="width"/> by <paramref name="height"/>, its lens and key cap with it.</summary>
    public void Lay(Vector2 at, float width, float height)
    {
        Position = at;
        Size = new Vector2(width, height);
        _lens.Size = new Vector2(24, height);
        _slash.Position = new Vector2(width - 22, 4);
        _slash.Size = new Vector2(16, height - 8);
    }

    /// <summary>Empties it and lets go of the keys.</summary>
    public void Drop()
    {
        Text = "";
        ReleaseFocus();
    }
}
