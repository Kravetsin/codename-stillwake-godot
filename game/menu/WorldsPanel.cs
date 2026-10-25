using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Stillwake.Game.Platform;
using Stillwake.Game.Render;
using static Stillwake.Game.I18n.En;

namespace Stillwake.Game.Menu;

/// <summary>
/// The title's Worlds panel (0031): New world first, a row that opens into its name, the name it gets if left empty shown
/// in it; then the worlds, the last played first, each a card with its ship as last saved, when it was played and how
/// long, and why this build cannot play it if it cannot. The card picked holds what can be done with it: Play, Delete
/// after a word in the card — any world, one that cannot be played too (<see cref="WorldPick"/>). A double click plays.
/// </summary>
public partial class WorldsPanel : VBoxContainer
{
    /// <summary>A card's picture of its ship, in pixels: 16 by 9.</summary>
    private static readonly Vector2 ShotSize = new(128, 72);
    private static readonly Color Amber = new("fbbf24"), Card = new("0f172a"), CardHover = new("1e293b"), CardPicked = new("0c2a40");

    private readonly Func<IReadOnlyList<WorldEntry>> _list;
    private readonly Action<string> _play, _delete, _create;
    private readonly Func<string, Texture2D?> _shot;
    private readonly Func<bool> _busy;
    private readonly VBoxContainer _rows;
    private readonly Label _status;
    private IReadOnlyList<WorldEntry> _worlds = [];
    private bool _naming, _sure;

    /// <summary>The world picked, if any, playable or not: what Delete takes.</summary>
    public string? Picked { get; private set; }

    /// <summary>The world picked when it can be played: what Play takes and Play together hosts.</summary>
    public string? Playable => WorldPick.Playable(_worlds, Picked);

    /// <param name="list">The worlds on disk, read afresh.</param>
    /// <param name="play">Plays a world.</param>
    /// <param name="delete">Deletes a world.</param>
    /// <param name="create">Makes a world of the name given and plays it.</param>
    /// <param name="shot">A world's ship as last saved, or none.</param>
    /// <param name="busy">Whether joining a friend is under way: nothing is played meanwhile.</param>
    public WorldsPanel(Func<IReadOnlyList<WorldEntry>> list, Action<string> play, Action<string> delete, Action<string> create,
        Func<string, Texture2D?> shot, Func<bool> busy)
    {
        (_list, _play, _delete, _create, _shot, _busy) = (list, play, delete, create, shot, busy);
        Style.Gap(this, 10);
        AddChild(Line(T("title.worlds"), Style.Slate300, 16));
        var scroll = new ScrollContainer { CustomMinimumSize = new Vector2(440, 380), HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        scroll.AddChild(_rows = Style.Gap(new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill }, 6));
        AddChild(scroll);
        AddChild(_status = Line("", Style.Red300, 12));
        _status.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _status.CustomMinimumSize = new Vector2(440, 0);
    }

    /// <summary>Says what went wrong, under the panel: a world that would not load.</summary>
    public void Say(string text) => _status.Text = text;

    /// <summary>Reads the worlds afresh and redraws the list, keeping the pick if it is still there.</summary>
    public void Refresh()
    {
        _worlds = _list();
        Picked = WorldPick.After(_worlds, Picked);
        Redraw();
    }

    private void Redraw()
    {
        foreach (var child in _rows.GetChildren()) child.QueueFree();
        _rows.AddChild(_naming ? Naming() : NewRow());
        foreach (var world in _worlds) _rows.AddChild(CardOf(world));
    }

    private void Pick(string id)
    {
        (Picked, _sure, _naming) = (id, false, false);
        Redraw();
    }

    private Button NewRow()
    {
        var row = Style.Button($"+  {T("worlds.new")}", () =>
        {
            (_naming, _sure) = (true, false);
            Redraw();
        }, size: 14, padX: 14, padY: 12);
        Style.Paint(row, Card with { A = 0.5f }, CardHover, 14, 12);
        row.AddThemeStyleboxOverride("normal", Edged(Card with { A = 0.5f }, Style.Sky700));
        row.AddThemeStyleboxOverride("hover", Edged(CardHover, Style.Sky400));
        row.AddThemeColorOverride("font_color", Style.Sky200);
        row.AddThemeColorOverride("font_hover_color", Style.Sky200);
        row.Alignment = HorizontalAlignment.Left;
        return row;
    }

    /// <summary>New world, open: its name typed, the name it gets if left empty shown in the field; Enter makes it, Esc puts it away.</summary>
    private PanelContainer Naming()
    {
        var box = new PanelContainer();
        box.AddThemeStyleboxOverride("panel", Edged(CardPicked, Style.Sky400, 12));
        var column = Style.Gap(new VBoxContainer(), 8);
        column.AddChild(Line(T("worlds.newTitle"), Style.Sky200, 13));
        var row = Style.Gap(new HBoxContainer(), 8);
        var name = new LineEdit
        {
            PlaceholderText = WorldIndex.NameOf("", _worlds.Count),
            MaxLength = WorldIndex.NameMost,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        name.AddThemeFontOverride("font", Style.Mono);
        name.AddThemeFontSizeOverride("font_size", 14);
        void Make()
        {
            if (_busy()) return;
            _naming = false;
            _create(name.Text);
        }
        name.TextSubmitted += _ => Make();
        name.GuiInput += e =>
        {
            if (e is not InputEventKey { Pressed: true, PhysicalKeycode: Key.Escape }) return;
            _naming = false;
            Redraw();
            name.AcceptEvent();
        };
        row.AddChild(name);
        var make = Style.Button(T("worlds.create"), Make, size: 14, padX: 16, padY: 6);
        Style.Paint(make, Style.Sky600, Style.Sky400, 16, 6);
        row.AddChild(make);
        column.AddChild(row);
        column.AddChild(Line(T("worlds.newHint"), Style.Slate500, 11));
        box.AddChild(column);
        Callable.From(() => name.GrabFocus()).CallDeferred();
        return box;
    }

    private Button CardOf(WorldEntry world)
    {
        bool picked = world.Id == Picked, playable = world.Status == WorldStatus.Playable;
        var card = new Button { FocusMode = FocusModeEnum.None, MouseDefaultCursorShape = CursorShape.PointingHand };
        card.AddThemeStyleboxOverride("normal", Edged(picked ? CardPicked : Card with { A = 0.7f }, picked ? Style.Sky400 : Style.Slate800));
        card.AddThemeStyleboxOverride("hover", Edged(picked ? CardPicked : CardHover, picked ? Style.Sky400 : Style.Slate600));
        card.AddThemeStyleboxOverride("pressed", Edged(CardPicked, Style.Sky400));
        var row = Style.Gap(new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore }, 12);
        row.Position = new Vector2(8, 8);
        row.AddChild(Shot(world));
        var text = Style.Gap(new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore, SizeFlagsHorizontal = SizeFlags.ExpandFill }, 3);
        text.AddChild(Line(world.Name, playable ? Style.Slate100 : Style.Slate400, 15));
        text.AddChild(Line($"{Ago(world.LastPlayed)} · {Played(world.PlaySeconds)}", Style.Slate400, 12));
        string why = world.Status switch
        {
            WorldStatus.Newer => T("worlds.newer"),
            WorldStatus.TooOld => T("worlds.tooOld"),
            WorldStatus.Damaged => T("worlds.damaged"),
            _ => "",
        };
        if (why != "")
        {
            var said = Line(why, Amber, 11);
            said.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            said.CustomMinimumSize = new Vector2(270, 0);
            text.AddChild(said);
        }
        if (picked) text.AddChild(Actions(world, playable));
        row.AddChild(text);
        card.AddChild(row);
        row.Resized += () => card.CustomMinimumSize = new Vector2(0, row.Size.Y + 16);
        row.CustomMinimumSize = new Vector2(424, 0);
        card.Pressed += () =>
        {
            if (!picked) Pick(world.Id);
        };
        card.GuiInput += e =>
        {
            if (e is InputEventMouseButton { DoubleClick: true, ButtonIndex: MouseButton.Left } && playable && !_busy()) _play(world.Id);
        };
        return card;
    }

    /// <summary>What the picked card can do: Play when it can be played, Delete, and Delete asked once more inside the card.</summary>
    private HBoxContainer Actions(WorldEntry world, bool playable)
    {
        var row = Style.Gap(new HBoxContainer(), 8);
        if (_sure)
        {
            row.AddChild(Line(T("worlds.deleteSure"), Style.Red300, 12));
            var yes = Style.Button(T("worlds.deleteYes"), () =>
            {
                _sure = false;
                _delete(world.Id);
                Refresh();
            }, size: 12, padX: 10, padY: 4);
            Style.Paint(yes, new Color("7f1d1d"), new Color("991b1b"), 10, 4);
            row.AddChild(yes);
            row.AddChild(Style.Button(T("worlds.keep"), () =>
            {
                _sure = false;
                Redraw();
            }, size: 12, padX: 10, padY: 4));
            return row;
        }
        if (playable)
        {
            var play = Style.Button(T("worlds.playNone"), () =>
            {
                if (!_busy()) _play(world.Id);
            }, size: 13, padX: 18, padY: 5);
            Style.Paint(play, Style.Sky600, Style.Sky400, 18, 5);
            foreach (var state in (string[])["font_color", "font_hover_color", "font_pressed_color"]) play.AddThemeColorOverride(state, new Color("f0f9ff"));
            play.Disabled = _busy();
            row.AddChild(play);
        }
        var delete = Style.Button(T("worlds.delete"), () =>
        {
            _sure = true;
            Redraw();
        }, size: 13, padX: 12, padY: 5);
        Style.Paint(delete, Colors.Transparent, new Color("450a0a"), 12, 5);
        foreach (var state in (string[])["font_color", "font_hover_color", "font_pressed_color"]) delete.AddThemeColorOverride(state, Style.Red300);
        row.AddChild(delete);
        return row;
    }

    /// <summary>A world's ship as last saved, or a dark frame with a faint mark for one saved before pictures were kept.</summary>
    private Control Shot(WorldEntry world)
    {
        var frame = new PanelContainer { CustomMinimumSize = ShotSize, MouseFilter = MouseFilterEnum.Ignore, SizeFlagsVertical = SizeFlags.ShrinkBegin };
        frame.AddThemeStyleboxOverride("panel", Style.Box(new Color("05080e"), Style.Slate800, 0, radius: 3));
        if (_shot(world.Id) is { } picture)
            frame.AddChild(new TextureRect
            {
                Texture = picture,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered,
                CustomMinimumSize = ShotSize,
                MouseFilter = MouseFilterEnum.Ignore,
                Modulate = world.Status == WorldStatus.Playable ? Colors.White : new Color(1, 1, 1, 0.45f),
            });
        else
        {
            var mark = Line("◇", Style.Slate700, 22);
            (mark.HorizontalAlignment, mark.VerticalAlignment) = (HorizontalAlignment.Center, VerticalAlignment.Center);
            frame.AddChild(mark);
        }
        return frame;
    }

    /// <summary>When a world was last played, as a person says it: today, yesterday, or its date.</summary>
    public static string Ago(DateTime utc)
    {
        var day = utc.ToLocalTime().Date;
        var today = DateTime.Now.Date;
        return day == today ? T("worlds.today") : day == today.AddDays(-1) ? T("worlds.yesterday")
            : day.ToString("d MMM", System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>How long a world has been played: minutes, or hours and minutes.</summary>
    public static string Played(int seconds)
    {
        int minutes = seconds / 60;
        return minutes < 60 ? T("worlds.minutes", ("m", minutes)) : T("worlds.hours", ("h", minutes / 60), ("m", minutes % 60));
    }

    public override void _Process(double delta)
    {
        // A friend's world on its way in: nothing is played meanwhile.
        if (IsVisibleInTree() && _busy() != _wasBusy)
        {
            _wasBusy = !_wasBusy;
            Redraw();
        }
    }

    private bool _wasBusy;

    private static StyleBoxFlat Edged(Color fill, Color edge, int padding = 0)
    {
        var box = Style.Box(fill, edge, padding, radius: 5);
        if (padding == 0) (box.ContentMarginLeft, box.ContentMarginRight, box.ContentMarginTop, box.ContentMarginBottom) = (14, 14, 12, 12);
        return box;
    }

    private static Label Line(string text, Color colour, int size) => new()
    {
        Text = text,
        LabelSettings = Style.Text(Style.Mono, size, colour),
        MouseFilter = MouseFilterEnum.Ignore,
    };
}
