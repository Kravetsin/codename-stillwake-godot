using System.Collections.Generic;
using System.Linq;
using Stillwake.Game.Features.Crew;
using Stillwake.Game.Render;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>An effect skin replaces the swing's haze and lives on the blade at rest (skins): «Пламя» and «Разлом».</summary>
public class BladeFxTests
{
    private const double Frame = 1000.0 / 60;

    // A blade held still at the world's origin, its picture's points the world's: its edge along x from 0 to 6, its lip facing up.
    private static readonly IReadOnlyList<EdgeLip.Point> Lip = [.. Enumerable.Range(0, 7).Select(i => new EdgeLip.Point(i, 0, 0, -1, i / 6.0))];

    private static (double X, double Y)[] Edge(double y) => [.. Enumerable.Range(0, HeldLook.EdgePoints).Select(i => (i * 1.0, y))];

    private static BladeFx.Held Held(double y = 0) => new(p => p, Lip, Edge(y));

    private static (Ink Under, Ink Dark, Ink Over) Drawn(BladeFx fx)
    {
        var (under, dark, over) = (new Ink(), new Ink(), new Ink());
        fx.Draw(under, dark, over, p => p);
        return (under, dark, over);
    }

    // A fast swing down: 8 px a frame, over the speed a trail is whole at.
    private static void Swing(BladeFx fx, bool held)
    {
        for (int i = 0; i <= 5; i++) fx.Step(Frame, held ? Held(i * 8) : null, Edge(i * 8));
    }

    [Fact]
    public void EveryEffectSkinHasItsLook()
    {
        Assert.All(Skins.All.Where(s => s.Kind == Skins.Effect), s => Assert.NotNull(SkinEffects.Of(s.Id)));
        Assert.Null(SkinEffects.Of(null));
    }

    [Fact]
    public void FireLicksTheBladeAtRestAndThrowsEmbers()
    {
        var fx = BladeFx.Of(new FireLook());
        for (int i = 0; i < 120; i++) fx.Step(Frame, Held(), Edge(0));
        Assert.True(fx.Live);
        Assert.False(fx.Trail.Live);
        Assert.NotEmpty(Drawn(fx).Over.Points);
        // Let go of it: its embers rise on a while, then nothing is left.
        fx.Step(Frame, null, null);
        Assert.True(fx.Live);
        for (int i = 0; i < 120; i++) fx.Step(Frame, null, null);
        Assert.False(fx.Live);
    }

    [Fact]
    public void FireLeavesItsCindersAfterASwing()
    {
        var fx = BladeFx.Of(new FireLook());
        Swing(fx, held: false);
        Assert.True(fx.Trail.Live);
        // Past the trail's life only the cinders it dropped are left, and they are drawn.
        fx.Step(new FireLook().LifeMs + 1, null, null);
        Assert.False(fx.Trail.Live);
        Assert.True(fx.Live);
        Assert.NotEmpty(Drawn(fx).Over.Points);
    }

    [Fact]
    public void RiftTearsSpaceOnASwing()
    {
        var fx = BladeFx.Of(new RiftLook());
        Swing(fx, held: false);
        var (under, dark, _) = Drawn(fx);
        Assert.NotEmpty(dark.Points);
        Assert.NotEmpty(under.Points);
        // The void lies where the edge swept, from y 0 to 40, closing toward the edge's middle.
        Assert.All(dark.Points, p => Assert.InRange(p.Y, -0.5, 40.5));
        Assert.All(dark.Points, p => Assert.InRange(p.X, -0.5, 6.5));
    }

    [Fact]
    public void RiftBreathesASeamAlongTheEdgeAtRest()
    {
        var fx = BladeFx.Of(new RiftLook());
        for (int i = 0; i < 30; i++) fx.Step(Frame, Held(), Edge(0));
        var (under, dark, over) = Drawn(fx);
        Assert.NotEmpty(dark.Points);
        Assert.NotEmpty(under.Points);
        Assert.Empty(over.Points);
        Assert.All(dark.Points, p => Assert.InRange(p.Y, -1, 1));
    }

    [Fact]
    public void AStillBladeAtRestLeavesNoTrail()
    {
        foreach (EffectLook look in new EffectLook[] { new FireLook(), new RiftLook() })
        {
            var fx = BladeFx.Of(look);
            for (int i = 0; i < 30; i++) fx.Step(Frame, Held(), Edge(0));
            Assert.False(fx.Trail.Live);
        }
    }

    [Fact]
    public void ItsLooseBitsAreCapped()
    {
        var fx = BladeFx.Of(new FireLook { CindersPerPx = 100 });
        for (int n = 0; n < 4; n++) Swing(fx, held: true);
        var (_, _, over) = Drawn(fx);
        // Each loose bit is one disc of 8 points round its heart; the tongues and ribbon add a bounded few more.
        Assert.InRange(over.Points.Count, 1, BladeFx.MostMotes * 8 * 3 + 2000);
    }

    [Fact]
    public void TheLipFacesOutOfTheEdgeAndRunsAlongIt()
    {
        // A picture 12 by 6, drawn where 2 <= y < 5; the edge along its top from x 2 to 10 faces up (out of its bow is to the right of from → to).
        var look = new HeldLook("test", (0, 0), 0, (10, 2), (2, 2));
        var area = EffectArea.Of(12, 6, (x, y) => y >= 2 && y < 5, 1, look.EdgeBox());
        var lip = EdgeLip.Of(area, look);
        Assert.True(lip.Count >= 4);
        Assert.All(lip, p => Assert.True(p.Ny < 0, $"({p.X}, {p.Y}) faces ({p.Nx}, {p.Ny})"));
        Assert.Equal(lip.OrderBy(p => p.T).Select(p => p.T), lip.Select(p => p.T));
    }
}
