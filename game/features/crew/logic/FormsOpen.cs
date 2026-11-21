using System;
using System.Collections.Generic;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// The drone's forms as its own player's client opens them (drone-thoughts): a form the sim says is known opens only once its lesson has played
/// and its icon landed on the research key, so the scanner works no sooner than the show says it was learned. Known when the client first sees
/// the world, or in creative, a form is open at once; one whose lesson the client never hears of within <see cref="UnheardMs"/> opens then.
/// Milliseconds.
/// </summary>
public sealed class FormsOpen
{
    /// <summary>How long a form newly known waits for its lesson to be heard before it opens anyway, ms: the research key reads every 0.25 s.</summary>
    public const double UnheardMs = 2000;

    private readonly HashSet<string> _open = new(StringComparer.Ordinal);
    private readonly Dictionary<string, (bool Heard, double WaitedMs)> _waiting = new(StringComparer.Ordinal);
    private object? _world;

    /// <summary>
    /// Moves it <paramref name="ms"/> on in <paramref name="world"/>: what the sim says its player <paramref name="knows"/>, whether a form's lesson
    /// <paramref name="held"/> is still to play or playing, and whether the player is in <paramref name="creative"/>.
    /// </summary>
    public void Step(object world, Func<string, bool> knows, Func<string, bool> held, bool creative, double ms)
    {
        bool first = world != _world;
        if (first)
        {
            _world = world;
            _open.Clear();
            _waiting.Clear();
        }
        foreach (string form in Stillwake.Content.Lessons.Forms)
        {
            if (!knows(form))
            {
                _open.Remove(form);
                _waiting.Remove(form);
                continue;
            }
            if (_open.Contains(form)) continue;
            var (heard, waited) = _waiting.GetValueOrDefault(form);
            bool holds = held(form);
            if (first || creative || heard && !holds || !heard && !holds && waited + ms >= UnheardMs)
            {
                _open.Add(form);
                _waiting.Remove(form);
            }
            else _waiting[form] = (heard || holds, waited + ms);
        }
    }

    /// <summary>Whether <paramref name="form"/> is open to its player now.</summary>
    public bool Opened(string form) => _open.Contains(form);
}
