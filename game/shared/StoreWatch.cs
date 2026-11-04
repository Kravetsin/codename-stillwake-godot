using System;
using Godot;

namespace Stillwake.Game.Shared;

/// <summary>A node hearing a <see cref="Store{T}"/> only while it is in the tree, so one freed before the store is never called.</summary>
public static class StoreWatch
{
    /// <summary>Calls <paramref name="heard"/> after every change of <paramref name="store"/> while <paramref name="node"/> is in the tree.</summary>
    public static void Watch<T>(this Node node, Store<T> store, Action heard)
    {
        IDisposable? held = null;
        void Hold() => held ??= store.Subscribe(heard);
        node.TreeEntered += Hold;
        node.TreeExiting += () =>
        {
            held?.Dispose();
            held = null;
        };
        if (node.IsInsideTree()) Hold();
    }
}
