using System;
using System.Collections.Generic;
using Stillwake.Sim;

namespace Stillwake.Game.Platform;

/// <summary>A world started from the title: its Rng's seed and its id each drawn, so no two new worlds play alike.</summary>
public static class NewWorlds
{
    /// <summary>A new world of <paramref name="layout"/> run by <paramref name="modules"/>, its seed and id from <paramref name="random"/>.</summary>
    public static World Create(Layout layout, IReadOnlyList<Module> modules, Random random) =>
        World.Create((uint)random.NextInt64(1L << 32), layout, modules, (uint)random.NextInt64(1, 1L << 32));
}
