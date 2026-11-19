using Stillwake.Game.Features.Crew;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>The light drone's soot and plates re-form into a shape per mode (drone-swarm, «Пепел в латах»).</summary>
public class DroneSwarmTests
{
    private const int Plates = DroneSwarm.Tune.Plates, Motes = DroneSwarm.Tune.Motes;
    private static readonly SwarmShape[] Shapes = [SwarmShape.Idle, SwarmShape.Lamp, SwarmShape.Scan, SwarmShape.Sign];
    private static readonly float[] Aims = [0, 1.2f, MathF.PI, -2.5f, -MathF.PI / 2];

    /// <summary>How far from the core anything of it reaches, world px: the comb's teeth at their longest (the pick's 23 canvas px).</summary>
    private const float Reach = 6;

    private static float Apart(Plate a, Plate b) => MathF.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));
    private static float Apart(Mote a, Mote b) => MathF.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));
    private static float Far(float x, float y) => MathF.Sqrt(x * x + y * y);

    [Fact]
    public void EveryShapeKeepsEveryPlateAndMoteWithinReachOfTheCore()
    {
        foreach (var shape in Shapes)
            foreach (float aim in Aims)
                for (float t = 0; t < 30; t += 0.37f)
                {
                    for (int i = 0; i < Plates; i++)
                    {
                        var plate = DroneSwarm.PlateIn(shape, i, aim, t);
                        Assert.InRange(Far(plate.X, plate.Y), 0, Reach);
                    }
                    for (int i = 0; i < Motes; i++)
                    {
                        var mote = DroneSwarm.MoteIn(shape, i, aim, t);
                        Assert.InRange(Far(mote.X, mote.Y), 0, Reach);
                        Assert.InRange(mote.Side, 0.1f, 0.5f);
                    }
                }
    }

    [Fact]
    public void ReFormingKeepsEverythingNearTheCoreTooOvershootAndArcIncluded()
    {
        foreach (var (from, to) in (ValueTuple<SwarmShape, SwarmShape>[])[(SwarmShape.Idle, SwarmShape.Lamp), (SwarmShape.Lamp, SwarmShape.Scan), (SwarmShape.Scan, SwarmShape.Sign), (SwarmShape.Sign, SwarmShape.Lamp)])
        {
            var swarm = new DroneSwarm();
            swarm.Become(from, 0.5f, 0);
            swarm.Become(to, 0.5f, 5000);
            for (double ms = 5000; ms < 5000 + DroneSwarm.SettleMs; ms += 23)
            {
                for (int i = 0; i < Plates; i++) Assert.InRange(Far(swarm.PlateAt(i, 0.5f, ms).X, swarm.PlateAt(i, 0.5f, ms).Y), 0, Reach + 1.5f);
                for (int i = 0; i < Motes; i++) Assert.InRange(Far(swarm.MoteAt(i, 0.5f, ms).X, swarm.MoteAt(i, 0.5f, ms).Y), 0, Reach);
            }
        }
    }

    [Fact]
    public void AChangeEndsExactlyInTheNewShapeAfterItsLastDelays()
    {
        var swarm = new DroneSwarm();
        swarm.Become(SwarmShape.Lamp, 0.4f, 1000);
        Assert.True(swarm.PlateAt(Plates - 1, 0.4f, 1000 + DroneSwarm.PlatesSettleMs - 1).Flight < 1);
        Assert.Contains(Enumerable.Range(0, Motes), i => swarm.MoteAt(i, 0.4f, 1000 + DroneSwarm.DustSettleMs - 30) != DroneSwarm.MoteIn(SwarmShape.Lamp, i, 0.4f, (float)((1000 + DroneSwarm.DustSettleMs - 30) / 1000)));
        double done = 1000 + DroneSwarm.SettleMs;
        float t = (float)(done / 1000);
        for (int i = 0; i < Plates; i++)
        {
            var at = swarm.PlateAt(i, 0.4f, done);
            Assert.Equal(DroneSwarm.PlateIn(SwarmShape.Lamp, i, 0.4f, t), at);
            Assert.Equal(1, at.Flight);
        }
        for (int i = 0; i < Motes; i++) Assert.Equal(DroneSwarm.MoteIn(SwarmShape.Lamp, i, 0.4f, t), swarm.MoteAt(i, 0.4f, done));
    }

    [Fact]
    public void PlatesLeaveLateByTheirIndexAndNoneIsMidFlightOnceDone()
    {
        var swarm = new DroneSwarm();
        swarm.Become(SwarmShape.Scan, 0, 0);
        Assert.All(Enumerable.Range(1, Plates - 1), i => Assert.Equal(0, swarm.PlateAt(i, 0, 0).Flight));
        // A moment in, the first has left, the last still waits.
        Assert.InRange(swarm.PlateAt(0, 0, 100).Flight, 0.01f, 0.99f);
        Assert.Equal(0, swarm.PlateAt(Plates - 1, 0, 100).Flight);
        for (double ms = DroneSwarm.PlatesSettleMs; ms < DroneSwarm.PlatesSettleMs + 5000; ms += 97)
            for (int i = 0; i < Plates; i++) Assert.Equal(1, swarm.PlateAt(i, 0, ms).Flight);
    }

    [Fact]
    public void AFlightArcsOffTheStraightLineAndOvershootsHome()
    {
        var swarm = new DroneSwarm();
        swarm.Become(SwarmShape.Sign, 0, 0);
        var from = DroneSwarm.PlateIn(SwarmShape.Idle, 0, 0, 0.3f);
        var to = DroneSwarm.PlateIn(SwarmShape.Sign, 0, 0, 0.3f);
        var mid = swarm.PlateAt(0, 0, 300);
        float cross = (to.X - from.X) * (mid.Y - from.Y) - (to.Y - from.Y) * (mid.X - from.X);
        Assert.True(MathF.Abs(cross) / Apart(from, to) > 0.3f, "mid-flight it bows off the line");
        // Late in its flight the back-out passes home before it settles.
        bool past = false;
        for (double ms = 420; ms < 650; ms += 5)
        {
            var at = swarm.PlateAt(0, 0, ms);
            float along = ((at.X - from.X) * (to.X - from.X) + (at.Y - from.Y) * (to.Y - from.Y)) / (Apart(from, to) * Apart(from, to));
            past |= along > 1.01f;
        }
        Assert.True(past);
    }

    [Fact]
    public void AFlyingPlateTurnsOverItsKPassingThroughNoughtShowingItsBackMidFlight()
    {
        var swarm = new DroneSwarm();
        swarm.Become(SwarmShape.Lamp, 0, 0);
        swarm.Become(SwarmShape.Scan, 0, 5000);
        for (int i = 0; i < Plates; i++)
        {
            var flips = new List<float>();
            for (double ms = 5000; ms <= 5000 + DroneSwarm.PlatesSettleMs; ms += 5) flips.Add(swarm.PlateAt(i, 0, ms).Flip);
            Assert.Equal(1, flips[0], 3);
            Assert.Equal(1, flips[^1], 3);
            Assert.Contains(flips, k => k < -0.9f);
            // From face on to its back and back again, through edge-on each time.
            int crossings = flips.Zip(flips.Skip(1)).Count(p => MathF.Sign(p.First) != MathF.Sign(p.Second));
            Assert.Equal(2, crossings);
        }
    }

    [Fact]
    public void AShapeHoldsItsPlatesFaceOnAndIdleOnesTumble()
    {
        foreach (var shape in (SwarmShape[])[SwarmShape.Lamp, SwarmShape.Scan, SwarmShape.Sign])
            for (int i = 0; i < Plates; i++) Assert.Equal(1, DroneSwarm.PlateIn(shape, i, 0.3f, 7).Flip);
        for (int i = 0; i < Plates; i++)
        {
            var flips = Enumerable.Range(0, 400).Select(k => DroneSwarm.PlateIn(SwarmShape.Idle, i, 0, k * 0.1f).Flip).ToArray();
            Assert.True(flips.Min() < -0.9f && flips.Max() > 0.9f, $"plate {i} never turned over");
        }
    }

    [Fact]
    public void TheLampsDishAndMirrorSitBehindTheCoreOpenTowardTheAimOnBothSides()
    {
        foreach (float aim in (float[])[0, MathF.PI, 0.7f, -2.2f])
        {
            var (ax, ay) = (MathF.Cos(aim), MathF.Sin(aim));
            float Ahead(float x, float y) => x * ax + y * ay;
            var middle = DroneSwarm.PlateIn(SwarmShape.Lamp, 1, aim, 3);
            Assert.True(Ahead(middle.X, middle.Y) < -1, "the dish's middle is behind the core");
            foreach (int edge in (int[])[0, Plates - 1])
            {
                var rim = DroneSwarm.PlateIn(SwarmShape.Lamp, edge, aim, 3);
                Assert.True(Ahead(rim.X, rim.Y) > Ahead(middle.X, middle.Y) + 0.5f, "its rims curve toward the aim");
            }
            Assert.All(Enumerable.Range(0, Plates), i => Assert.False(DroneSwarm.PlateIn(SwarmShape.Lamp, i, aim, 3).Front));
            Assert.All(Enumerable.Range(0, Motes), i => Assert.False(DroneSwarm.MoteIn(SwarmShape.Lamp, i, aim, 3).Front));
            // The mirror's lit face is a share of its soot, as the pick's.
            int lit = Enumerable.Range(0, Motes).Count(i => DroneSwarm.MoteIn(SwarmShape.Lamp, i, aim, 3).Glow > 0);
            Assert.InRange(lit, Motes / 8, Motes / 3);
        }
    }

    [Fact]
    public void TheCombsTeethReachAheadAndGlowMostAtTheTips()
    {
        var teeth = Enumerable.Range(0, Motes).Select(i => DroneSwarm.MoteIn(SwarmShape.Scan, i, 0, 2)).ToArray();
        Assert.All(teeth, m => Assert.True(m.Front && m.X > 1.5f, "the comb stands before the core"));
        var far = teeth.OrderByDescending(m => m.X).Take(20).ToArray();
        var near = teeth.OrderBy(m => m.X).Take(20).ToArray();
        Assert.True(far.Average(m => m.Glow) > near.Average(m => m.Glow) + 0.3f);
        Assert.True(far.Average(m => m.Side) < near.Average(m => m.Side));
    }

    [Fact]
    public void IdleOrbitsNeverStopMovingAndTheMiddleOneRunsTheOtherWay()
    {
        for (float t = 0; t < 60; t += 0.05f)
        {
            for (int i = 0; i < Plates; i++)
                Assert.True(Apart(DroneSwarm.PlateIn(SwarmShape.Idle, i, 0, t), DroneSwarm.PlateIn(SwarmShape.Idle, i, 0, t + 0.05f)) > 0.005f, $"plate {i} still at {t} s");
            for (int i = 0; i < Motes; i += 7)
                Assert.True(Apart(DroneSwarm.MoteIn(SwarmShape.Idle, i, 0, t), DroneSwarm.MoteIn(SwarmShape.Idle, i, 0, t + 0.05f)) > 0.005f, $"mote {i} still at {t} s");
        }
        // Three orbits, each tilted its own way: no two motes of different orbits share a tilt.
        float Tilt(int i)
        {
            var a = DroneSwarm.MoteIn(SwarmShape.Idle, i, 0, 0);
            var b = DroneSwarm.MoteIn(SwarmShape.Idle, i, 0, 1.3f);
            return MathF.Atan2(a.X * b.Y - a.Y * b.X, a.X * b.X + a.Y * b.Y);
        }
        Assert.True(Tilt(0) * Tilt(1) < 0, "the second orbit runs against the first");
    }

    [Fact]
    public void TheSignsSootKeepsItsIdleOrbitsWhileThePlatesFanUpGlowing()
    {
        var swarm = new DroneSwarm();
        swarm.Become(SwarmShape.Sign, 0.3f, 0);
        for (double ms = 0; ms < 3000; ms += 111)
            for (int i = 0; i < Motes; i += 5)
                Assert.Equal(DroneSwarm.MoteIn(SwarmShape.Idle, i, 0.3f, (float)(ms / 1000)), swarm.MoteAt(i, 0.3f, ms));
        for (int i = 0; i < Plates; i++)
        {
            var plate = DroneSwarm.PlateIn(SwarmShape.Sign, i, 0.3f, 2);
            Assert.True(plate.Front && plate.Y < 0, "the fan opens up, toward the hologram, whatever the lens aims at");
            Assert.Equal(1, plate.Glow);
        }
    }

    [Fact]
    public void AShapeHeldStaysPutUpToItsOwnMotion()
    {
        var swarm = new DroneSwarm();
        swarm.Become(SwarmShape.Lamp, 1, 0);
        for (double ms = 2000; ms < 20000; ms += 333)
        {
            float t = (float)(ms / 1000);
            for (int i = 0; i < Plates; i++) Assert.Equal(DroneSwarm.PlateIn(SwarmShape.Lamp, i, 1, t), swarm.PlateAt(i, 1, ms));
            for (int i = 0; i < Motes; i += 3) Assert.Equal(DroneSwarm.MoteIn(SwarmShape.Lamp, i, 1, t), swarm.MoteAt(i, 1, ms));
        }
    }

    [Fact]
    public void AChangeMidFlightTakesEverythingOnFromWhereItIs()
    {
        var swarm = new DroneSwarm();
        swarm.Become(SwarmShape.Scan, 0, 0);
        var plates = Enumerable.Range(0, Plates).Select(i => swarm.PlateAt(i, 0, 300)).ToArray();
        var motes = Enumerable.Range(0, Motes).Select(i => swarm.MoteAt(i, 0, 300)).ToArray();
        swarm.Become(SwarmShape.Lamp, 0, 300);
        for (int i = 0; i < Plates; i++)
        {
            Assert.True(Apart(plates[i], swarm.PlateAt(i, 0, 300)) < 1e-4f, $"plate {i} jumped");
            Assert.Equal(plates[i].Flip, swarm.PlateAt(i, 0, 300).Flip, 4);
        }
        for (int i = 0; i < Motes; i++) Assert.True(Apart(motes[i], swarm.MoteAt(i, 0, 300)) < 1e-4f, $"mote {i} jumped");
        double done = 300 + DroneSwarm.SettleMs;
        for (int i = 0; i < Plates; i++) Assert.Equal(DroneSwarm.PlateIn(SwarmShape.Lamp, i, 0, (float)(done / 1000)), swarm.PlateAt(i, 0, done));
        for (int i = 0; i < Motes; i++) Assert.Equal(DroneSwarm.MoteIn(SwarmShape.Lamp, i, 0, (float)(done / 1000)), swarm.MoteAt(i, 0, done));
    }

    [Fact]
    public void AFewMotesAreRedHotFlickeringAndOnlyThoseGlowOrange()
    {
        var hot = Enumerable.Range(0, Motes).Where(i => DroneSwarm.MoteIn(SwarmShape.Idle, i, 0, 0).Heat > 0).ToArray();
        // The pick's seeds drew 13 of 170, its 7 % share.
        Assert.Equal(13, hot.Length);
        foreach (var shape in Shapes)
            for (float t = 0; t < 5; t += 0.13f)
                for (int i = 0; i < Motes; i++)
                {
                    float heat = DroneSwarm.MoteIn(shape, i, 0.8f, t).Heat;
                    if (hot.Contains(i)) Assert.InRange(heat, 0.04f, 0.86f);
                    else Assert.Equal(0, heat);
                }
        // It flickers: each hot mote swings across the glow's threshold.
        foreach (int i in hot)
        {
            var heats = Enumerable.Range(0, 60).Select(k => DroneSwarm.MoteIn(SwarmShape.Idle, i, 0, k * 0.03f).Heat).ToArray();
            Assert.True(heats.Max() - heats.Min() > 0.5f);
        }
        // And keeps its heat on the move.
        var swarm = new DroneSwarm();
        swarm.Become(SwarmShape.Lamp, 0, 0);
        Assert.All(hot, i => Assert.True(swarm.MoteAt(i, 0, 600).Heat > 0));
    }

    [Fact]
    public void TheSootIsThePicksOwnSeeds()
    {
        // The pick's dustSet(170, 530): the first mote's phase 4.1764 and r1 0.1210 put it here on the first orbit at t = 0.
        var first = DroneSwarm.MoteIn(SwarmShape.Idle, 0, 0, 0);
        float r = 2.5f + (0.12103f - 0.5f) * 0.4f, th = 4.17642f;
        float x = MathF.Cos(th) * r, y = MathF.Sin(th) * r * 0.3f, tilt = -0.3f;
        Assert.Equal(x * MathF.Cos(tilt) - y * MathF.Sin(tilt), first.X, 3);
        Assert.Equal(x * MathF.Sin(tilt) + y * MathF.Cos(tilt), first.Y, 3);
        Assert.True(first.Dark);
        Assert.False(DroneSwarm.MoteIn(SwarmShape.Idle, 1, 0, 0).Dark);
    }

    [Fact]
    public void BareKeepsExactlyTheEighteenRingMotes()
    {
        var swarm = new DroneSwarm();
        swarm.Become(SwarmShape.Lamp, 0.4f, 0);
        Assert.All(Enumerable.Range(0, Motes), i => Assert.True(swarm.Kept(i)));
        swarm.Become(SwarmShape.Lamp, 0.4f, 3000, bare: true);
        var kept = Enumerable.Range(0, Motes).Where(swarm.Kept).ToArray();
        Assert.Equal(18, kept.Length);
        Assert.Equal(DroneSwarm.Ring.SelectMany(o => o).Order(), kept);
        // Once re-formed they run the idle orbits, whatever the mode.
        double done = 3000 + DroneSwarm.DustSettleMs;
        foreach (int i in kept) Assert.Equal(DroneSwarm.MoteIn(SwarmShape.Idle, i, 0.4f, (float)(done / 1000)), swarm.MoteAt(i, 0.4f, done));
        // Dressed again, every mote is its own and flows back into the mode's shape.
        swarm.Become(SwarmShape.Lamp, 0.4f, done);
        Assert.All(Enumerable.Range(0, Motes), i => Assert.True(swarm.Kept(i)));
        double back = done + DroneSwarm.DustSettleMs;
        for (int i = 0; i < Motes; i++) Assert.Equal(DroneSwarm.MoteIn(SwarmShape.Lamp, i, 0.4f, (float)(back / 1000)), swarm.MoteAt(i, 0.4f, back));
    }

    [Fact]
    public void RingIsSixOnEachOrbit()
    {
        Assert.Equal(3, DroneSwarm.Ring.Count);
        Assert.Equal(18, DroneSwarm.Ring.SelectMany(o => o).Distinct().Count());
        for (int o = 0; o < 3; o++)
        {
            var ring = DroneSwarm.Ring[o];
            Assert.Equal(6, ring.Count);
            Assert.All(ring, i => Assert.Equal(o, i % 3));
            // In order round the orbit, spread round it: each the next by its phase, none bunched.
            float tilt = DroneSwarm.Tune.DustTilt + o * DroneSwarm.Tune.DustTiltMore;
            float AngleOf(int i)
            {
                var m = DroneSwarm.MoteIn(SwarmShape.Idle, i, 0, 0);
                float x = m.X * MathF.Cos(-tilt) - m.Y * MathF.Sin(-tilt), y = m.X * MathF.Sin(-tilt) + m.Y * MathF.Cos(-tilt);
                float a = MathF.Atan2(y / DroneSwarm.Tune.DustOrbitFlat, x);
                return a < 0 ? a + MathF.Tau : a;
            }
            var angles = ring.Select(AngleOf).ToArray();
            for (int k = 0; k < 6; k++)
            {
                float gap = angles[(k + 1) % 6] - angles[k];
                if (gap < 0) gap += MathF.Tau;
                Assert.InRange(gap, 0.3f, 2.2f);
            }
        }
    }

    [Fact]
    public void BareKeepsTheModesPlates()
    {
        foreach (var shape in (SwarmShape[])[SwarmShape.Lamp, SwarmShape.Scan])
        {
            var swarm = new DroneSwarm();
            swarm.Become(shape, 1.1f, 0);
            swarm.Become(shape, 1.1f, 4000, bare: true);
            Assert.Equal(shape, swarm.Shape);
            for (double ms = 4000; ms < 6000; ms += 77)
                for (int i = 0; i < Plates; i++) Assert.Equal(DroneSwarm.PlateIn(shape, i, 1.1f, (float)(ms / 1000)), swarm.PlateAt(i, 1.1f, ms));
        }
    }

    [Fact]
    public void ASignTakesTheSignsShapeOverEitherMode()
    {
        Assert.Equal(SwarmShape.Sign, DroneSwarm.ShapeOf(signs: true, scanning: false));
        Assert.Equal(SwarmShape.Sign, DroneSwarm.ShapeOf(signs: true, scanning: true));
        Assert.Equal(SwarmShape.Lamp, DroneSwarm.ShapeOf(signs: false, scanning: false));
        Assert.Equal(SwarmShape.Scan, DroneSwarm.ShapeOf(signs: false, scanning: true));
    }
}
