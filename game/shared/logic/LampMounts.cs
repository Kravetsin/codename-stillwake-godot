using System.Collections.Generic;

namespace Stillwake.Game.Shared;

/// <summary>
/// Where the helmet lamp's picture's top left sits on each helmet's picture, in its pixels, by the helmet's picture id: an
/// item's or a skin's (hero-skins). The basic one's on its brow, the heavy one's on its cap.
/// </summary>
public static class LampMounts
{
    public static readonly IReadOnlyDictionary<string, (double X, double Y)> On = new Dictionary<string, (double X, double Y)>
    {
        [Stillwake.Content.Helmet.Basic] = (8, -2),
        [Stillwake.Content.Helmet.Heavy] = (6, -4.8),
        // The raw painted helmets (gear-roles), laid in the basic ball's box: on its brow too.
        [Stillwake.Content.Helmet.Heated] = (8, -2),
        [Stillwake.Content.Helmet.HeatProof] = (8, -2),
        // The helmet skins: on the brow of each painted dome, over its face opening as on the basic one.
        ["brassHelmet"] = (8, -2.4),
        ["beakHelmet"] = (8, -2.8),
        ["roninHelmet"] = (8, -2),
        ["knightHelmet"] = (8, -2.2),
        ["reliquaryHelmet"] = (8, -2.4),
        ["skullHelmet"] = (8, -2.4),
        ["ramHelmet"] = (8, -2.2),
        ["bathysphereHelmet"] = (8, -2.9),
        ["paradeHelmet"] = (8, -2.2),
        ["dustHelmet"] = (8, -2),
        ["hideHelmet"] = (8, -2),
        ["boilerHelmet"] = (8, -2),
    };

    /// <summary>Where it sits on <paramref name="helmet"/>'s picture; the basic helmet's for none or one without a row.</summary>
    public static (double X, double Y) Of(string? helmet) =>
        helmet != null && On.TryGetValue(helmet, out var at) ? at : On[Stillwake.Content.Helmet.Basic];
}
