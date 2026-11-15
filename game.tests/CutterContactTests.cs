using System;
using Stillwake.Game.Features.Crew;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>The plasma cutter's slag blows on along its jet out of the far side of what it cuts (cutter-contact).</summary>
public class CutterContactTests
{
    // A plate from x = 3 to x = 10, the jet run right from the origin.
    private static bool Plate(double x, double _) => x is > 3 and < 10;

    private static ToolContact.Exit? Through((double X, double Y) at, Func<double, double, bool> inside) =>
        ToolContact.Through(at, (1, 0), inside, WorkLooks.SlagStep, WorkLooks.SlagDepth);

    [Fact]
    public void TheJetComesOutOfTheFarSide()
    {
        var exit = Assert.NotNull(Through((0, 0), Plate));
        Assert.Equal(10, exit.At.X, 9);
        Assert.Equal((1.0, 0.0), exit.Way);
    }

    [Fact]
    public void ATouchInsideStillComesOutOfTheFarSide()
    {
        Assert.Equal(10, Assert.NotNull(Through((5, 0), Plate)).At.X, 9);
    }

    [Fact]
    public void NoExitWhileItMissesOrDoesNotComeOutInReach()
    {
        Assert.Null(Through((0, 0), (_, _) => false));
        Assert.Null(Through((0, 0), (x, _) => x > 3));
    }

    [Fact]
    public void SlagFliesFromTheExitAlongTheJetWithNoBead()
    {
        var sparks = new ToolSparks();
        var exit = new ToolContact.Exit((10, 0), (1, 0), 0, 7);
        sparks.Begin(0);
        sparks.Touch((1, 7), Content.Weapons.PlasmaCutter.Item, (3, 0), (-1, 0), 0, exits: [exit]);
        sparks.End();
        sparks.Begin(100);
        sparks.Touch((1, 7), Content.Weapons.PlasmaCutter.Item, (3, 0), (-1, 0), 100, exits: [exit]);
        sparks.End();
        Assert.True(sparks.Count > 0);
        for (int n = 0; n < sparks.Count; n++)
            if (sparks.At(n) is var (_, head, _)) Assert.True(head.X >= 10, $"spark {n} at {head}");
        Assert.Equal(0, sparks.Beads);
    }
}
