using Stillwake.Game.Features.Crew;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>The light drone's shards fly into a shape per mode (drone-swarm).</summary>
public class DroneSwarmTests
{
    private const int N = DroneSwarm.Tune.Shards;
    private static readonly SwarmShape[] Shapes = [SwarmShape.Idle, SwarmShape.Lamp, SwarmShape.Scan, SwarmShape.Sign];
    private static readonly float[] Aims = [0, 1.2f, MathF.PI, -2.5f, -MathF.PI / 2];

    private static float Apart(Shard a, Shard b) => MathF.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));

    [Fact]
    public void EveryShapeKeepsEveryShardWithinTheOldDiscsBandOfTheCore()
    {
        foreach (var shape in Shapes)
            foreach (float aim in Aims)
                for (float t = 0; t < 30; t += 0.37f)
                    for (int i = 0; i < N; i++)
                    {
                        var shard = DroneSwarm.In(shape, i, aim, t);
                        // The disc's band reached 5.1 px: the swarm is no wider than what it replaces.
                        Assert.InRange(MathF.Sqrt(shard.X * shard.X + shard.Y * shard.Y), 0, 5.1f);
                    }
    }

    [Fact]
    public void AChangeEndsExactlyInTheNewShapeAfterTheLastDelayAndTheFlight()
    {
        var swarm = new DroneSwarm();
        swarm.Become(SwarmShape.Lamp, 0.4f, 1000);
        double done = 1000 + DroneSwarm.SettleMs;
        Assert.True(swarm.At(N - 1, 0.4f, done - 1).Flight < 1);
        for (int i = 0; i < N; i++)
        {
            var at = swarm.At(i, 0.4f, done);
            Assert.Equal(DroneSwarm.In(SwarmShape.Lamp, i, 0.4f, (float)(done / 1000)), at);
            Assert.Equal(1, at.Flight);
        }
    }

    [Fact]
    public void ShardsLeaveLateByTheirIndexTravelThenRestAndNoneIsMidFlightOnceDone()
    {
        var swarm = new DroneSwarm();
        swarm.Become(SwarmShape.Scan, 0, 0);
        Assert.All(Enumerable.Range(1, N - 1), i => Assert.Equal(0, swarm.At(i, 0, 0).Flight));
        // A moment in, the first has left, the last still waits.
        Assert.InRange(swarm.At(0, 0, 100).Flight, 0.01f, 0.99f);
        Assert.Equal(0, swarm.At(N - 1, 0, 100).Flight);
        for (double ms = DroneSwarm.SettleMs; ms < DroneSwarm.SettleMs + 5000; ms += 97)
            for (int i = 0; i < N; i++) Assert.Equal(1, swarm.At(i, 0, ms).Flight);
    }

    [Fact]
    public void AFlightArcsOffTheStraightLineAndOvershootsHome()
    {
        var swarm = new DroneSwarm();
        swarm.Become(SwarmShape.Sign, 0, 0);
        var from = DroneSwarm.In(SwarmShape.Idle, 0, 0, 0.3f);
        var to = DroneSwarm.In(SwarmShape.Sign, 0, 0, 0.3f);
        var mid = swarm.At(0, 0, 300);
        float cross = (to.X - from.X) * (mid.Y - from.Y) - (to.Y - from.Y) * (mid.X - from.X);
        Assert.True(MathF.Abs(cross) / Apart(from, to) > 0.3f, "mid-flight it bows off the line");
        // Late in its flight the back-out passes home before it settles.
        bool past = false;
        for (double ms = 400; ms < 600; ms += 5)
        {
            var at = swarm.At(0, 0, ms);
            float along = ((at.X - from.X) * (to.X - from.X) + (at.Y - from.Y) * (to.Y - from.Y)) / (Apart(from, to) * Apart(from, to));
            past |= along > 1.01f;
        }
        Assert.True(past);
    }

    [Fact]
    public void TheLampsDishSitsBehindTheCoreOpenTowardTheAimOnBothSides()
    {
        foreach (float aim in (float[])[0, MathF.PI, 0.7f, -2.2f])
        {
            var (ax, ay) = (MathF.Cos(aim), MathF.Sin(aim));
            float Ahead(Shard s) => s.X * ax + s.Y * ay;
            var middle = DroneSwarm.In(SwarmShape.Lamp, N / 2, aim, 3);
            Assert.True(Ahead(middle) < -1, "the dish's middle is behind the core");
            foreach (int edge in (int[])[0, N - 1])
                Assert.True(Ahead(DroneSwarm.In(SwarmShape.Lamp, edge, aim, 3)) > Ahead(middle) + 1, "its rims curve toward the aim");
            Assert.All(Enumerable.Range(0, N), i => Assert.False(DroneSwarm.In(SwarmShape.Lamp, i, aim, 3).Front));
        }
    }

    [Fact]
    public void IdleShardsNeverStopMoving()
    {
        for (float t = 0; t < 60; t += 0.05f)
            for (int i = 0; i < N; i++)
            {
                Shard a = DroneSwarm.In(SwarmShape.Idle, i, 0, t), b = DroneSwarm.In(SwarmShape.Idle, i, 0, t + 0.05f);
                Assert.True(Apart(a, b) > 0.01f, $"shard {i} still at {t} s");
            }
    }

    [Fact]
    public void AShapeHeldStaysPutUpToItsOwnMotion()
    {
        var swarm = new DroneSwarm();
        swarm.Become(SwarmShape.Lamp, 1, 0);
        var settled = swarm.At(3, 1, 2000);
        for (double ms = 2000; ms < 20000; ms += 333)
        {
            Assert.Equal(settled, swarm.At(3, 1, ms));
            for (int i = 0; i < N; i++) Assert.Equal(DroneSwarm.In(SwarmShape.Lamp, i, 1, (float)(ms / 1000)), swarm.At(i, 1, ms));
        }
        swarm.Become(SwarmShape.Scan, 1, 20000);
        for (double ms = 22000; ms < 30000; ms += 333)
            for (int i = 0; i < N; i++) Assert.Equal(DroneSwarm.In(SwarmShape.Scan, i, 1, (float)(ms / 1000)), swarm.At(i, 1, ms));
    }

    [Fact]
    public void AChangeMidFlightTakesTheShardsOnFromWhereTheyAre()
    {
        var swarm = new DroneSwarm();
        swarm.Become(SwarmShape.Scan, 0, 0);
        var before = Enumerable.Range(0, N).Select(i => swarm.At(i, 0, 300)).ToArray();
        swarm.Become(SwarmShape.Sign, 0, 300);
        for (int i = 0; i < N; i++) Assert.True(Apart(before[i], swarm.At(i, 0, 300)) < 1e-4f, $"shard {i} jumped");
        for (int i = 0; i < N; i++) Assert.Equal(DroneSwarm.In(SwarmShape.Sign, i, 0, (float)((300 + DroneSwarm.SettleMs) / 1000)), swarm.At(i, 0, 300 + DroneSwarm.SettleMs));
    }

    [Fact]
    public void TheScannersCombAndTheSignsFanGlowAndStandBeforeTheCore()
    {
        foreach (var shape in (SwarmShape[])[SwarmShape.Scan, SwarmShape.Sign])
            for (int i = 0; i < N; i++)
            {
                var shard = DroneSwarm.In(shape, i, 0.3f, 2);
                Assert.True(shard.Front);
                Assert.Equal(1, shard.Glow);
            }
        // The fan opens up, toward the hologram over the drone, whatever the lens aims at.
        Assert.All(Enumerable.Range(0, N), i => Assert.True(DroneSwarm.In(SwarmShape.Sign, i, 0.3f, 2).Y < 0));
    }
}
