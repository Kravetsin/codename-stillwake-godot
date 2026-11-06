using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Stillwake.Game.Shared;

/// <summary>A game action a player may put on a key of their own, a row of the Controls page (2026-11-04-key-binds).</summary>
public enum Bind
{
    WalkLeft,
    WalkRight,
    Up,
    Down,
    Jump,
    /// <summary>Sprint on foot, the jetpack in space.</summary>
    Sprint,
    Use,
    Breathe,
    Suit,
    Inventory,
    /// <summary>Drops the chosen slot's stack with a hand free; picks what is pointed at while building.</summary>
    Drop,
    /// <summary>The bag into the crates in reach that hold its items (2026-11-06-quick-stash).</summary>
    Stash,
    Build,
    Search,
    ModeBuild,
    ModeWiring,
    ModePiping,
    Tablet,
    Market,
    Handbook,
    Air,
    ZoomIn,
    ZoomOut,
}

/// <summary>
/// Which key does each action (<see cref="Bind"/>): one key an action, none shared, none of <see cref="Reserved"/>. Keys are Godot's
/// names of physical keys (<c>A</c>, <c>Space</c>, <c>Slash</c>), so this stays Godot-free; <c>BindKeys</c> turns them into keys.
/// </summary>
public sealed partial class Binds
{
    /// <summary>The keys the game starts with.</summary>
    public static readonly Binds Default = new(new Dictionary<Bind, string>
    {
        [Bind.WalkLeft] = "A",
        [Bind.WalkRight] = "D",
        [Bind.Up] = "W",
        [Bind.Down] = "S",
        [Bind.Jump] = "Space",
        [Bind.Sprint] = "Shift",
        [Bind.Use] = "E",
        [Bind.Breathe] = "R",
        [Bind.Suit] = "V",
        [Bind.Inventory] = "Tab",
        [Bind.Drop] = "G",
        [Bind.Stash] = "Bracketright",
        [Bind.Build] = "B",
        [Bind.Search] = "Slash",
        [Bind.ModeBuild] = "Z",
        [Bind.ModeWiring] = "X",
        [Bind.ModePiping] = "C",
        [Bind.Tablet] = "T",
        [Bind.Market] = "M",
        [Bind.Handbook] = "J",
        [Bind.Air] = "Q",
        [Bind.ZoomIn] = "Equal",
        [Bind.ZoomOut] = "Minus",
    });

    /// <summary>
    /// Keys no action takes: Esc and the dev panel's backquote, the hotbar's digits, Enter, the modifiers clicks use, the
    /// system key, and the arrows and keypad + and − that walk and zoom always.
    /// </summary>
    public static readonly IReadOnlySet<string> Reserved = new HashSet<string>(StringComparer.Ordinal)
    {
        "Escape", "Quoteleft", "Key0", "Key1", "Key2", "Key3", "Key4", "Key5", "Key6", "Key7", "Key8", "Key9",
        "Enter", "KpEnter", "Ctrl", "Alt", "Meta", "Left", "Right", "Up", "Down", "KpAdd", "KpSubtract",
    };

    /// <summary>The binds played by: set at the start and by the Controls page, <see cref="Default"/> until then.</summary>
    public static Binds Current { get; set; } = Default;

    /// <summary>How a key's name is shown; <c>BindKeys</c> gives the current layout's letter for it.</summary>
    public static Func<string, string> Shown { get; set; } = QwertyName;

    private readonly Dictionary<Bind, string> _keys;

    private Binds(Dictionary<Bind, string> keys) => _keys = keys;

    /// <summary>The key <paramref name="act"/> is on.</summary>
    public string KeyOf(Bind act) => _keys[act];

    /// <summary>The action on <paramref name="key"/>, if any.</summary>
    public Bind? ActOn(string key) => _keys.Where(pair => pair.Value == key).Select(pair => (Bind?)pair.Key).FirstOrDefault();

    /// <summary>
    /// <paramref name="act"/> put on <paramref name="key"/>; the action that had it takes <paramref name="act"/>'s old key,
    /// and is <paramref name="swapped"/>. A reserved key is refused: null, with <paramref name="refused"/> the i18n key why.
    /// </summary>
    public Binds? Rebind(Bind act, string key, out Bind? swapped, out string? refused)
    {
        (swapped, refused) = (null, null);
        if (Reserved.Contains(key))
        {
            refused = key.StartsWith("Key", StringComparison.Ordinal) ? "bind.hotbar" : "bind.reserved";
            return null;
        }
        var keys = new Dictionary<Bind, string>(_keys);
        if (ActOn(key) is Bind other && other != act)
        {
            keys[other] = _keys[act];
            swapped = other;
        }
        keys[act] = key;
        return new Binds(keys);
    }

    /// <summary>An action's id in the settings file and in text placeholders: <c>walkLeft</c>, <c>use</c>.</summary>
    public static string IdOf(Bind act)
    {
        string name = act.ToString();
        return char.ToLowerInvariant(name[0]) + name[1..];
    }

    /// <summary>The action named <paramref name="id"/>; none for an id this build does not know.</summary>
    public static Bind? Named(string id) => Enum.GetValues<Bind>().Select(a => (Bind?)a).FirstOrDefault(a => IdOf(a!.Value) == id);

    /// <summary>The binds that differ from the defaults, by id, as the settings file keeps them.</summary>
    public IReadOnlyList<(string Id, string Key)> Changed() =>
        Enum.GetValues<Bind>().Where(a => _keys[a] != Default._keys[a]).Select(a => (IdOf(a), _keys[a])).ToList();

    /// <summary>
    /// The binds a settings file keeps, each put on the defaults in turn as a player would, so none is shared; an id or a key
    /// this build does not know, or a reserved key, is left at its default.
    /// </summary>
    public static Binds Read(IEnumerable<(string Id, string Key)> kept)
    {
        var binds = Default;
        foreach (var (id, key) in kept)
            if (Named(id) is Bind act && key.Length > 0 && binds.Rebind(act, key, out _, out _) is { } next) binds = next;
        return binds;
    }

    /// <summary>A key's name as a QWERTY keyboard prints it: the letter or sign, or the i18n <c>key.&lt;name&gt;</c> for a named key.</summary>
    public static string QwertyName(string key) => key switch
    {
        _ when key.Length == 1 => key,
        "Slash" => "/",
        "Backslash" => "\\",
        "Equal" => "=",
        "Minus" => "-",
        "Comma" => ",",
        "Period" => ".",
        "Semicolon" => ";",
        "Apostrophe" => "'",
        "Bracketleft" => "[",
        "Bracketright" => "]",
        _ => I18n.En.T("key." + IdOf(key)) is var said && said != "key." + IdOf(key) ? said : key,
    };

    private static string IdOf(string key) => char.ToLowerInvariant(key[0]) + key[1..];

    /// <summary>What <paramref name="act"/>'s key is shown as now.</summary>
    public static string NameOf(Bind act) => Shown(Current.KeyOf(act));

    /// <summary><paramref name="text"/> with each <c>{key:id}</c> the name of the key that action is on now; an unknown id left as it is.</summary>
    public static string Fill(string text) =>
        text.Contains("{key:", StringComparison.Ordinal) ? Placeholder().Replace(text, m => Named(m.Groups[1].Value) is Bind act ? NameOf(act) : m.Value) : text;

    /// <summary>The action ids a text's placeholders name, each once, sorted: what a translation must have the same of.</summary>
    public static SortedSet<string> IdsIn(string text) =>
        new(Placeholder().Matches(text).Select(m => m.Groups[1].Value), StringComparer.Ordinal);

    [GeneratedRegex(@"\{key:(\w+)\}")]
    private static partial Regex Placeholder();
}
