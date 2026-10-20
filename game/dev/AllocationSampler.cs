using System;
using System.Collections.Concurrent;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;

namespace Stillwake.Game.Dev;

/// <summary>
/// What the game allocates, by type, for development (<c>-- --allocs</c>): the runtime reports about every 100 KB allocated, with
/// the type of the object that crossed it; counted by type and heap (small or large objects), the most printed by <see cref="Report"/>.
/// </summary>
public sealed class AllocationSampler : EventListener
{
    private readonly ConcurrentDictionary<(string Type, bool Large), long> _bytes = new();

    /// <summary>Whether allocations are counted: not while the world loads, only in play.</summary>
    public volatile bool Counting;

    protected override void OnEventSourceCreated(EventSource source)
    {
        // The GC keyword at verbose level brings the allocation ticks.
        if (source.Name == "Microsoft-Windows-DotNETRuntime") EnableEvents(source, EventLevel.Verbose, (EventKeywords)0x1);
    }

    protected override void OnEventWritten(EventWrittenEventArgs e)
    {
        if (!Counting || e.EventName == null || !e.EventName.StartsWith("GCAllocationTick", StringComparison.Ordinal) || e.Payload == null) return;
        int type = e.PayloadNames!.IndexOf("TypeName"), kind = e.PayloadNames.IndexOf("AllocationKind"), amount = e.PayloadNames.IndexOf("AllocationAmount64");
        string name = type >= 0 ? e.Payload[type]?.ToString() ?? "?" : "?";
        bool large = kind >= 0 && Convert.ToInt32(e.Payload[kind]) == 1;
        long bytes = amount >= 0 ? Convert.ToInt64(e.Payload[amount]) : 100_000;
        _bytes.AddOrUpdate((name, large), bytes, (_, b) => b + bytes);
    }

    /// <summary>The types that took the most, in MB, small and large objects apart.</summary>
    public string Report()
    {
        var o = new StringBuilder();
        foreach (var ((type, large), bytes) in _bytes.OrderByDescending(p => p.Value).Take(60))
            o.AppendLine($"ALLOCS {(large ? "large" : "small")} {bytes / 1024.0 / 1024:F1}MB {type}");
        return o.ToString().TrimEnd();
    }
}
