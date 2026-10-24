using System;
using System.Collections.Generic;
using System.Linq;

namespace Stillwake.Game.Menu;

/// <summary>Where playing together stands on this machine.</summary>
public enum TogetherMode
{
    /// <summary>Playing alone: neither hosting nor joined.</summary>
    Alone,
    /// <summary>Asking the relay to open a session.</summary>
    Opening,
    /// <summary>Hosting: friends join by the code.</summary>
    Hosting,
    /// <summary>On the way into a friend's session.</summary>
    Joining,
    /// <summary>In a friend's session.</summary>
    Joined,
}

/// <summary>
/// Playing together as the menu shows it: the mode, the session's code, the suit colours aboard (this
/// player's first), and what went wrong last, if anything; <see cref="PasswordRefused"/> when the
/// relay could not be reached, most likely for its password.
/// </summary>
public sealed record TogetherState(TogetherMode Mode, string? Code, IReadOnlyList<int> Suits, string? Problem, bool PasswordRefused)
{
    public static readonly TogetherState Alone = new(TogetherMode.Alone, null, [], null, false);
}

/// <summary>What the menu's Play together page asks of the game.</summary>
public interface ITogether
{
    TogetherState Together { get; }

    /// <summary>Opens this world to friends on the playtest relay, under a new code.</summary>
    void HostTogether(string password);

    /// <summary>Joins the session <paramref name="code"/> names on the playtest relay.</summary>
    void JoinTogether(string code, string password);

    /// <summary>Stops hosting: the friends aboard are let go, the world plays on alone.</summary>
    void StopTogether();
}

/// <summary>A session's code: four letters and digits a player reads out, none mistaken for another.</summary>
public static class Codes
{
    /// <summary>No O and 0, no I and 1: read aloud or off a screen, none is taken for another.</summary>
    public const string Letters = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    public const int Length = 4;

    public static string New(Random random) =>
        new(Enumerable.Range(0, Length).Select(_ => Letters[random.Next(Letters.Length)]).ToArray());

    /// <summary>A code as typed: upper case, of the code's letters only, at most <see cref="Length"/>.</summary>
    public static string Typed(string text) =>
        new(text.ToUpperInvariant().Where(c => Letters.Contains(c)).Take(Length).ToArray());
}
