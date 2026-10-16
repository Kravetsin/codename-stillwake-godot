using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Stillwake.Game.Platform;

/// <summary>
/// Who is who in a world (0032): each player's key and the peer id — the seat — the world knows them
/// by, the same every time they come aboard, so their hero comes back with what they carried. Kept by
/// the host beside the world's save as soon as a seat is given, never in the sim: the host picks peer
/// ids outside it. Seats are counted up and never handed out twice. A world saved before rosters has
/// none: its host takes seat 0, the host's then as always, and new seats start past
/// <see cref="LegacyFirst"/>, above every guest the world could hold from then. Plain C#, tested without Godot.
/// </summary>
public sealed class Roster
{
    /// <summary>Where seats start in a world from before rosters: past every id its guests were given.</summary>
    public const int LegacyFirst = 64;

    private readonly Dictionary<string, int> _seats;
    private int _next;

    private Roster(Dictionary<string, int> seats, int next) => (_seats, _next) = (seats, next);

    /// <summary>Every key and its seat.</summary>
    public IReadOnlyDictionary<string, int> Seats => _seats;

    /// <summary>A new world's roster, nobody on it yet: its host takes the first seat.</summary>
    public static Roster Empty() => new(new Dictionary<string, int>(StringComparer.Ordinal), 0);

    /// <summary>The roster saved as <paramref name="json"/>, or a world's from before rosters, its host <paramref name="hostKey"/>.</summary>
    public static Roster Read(string? json, string hostKey)
    {
        if (json == null) return new Roster(new Dictionary<string, int>(StringComparer.Ordinal) { [hostKey] = 0 }, LegacyFirst);
        var saved = JsonSerializer.Deserialize<Saved>(json) ?? new Saved(0, []);
        return new Roster(new Dictionary<string, int>(saved.Seats, StringComparer.Ordinal), saved.Next);
    }

    public string ToJson() => JsonSerializer.Serialize(new Saved(_next, _seats.OrderBy(p => p.Value).ToDictionary(p => p.Key, p => p.Value)));

    /// <summary>The seat of <paramref name="key"/>: its own, or the next never given, kept from now on.</summary>
    public int SeatOf(string key)
    {
        if (_seats.TryGetValue(key, out int seat)) return seat;
        _seats[key] = seat = Math.Max(_next, _seats.Values.DefaultIfEmpty(-1).Max() + 1);
        _next = seat + 1;
        return seat;
    }

    private sealed record Saved(int Next, Dictionary<string, int> Seats);
}
