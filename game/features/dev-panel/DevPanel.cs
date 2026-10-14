using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Net;
using Stillwake.Sim;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Hazards;
using Stillwake.Sim.Items;

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
    private readonly Button _speed, _creative, _light;
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
        buttons.AddChild(Style.Button("meteor", Meteor));
        buttons.AddChild(_light = Style.Button(LightName(), ToggleLight));
        column.AddChild(buttons);
        column.AddChild(_status);
        Say("");
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

    /// <summary>Hits a random wall of the outer hull, where a meteor would, if any is left.</summary>
    private void Meteor()
    {
        if (_session() is not Session s) return;
        var m = _modules();
        var walls = m.Rooms.All.SelectMany(r => r.Hull).Where(cell => m.Grid.StructureAt(cell) == Structure.Wall).ToList();
        if (walls.Count == 0)
        {
            Say("No hull wall left to hit.");
            return;
        }
        s.Client.Send(new MeteorHit(walls[Random.Shared.Next(walls.Count)]));
        Say("");
    }

    /// <summary>Lights by cells from emitters (0026) or by Godot's lights, the other of the two from now on: F9 too.</summary>
    private void ToggleLight()
    {
        Lit.UseCells(!Lit.Cells);
        _light.Text = LightName();
    }

    private static string LightName() => Lit.Cells ? "light: cells" : "light: godot";

    public override void _UnhandledInput(InputEvent e)
    {
        if (e is InputEventKey { Pressed: true, Echo: false, PhysicalKeycode: Key.F9 })
        {
            ToggleLight();
            GetViewport().SetInputAsHandled();
            return;
        }
        if (e is not InputEventKey { Pressed: true, Echo: false, PhysicalKeycode: Key.Quoteleft }) return;
        Visible = !Visible;
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
        ShowSlices(client.World, m);
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
    private void ShowSlices(World world, Registry m)
    {
        var ids = m.All.Select(module => module.Id).ToList();
        foreach (string id in ids.Where(id => !_entries.ContainsKey(id))) _entries[id] = Entry(id);
        var slices = _expanded.Count > 0 ? JsonNode.Parse(world.Save())!["slices"] : null;
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

    private (Button, ScrollContainer, Label) Entry(string id)
    {
        var toggle = new Button { Flat = true, FocusMode = Control.FocusModeEnum.None, Alignment = HorizontalAlignment.Left };
        toggle.AddThemeFontOverride("font", Style.Mono);
        toggle.AddThemeFontSizeOverride("font_size", 12);
        foreach (string state in new[] { "font_color", "font_hover_color", "font_pressed_color" }) toggle.AddThemeColorOverride(state, Style.Slate300);
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
