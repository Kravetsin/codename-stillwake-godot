using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Godot;

namespace Stillwake.Game.Dev;

/// <summary>
/// What each node's own processing costs a frame, for development (<c>-- --ablate</c>, <c>=Name</c> for the children of the first node of that name or type):
/// vsync off, after a warm-up each of the parent's children in turn has its processing switched off for a window of frames, between
/// windows with every node on; a line on stdout per node, the frame time saved, then the game quits. What a node draws stays drawn,
/// so the saving is its <c>_Process</c> and <c>_PhysicsProcess</c> and what they make the renderer redo.
/// </summary>
public partial class ProcessAblation : Node
{
    private const int Warm = 600, Window = 120;
    private readonly string? _under;
    private List<Node> _nodes = [];
    private int _frame, _at = -1;
    private long _last;
    private readonly List<(string Name, double Off, double On)> _rows = [];
    private double _sum;
    private int _count;
    private double _baseline;

    /// <param name="under">The name of the child whose children are switched off; null for the parent's own children.</param>
    public ProcessAblation(string? under) => _under = under;

    public override void _Ready()
    {
        DisplayServer.WindowSetVsyncMode(DisplayServer.VSyncMode.Disabled);
        Engine.MaxFps = 0;
        ProcessMode = ProcessModeEnum.Always;
    }

    /// <summary>The first node under <paramref name="from"/> named <paramref name="name"/> or of that type, depth first: most views go unnamed.</summary>
    private static Node? Find(Node from, string name)
    {
        foreach (var child in from.GetChildren())
        {
            if (child.Name == name || child.GetType().Name == name) return child;
            if (Find(child, name) is { } found) return found;
        }
        return null;
    }

    public override void _Process(double delta)
    {
        long now = Stopwatch.GetTimestamp();
        double ms = _last == 0 ? 0 : Stopwatch.GetElapsedTime(_last, now).TotalMilliseconds;
        _last = now;
        if (++_frame < Warm) return;
        if (_frame == Warm)
        {
            var root = _under == null ? GetParent() : Find(GetParent(), _under) ?? GetParent();
            _nodes = [.. root.GetChildren().Where(n => n != this)];
            return;
        }
        int slot = (_frame - Warm - 1) / Window, inSlot = (_frame - Warm - 1) % Window;
        // Even slots measure with everything on, odd ones with node slot / 2 off; the first frames of a slot settle.
        if (inSlot >= Window / 4)
        {
            _sum += ms;
            _count++;
        }
        if (inSlot != Window - 1) return;
        double avg = _sum / _count;
        (_sum, _count) = (0, 0);
        if (slot % 2 == 0) _baseline = avg;
        else
        {
            var node = _nodes[slot / 2];
            node.ProcessMode = ProcessModeEnum.Inherit;
            _rows.Add(($"{node.Name} ({node.GetType().Name})", avg, _baseline));
        }
        int next = slot + 1;
        if (next / 2 >= _nodes.Count)
        {
            foreach (var (name, off, on) in _rows.OrderByDescending(r => r.On - r.Off))
                GD.Print($"ABLATE {on - off,7:F3}ms saved  off={off:F3} on={on:F3}  {name}");
            GetTree().Quit();
            return;
        }
        if (next % 2 == 1) _nodes[next / 2].ProcessMode = ProcessModeEnum.Disabled;
    }
}
