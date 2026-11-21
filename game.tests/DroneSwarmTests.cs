using Stillwake.Game.Features.Crew;
using Stillwake.Game.Shared;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>The light drone's sparks: on three orbits, a share forming one shape at a time (drone-threads, «Нити»).</summary>
public class DroneSwarmTests
{
    private const int Sparks = DroneInk.Sparks;
    private static readonly SwarmShape[] Shapes = [SwarmShape.Lamp, SwarmShape.Scan, SwarmShape.Sign];

    /// <summary>Long enough after a change for every spark to have arrived, s: the latest one's wait and its flight.</summary>
    private const double Settled = DroneInk.Form * (DroneInk.LagOut + DroneInk.SpanOut) + 0.01;

    private static float Apart(Spark a, Spark b) => MathF.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));

    private static Spark[] All(DroneSwarm swarm, float aim, double t) => Enumerable.Range(0, Sparks).Select(i => swarm.SparkAt(i, aim, t)).ToArray();

    /// <summary>Whether spark <paramref name="i"/> takes part in <paramref name="shape"/>: the reticle takes every one.</summary>
    private static bool Takes(SwarmShape shape, int i) => shape == SwarmShape.Scan || shape != SwarmShape.Idle && DroneSwarm.RankOf(i) >= 0;

    /// <summary>How far (<paramref name="x"/>, <paramref name="y"/>) is from <paramref name="shape"/>'s threads turned <paramref name="turn"/>, as <paramref name="swarm"/> has them at <paramref name="t"/>.</summary>
    private static float OffShape(DroneSwarm swarm, SwarmShape shape, float turn, float x, float y, double t)
    {
        float best = float.MaxValue, c = MathF.Cos(turn), s = MathF.Sin(turn);
        foreach (var line in swarm.LinesOf(shape, t))
            for (int k = 2; k + 1 < line.Length; k += 2)
            {
                float ax = line[k - 2] * c - line[k - 1] * s, ay = line[k - 2] * s + line[k - 1] * c;
                float bx = line[k] * c - line[k + 1] * s, by = line[k] * s + line[k + 1] * c;
                float dx = bx - ax, dy = by - ay, u = Math.Clamp(((x - ax) * dx + (y - ay) * dy) / (dx * dx + dy * dy), 0, 1);
                best = MathF.Min(best, MathF.Sqrt((ax + dx * u - x) * (ax + dx * u - x) + (ay + dy * u - y) * (ay + dy * u - y)));
            }
        return best;
    }

    private static void OnOrbit(Spark spark, int i, double t)
    {
        var orbit = DroneSwarm.Orbit(i, (float)t);
        Assert.False(spark.Flying, $"spark {i} still flies");
        Assert.Equal(orbit.X, spark.X, 4);
        Assert.Equal(orbit.Y, spark.Y, 4);
    }

    [Fact]
    public void AShapeTakesThePicksShareAndTheRestKeepOrbiting()
    {
        var ranked = Enumerable.Range(0, Sparks).Where(i => DroneSwarm.RankOf(i) >= 0).ToArray();
        Assert.Equal(DroneSwarm.Shared, ranked.Length);
        Assert.InRange(ranked.Length, 86, 106);
        Assert.Equal(Enumerable.Range(0, ranked.Length), ranked.Select(DroneSwarm.RankOf));
        Assert.All(ranked, i => Assert.True(DroneSwarm.Hash(3 * i + 1) < DroneInk.Share));
        foreach (var shape in Shapes)
        {
            var swarm = new DroneSwarm();
            swarm.Become(shape, 0.4f, 1);
            double t = 1 + Settled;
            for (int i = 0; i < Sparks; i++)
            {
                var spark = swarm.SparkAt(i, 0.4f, t);
                if (!Takes(shape, i)) OnOrbit(spark, i, t);
                else Assert.True(OffShape(swarm, shape, swarm.TurnOf(shape, 0.4f, t), spark.X, spark.Y, t) < 1e-3f, $"spark {i} off the {shape}");
            }
        }
    }

    /// <summary>A thought (drone-thoughts): its share of sparks on its picture over the core, unturned, the rest orbiting; another thought is a change.</summary>
    [Fact]
    public void AThoughtTakesItsShareOntoItsPictureOverTheCore()
    {
        var swarm = new DroneSwarm();
        foreach (var thought in (DroneThought[])[DroneThought.Cold, DroneThought.Love])
        {
            swarm.Become(SwarmShape.Thought, 0.4f, 1, thought: thought);
            Assert.Equal(thought, swarm.Thought);
            double t = 6;
            int taken = 0;
            for (int i = 0; i < Sparks; i++)
            {
                var spark = swarm.SparkAt(i, 0.4f, t);
                if (Math.Abs(spark.X - DroneSwarm.Orbit(i, (float)t).X) < 1e-4f && Math.Abs(spark.Y - DroneSwarm.Orbit(i, (float)t).Y) < 1e-4f) continue;
                taken++;
                Assert.True(OffShape(swarm, SwarmShape.Thought, 0, spark.X, spark.Y, t) < 1e-3f, $"spark {i} off the {thought}");
                Assert.True(spark.Y < -DroneInk.ThoughtAbove + DroneInk.ThoughtSize + 1e-3f, $"spark {i} under the {thought}");
            }
            Assert.InRange(taken, (int)(Sparks * DroneInk.ThoughtShare * 0.8), (int)(Sparks * DroneInk.ThoughtShare * 1.2));
        }
    }

    [Fact]
    public void TheReticleTakesEverySparkByThePicksSharesOneSittingOnEachMark()
    {
        // 26 marks, 40% of 120 on the ring, the rest on the ticks: the pick's alloc, the sparks in their hash's order.
        Assert.Equal([DroneInk.Dots, 48, 46], DroneSwarm.ScanShares);
        var byHash = Enumerable.Range(0, Sparks).OrderBy(i => DroneSwarm.Hash(3 * i + 1)).ToArray();
        Assert.Equal(byHash.Take(DroneInk.Dots), Enumerable.Range(0, DroneInk.Dots).Select(DroneSwarm.SparkOfSeed));
        for (int part = 0; part < 3; part++)
        {
            var ranks = Enumerable.Range(0, Sparks).Where(i => DroneSwarm.ScanPartOf(i).Part == part).Select(i => DroneSwarm.ScanPartOf(i).Rank).Order();
            Assert.Equal(Enumerable.Range(0, DroneSwarm.ScanShares[part]), ranks);
        }
        var swarm = new DroneSwarm();
        swarm.Become(SwarmShape.Scan, 0.9f, 1);
        double t = 1 + Settled;
        for (int j = 0; j < DroneInk.Dots; j++)
        {
            int i = DroneSwarm.SparkOfSeed(j);
            Assert.Equal(j, DroneSwarm.SeedOf(i));
            // It sits on the middle of its mark, the gauge's dot j's seat, still while the rest flow.
            var (x, y) = DroneSwarm.Seat(j, 0.9f);
            var mark = swarm.LinesOf(SwarmShape.Scan, t)[j];
            var (mx, my) = ((mark[0] + mark[2]) / 2, (mark[1] + mark[3]) / 2);
            Assert.Equal(mx * MathF.Cos(0.9f) - my * MathF.Sin(0.9f), x, 4);
            Assert.Equal(mx * MathF.Sin(0.9f) + my * MathF.Cos(0.9f), y, 4);
            Assert.Equal((x, y), (swarm.SparkAt(i, 0.9f, t).X, swarm.SparkAt(i, 0.9f, t).Y));
            Assert.Equal((x, y), (swarm.SparkAt(i, 0.9f, t + 1).X, swarm.SparkAt(i, 0.9f, t + 1).Y));
            Assert.InRange(MathF.Sqrt(x * x + y * y), 10.19f, 10.21f);
        }
    }

    [Fact]
    public void TheRingAndTheTicksFlowEachAtItsOwnPace()
    {
        var swarm = new DroneSwarm();
        swarm.Become(SwarmShape.Scan, 0, 0);
        int ring = Enumerable.Range(0, Sparks).First(i => DroneSwarm.ScanPartOf(i).Part == 1);
        // On the ring 12 px a second, a chord a little shorter; on a tick 8, away from its ends.
        Assert.InRange(Apart(swarm.SparkAt(ring, 0, 3), swarm.SparkAt(ring, 0, 3.1)), 1.15f, 1.2f + 1e-3f);
        var ticks = Enumerable.Range(0, Sparks).Where(i => DroneSwarm.ScanPartOf(i).Part == 2).ToArray();
        Assert.Contains(ticks, i => MathF.Abs(Apart(swarm.SparkAt(i, 0, 3), swarm.SparkAt(i, 0, 3.1)) - 0.8f) < 1e-3f);
        Assert.All(ticks, i => Assert.True(Apart(swarm.SparkAt(i, 0, 3), swarm.SparkAt(i, 0, 3.1)) <= 0.8f + 1e-3f || Apart(swarm.SparkAt(i, 0, 3), swarm.SparkAt(i, 0, 3.1)) > 5));
    }

    [Fact]
    public void TheReticleOpensAsTheScannerReadsAndClosesOnceItsDotsAreHome()
    {
        var swarm = new DroneSwarm();
        swarm.Become(SwarmShape.Scan, 0, 0);
        float Tick(double t) => swarm.LinesOf(SwarmShape.Scan, t)[DroneInk.Dots + 1][0];
        Assert.Equal(0, swarm.Open(2));
        Assert.Equal(5.4f, Tick(2), 4);
        swarm.Read(true, 2);
        Assert.InRange(swarm.Open(2 + DroneInk.OpenIn / 2), 0.1f, 0.9f);
        Assert.Equal(1, swarm.Open(2 + DroneInk.OpenIn));
        // Opened, the ticks reach in from 5.4 to 4.3 and out from 12.5 to 13.5, the ring shrinks to 7.1; the marks stay.
        Assert.Equal(4.3f, Tick(3), 4);
        Assert.Equal(13.5f, swarm.LinesOf(SwarmShape.Scan, 3)[DroneInk.Dots + 1][2], 4);
        Assert.Equal(7.1f, swarm.LinesOf(SwarmShape.Scan, 3)[DroneInk.Dots][0], 4);
        Assert.Equal(DroneInk.ScanMarks[3], swarm.LinesOf(SwarmShape.Scan, 3)[3]);
        // Every spark on the opened reticle.
        for (int i = 0; i < Sparks; i++)
        {
            var spark = swarm.SparkAt(i, 0, 3);
            Assert.True(OffShape(swarm, SwarmShape.Scan, 0, spark.X, spark.Y, 3) < 1e-3f, $"spark {i} off the opened reticle");
        }
        swarm.Read(false, 4);
        Assert.Equal(1, swarm.Open(4 + DroneInk.CloseAfter));
        Assert.InRange(swarm.Open(4 + DroneInk.CloseAfter + DroneInk.CloseIn / 2), 0.1f, 0.9f);
        Assert.Equal(0, swarm.Open(4 + DroneInk.CloseAfter + DroneInk.CloseIn));
        // Reading again as it closes, it opens again from where it is, no jump.
        float closing = swarm.Open(4.6);
        Assert.InRange(closing, 0.1f, 0.9f);
        swarm.Read(true, 4.6);
        Assert.Equal(closing, swarm.Open(4.6), 4);
        Assert.Equal(1, swarm.Open(4.6 + DroneInk.OpenIn));
    }

    [Fact]
    public void AWindowOutFormsNoneElseAGestureElseTheMode()
    {
        foreach (bool signs in (bool[])[false, true])
            foreach (bool scanning in (bool[])[false, true])
                foreach (bool idle in (bool[])[false, true])
                {
                    Assert.Equal(SwarmShape.Idle, DroneSwarm.ShapeOf(projecting: true, signs, scanning, idle));
                    var shape = DroneSwarm.ShapeOf(projecting: false, signs, scanning, idle);
                    Assert.Equal(signs ? SwarmShape.Sign : idle ? SwarmShape.Idle : scanning ? SwarmShape.Scan : SwarmShape.Lamp, shape);
                }
    }

    [Fact]
    public void ANewDroneIsIdleOnItsOrbits()
    {
        var swarm = new DroneSwarm();
        Assert.Equal(SwarmShape.Idle, swarm.Shape);
        Assert.Equal(0, swarm.Form(5));
        for (int i = 0; i < Sparks; i++) OnOrbit(swarm.SparkAt(i, 0, 5), i, 5);
    }

    [Fact]
    public void AReShownDroneStartsIdleWithNothingInFlight()
    {
        var swarm = new DroneSwarm();
        swarm.Become(SwarmShape.Scan, 1, 2);
        swarm.Reset();
        Assert.Equal(SwarmShape.Idle, swarm.Shape);
        Assert.Equal(0, swarm.Leaving(2.1).Form);
        for (int i = 0; i < Sparks; i++) OnOrbit(swarm.SparkAt(i, 1, 2.1), i, 2.1);
    }

    [Fact]
    public void ASwitchSendsEachSparkOnFromWhereItIsNoneJumping()
    {
        foreach (var (from, to) in (ValueTuple<SwarmShape, SwarmShape>[])[(SwarmShape.Idle, SwarmShape.Lamp), (SwarmShape.Lamp, SwarmShape.Scan),
            (SwarmShape.Scan, SwarmShape.Sign), (SwarmShape.Sign, SwarmShape.Idle), (SwarmShape.Lamp, SwarmShape.Idle)])
            foreach (double at in (double[])[0.2, 0.45, 3])
            {
                var swarm = new DroneSwarm();
                swarm.Become(from, 0.7f, 10);
                var before = All(swarm, 0.7f, 10 + at);
                swarm.Become(to, 0.7f, 10 + at);
                var after = All(swarm, 0.7f, 10 + at);
                for (int i = 0; i < Sparks; i++) Assert.True(Apart(before[i], after[i]) < 1e-3f, $"spark {i} jumped {from} to {to} at {at} s");
                // And it moves on smoothly, a frame at a time (a fast flight 3.5 px at most); but for a spark flowing along a shape past a line's end onto the
                // next line (as the pick's): the fan's short rays a few times while it changes.
                var prev = after;
                var leaps = new int[Sparks];
                for (double t = 10 + at + 1 / 60.0; t < 10 + at + Settled + 0.2; t += 1 / 60.0)
                {
                    var now = All(swarm, 0.7f, t);
                    for (int i = 0; i < Sparks; i++)
                        if (Apart(prev[i], now[i]) >= 5) leaps[i]++;
                    prev = now;
                }
                for (int i = 0; i < Sparks; i++) Assert.True(leaps[i] <= (Takes(from, i) || Takes(to, i) ? 4 : 0), $"spark {i} leapt {leaps[i]} times, {from} to {to} at {at} s");
            }
    }

    [Fact]
    public void AModeChangedMidGestureWaitsForTheGestureThenGoesStraightFromTheFan()
    {
        var swarm = new DroneSwarm();
        swarm.Become(DroneSwarm.ShapeOf(false, signs: false, scanning: false, idle: false), 0, 0);
        swarm.Become(DroneSwarm.ShapeOf(false, signs: true, scanning: false, idle: false), 0, 3);
        // The scanner put on mid-gesture: still the sign.
        swarm.Become(DroneSwarm.ShapeOf(false, signs: true, scanning: true, idle: false), 0, 3.5);
        Assert.Equal(SwarmShape.Sign, swarm.Shape);
        var fan = All(swarm, 0, 4);
        swarm.Become(DroneSwarm.ShapeOf(false, signs: false, scanning: true, idle: false), 0, 4);
        Assert.Equal(SwarmShape.Scan, swarm.Shape);
        var leaving = All(swarm, 0, 4);
        for (int i = 0; i < Sparks; i++) Assert.True(Apart(fan[i], leaving[i]) < 1e-3f, $"spark {i} jumped");
        double t = 4 + Settled;
        for (int i = 0; i < Sparks; i++)
            Assert.True(OffShape(swarm, SwarmShape.Scan, 0, swarm.SparkAt(i, 0, t).X, swarm.SparkAt(i, 0, t).Y, t) < 1e-3f);
    }

    [Fact]
    public void TurningTheAimTurnsTheShapeWithNoJump()
    {
        var swarm = new DroneSwarm();
        swarm.Become(SwarmShape.Lamp, 0, 0);
        // A frame's turn moves a spark no further than that arc at the shape's farthest, forming or held.
        foreach (double t in (double[])[0.2, 0.4, 2, 5])
            for (float aim = -3; aim < 3; aim += 0.3f)
            {
                var now = All(swarm, aim, t);
                var turned = All(swarm, aim + 0.05f, t);
                for (int i = 0; i < Sparks; i++) Assert.True(Apart(now[i], turned[i]) < 0.05f * 13, $"spark {i} jumped as the aim turned");
            }
        Assert.Equal(1.5f, swarm.TurnOf(SwarmShape.Lamp, 1.5f, 5));
    }

    [Fact]
    public void ShownAgainMidFlightItTurnsBackFromWhereItIs()
    {
        // A window opened and shut again before the sparks were home: they turn round where they are.
        var swarm = new DroneSwarm();
        swarm.Become(SwarmShape.Lamp, 0, 0);
        swarm.Become(SwarmShape.Idle, 0, 2);
        var midway = All(swarm, 0, 2.3);
        Assert.Contains(midway, s => s.Flying);
        swarm.Become(SwarmShape.Lamp, 0, 2.3);
        var back = All(swarm, 0, 2.3);
        for (int i = 0; i < Sparks; i++) Assert.True(Apart(midway[i], back[i]) < 1e-3f, $"spark {i} jumped");
    }

    [Fact]
    public void HeldSparksFlowAlongTheShape()
    {
        var swarm = new DroneSwarm();
        swarm.Become(SwarmShape.Lamp, 0, 0);
        var held = Enumerable.Range(0, Sparks).Where(k => DroneSwarm.RankOf(k) >= 0).ToArray();
        // 30 px a second, round corners at most shorter; but for a few stepping past a line's end onto the next.
        var moved = held.Select(i => Apart(swarm.SparkAt(i, 0, 3), swarm.SparkAt(i, 0, 3.1))).ToArray();
        Assert.All(moved, d => Assert.True(d is >= 1 and <= DroneInk.Flow * 0.1f + 0.01f || d > 5, $"moved {d}"));
        Assert.True(moved.Count(d => d <= 5) > held.Length * 0.9);
        Assert.All(held, i => Assert.True(swarm.SparkAt(i, 0, 3).Front));
    }

    [Fact]
    public void ALeavingShapeFadesOverTheFormAndTheNewOneGrows()
    {
        var swarm = new DroneSwarm();
        swarm.Become(SwarmShape.Lamp, 0, 0);
        Assert.Equal(1, swarm.Form(5));
        swarm.Become(SwarmShape.Idle, 0, 5);
        Assert.Equal((SwarmShape.Lamp, 1f), (swarm.Leaving(5).Shape, swarm.Leaving(5).Form));
        Assert.InRange(swarm.Leaving(5 + DroneInk.Form / 2).Form, 0.1f, 0.9f);
        Assert.Equal(0, swarm.Leaving(5 + DroneInk.Form).Form);
        Assert.Equal(0, swarm.Form(5.3));
        swarm.Become(SwarmShape.Sign, 0, 6);
        Assert.InRange(swarm.Form(6 + DroneInk.Form / 2), 0.1f, 0.9f);
        Assert.Equal(1, swarm.Form(6 + DroneInk.Form));
    }

    [Fact]
    public void ASignStandsUpSwayingUnlessPointed()
    {
        var swarm = new DroneSwarm();
        swarm.Become(SwarmShape.Sign, 2, 0);
        var turns = Enumerable.Range(0, 200).Select(k => swarm.TurnOf(SwarmShape.Sign, 2, k * 0.01)).ToArray();
        Assert.All(turns, a => Assert.InRange(a, -MathF.PI / 2 - DroneInk.FanSway - 1e-4f, -MathF.PI / 2 + DroneInk.FanSway + 1e-4f));
        Assert.True(turns.Max() - turns.Min() > DroneInk.FanSway, "it sways");
        swarm.Become(SwarmShape.Sign, 2, 1, pointed: 0.6f);
        Assert.Equal(0.6f, swarm.TurnOf(SwarmShape.Sign, 2, 1.5));
    }

    [Fact]
    public void ShapeSparksBurnHotterThanOrbitingOnesAndHalfTheOrbitsFlicker()
    {
        var swarm = new DroneSwarm();
        swarm.Become(SwarmShape.Lamp, 0, 0);
        for (double t = 2; t < 4; t += 0.07)
            for (int i = 0; i < Sparks; i++)
            {
                float heat = swarm.SparkAt(i, 0, t).Heat;
                if (DroneSwarm.RankOf(i) >= 0) Assert.InRange(heat, DroneInk.OnShape - DroneInk.OnShapeSwing + DroneInk.LampMore - 1e-4f, 1.08f);
                else Assert.InRange(heat, DroneInk.Dim, DroneInk.Bright + DroneInk.BrightSwing + 1e-4f);
            }
        int bright = Enumerable.Range(0, Sparks).Count(i => DroneSwarm.Hash(i + 3) < DroneInk.BrightShare);
        Assert.InRange(bright, Sparks / 3, Sparks * 2 / 3);
    }

    [Fact]
    public void EverySparkStaysNearTheCore()
    {
        foreach (var shape in Shapes)
        {
            var swarm = new DroneSwarm();
            swarm.Become(shape, -2.2f, 0);
            for (double t = 0; t < 8; t += 0.05)
                foreach (var spark in All(swarm, -2.2f, t))
                    Assert.InRange(MathF.Sqrt(spark.X * spark.X + spark.Y * spark.Y), 0, 24);
        }
    }

    [Fact]
    public void TheOrbitsNeverStopAndTheMiddleOneRunsTheOtherWay()
    {
        for (int i = 0; i < Sparks; i += 7)
            for (float t = 0; t < 20; t += 0.1f)
            {
                var (x0, y0, _, _, _) = DroneSwarm.Orbit(i, t);
                var (x1, y1, _, _, _) = DroneSwarm.Orbit(i, t + 0.05f);
                Assert.True(MathF.Abs(x1 - x0) + MathF.Abs(y1 - y0) > 0.05f, $"spark {i} still at {t} s");
            }
        float Spin(int i)
        {
            var a = DroneSwarm.Orbit(i, 0);
            var b = DroneSwarm.Orbit(i, 0.1f);
            return a.X * b.Y - a.Y * b.X;
        }
        Assert.True(Spin(0) * Spin(1) < 0);
        Assert.True(Spin(0) * Spin(2) > 0);
    }
}
