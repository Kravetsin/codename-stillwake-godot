using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Stillwake.Game.Dev;

/// <summary>
/// What frames cost, for development: with vsync off, two seconds to warm up, then five measured, one
/// line on stdout (fps, frame times, the renderer's CPU and GPU time, draw calls, how long the game took
/// from its start to its first frame), and the game quits.
/// </summary>
public partial class PerfReadout : Node
{
    private const double Warm = 2, Span = 5;
    private readonly Func<string> _extra;
    private readonly List<double> _frames = new();
    private double _clock, _cpu, _gpu, _draws;
    private ulong _loadMs;

    public PerfReadout(Func<string> extra) => _extra = extra;

    public override void _Ready()
    {
        DisplayServer.WindowSetVsyncMode(DisplayServer.VSyncMode.Disabled);
        Engine.MaxFps = 0;
        RenderingServer.ViewportSetMeasureRenderTime(GetViewport().GetViewportRid(), true);
    }

    public override void _Process(double delta)
    {
        if (_loadMs == 0) _loadMs = Time.GetTicksMsec();
        _clock += delta;
        if (_clock < Warm) return;
        if (_clock < Warm + Span)
        {
            var vp = GetViewport().GetViewportRid();
            _frames.Add(delta * 1000);
            _cpu += RenderingServer.ViewportGetMeasuredRenderTimeCpu(vp) + RenderingServer.GetFrameSetupTimeCpu();
            _gpu += RenderingServer.ViewportGetMeasuredRenderTimeGpu(vp);
            _draws += Performance.GetMonitor(Performance.Monitor.RenderTotalDrawCallsInFrame);
            return;
        }
        int n = _frames.Count;
        var sorted = _frames.OrderBy(f => f).ToList();
        double avg = _frames.Average(), p99 = sorted[Math.Max(0, (int)Math.Ceiling(n * 0.99) - 1)];
        var camera = GetViewport().GetCamera2D();
        GD.Print($"PERF zoom={camera?.Zoom.X:F2} fps={1000 / avg:F0} avg={avg:F2}ms p99={p99:F2}ms render-cpu={_cpu / n:F2}ms "
            + $"gpu={_gpu / n:F2}ms draws={_draws / n:F0} load={_loadMs / 1000.0:F1}s {_extra()} adapter={RenderingServer.GetVideoAdapterName().Replace(' ', '_')}");
        GetTree().Quit();
    }
}
