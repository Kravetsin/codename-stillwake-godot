using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json.Nodes;
using Godot;
using Stillwake.Sim;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Dev;

/// <summary>
/// <c>-- --testbed --open=charger --charger=arc|plain [--shots DIR]</c>: the testbed's charging station filling an empty
/// accumulator, the arc module fitted into it or not (0100), for recording the difference. Over the screen, the world's seconds
/// since the start, the accumulator's charge, what the station draws and its share, and when it came full; with
/// <c>--shots</c> the screen saved to DIR at a few moments of the run, then quit.
/// </summary>
public partial class ChargerArcDemo : Control
{
    /// <summary>The world's seconds the shots are taken at: the start, the arc's full at 18 s, and the plain station's at 90 s.</summary>
    private static readonly double[] Arc = [2, 9, 19], Plain = [2, 19, 45, 91];

    /// <summary>The testbed's world with an empty accumulator in the charging station and, with <paramref name="arc"/>, the arc module fitted into it.</summary>
    public static World Seed(World world, Func<IReadOnlyList<Module>> modules, bool arc)
    {
        var root = JsonNode.Parse(world.Save())!;
        var items = root["slices"]!["items"]!;
        int charger = (int)root["slices"]!["grid"]!["machines"]!.AsArray().First(m => (string?)m!["kind"] == "charger")!["id"]!;
        items["crates"]!.AsArray().First(c => (int)c!["id"]! == charger)!["slots"] =
            new JsonArray(new JsonObject { ["item"] = "accumulator", ["count"] = 1, ["charge"] = 0 });
        if (arc) items["fitted"] = new JsonArray(new JsonObject { ["machine"] = charger, ["module"] = Spec.Modules.Arc.Item });
        return World.Load(root.ToJsonString(), modules());
    }

    private readonly Func<Registry> _m;
    private readonly Func<int?> _tick;
    private readonly bool _arc;
    private readonly string? _shots;
    private readonly Queue<double> _at;
    private int? _start;
    private double? _full;

    public ChargerArcDemo(Func<Registry> m, Func<int?> tick, bool arc, string? shots)
    {
        (_m, _tick, _arc, _shots) = (m, tick, arc, shots);
        _at = new Queue<double>(arc ? Arc : Plain);
        MouseFilter = MouseFilterEnum.Ignore;
        SetAnchorsPreset(LayoutPreset.FullRect);
    }

    private double Seconds => _tick() is int tick && _start is int start ? (double)(tick - start) / World.TicksPerSecond : 0;

    public override void _Process(double delta)
    {
        if (_tick() is not int tick) return;
        _start ??= tick;
        var m = _m();
        int charger = m.Grid.MachinesOf("charger")[0].Id;
        if (_full == null && m.Items.Crate(charger)[0] is { Charge: >= Spec.Charge.Accumulator }) _full = Seconds;
        QueueRedraw();
        if (_shots == null || _at.Count == 0 || Seconds < _at.Peek()) return;
        string path = $"{_shots}/{(_arc ? "arc" : "plain")}-{_at.Dequeue().ToString("00", CultureInfo.InvariantCulture)}s.png";
        RenderingServer.FramePostDraw += Snap;
        void Snap()
        {
            RenderingServer.FramePostDraw -= Snap;
            GetViewport().GetTexture().GetImage().SavePng(path);
            GD.Print($"charger-arc: saved {path}");
            if (_at.Count == 0) GetTree().Quit();
        }
    }

    public override void _Draw()
    {
        var m = _m();
        int charger = m.Grid.MachinesOf("charger")[0].Id;
        var held = m.Items.Crate(charger)[0];
        int percent = held == null ? 0 : (int)((long)held.Charge * 100 / Spec.Charge.Accumulator);
        var lines = new List<string>
        {
            _arc ? "CHARGER + ARC MODULE" : "CHARGER, NO MODULE",
            $"t = {Seconds.ToString("0.0", CultureInfo.InvariantCulture)} s",
            $"accumulator {percent}%",
            $"draws {m.Power.Draw(charger)}, share {m.Power.Satisfaction(charger) / 10}%",
            _full is double full ? $"full at {full.ToString("0.0", CultureInfo.InvariantCulture)} s" : "charging...",
        };
        var font = ThemeDB.FallbackFont;
        DrawRect(new Rect2(880, 420, 360, 24 * lines.Count + 16), new Color(0, 0, 0, 0.7f));
        for (int i = 0; i < lines.Count; i++)
            DrawString(font, new Vector2(892, 448 + 24 * i), lines[i], fontSize: 20, modulate: i == 0 && _arc ? new Color("d9c8ff") : Colors.White);
    }
}
