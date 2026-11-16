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
    private static (double X, double Y)[] Edge(double x) => [(x, 0), (x, 6)];

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
        var (points, alphas) = Drawn(trail);
        // What is seen: the feather beyond the edge's ends is drawn clear.
        var seen = points.Where((_, i) => alphas[i] > 0).ToList();
        Assert.NotEmpty(seen);
        Assert.All(seen, p => Assert.InRange(p.X, 0, 40));
        Assert.All(seen, p => Assert.InRange(p.Y, 0, 6));
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

    [Fact]
    public void ARoundedBladeLeavesATrailOfItsOwnCurve()
    {
        // The fire axe's edge, bowed: swung straight along its ends' line, its trail's ends are its curve, not a straight cut.
        var axe = HeldLook.FireAxe;
        var trail = new BladeTrail();
        for (int i = 0; i <= 3; i++)
        {
            var edge = new (double X, double Y)[HeldLook.EdgePoints];
            for (int k = 0; k < edge.Length; k++)
            {
                var p = axe.EdgePoint((double)k / (edge.Length - 1));
                edge[k] = (p.X, p.Y - i * 8);
            }
            trail.Step(Frame, edge);
        }
        var (points, _) = Drawn(trail);
        double ends = Math.Max(axe.EdgeFrom.Y, axe.EdgeTo.Y), lowest = points.Max(p => p.Y);
        Assert.True(lowest > ends + 0.5, $"the trail reaches {lowest}, the edge's ends {ends}");
    }

    [Fact]
    public void AnEdgeBowsOutAtItsMiddleAndKeepsItsEnds()
    {
        var look = HeldLook.FireAxe with { EdgeFrom = (0, 0), EdgeTo = (10, 0), Bow = 2 };
        Assert.Equal((0.0, 0.0), look.EdgePoint(0));
        Assert.Equal((10.0, 0.0), look.EdgePoint(1));
        // To the right of the way from its one end to the other, +y down: below an edge running +x.
        var (x, y) = look.EdgePoint(0.5);
        Assert.Equal(5, x, 6);
        Assert.Equal(2, y, 6);
        Assert.Equal((5.0, 0.0), (look with { Bow = 0 }).EdgePoint(0.5));
    }

    [Fact]
    public void ItsSmoothedSweepPassesThroughItsSamplesOnACurveBetween()
    {
        var trail = new BladeTrail();
        // The edge swung round the origin a tenth of a turn a frame, 30 px out: 18 px a frame, a straight chord between frames.
        static (double X, double Y)[] At(int i) => [(30 * Math.Cos(i * 0.6), 30 * Math.Sin(i * 0.6)), (36 * Math.Cos(i * 0.6), 36 * Math.Sin(i * 0.6))];
        for (int i = 0; i <= 4; i++) trail.Step(Frame, At(i));
        var fine = trail.Smoothed(1.5);
        Assert.True(fine.Count > 20);
        foreach (var s in trail.Samples) Assert.Contains(fine, f => f.Edge[0] == s.Edge[0]);
        // Between the frames it keeps near the circle the edge swung on, where a chord would cut inside it by 1.3 px.
        Assert.All(fine, f => Assert.InRange(Math.Sqrt(f.Edge[0].X * f.Edge[0].X + f.Edge[0].Y * f.Edge[0].Y), 29.6, 30.4));
        Assert.Equal(fine.OrderBy(f => f.Place).Select(f => f.Place), fine.Select(f => f.Place));
    }

    [Fact]
    public void ItsSmoothedSweepPutsNothingAcrossAJump()
    {
        var trail = new BladeTrail();
        trail.Step(Frame, Edge(0));
        trail.Step(Frame, Edge(8));
        trail.Step(Frame, Edge(8 + BladeTrail.Jump + 5));
        trail.Step(Frame, Edge(8 + BladeTrail.Jump + 13));
        var fine = trail.Smoothed(1.5);
        Assert.DoesNotContain(fine, f => f.Edge[0].X > 8 && f.Edge[0].X < 8 + BladeTrail.Jump + 5);
    }
}
