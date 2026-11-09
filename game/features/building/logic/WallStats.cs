using System.Collections.Generic;
using Stillwake.Content;
using Stillwake.Game.I18n;
using Stillwake.Game.Shared;

namespace Stillwake.Game.Features.Building;

/// <summary>
/// A wall's numbers on its build card: the oxygen it lets out a minute through each cell, by the leak sign (tip-signs). Its
/// strength is not a number on the card (unified-damage): a burst cracks what is built and a second breaks it.
/// </summary>
public static class WallStats
{
    /// <summary>The lines for a wall or back wall of <paramref name="material"/>.</summary>
    public static IReadOnlyList<TipLine> Of(Material material) =>
        [new(En.T("wall.leak", ("rate", LeakAMinute(material))), Sign: Sign.Leak)];

    /// <summary>The oxygen a cell of <paramref name="material"/> lets out a minute, in litres (industrial-oxygen).</summary>
    public static string LeakAMinute(Material material) => Litres.PerMinute(material.Leak);
}
