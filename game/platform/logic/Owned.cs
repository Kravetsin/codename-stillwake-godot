using System;
using System.Collections.Generic;
using Stillwake.Game.Features.Crew;

namespace Stillwake.Game.Platform;

/// <summary>
/// What skins this player owns (skins), the one place that says: a free skin always; a DLC's when <paramref name="everything"/>
/// (a debug build without <c>--own-none</c>) or when <paramref name="dlc"/> says its app is owned. No release build owns a
/// DLC till Steam is in: anything else would give paid skins away in builds already shipped.
/// </summary>
public sealed class Owned(bool everything, Func<uint, bool> dlc)
{
    /// <summary>Whether this player may pick <paramref name="skin"/>.</summary>
    public bool Has(Skin skin) => skin.DlcAppId is not uint app || everything || dlc(app);

    /// <summary>What goes out in <c>ChooseSkins</c> from the <paramref name="kept"/> picks: the owned ones, one a kind, the empty set too.</summary>
    public IReadOnlyList<string> Sendable(IReadOnlyList<string> kept, IReadOnlyList<Skin>? catalogue = null)
    {
        var owned = new List<string>();
        foreach (var id in kept)
            if (Skins.Of(id, catalogue) is { } skin && Has(skin)) owned.Add(id);
        return Skins.Set(owned, catalogue);
    }
}
