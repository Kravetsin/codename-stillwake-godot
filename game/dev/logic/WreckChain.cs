using System;
using System.Collections.Generic;
using System.Linq;
using Stillwake.Content;

namespace Stillwake.Game.Dev;

/// <summary>
/// A forced chain of a wreck's events as the dev flag writes it (vessel-wrecks): <c>blast,fire,rock:old</c>, each event's name, old
/// with <c>:old</c>, fresh without; <c>none</c> an intact vessel.
/// </summary>
public static class WreckChain
{
    /// <summary>Each event by the name the flag gives it.</summary>
    public static readonly IReadOnlyList<(string Name, WreckKind Kind)> Names =
        [("blast", WreckKind.Explosion), ("fire", WreckKind.Fire), ("ice", WreckKind.Icing), ("rock", WreckKind.Asteroid), ("break", WreckKind.Break)];

    /// <summary>The chain <paramref name="text"/> writes; throws on a name none has, saying which.</summary>
    public static IReadOnlyList<WreckStep> Parse(string text)
    {
        if (text.Trim() == "none") return [];
        return text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(step =>
        {
            var bits = step.Split(':');
            var kind = Names.Where(n => n.Name == bits[0]).Select(n => (WreckKind?)n.Kind).FirstOrDefault()
                ?? throw new ArgumentException($"--wrecked: no event '{bits[0]}' (blast, fire, ice, rock, break)");
            return new WreckStep(kind, bits.Length > 1 && bits[1] == "old");
        }).ToList();
    }

    /// <summary>The flag's words for <paramref name="chain"/>: what a dev reads back.</summary>
    public static string Write(IEnumerable<WreckStep> chain) =>
        string.Join(",", chain.Select(s => Names.First(n => n.Kind == s.Kind).Name + (s.Old ? ":old" : "")));
}
