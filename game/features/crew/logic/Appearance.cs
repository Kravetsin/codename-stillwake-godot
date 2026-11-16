using System;
using System.Collections.Generic;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// The Appearance tab (hero-skins), Godot-free: its slots in rows, the strip a slot opens, and what a pick makes of the skins kept.
/// </summary>
public static class Appearance
{
    /// <summary>The kinds as the tab lays them out, a row each by where they are worn: the head; the body and the cape; the weapon effect.</summary>
    public static readonly IReadOnlyList<IReadOnlyList<string>> Rows = [[Skins.Head], [Skins.Suit, Skins.Cape], [Skins.Effect]];

    /// <summary>One entry of a strip: a skin, none for "none", and whether this player may pick it.</summary>
    public readonly record struct Choice(string? Id, bool Owned);

    /// <summary>The strip of <paramref name="kind"/>: "none" first, always owned, then every skin of it in the catalogue's order.</summary>
    public static IReadOnlyList<Choice> Strip(string kind, Func<Skin, bool> owned, IReadOnlyList<Skin>? catalogue = null)
    {
        var strip = new List<Choice> { new(null, true) };
        foreach (var skin in catalogue ?? Skins.All)
            if (skin.Kind == kind) strip.Add(new(skin.Id, owned(skin)));
        return strip;
    }

    /// <summary><paramref name="kept"/> with <paramref name="kind"/>'s skin made <paramref name="id"/>, none for none; the rest as they were.</summary>
    public static IReadOnlyList<string> Pick(IReadOnlyList<string> kept, string kind, string? id, IReadOnlyList<Skin>? catalogue = null)
    {
        var picked = new List<string>();
        foreach (var k in kept)
            if (Skins.Of(k, catalogue) is { } skin && skin.Kind != kind) picked.Add(k);
        if (id != null) picked.Add(id);
        return Skins.Set(picked, catalogue);
    }
}
