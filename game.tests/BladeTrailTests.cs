using Stillwake.Game.Features.Crew;
using Stillwake.Game.Render;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>The trail a held blade leaves by its own motion (blade-trail).</summary>
public class BladeTrailTests
{
    private const double Frame = 1000.0 / 60;
    private static readonly Ink.Tone Tone = Ink.Tone.Of(255, 255, 255, 1);

    /// <summary>What the trail draws now: its points and their opacities.</summary>
    private static (List<(float X, float Y)> Points, List<float> Alphas) Drawn(BladeTrail trail)
    {
        var ink = new Ink();
        trail.Draw(ink, p => p, Tone);
        return ([.. ink.Points], [.. ink.Tones.Select(t => t.A)]);
    }

    /// <summary>An edge 6 px long, its near end at (<paramref name="x"/>, 0).</summary>
    private static ((double, double), (double, double)) Edge(double x) => ((x, 0), (x, 6));

    [Fact]
    public void AStillBladeLeavesNothing()
    {
        var trail = new BladeTrail();
        for (int i = 0; i < 30; i++) trail.Step(Frame, Edge(10));
        Assert.False(trail.Live);
        Assert.Empty(Drawn(trail).Points);
    }

    [Fact]
    public void AFastBladeLeavesItsTrailExactlyWhereItsEdgeWas()
    {
        var trail = new BladeTrail();
        // 8 px a frame: 480 px a second, over the speed a trail is whole at.
        for (int i = 0; i <= 5; i++) trail.Step(Frame, Edge(i * 8));
        Assert.True(trail.Live);
        var (points, _) = Drawn(trail);
        Assert.NotEmpty(points);
        Assert.All(points, p => Assert.InRange(p.X, 0, 40));
        Assert.All(points, p => Assert.InRange(p.Y, 0, 6));
    }

    [Fact]
    public void ASlowBladeLeavesAFainterTrailThanAFastOne()
    {
        static float Most(double perFrame)
        {
            var trail = new BladeTrail();
            for (int i = 0; i <= 3; i++) trail.Step(Frame, Edge(i * perFrame));
            var (_, alphas) = Drawn(trail);
            return alphas.Count == 0 ? 0 : alphas.Max();
        }
        // 2.5 px a frame is 150 px a second, between the two speeds; 0.5 px a frame is under the slower.
        Assert.True(Most(8) > Most(2.5));
        Assert.True(Most(2.5) > 0);
        Assert.Equal(0, Most(0.5));
    }

    [Fact]
    public void TheTrailFadesAndIsGoneAfterItsLife()
    {
        var trail = new BladeTrail();
        for (int i = 0; i <= 3; i++) trail.Step(Frame, Edge(i * 8));
        float fresh = Drawn(trail).Alphas.Max();
        trail.Step(BladeTrail.LifeMs / 2, null);
        float older = Drawn(trail).Alphas.Max();
        Assert.True(older < fresh);
        trail.Step(BladeTrail.LifeMs, null);
        Assert.False(trail.Live);
        Assert.Empty(Drawn(trail).Points);
    }

    [Fact]
    public void AJumpBreaksTheTrail()
    {
        var trail = new BladeTrail();
        trail.Step(Frame, Edge(0));
        trail.Step(Frame, Edge(BladeTrail.Jump + 1));
        Assert.False(trail.Live);
    }

    [Fact]
    public void TheTrailStaysInTheWorldWhateverTheDrawerDoes()
    {
        var trail = new BladeTrail();
        for (int i = 0; i <= 3; i++) trail.Step(Frame, Edge(i * 8));
        var ink = new Ink();
        // The drawer has moved 100 px on: the trail is drawn 100 px back in its frame.
        trail.Draw(ink, p => (p.X - 100, p.Y), Tone);
        Assert.NotEmpty(ink.Points);
        Assert.All(ink.Points, p => Assert.InRange(p.X, -100, -76));
    }
}
