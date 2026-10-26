using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using Stillwake.Net;
using Stillwake.Sim;
using Stillwake.Sim.Building;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Hazards;
using Stillwake.Sim.Items;
using S = Stillwake.Game.Sound;

namespace Stillwake.Game.Features.Dev;

/// <summary>
/// Development readouts and controls (features/dev-panel/DevPanel.tsx), in the top-right corner, hidden
/// at the start; the backquote key shows and hides it. Refreshed four times a second, never every
/// frame, and not at all while hidden: on a big world the readouts themselves would cost frames.
/// </summary>
public partial class DevPanel : CanvasLayer
{
    private const double RefreshMs = 250;
    /// <summary>A whole grid is megabytes of JSON; the viewer shows the start of it.</summary>
    private const int MaxJson = 20_000;
    /// <summary>How tall an open module's JSON may grow before it scrolls, <c>max-h-48</c>.</summary>
    private const int MaxJsonHeight = 192;
    private static readonly Color Amber600 = new("d97706");
    private static readonly JsonSerializerOptions Indented = new() { WriteIndented = true };

    private readonly Func<Session?> _session;
    private readonly Func<Registry> _modules;
    private readonly Func<string?> _link;

    private readonly PanelContainer _panel = new() { MouseFilter = Control.MouseFilterEnum.Stop };
    private readonly Label _fps = Text(Style.Slate400), _status = Text(Style.Slate400);
    private readonly GridContainer _readings = new() { Columns = 2 };
    private readonly VBoxContainer _slices = new();
    private readonly HFlowContainer _sounds = Style.Gap(new HFlowContainer { Visible = false }, 4);
    private readonly Button _speed, _creative, _instant;
    private readonly VBoxContainer _picker = Style.Gap(new VBoxContainer { Visible = false }, 4);
    private readonly LineEdit _search = new() { PlaceholderText = "find an item…", ContextMenuEnabled = false, CaretBlink = true, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
    private readonly Label _pickerNote = Text(Style.Amber400);
    private readonly Dictionary<string, Button> _rows = [];
    private readonly List<(Button Button, int? Count)> _counts = [];
    /// <summary>How many a pick gives; none for a whole stack of the item.</summary>
    private int? _count;
    private readonly HashSet<string> _expanded = [];
    private readonly Dictionary<string, (Button Toggle, ScrollContainer Box, Label Json)> _entries = [];
    private double _sinceRead = RefreshMs;

    /// <param name="session">The session this machine plays in now; none before it has one.</param>
    /// <param name="modules">The registry of the world it plays: a load or a resync brings a new one.</param>
    /// <param name="link">Where this machine stands on the network, in a line; none alone.</param>
    public DevPanel(Func<Session?> session, Func<Registry> modules, Func<string?> link)
    {
        (_session, _modules, _link) = (session, modules, link);
        // Hidden until the backquote: it covers much of the screen, and players never need it.
        Visible = false;
        Layer = ScreenLayers.Dev;
        var box = Style.Box(Style.Slate900 with { A = 0.9f }, Style.Slate700, 12);
        _panel.AddThemeStyleboxOverride("panel", box);
        (_panel.AnchorLeft, _panel.AnchorRight) = (1, 1);
        (_panel.OffsetLeft, _panel.OffsetRight, _panel.OffsetTop) = (-12 - 320, -12, 12);
        AddChild(_panel);

        var column = Style.Gap(new VBoxContainer(), 8);
        _panel.AddChild(column);
        var head = new HBoxContainer();
        var title = Text(Style.Slate400);
        title.Text = "dev · ` to hide";
        title.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        head.AddChild(title);
        head.AddChild(_fps);
        column.AddChild(head);
        _readings.AddThemeConstantOverride("v_separation", 4);
        column.AddChild(_readings);

        var buttons = Style.Gap(new HBoxContainer(), 8);
        buttons.AddChild(_speed = Style.Button("×4", ToggleSpeed));
        buttons.AddChild(_creative = Style.Button("creative", ToggleCreative));
        buttons.AddChild(_instant = Style.Button("instant", ToggleInstant));
        buttons.AddChild(Style.Button("meteor", Meteor));
        column.AddChild(buttons);
        // Surfaces (0076): a planet (0077) or the test strip opened, and this hero beamed between the last opened and the ship.
        var surfaces = Style.Gap(new HBoxContainer(), 8);
        surfaces.AddChild(Style.Button("open planet", () => Send(new OpenSurface(Content.PlanetTypes.First.Id))));
        surfaces.AddChild(Style.Button("open strip", () => Send(new OpenSurface(Content.Surfaces.TestStrip.Id))));
        surfaces.AddChild(Style.Button("to ship", () => Send(new Transfer(0))));
        surfaces.AddChild(Style.Button("down", ToLast));
        column.AddChild(surfaces);
        column.AddChild(_status);
        Say("");
        var items = Toggle("▸ items", Style.Slate400);
        items.Pressed += () =>
        {
            _picker.Visible = !_picker.Visible;
            items.Text = $"{(_picker.Visible ? "▾" : "▸")} items";
            if (_picker.Visible) _search.GrabFocus();
            else _search.ReleaseFocus();
        };
        column.AddChild(items);
        MakePicker();
        column.AddChild(_picker);
        // Shut at the start: open, the board takes a third of a 720-pixel screen.
        var sounds = Toggle("▸ sounds", Style.Slate400);
        sounds.Pressed += () =>
        {
            _sounds.Visible = !_sounds.Visible;
            sounds.Text = $"{(_sounds.Visible ? "▾" : "▸")} sounds";
        };
        column.AddChild(sounds);
        foreach (var cue in S.All) _sounds.AddChild(SoundButton(cue));
        column.AddChild(_sounds);
        var heading = Text(Style.Slate400);
        heading.Text = "modules";
        column.AddChild(heading);
        _slices.AddThemeConstantOverride("separation", 4);
        column.AddChild(_slices);
    }

    private static Label Text(Color colour, int size = 12) => new() { LabelSettings = Style.Text(Style.Mono, size, colour) };

    /// <summary>A line under the buttons saying how the last of them went; none when there is nothing to say.</summary>
    private void Say(string text)
    {
        _status.Text = text;
        _status.Visible = text != "";
    }

    private void ToggleSpeed()
    {
        if (_session() is not Session s) return;
        s.Speed = s.Speed == 1 ? 4 : 1;
        Read();
    }

    private void ToggleCreative()
    {
        if (_session() is not Session s) return;
        // Read now, not from the last reading: two quick clicks must switch it back.
        s.Client.Send(new SetCreative(!_modules().Items.Creative));
    }

    private void ToggleInstant()
    {
        if (_session() is not Session s) return;
        // Read now, as creative is: two quick clicks must switch it back.
        s.Client.Send(new SetInstant(!_modules().Building.Instant));
    }

    /// <summary>
    /// The item picker (creative mode): a search by name or id, how many a pick gives, and a row for every item, those
    /// the search names shown. A row clicked, or Enter for the first shown, puts that many into this hero's bag.
    /// </summary>
    private void MakePicker()
    {
        var line = Style.Gap(new HBoxContainer(), 4);
        _search.AddThemeFontOverride("font", Style.Mono);
        _search.AddThemeFontSizeOverride("font_size", 12);
        _search.AddThemeColorOverride("font_color", Style.Slate100);
        _search.AddThemeColorOverride("font_placeholder_color", Style.Slate500);
        _search.AddThemeStyleboxOverride("normal", Style.Box(Style.Slate950, Style.Slate700, 4));
        _search.AddThemeStyleboxOverride("focus", Style.Box(Style.Slate950, Amber600, 4));
        _search.TextChanged += Filter;
        _search.TextSubmitted += _ =>
        {
            if (ItemPicker.Matching(_search.Text).FirstOrDefault() is { } first) Pick(first.Id);
        };
        line.AddChild(_search);
        foreach (int? count in new int?[] { 1, 10, null })
        {
            var button = Style.Button(count?.ToString() ?? "stack", () => ShowCount(count), size: 11, padX: 6, padY: 2);
            _counts.Add((button, count));
            line.AddChild(button);
        }
        _picker.AddChild(line);
        _picker.AddChild(_pickerNote);
        var list = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        list.AddThemeConstantOverride("separation", 0);
        foreach (var item in ItemPicker.Matching(""))
        {
            var row = Toggle(ItemPicker.NameOf(item.Id), Style.Slate300);
            row.Icon = ItemIcons.Icon(item.Id);
            row.TooltipText = $"{item.Id} · stack {item.Stack}";
            row.TextureFilter = CanvasItem.TextureFilterEnum.Nearest;
            row.AddThemeConstantOverride("icon_max_width", 2 * ItemIcons.Pixels);
            row.Pressed += () => Pick(item.Id);
            _rows[item.Id] = row;
            list.AddChild(row);
        }
        var scroll = new ScrollContainer { CustomMinimumSize = new Vector2(0, 280), HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        scroll.AddChild(list);
        _picker.AddChild(scroll);
        ShowCount(null);
    }

    private void Filter(string query)
    {
        var shown = ItemPicker.Matching(query).Select(item => item.Id).ToHashSet();
        foreach (var (id, row) in _rows) row.Visible = shown.Contains(id);
    }

    private void ShowCount(int? count)
    {
        _count = count;
        foreach (var (button, each) in _counts)
        {
            bool on = each == count;
            Style.Paint(button, on ? Style.Amber700 : Style.Slate700, on ? Amber600 : Style.Slate600, 6, 2);
        }
    }

    /// <summary>Gives this hero <see cref="_count"/> of <paramref name="item"/>, a stack for none; says so when creative mode is off.</summary>
    private void Pick(string item)
    {
        if (_session() is not Session s) return;
        // Read now: creative switched on a moment ago may not be in the last reading.
        if (!_modules().Items.Creative)
        {
            _pickerNote.Text = "Creative mode is off: switch it on first.";
            return;
        }
        int count = _count ?? Content.Items.Get(item).Stack;
        s.Client.Send(new Spawn(item, count));
        _pickerNote.Text = $"+{count} {ItemPicker.NameOf(item)}";
    }

    private void Send(Command command)
    {
        if (_session() is Session s) s.Client.Send(command);
    }

    /// <summary>Beams this hero down to the surface opened last, a planet or a strip; says so when none is.</summary>
    private void ToLast()
    {
        if (_session() is not Session s) return;
        var strip = s.Client.World.Surfaces.Skip(1).LastOrDefault();
        if (strip == null)
        {
            Say("Nothing open: open a planet first.");
            return;
        }
        s.Client.Send(new Transfer(strip.Id));
        Say("");
    }

    /// <summary>
    /// Hits a random wall of the outer hull beside a room, where a meteor would, if any is left, and the wall cell behind it
    /// away from the room: a hull two cells thick (0104) is breached through.
    /// </summary>
    private void Meteor()
    {
        if (_session() is not Session s) return;
        var m = _modules();
        int width = m.Grid.Width, height = m.Grid.Height;
        bool Wall(int x, int y) => x >= 0 && y >= 0 && x < width && y < height && m.Grid.WallAt(y * width + x) != null;
        var hits = new List<(int Cell, int? Behind)>();
        foreach (var room in m.Rooms.All)
            foreach (int cell in room.Hull)
            {
                int x = cell % width, y = cell / width;
                if (!Wall(x, y)) continue;
                foreach (var (dx, dy) in ((int, int)[])[(0, -1), (-1, 0), (1, 0), (0, 1)])
                {
                    int rx = x + dx, ry = y + dy;
                    if (rx < 0 || ry < 0 || rx >= width || ry >= height || m.Rooms.RoomAt(ry * width + rx) != room.Id) continue;
                    hits.Add((cell, Wall(x - dx, y - dy) ? (y - dy) * width + x - dx : null));
                    break;
                }
            }
        if (hits.Count == 0)
        {
            Say("No hull wall left to hit.");
            return;
        }
        var (hit, behind) = hits[Random.Shared.Next(hits.Count)];
        s.Client.Send(new MeteorHit(hit));
        if (behind is int through) s.Client.Send(new MeteorHit(through));
        Say("");
    }

    public override void _UnhandledInput(InputEvent e)
    {
        if (e is not InputEventKey { Pressed: true, Echo: false, PhysicalKeycode: Key.Quoteleft }) return;
        Visible = !Visible;
        // A hidden search would keep the keys from the hero.
        if (!Visible) _search.ReleaseFocus();
        _sinceRead = RefreshMs;
        GetViewport().SetInputAsHandled();
    }

    public override void _Process(double delta)
    {
        if (!Visible) return;
        _sinceRead += delta * 1000;
        if (_sinceRead < RefreshMs) return;
        _sinceRead = 0;
        Read();
    }

    /// <summary>What the panel shows; the hash is the one the client last sent, never hashed again here.</summary>
    private void Read()
    {
        _fps.Text = $"{Engine.GetFramesPerSecond():0} fps";
        if (_session() is not Session s) return;
        var client = s.Client;
        var m = _modules();
        var lines = new List<(string, string)>
        {
            ("tick", client.World.Tick.ToString()),
            ("surface", $"{client.World.SurfaceOf(client.Id)} of {client.World.Surfaces.Count}"),
            ("hash", client.LastHash is ulong hash ? hash.ToString("x16") : "…"),
        };
        if (s.RoundTrip.Last is double last) lines.Add(("act", $"{last:0} ms · median {s.RoundTrip.Median ?? 0:0} ms"));
        if (_link() is { } link)
        {
            string held = s.Host == null ? $" · queued {client.Queued}" + (s.Reserve is double reserve ? $" · reserve {reserve:0} ms" : "") : "";
            string resynced = s.Desyncs.Count > 0 ? $" · resynced {s.Desyncs.Count}×" : "";
            lines.Add(("net", link + held + resynced));
        }
        ShowReadings(lines);
        _speed.Text = s.Speed == 1 ? "×4" : "×1";
        bool creative = m.Items.Creative;
        Style.Paint(_creative, creative ? Style.Amber700 : Style.Slate700, creative ? Amber600 : Style.Slate600);
        bool instant = m.Building.Instant;
        Style.Paint(_instant, instant ? Style.Amber700 : Style.Slate700, instant ? Amber600 : Style.Slate600);
        ShowSlices(client.World, client.World.SurfaceOf(client.Id), m);
    }

    private void ShowReadings(IReadOnlyList<(string Name, string Value)> lines)
    {
        while (_readings.GetChildCount() < lines.Count * 2)
        {
            var name = Text(Style.Slate300);
            name.CustomMinimumSize = new Vector2(64, 0);
            _readings.AddChild(name);
            _readings.AddChild(new Label { LabelSettings = Style.Text(Style.Mono, 12, Style.Slate300), AutowrapMode = TextServer.AutowrapMode.WordSmart, CustomMinimumSize = new Vector2(232, 0) });
        }
        for (int i = 0; i < _readings.GetChildCount(); i++)
        {
            var label = (Label)_readings.GetChild(i);
            label.Visible = i < lines.Count * 2;
            if (label.Visible) label.Text = i % 2 == 0 ? lines[i / 2].Name : lines[i / 2].Value;
        }
    }

    /// <summary>Every module by its id, with its saved state as JSON only for those whose entry is open.</summary>
    private void ShowSlices(World world, int surface, Registry m)
    {
        var ids = m.All.Select(module => module.Id).ToList();
        foreach (string id in ids.Where(id => !_entries.ContainsKey(id))) _entries[id] = Entry(id);
        // The world's globals are listed with the ship's modules only.
        foreach (var (id, entry) in _entries)
        {
            entry.Toggle.Visible = ids.Contains(id);
            if (!entry.Toggle.Visible) entry.Box.Visible = false;
        }
        // The slices of the surface this hero is on: another's are saved under the world's surfaces (0076).
        var root = _expanded.Count > 0 ? JsonNode.Parse(world.Save()) : null;
        var slices = surface == 0 ? root?["slices"] : root?["surfaces"]?[surface - 1]?["slices"];
        foreach (string id in ids)
        {
            var (toggle, box, json) = _entries[id];
            bool open = _expanded.Contains(id);
            toggle.Text = $"{(open ? "▾" : "▸")} {id}";
            box.Visible = open;
            if (!open) continue;
            // The writer breaks lines as the system does; a label would show Windows' as two.
            string text = slices?[id]?.ToJsonString(Indented).ReplaceLineEndings("\n") ?? "…";
            if (text.Length > MaxJson) text = $"{text[..MaxJson]}\n… {text.Length - MaxJson} more characters";
            json.Text = text;
            box.CustomMinimumSize = new Vector2(0, Math.Min(MaxJsonHeight, text.Count(c => c == '\n') * 14 + 18));
        }
    }

    /// <summary>
    /// A cue played on the UI bus, so floating's silence leaves it heard: a one-shot at each press, a loop
    /// on and off. Its player is made at the first press, so no stream loads before a sound is asked for.
    /// </summary>
    private Button SoundButton(S.Cue cue)
    {
        AudioStreamPlayer? player = null;
        Button button = null!;
        bool on = false;
        button = Style.Button(CueNames.Of(cue), () =>
        {
            if (player == null) AddChild(player = Cues.Player(cue, S.Ui));
            if (!cue.Loops)
            {
                player.Play();
                return;
            }
            on = !on;
            if (on) player.Play();
            else player.Stop();
            Style.Paint(button, on ? Style.Amber700 : Style.Slate700, on ? Amber600 : Style.Slate600, 6, 2);
        }, size: 11, padX: 6, padY: 2);
        return button;
    }

    /// <summary>A flat line that opens and closes what is under it.</summary>
    private static Button Toggle(string text, Color colour)
    {
        var toggle = new Button { Text = text, Flat = true, FocusMode = Control.FocusModeEnum.None, Alignment = HorizontalAlignment.Left };
        toggle.AddThemeFontOverride("font", Style.Mono);
        toggle.AddThemeFontSizeOverride("font_size", 12);
        foreach (string state in new[] { "font_color", "font_hover_color", "font_pressed_color" }) toggle.AddThemeColorOverride(state, colour);
        return toggle;
    }

    private (Button, ScrollContainer, Label) Entry(string id)
    {
        var toggle = Toggle("", Style.Slate300);
        toggle.Pressed += () =>
        {
            if (!_expanded.Remove(id)) _expanded.Add(id);
            Read();
        };
        var json = Text(Style.Slate400, 11);
        var box = new ScrollContainer { Visible = false };
        box.AddChild(json);
        _slices.AddChild(toggle);
        _slices.AddChild(box);
        return (toggle, box, json);
    }
}
