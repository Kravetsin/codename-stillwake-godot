using System.Collections.Generic;
using Stillwake.Game.Render;

namespace Stillwake.Game;

/// <summary>
/// How each weapon effect skin's charge looks (skins): its colours and numbers by the skin's id, run on any struck blade held
/// without an arc module (<see cref="ChargeOn"/>).
/// </summary>
public static class SkinEffects
{
    /// <summary>By an effect skin's id, its charge's look.</summary>
    public static readonly IReadOnlyDictionary<string, ChargeLook> On = new Dictionary<string, ChargeLook>();

    /// <summary>The charge of effect skin <paramref name="id"/>; none for none or one without a row.</summary>
    public static ChargeLook? Of(string? id) => id != null && On.TryGetValue(id, out var look) ? look : null;
}
