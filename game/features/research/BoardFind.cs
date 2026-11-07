using System;
using System.Collections.Generic;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using static Stillwake.Game.I18n.En;

namespace Stillwake.Game.Features.Research;

/// <summary>
/// The board's search (board-search), at the view's top right: what is typed lists what on the board is so named
/// (<see cref="BoardSearch"/>), a row a thing; a click on a row, or Enter on the one lit by the arrows, picks it on the board.
/// The list shows only while the field is typed in. It is not the build menu's search: that one finds cards to build.
/// </summary>
public partial class BoardFind : Control
{
    /// <summary>The field's width, a row's height and its picture, and the gap from the view's edge.</summary>
    private const int Wide = 240, Row = 26, Icon = 20, Edge = 8;

    private readonly SearchField _field = new(T("research.search"));
    private readonly PanelContainer _list = new() { MouseFilter = MouseFilterEnum.Stop, Visible = false };
    private readonly VBoxContainer _rows = new() { MouseFilter = MouseFilterEnum.Ignore };
    private readonly Func<IEnumerable<BoardFound>> _shown;
    private readonly Func<string, int, TextureRect> _picture;
    private readonly Action<string> _pick;
    private IReadOnlyList<BoardFound> _found = [];
    private readonly List<PanelContainer> _boxes = [];
    private int? _lit;

    /// <param name="shown">What the board shows now, named.</param>
    /// <param name="picture">A thing's picture, so many pixels across.</param>
    /// <param name="pick">Picks a thing on the board.</param>
    public BoardFind(Func<IEnumerable<BoardFound>> shown, Func<string, int, TextureRect> picture, Action<string> pick)
    {
        (_shown, _picture, _pick) = (shown, picture, pick);
        MouseFilter = MouseFilterEnum.Ignore;
        AddChild(_field);
        var box = Style.Box(Style.Slate950, Style.Slate700);
        (box.ContentMarginLeft, box.ContentMarginRight, box.ContentMarginTop, box.ContentMarginBottom) = (2, 2, 2, 2);
        _list.AddThemeStyleboxOverride("panel", box);
        Style.Gap(_rows, 0);
        _list.AddChild(_rows);
        AddChild(_list);
        _field.TextChanged += _ => List();
        _field.FocusEntered += List;
        _field.FocusExited += () => _list.Visible = false;
        _field.TextSubmitted += _ =>
        {
            if (_lit is { } lit) Pick(lit);
            else _field.ReleaseFocus();
        };
        _field.GuiInput += Arrows;
    }

    /// <summary>Whether the field has the keys.</summary>
    public bool Typing => _field.HasFocus();

    /// <summary>Puts the keys in the field, what was typed before selected so a new name replaces it: the Search key (/).</summary>
    public void Type()
    {
        _field.GrabFocus();
        _field.SelectAll();
    }

    /// <summary>
    /// Esc: a search typed or shown is emptied and the keys let go. Answers whether there was one, so Esc goes on to put
    /// the tablet away only when there was not.
    /// </summary>
    public bool Drop()
    {
        if (_field.Text == "" && !_field.HasFocus()) return false;
        _field.Drop();
        return true;
    }

    /// <summary>Sets it at the top right of a view <paramref name="width"/> wide.</summary>
    public void Lay(float width)
    {
        float left = width - Wide - Edge;
        _field.Lay(new Vector2(left, Edge), Wide, TabletLayout.TabHeight);
        _list.Position = new Vector2(left, Edge + TabletLayout.TabHeight + 2);
        _list.Size = new Vector2(Wide, 0);
    }

    /// <summary>The rows for what is typed now, the first lit.</summary>
    private void List()
    {
        _found = BoardSearch.Matching(_shown(), _field.Text);
        foreach (var child in _rows.GetChildren())
        {
            _rows.RemoveChild(child);
            child.QueueFree();
        }
        _boxes.Clear();
        if (_field.Text.Trim() == "")
        {
            _list.Visible = false;
            return;
        }
        if (_found.Count == 0)
        {
            var none = new MarginContainer { MouseFilter = MouseFilterEnum.Ignore, CustomMinimumSize = new Vector2(0, Row) };
            none.AddThemeConstantOverride("margin_left", 6);
            none.AddChild(Tablet.Text(T("research.search.none"), Style.Slate500));
            _rows.AddChild(none);
        }
        for (int i = 0; i < _found.Count; i++) _rows.AddChild(RowOf(i));
        _lit = _found.Count > 0 ? 0 : null;
        Paint();
        // Shrunk to its rows once they are laid out: fewer rows than before leave it as tall till then.
        Callable.From(() => _list.Size = new Vector2(Wide, 0)).CallDeferred();
        _list.Visible = _field.HasFocus();
    }

    private PanelContainer RowOf(int index)
    {
        var found = _found[index];
        var box = new PanelContainer { MouseFilter = MouseFilterEnum.Stop, CustomMinimumSize = new Vector2(0, Row) };
        var line = Style.Gap(new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore }, 6);
        var picture = _picture(found.Id, Icon);
        picture.CustomMinimumSize = new Vector2(Icon, Icon);
        picture.SizeFlagsVertical = SizeFlags.ShrinkCenter;
        picture.TextureFilter = TextureFilterEnum.Linear;
        // A shape is only sketched on the board: pale here too.
        if (!found.Note) picture.Modulate = new Color(1, 1, 1, 0.4f);
        line.AddChild(picture);
        var name = Tablet.Text(found.Name, found.Note ? Style.Slate100 : Style.Slate500);
        name.ClipText = true;
        name.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        line.AddChild(name);
        box.AddChild(line);
        box.MouseEntered += () =>
        {
            _lit = index;
            Paint();
        };
        box.GuiInput += e =>
        {
            if (e is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left }) return;
            box.AcceptEvent();
            Pick(index);
        };
        _boxes.Add(box);
        return box;
    }

    private void Paint()
    {
        for (int i = 0; i < _boxes.Count; i++)
        {
            var style = Style.Box(i == _lit ? Style.Slate700 : Style.Slate950, i == _lit ? Style.Slate700 : Style.Slate950);
            (style.ContentMarginLeft, style.ContentMarginRight) = (6, 6);
            _boxes[i].AddThemeStyleboxOverride("panel", style);
        }
    }

    /// <summary>Up and down light the row above or below, round the list; the field keeps every other key.</summary>
    private void Arrows(InputEvent e)
    {
        if (e is not InputEventKey { Pressed: true } key || _found.Count == 0) return;
        int by = key.Keycode switch { Key.Up => -1, Key.Down => 1, _ => 0 };
        if (by == 0) return;
        _lit = BoardSearch.Step(_lit, by, _found.Count);
        Paint();
        _field.AcceptEvent();
    }

    /// <summary>Picks the row <paramref name="index"/>, if there is one, and lets go of the keys; what was typed stays for another look.</summary>
    private void Pick(int index)
    {
        if (index >= _found.Count) return;
        string id = _found[index].Id;
        _field.ReleaseFocus();
        _pick(id);
    }

    /// <summary>A press anywhere but the field and its list ends the typing; the press goes on to whatever it was for.</summary>
    public override void _Input(InputEvent e)
    {
        if (e is InputEventMouseButton { Pressed: true } press && _field.HasFocus()
            && !_field.GetGlobalRect().HasPoint(press.Position) && !(_list.Visible && _list.GetGlobalRect().HasPoint(press.Position)))
            _field.ReleaseFocus();
    }
}
