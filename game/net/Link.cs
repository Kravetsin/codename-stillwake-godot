using System;
using System.IO;
using Godot;
using Stillwake.Net;

namespace Stillwake.Game.Net;

/// <summary>
/// What carries a session between peers, ENet or the playtest relay: peers coming and going, each a
/// channel, delivered when the game calls <see cref="Poll"/> every frame on the main thread.
/// </summary>
public interface ILink<TIn, TOut>
{
    /// <summary>A peer at the other end connected: its id on this link, and the channel to it.</summary>
    event Action<long, IChannel<TIn, TOut>>? Connected;

    /// <summary>A peer at the other end went: its id on this link.</summary>
    event Action<long>? Disconnected;

    void Poll();

    void Close();
}

/// <summary>Reading messages off a link.</summary>
public static class Links
{
    /// <summary>
    /// The message in <paramref name="bytes"/>, or none, with a warning, for bytes no peer of this build writes: the other
    /// end runs another build, whose welcome cannot be read to say so.
    /// </summary>
    public static T? Read<T>(byte[]? bytes)
    {
        try
        {
            if (bytes != null) return Protocol.Read<T>(bytes);
        }
        catch (InvalidDataException)
        {
        }
        GD.PushWarning("A message no peer of this build writes came in: does the other end run another build?");
        return default;
    }
}
