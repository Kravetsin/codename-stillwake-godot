using System;
using System.Collections.Generic;
using Godot;

namespace Stillwake.Game.Shared;

/// <summary>
/// The one place an action (<see cref="Bind"/>) becomes a Godot key (2026-11-04-key-binds): a press heard, a key held, a key's
/// name as the current layout prints it. Every feature asks here instead of naming <c>Key.E</c>.
/// </summary>
public static class BindKeys
{
    private static readonly Dictionary<string, Key> Parsed = [];
    private static readonly Dictionary<string, string> Names = [];

    /// <summary>The physical key a Godot key name means; <see cref="Key.None"/> for a name Godot does not know.</summary>
    public static Key KeyNamed(string name)
    {
        if (Parsed.TryGetValue(name, out var key)) return key;
        return Parsed[name] = Enum.TryParse<Key>(name, out var parsed) ? parsed : Key.None;
    }

    /// <summary>The physical key <paramref name="bind"/> is on now.</summary>
    public static Key KeyOf(Bind bind) => KeyNamed(Binds.Current.KeyOf(bind));

    /// <summary>Whether <paramref name="key"/> is a press of <paramref name="bind"/>'s key.</summary>
    public static bool Is(this InputEventKey key, Bind bind) => key.PhysicalKeycode == KeyOf(bind);

    /// <summary>Whether <paramref name="bind"/>'s key is held down now.</summary>
    public static bool Held(Bind bind) => Input.IsPhysicalKeyPressed(KeyOf(bind));

    /// <summary>
    /// A key's name as this keyboard prints it: the current layout's letter there when it is Latin, else the QWERTY name. Asked
    /// every frame by tips, so kept, and forgotten on <see cref="Relearn"/>: a layout switched mid-run shows after the next rebind.
    /// </summary>
    public static string Shown(string name) => Names.TryGetValue(name, out var shown) ? shown : Names[name] = Read(name);

    /// <summary>Forgets the names kept, so the next ones are read off the layout in use and the language spoken.</summary>
    public static void Relearn() => Names.Clear();

    private static string Read(string name)
    {
        var physical = KeyNamed(name);
        if (physical == Key.None) return Binds.QwertyName(name);
        string local = OS.GetKeycodeString(DisplayServer.KeyboardGetKeycodeFromPhysical(physical));
        return local.Length == 1 && local[0] is >= 'A' and <= 'Z' ? local : Binds.QwertyName(name);
    }

    /// <summary>The Godot name a press of a physical key is kept as.</summary>
    public static string NameOf(InputEventKey key) => key.PhysicalKeycode.ToString();
}
