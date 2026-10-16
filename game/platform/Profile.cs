using System;
using System.Linq;
using Godot;

namespace Stillwake.Game.Platform;

/// <summary>
/// Who plays on this machine (0032): a player key made on the first run and kept in Godot's user folder,
/// so an update, a fresh download or a reinstall keep it; another PC makes another. A world seats its
/// players by it. Steam's id takes its place once there is Steam.
/// </summary>
public static class Profile
{
    private const string File = "user://profile.cfg";

    /// <summary>
    /// This player's key: <c>--player=name</c> if given; a window joining over ENet in development, a
    /// key of its own, so two windows on one PC are two players; else the one kept, made and kept on the
    /// first run. A profile that cannot be kept gives a key for this run alone.
    /// </summary>
    public static string Key(string[] args)
    {
        if (args.FirstOrDefault(a => a.StartsWith("--player="))?[9..] is { Length: > 0 } given) return given;
        bool devJoin = args.Any(a => a == "--join" || a.StartsWith("--join=")) && !args.Any(a => a.StartsWith("--relay"));
        if (devJoin) return $"dev-guest-{OS.GetProcessId()}";
        var kept = new ConfigFile();
        kept.Load(File);
        if (kept.GetValue("player", "key", "").AsString() is { Length: > 0 } key) return key;
        key = Guid.NewGuid().ToString("N");
        kept.SetValue("player", "key", key);
        if (kept.Save(File) != Error.Ok) GD.PrintErr($"[profile] cannot keep the player's key in {File}: this run plays as a newcomer");
        return key;
    }
}
