using System.Collections.Generic;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// A skin (hero-skins): <c>Id</c>, the picture id its gear is drawn by; <c>Kind</c>, one of <see cref="Skins.Kinds"/>; <c>DlcAppId</c>,
/// the Steam DLC that unlocks it, none for a free one. Its art ships in every build: owning only lets a player pick it.
/// </summary>
public sealed record Skin(string Id, string Kind, uint? DlcAppId = null);

/// <summary>
/// What a hero is drawn in (hero-skins): the helmet's, the body's and the cape's picture ids, each the skin chosen while its slot
/// holds an item, else the item's own, none for an empty slot; and the weapon effect chosen, none without.
/// </summary>
public readonly record struct Dressed(string? Helmet, string? Body, string? Cape, string? Effect);

/// <summary>The skins' catalogue (hero-skins), the client's alone: the sim keeps a peer's ids and never reads them.</summary>
public static class Skins
{
    public const string Head = "head", Suit = "suit", Cape = "cape", Effect = "effect";

    /// <summary>The kinds a hero can be skinned in, in the order a client sends and keeps them.</summary>
    public static readonly IReadOnlyList<string> Kinds = [Head, Suit, Cape, Effect];

    /// <summary>Every skin, rows added with their art.</summary>
    public static readonly IReadOnlyList<Skin> All =
    [
        new("brassHelmet", Head, DlcAppId: 0),
        new("beakHelmet", Head, DlcAppId: 0),
        new("roninHelmet", Head, DlcAppId: 0),
        new("knightHelmet", Head, DlcAppId: 0),
        new("reliquaryHelmet", Head, DlcAppId: 0),
        new("skullHelmet", Head, DlcAppId: 0),
        new("ramHelmet", Head, DlcAppId: 0),
        new("bathysphereHelmet", Head, DlcAppId: 0),
        new("paradeHelmet", Head, DlcAppId: 0),
        // Raw helmets picked with the heated and heat helmets (gear-roles), worn as skins until gear of their own needs them.
        new("dustHelmet", Head, DlcAppId: 0),
        new("hideHelmet", Head, DlcAppId: 0),
        new("boilerHelmet", Head, DlcAppId: 0),
        new("diverSuit", Suit, DlcAppId: 0),
        new("platedSuit", Suit, DlcAppId: 0),
        new("rescueSuit", Suit, DlcAppId: 0),
        new("mummySuit", Suit, DlcAppId: 0),
        new("brassSuit", Suit, DlcAppId: 0),
        new("beakSuit", Suit, DlcAppId: 0),
        new("roninSuit", Suit, DlcAppId: 0),
        new("knightSuit", Suit, DlcAppId: 0),
        new("reliquarySuit", Suit, DlcAppId: 0),
        new("skullSuit", Suit, DlcAppId: 0),
        new("ramSuit", Suit, DlcAppId: 0),
        new("bathysphereSuit", Suit, DlcAppId: 0),
        new("paradeSuit", Suit, DlcAppId: 0),
        new("tarpCape", Cape, DlcAppId: 0),
        new("blanketCape", Cape, DlcAppId: 0),
        new("woolCape", Cape, DlcAppId: 0),
        new(SkinEffects.Fire, Effect, DlcAppId: 0),
        new(SkinEffects.Rift, Effect, DlcAppId: 0),
    ];

    /// <summary>The skin <paramref name="id"/> in <paramref name="catalogue"/>; none for an id this build does not know.</summary>
    public static Skin? Of(string id, IReadOnlyList<Skin>? catalogue = null)
    {
        foreach (var skin in catalogue ?? All)
            if (skin.Id == id) return skin;
        return null;
    }

    /// <summary>The first of <paramref name="ids"/> this build knows as a skin of <paramref name="kind"/>; none if none is.</summary>
    public static string? Chosen(IReadOnlyList<string> ids, string kind, IReadOnlyList<Skin>? catalogue = null)
    {
        foreach (var id in ids)
            if (Of(id, catalogue) is { } skin && skin.Kind == kind) return id;
        return null;
    }

    /// <summary>
    /// A hero wearing <paramref name="helmet"/>, <paramref name="body"/> and <paramref name="cape"/> (items, none for an empty
    /// slot) in the skins <paramref name="ids"/>: a skin only over an item, an unknown or misplaced id the item's own.
    /// </summary>
    public static Dressed Dress(string? helmet, string? body, string? cape, IReadOnlyList<string> ids, IReadOnlyList<Skin>? catalogue = null) => new(
        helmet == null ? null : Chosen(ids, Head, catalogue) ?? helmet,
        body == null ? null : Chosen(ids, Suit, catalogue) ?? body,
        cape == null ? null : Chosen(ids, Cape, catalogue) ?? cape,
        Chosen(ids, Effect, catalogue));

    /// <summary><paramref name="ids"/> as a client sends and keeps them: the first known one of each kind, in <see cref="Kinds"/>' order.</summary>
    public static IReadOnlyList<string> Set(IReadOnlyList<string> ids, IReadOnlyList<Skin>? catalogue = null)
    {
        var set = new List<string>();
        foreach (var kind in Kinds)
            if (Chosen(ids, kind, catalogue) is { } id) set.Add(id);
        return set;
    }
}
