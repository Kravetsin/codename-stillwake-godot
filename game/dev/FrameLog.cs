using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Godot;

namespace Stillwake.Game.Dev;

/// <summary>
/// Every frame's cost, for development (<c>-- --frames</c>): vsync off, the wall time from one frame's
/// processing to the next (Godot's delta is smoothed) beside what <c>Main</c> spent where in it
/// (<see cref="Mark"/>) and the views timed apart (<see cref="Begin"/>, <see cref="End"/>), the renderer's CPU and GPU time for the
/// frame drawn in between, the collections the GC ran and its pauses, and the bytes the main thread allocated; on <see cref="Report"/>,
/// a histogram, percentiles, the collections a minute and every frame over a 240 Hz budget with its causes, on stdout. It allocates nothing a frame,
/// so what it counts is the game's.
/// </summary>
public partial class FrameLog : Node
{
    /// <summary>Frames this long or longer miss a 240 Hz screen's refresh.</summary>
    private const double Budget = 1000.0 / 240;
    /// <summary>The first frames load and compile shaders: not play.</summary>
    private const int Warm = 120;
    /// <summary>The frames kept: a few minutes uncapped.</summary>
    private const int Capacity = 400_000;
    private static readonly double[] Buckets = [Budget, 5, 6, 8, 12, 1000.0 / 60, 33, double.MaxValue];

    /// <summary>What <see cref="Mark"/> may be told: <c>Main</c>'s sections, in its order.</summary>
    public static readonly string[] Sections = ["net", "sim", "light", "hero", "grid", "items", "debris", "hands", "reveal", "lightmap", "flora"];

    /// <summary>The log of this run, if frames are logged: views timed apart from <c>Main</c> tell it through <see cref="End"/>.</summary>
    public static FrameLog? Active { get; private set; }

    /// <summary>A view's work starts: a timestamp for <see cref="End"/>, or 0 when frames are not logged.</summary>
    public static long Begin() => Active == null ? 0 : Stopwatch.GetTimestamp();

    /// <summary>The time since <paramref name="started"/> goes to <paramref name="section"/>, one of <see cref="Sections"/>.</summary>
    public static void End(string section, long started)
    {
        if (Active is not { } log || started == 0) return;
        int k = Array.IndexOf(Sections, section);
        if (log._n < Capacity) log._sections[log._n * Sections.Length + k] += Stopwatch.GetElapsedTime(started).TotalMilliseconds;
    }

    private readonly double[] _ms = new double[Capacity], _render = new double[Capacity], _gpu = new double[Capacity], _paused = new double[Capacity];
    private TimeSpan _pausedSoFar;
    private readonly double[] _sections = new double[Capacity * Sections.Length];
    /// <summary>The bytes each section allocated, summed over every frame.</summary>
    private readonly long[] _sectionBytes = new long[Sections.Length];
    private long _markBytes;
    private readonly int[] _gen0 = new int[Capacity], _gen1 = new int[Capacity], _gen2 = new int[Capacity];
    private readonly long[] _allocated = new long[Capacity];
    private int _n;
    private long _mark, _bytes, _last;
    private int _g0, _g1, _g2;
    private long _totalAtWarm;
    private double _secondsAtWarm;

    /// <summary>Whether frames wait for the screen, as a player's do: then the report counts the seconds that fell short of its rate.</summary>
    private readonly bool _vsync;

    /// <param name="vsync">Keep vsync on (<c>--frames=vsync</c>), to count the frames a screen misses rather than what each costs.</param>
    public FrameLog(bool vsync) => _vsync = vsync;

    public override void _Ready()
    {
        Active = this;
        if (!_vsync)
        {
            DisplayServer.WindowSetVsyncMode(DisplayServer.VSyncMode.Disabled);
            Engine.MaxFps = 0;
        }
        RenderingServer.ViewportSetMeasureRenderTime(GetViewport().GetViewportRid(), true);
        (_g0, _g1, _g2, _bytes) = (GC.CollectionCount(0), GC.CollectionCount(1), GC.CollectionCount(2), GC.GetAllocatedBytesForCurrentThread());
    }

    /// <summary>Starts timing a frame's sections: <c>Main</c> calls it first thing in its <c>_Process</c>.</summary>
    public void Start() => (_mark, _markBytes) = (Stopwatch.GetTimestamp(), GC.GetAllocatedBytesForCurrentThread());

    /// <summary>The time since the last mark goes to <paramref name="section"/>, one of <see cref="Sections"/>.</summary>
    public void Mark(string section)
    {
        long now = Stopwatch.GetTimestamp(), bytes = GC.GetAllocatedBytesForCurrentThread();
        int k = Array.IndexOf(Sections, section);
        if (_n < Capacity) _sections[_n * Sections.Length + k] += Stopwatch.GetElapsedTime(_mark, now).TotalMilliseconds;
        if (_n >= Warm) _sectionBytes[k] += bytes - _markBytes;
        (_mark, _markBytes) = (now, bytes);
    }

    public override void _Process(double delta)
    {
        if (_n >= Capacity) return;
        // A frame here is from the last call to this one: the frame drawn in between, then Main's sections.
        long now = Stopwatch.GetTimestamp();
        var vp = GetViewport().GetViewportRid();
        int g0 = GC.CollectionCount(0), g1 = GC.CollectionCount(1), g2 = GC.CollectionCount(2);
        long bytes = GC.GetAllocatedBytesForCurrentThread();
        (_gen0[_n], _gen1[_n], _gen2[_n], _allocated[_n]) = (g0 - _g0, g1 - _g1, g2 - _g2, bytes - _bytes);
        _ms[_n] = _last == 0 ? 0 : Stopwatch.GetElapsedTime(_last, now).TotalMilliseconds;
        _render[_n] = RenderingServer.ViewportGetMeasuredRenderTimeCpu(vp) + RenderingServer.GetFrameSetupTimeCpu();
        // The GC's own pauses: a background collection's stops are counted here, not as a new collection.
        var paused = GC.GetTotalPauseDuration();
        (_paused[_n], _pausedSoFar) = ((paused - _pausedSoFar).TotalMilliseconds, paused);
        _gpu[_n] = RenderingServer.ViewportGetMeasuredRenderTimeGpu(vp);
        (_g0, _g1, _g2, _bytes, _last) = (g0, g1, g2, bytes, now);
        if (_n == Warm) (_totalAtWarm, _secondsAtWarm) = (GC.GetTotalAllocatedBytes(), Time.GetTicksMsec() / 1000.0);
        _n++;
    }

    /// <summary>Prints what the frames cost since the warm-up.</summary>
    public void Report()
    {
        var frames = Enumerable.Range(Warm, Math.Max(0, _n - Warm)).Where(i => _ms[i] > 0).ToList();
        if (frames.Count == 0) return;
        var ms = frames.Select(i => _ms[i]).OrderBy(m => m).ToList();
        double P(double q) => ms[Math.Min(ms.Count - 1, (int)Math.Ceiling(ms.Count * q) - 1)];
        double seconds = ms.Sum() / 1000, minutes = seconds / 60;
        var o = new StringBuilder();
        if (_vsync)
        {
            // Frames counted by wall-clock second, as the fps counter reads them.
            var perSecond = new System.Collections.Generic.List<int>();
            double clock = 0;
            foreach (double m in frames.Select(i => _ms[i]))
            {
                clock += m;
                int second = (int)(clock / 1000);
                while (perSecond.Count <= second) perSecond.Add(0);
                perSecond[second]++;
            }
            if (perSecond.Count > 1) perSecond.RemoveAt(perSecond.Count - 1);
            float rate = DisplayServer.ScreenGetRefreshRate();
            o.AppendLine($"FRAMES vsync at {rate:F0} Hz: seconds {perSecond.Count}, below {Math.Round(rate) - 1:F0} fps {perSecond.Count(n => n < Math.Round(rate) - 1)}, "
                + $"lowest {perSecond.Min()} fps, frames longer than two refreshes {ms.Count(m => m > 2000 / rate)}");
        }
        o.AppendLine($"FRAMES n={frames.Count} over {seconds:F1}s avg={ms.Average():F3}ms ({frames.Count / seconds:F0} fps) "
            + $"p50={P(0.5):F3} p95={P(0.95):F3} p99={P(0.99):F3} p99.9={P(0.999):F3} max={ms[^1]:F3}ms");
        double from = 0;
        foreach (double to in Buckets)
        {
            int n = ms.Count(m => m >= from && m < to);
            o.AppendLine($"FRAMES  {from,6:F2}..{(to == double.MaxValue ? "inf" : to.ToString("F2")),6} ms {n,8} {100.0 * n / frames.Count,8:F3}%");
            from = to;
        }
        o.AppendLine($"FRAMES gc a minute: gen0={frames.Sum(i => _gen0[i]) / minutes:F1} gen1={frames.Sum(i => _gen1[i]) / minutes:F1} "
            + $"gen2={frames.Sum(i => _gen2[i]) / minutes:F1}; main thread allocates {frames.Average(i => _allocated[i]) / 1024:F1} KB a frame, "
            + $"{frames.Sum(i => _allocated[i]) / seconds / 1024 / 1024:F2} MB a second, every thread {(GC.GetTotalAllocatedBytes() - _totalAtWarm) / (Time.GetTicksMsec() / 1000.0 - _secondsAtWarm) / 1024 / 1024:F2} MB a second; frames over {Budget:F2}ms: {ms.Count(m => m >= Budget)}");
        o.AppendLine("FRAMES sections avg/max ms, KB allocated a frame: " + string.Join(" ", Sections.Select((s, k) =>
            $"{s}={frames.Average(i => _sections[i * Sections.Length + k]):F3}/{frames.Max(i => _sections[i * Sections.Length + k]):F2}/{_sectionBytes[k] / 1024.0 / frames.Count:F2}KB")));
        // Each frame over the budget put down to what took most of it: a section, the renderer, or the rest (Godot itself, other nodes).
        var causes = frames.Where(i => _ms[i] >= Budget).Select(Cause).GroupBy(c => c).OrderByDescending(g => g.Count());
        o.AppendLine("FRAMES over budget by cause: " + string.Join(" ", causes.Select(g => $"{g.Key}={g.Count()}")));
        o.AppendLine($"FRAMES gc paused {GC.GetTotalPauseDuration().TotalMilliseconds:F0} ms over {GC.CollectionCount(0)} collections since the start");
        var e = GC.GetGCMemoryInfo(GCKind.Ephemeral);
        o.AppendLine($"FRAMES last ephemeral gc: gen{e.Generation} pause={e.PauseDurations[0].TotalMilliseconds:F2}ms promoted={e.PromotedBytes / 1024}KB heap={e.HeapSizeBytes / 1024 / 1024}MB pinned={e.PinnedObjectsCount} concurrent={e.Concurrent} gen0size={e.GenerationInfo[0].SizeBeforeBytes / 1024}KB gen1={e.GenerationInfo[1].SizeBeforeBytes / 1024}KB gen2={e.GenerationInfo[2].SizeAfterBytes / 1024 / 1024}MB loh={e.GenerationInfo[3].SizeAfterBytes / 1024 / 1024}MB server={System.Runtime.GCSettings.IsServerGC} mode={System.Runtime.GCSettings.LatencyMode}");
        foreach (int i in frames.Where(i => _ms[i] >= Budget).Take(300))
            o.AppendLine($"FRAMES over #{i} {_ms[i],7:F2}ms render-cpu={_render[i]:F2} rest={Rest(i):F2} gc-paused={_paused[i]:F2} gpu={_gpu[i]:F2} gc={_gen0[i]}/{_gen1[i]}/{_gen2[i]} alloc={_allocated[i] / 1024}KB "
                + string.Join(" ", Sections.Select((s, k) => (s, v: _sections[i * Sections.Length + k])).Where(p => p.v >= 0.05)
                    .OrderByDescending(p => p.v).Select(p => $"{p.s}={p.v:F2}")));
        GD.Print(o.ToString().TrimEnd());
    }

    /// <summary>What of frame <paramref name="i"/> no section and not the renderer took: Godot's own work and the views not timed.</summary>
    private double Rest(int i)
    {
        double timed = _render[i];
        for (int k = 0; k < Sections.Length; k++) timed += _sections[i * Sections.Length + k];
        return _ms[i] - timed;
    }

    /// <summary>What took most of frame <paramref name="i"/>: a section, <c>render</c>, <c>rest</c>, or <c>gc</c> when a collection ran in it.</summary>
    private string Cause(int i)
    {
        if (_gen0[i] + _gen1[i] + _gen2[i] > 0 || _paused[i] >= 1) return "gc";
        var (cause, most) = ("render", _render[i]);
        for (int k = 0; k < Sections.Length; k++)
            if (_sections[i * Sections.Length + k] > most) (cause, most) = (Sections[k], _sections[i * Sections.Length + k]);
        return Rest(i) > most ? "rest" : cause;
    }
}
