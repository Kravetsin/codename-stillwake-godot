using System;
using System.Collections.Generic;
using System.IO;
using Stillwake.Net;

namespace Stillwake.Game.Net;

/// <summary>
/// Messages read off a link (2026-10-26-compact-wire). Bytes no peer of this build writes are left out with a warning, not
/// thrown: the other end runs another build, whose welcome cannot be read to say so. Plain .NET, tested without Godot.
/// </summary>
public static class Received
{
    private const string Garbled = "A message no peer of this build writes came in: does the other end run another build?";

    /// <summary>The message in <paramref name="bytes"/>, or none, warned of.</summary>
    public static T? Message<T>(byte[]? bytes, Action<string> warn)
    {
        try
        {
            if (bytes != null) return Protocol.Read<T>(bytes);
        }
        catch (InvalidDataException)
        {
        }
        warn(Garbled);
        return default;
    }

    /// <summary>The messages a relay frame carried (<see cref="RelayFrames.Batch"/>), in order, each one garbled left out.</summary>
    public static List<T> Batch<T>(string text, Action<string> warn)
    {
        var messages = new List<T>();
        if (RelayFrames.Unbatch(text) is not { } batch)
        {
            warn(Garbled);
            return messages;
        }
        foreach (var bytes in batch)
            if (Message<T>(bytes, warn) is { } message) messages.Add(message);
        return messages;
    }
}

/// <summary>A relay channel's messages waiting for the next frame: a game frame's, sent in one (<see cref="RelayFrames.Batch"/>).</summary>
public sealed class RelayOutbox
{
    private readonly List<byte[]> _messages = [];

    public void Add(byte[] message) => _messages.Add(message);

    /// <summary>The batch of everything added since the last call; none when nothing was.</summary>
    public string? Take()
    {
        if (_messages.Count == 0) return null;
        string batch = RelayFrames.Batch(_messages);
        _messages.Clear();
        return batch;
    }
}
