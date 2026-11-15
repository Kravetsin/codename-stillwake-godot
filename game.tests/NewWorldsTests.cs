using System;
using System.Linq;
using Stillwake.Game.Platform;
using Stillwake.Sim;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>A new world from the title: drawn afresh, so no two start alike (<see cref="NewWorlds"/>).</summary>
public class NewWorldsTests
{
    [Fact]
    public void TwoNewWorldsStartWithDifferentDebris()
    {
        string First(int draw)
        {
            var registry = new Registry();
            var world = NewWorlds.Create(Registry.Wreck, registry.All, new Random(draw));
            world.Step([]);
            return string.Join(";", registry.Debris.Pieces.Select(p => $"{p.Item}@{p.X},{p.Y}/{p.Speed}"));
        }

        Assert.NotEqual(First(1), First(2));
    }
}
