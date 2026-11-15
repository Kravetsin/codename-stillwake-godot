using System.Collections.Generic;

namespace Stillwake.Game.Shared;

/// <summary>
/// Where the helmet lamp's picture's top left sits on each helmet's picture, in its pixels, by the helmet's picture id: an
/// item's or a skin's (skins). The basic one's on its brow, the heavy one's on its cap, the quilted one's on its crown.
/// </summary>
public static class LampMounts
{
    public static readonly IReadOnlyDictionary<string, (double X, double Y)> On = new Dictionary<string, (double X, double Y)>
    {
        [Stillwake.Content.Helmet.Basic] = (8, -2),
        [Stillwake.Content.Helmet.Heavy] = (6, -4.8),
        [Stillwake.Content.Helmet.Quilted] = (7.2, -3.4),
    };

    /// <summary>Where it sits on <paramref name="helmet"/>'s picture; the basic helmet's for none or one without a row.</summary>
    public static (double X, double Y) Of(string? helmet) =>
        helmet != null && On.TryGetValue(helmet, out var at) ? at : On[Stillwake.Content.Helmet.Basic];
}
