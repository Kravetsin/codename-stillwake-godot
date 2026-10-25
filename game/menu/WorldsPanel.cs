using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Stillwake.Game.Platform;
using Stillwake.Game.Render;
using static Stillwake.Game.I18n.En;

namespace Stillwake.Game.Menu;

/// <summary>
/// The title's Worlds panel (0031, the approved mockup): the worlds, the last played first, each with
/// when it was last played and how long; one this build cannot play greyed with why. Play the one
/// picked, Delete it after a word of confirmation in the panel (any world, one that cannot be played
/// too), or make a new one by name.
/// </summary>
public partial class WorldsPanel : VBoxContainer
{
    private readonly Func<IReadOnlyList<WorldEntry>> _list;
    private readonly Action<string> _play, _delete;
    private readonly Action<string> _create;
    private readonly Func<bool> _busy;
    private readonly VBoxContainer _rows;
    private readonly Button _playButton, _deleteButton;
    private readonly HBoxContainer _confirm;
    private readonly Label _sure, _status;
    private readonly LineEdit _name;
    private IReadOnlyList<WorldEntry> _worlds = [];

    /// <summary>The world picked, if any, playable or not: what Delete takes.</summary>
    public string? Picked { get; private set; }

    /// <summary>The world picked when it can be played: what Play takes and Play together hosts.</summary>
    public string? Playable => WorldPick.Playable(_worlds, Picked);

    /// <param name="list">The worlds on disk, read afresh.</param>
    /// <param name="play">Plays a world.</param>
    /// <param name="delete">Deletes a world.</param>
    /// <param name="create">Makes a world of the name given and plays it.</param>
    /// <param name="busy">Whether joining a friend is under way: nothing is played meanwhile.</param>
    public WorldsPanel(Func<IReadOnlyList<WorldEntry>> list, Action<string> play, Action<string> delete, Action<string> create, Func<bool> busy)
    {
        (_list, _play, _delete, _create, _busy) = (list, play, delete, create, busy);
        Style.Gap(this, 8);
        AddChild(Line(T("title.worlds"), Style.Slate400, 12));
        var scroll = new ScrollContainer { CustomMinimumSize = new Vector2(420, 240), HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        scroll.AddChild(_rows = Style.Gap(new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill }, 4));
        AddChild(scroll);
        var actions = Style.Gap(new HBoxContainer(), 6);
        actions.AddChild(_playButton = Style.Button("", () => { if (Playable is string id) _play(id); }, size: 13, padX: 12, padY: 5));
        actions.AddChild(_deleteButton = Style.Button(T("worlds.delete"), AskDelete, size: 13, padX: 12, padY: 5));
        AddChild(actions);
        _confirm = Style.Gap(new HBoxContainer(), 6);
        _confirm.AddChild(_sure = Line("", Style.Red300, 12));
        _confirm.AddChild(Style.Button(T("worlds.deleteYes"), () =>
        {
            if (Picked is string id) _delete(id);
            Refresh();
        }, size: 12, padX: 10, padY: 4));
        _confirm.AddChild(Style.Button(T("worlds.keep"), () => _confirm.Visible = false, size: 12, padX: 10, padY: 4));
        _confirm.Visible = false;
        AddChild(_confirm);
        var make = Style.Gap(new HBoxContainer(), 6);
        make.AddChild(_name = new LineEdit
        {
            PlaceholderText = T("worlds.name"),
            MaxLength = WorldIndex.NameMost,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        });
        _name.AddThemeFontOverride("font", Style.Mono);
        _name.AddThemeFontSizeOverride("font_size", 13);
        _name.TextSubmitted += _ => Create();
        make.AddChild(Style.Button(T("worlds.new"), Create, size: 13, padX: 12, padY: 5));
        AddChild(make);
        AddChild(_status = Line("", Style.Red300, 12));
        _status.AutowrapMode = TextServer.AutowrapMode.WordSmart;
    }

    /// <summary>Says what went wrong, under the panel: a world that would not load.</summary>
    public void Say(string text) => _status.Text = text;

    /// <summary>Reads the worlds afresh and redraws the list, keeping the pick if it is still there.</summary>
    public void Refresh()
    {
        _worlds = _list();
        _confirm.Visible = false;
        Picked = WorldPick.After(_worlds, Picked);
        foreach (var child in _rows.GetChildren()) child.QueueFree();
        foreach (var world in _worlds) _rows.AddChild(Row(world));
        Buttons();
    }

    private void Buttons()
    {
        var picked = _worlds.FirstOrDefault(w => w.Id == Picked);
        bool playable = picked?.Status == WorldStatus.Playable;
        _playButton.Text = playable ? T("worlds.play", ("name", picked!.Name)) : T("worlds.playNone");
        _playButton.Disabled = !playable || _busy();
        _deleteButton.Disabled = picked == null;
    }

    private Button Row(WorldEntry world)
    {
        bool playable = world.Status == WorldStatus.Playable;
        string why = world.Status switch
        {
            WorldStatus.Newer => T("worlds.newer"),
            WorldStatus.TooOld => T("worlds.tooOld"),
            WorldStatus.Damaged => T("worlds.damaged"),
            _ => "",
        };
        string text = $"{world.Name}\n{Ago(world.LastPlayed)} · {Played(world.PlaySeconds)}" + (why != "" ? $"\n{why}" : "");
        var row = Style.Button(text, () =>
        {
            Picked = world.Id;
            _confirm.Visible = false;
            Refresh();
        }, size: 12, padX: 10, padY: 6);
        row.Alignment = HorizontalAlignment.Left;
        row.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        if (world.Id == Picked) row.Modulate = new Color(0.75f, 0.9f, 1.15f);
        else if (!playable) row.Modulate = new Color(1, 1, 1, 0.5f);
        return row;
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

    private void AskDelete()
    {
        if (_worlds.FirstOrDefault(w => w.Id == Picked) is not { } picked) return;
        _sure.Text = T("worlds.deleteSure", ("name", picked.Name));
        _confirm.Visible = true;
    }

    private void Create()
    {
        if (_busy()) return;
        _create(_name.Text);
        _name.Text = "";
    }

    public override void _Process(double delta)
    {
        if (IsVisibleInTree()) Buttons();
    }

    private static Label Line(string text, Color colour, int size) => new()
    {
        Text = text,
        LabelSettings = Style.Text(Style.Mono, size, colour),
        MouseFilter = MouseFilterEnum.Ignore,
    };
}
