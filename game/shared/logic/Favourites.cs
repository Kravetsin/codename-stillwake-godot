using System;
using System.Collections.Generic;
using System.Linq;

namespace Stillwake.Game.Shared;

/// <summary>
/// The items this machine's player keeps in the bag when stashing (2026-11-06-quick-stash): marked per kind of item, for every
/// world, by Alt+click in the open inventory. A value: <see cref="Toggled"/> gives a new one, so a <c>Store</c> hears the change.
/// </summary>
public sealed class Favourites : IEquatable<Favourites>
{
    /// <summary>None marked: a first run's, and a dev run's.</summary>
    public static readonly Favourites None = new([]);

    private readonly string[] _items;

    private Favourites(string[] items) => _items = items;

    /// <summary>The ids marked, sorted ordinally: what a <c>Stash</c> carries as its <c>Keep</c>.</summary>
    public IReadOnlyList<string> Kept => _items;

    public bool Has(string item) => Array.BinarySearch(_items, item, StringComparer.Ordinal) >= 0;

    /// <summary>These with <paramref name="item"/> marked, or no longer marked if it was.</summary>
    public Favourites Toggled(string item) =>
        new(Has(item) ? [.. _items.Where(i => i != item)] : [.. _items.Append(item).Order(StringComparer.Ordinal)]);

    /// <summary>
    /// The favourites as the settings file keeps them, ids joined by commas: what is not an item now (a typo, an item since
    /// removed) is dropped, so a broken line gives fewer marks, never an error.
    /// </summary>
    public static Favourites Read(string kept) =>
        new([.. kept.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(id => Content.Items.TryGet(id, out _)).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)]);

    /// <summary>The line <see cref="Read"/> reads back.</summary>
    public string Written() => string.Join(",", _items);

    public bool Equals(Favourites? other) => other != null && _items.SequenceEqual(other._items, StringComparer.Ordinal);

    public override bool Equals(object? obj) => Equals(obj as Favourites);

    public override int GetHashCode() => _items.Length;
}
