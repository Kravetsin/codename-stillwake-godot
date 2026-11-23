using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Stillwake.Sim.Creatures;
using Stillwake.Sim.Debris;
using Xunit;
using Spec = Stillwake.Content;

namespace Stillwake.Sim.Tests;

/// <summary>
/// Tumbling pieces (docs/plans/debris-physics.md, step 1): every piece keeps its angle and spin in the sim, every hit lands on its
/// box turned by that angle (<see cref="TurnedBox"/>), and a creature clamped on a wreck stands on it, turned with it.
/// </summary>
public class TumbleTests
{
    private const int Unit = DebrisModule.Unit;
    private const int Quarter = 16384, Eighth = 8192, Half = 32768;
    private static readonly string Axe = Spec.Weapons.Axe.Item, Nailer = Spec.Shots.Nailer.Item;
    private static readonly int Nail = Spec.Shots.Nailer.Damage + Spec.Shots.Nail.Damage;

    /// <summary>Headings (<see cref="Spec.Headings"/>): along +x, down and to the right, along −x, up and to the right.</summary>
    private const int Right = 0, DownRight = 32, Left = 128, UpRight = 224;

    /// <summary>A still wreck of <paramref name="item"/> put out by hand in the middle of cell (<paramref name="x"/>, <paramref name="y"/>), turned by <paramref name="angle"/>.</summary>
    private static Piece Wreck(Scenario w, int x, int y, string item, int angle = 0, int spin = 0)
    {
        var p = BarrelsTests.Put(w, x, y, item: item);
        (p.Angle, p.Spin) = (angle, spin);
        return p;
    }

    private static TurnedBox Shape(Scenario w, Piece p) => w.M.Debris.Shape(p)!.Value;

    [Fact]
    public void AnAngleAdvancesByItsSpinEachTickAndWrapsRoundTheTurn()
    {
        var w = ShotsTests.Space();
        var up = Wreck(w, 40, 10, "cableRack", angle: 65_500, spin: 50);
        var down = Wreck(w, 60, 10, "cableRack", angle: 10, spin: -30);
        w.Run();
        Assert.Equal((14, 65_516), (up.Angle, down.Angle));
        w.Run(3);
        Assert.Equal((164, 65_426), (up.Angle, down.Angle));
    }

    [Fact]
    public void ANewPieceStartsAtItsIdsAngleAndSpinAndKeepsTumblingThatWay()
    {
        var w = new Scenario(Registry.Wreck with { Meteors = false });
        var seen = new Dictionary<int, (int Angle, int Spin)>();
        for (int tick = 0; tick < 6 * Spec.Debris.Every; tick++)
        {
            w.Run();
            foreach (var p in w.M.Debris.Pieces)
            {
                if (seen.TryGetValue(p.Id, out var was))
                {
                    Assert.Equal((was.Angle + p.Spin) & 0xFFFF, p.Angle);
                    Assert.Equal(was.Spin, p.Spin);
                }
                // Those a world begins with tumbled once on their first tick; every later one is seen as it spawned.
                else if (tick > 0) Assert.Equal(DebrisModule.TumbleOf(p.Id, p.Item), (p.Angle, p.Spin));
                seen[p.Id] = (p.Angle, p.Spin);
            }
        }
        Assert.True(seen.Count > Spec.Debris.AtStart, $"{seen.Count} pieces seen");
    }

    [Fact]
    public void SmallPiecesTumbleFastAndBigWrecksSlowlyEitherWayAtAnyAngle()
    {
        var small = Enumerable.Range(1, 2000).Select(id => DebrisModule.TumbleOf(id, "scrapChunk")).ToList();
        var rack = Enumerable.Range(1, 2000).Select(id => DebrisModule.TumbleOf(id, "cableRack")).ToList();
        var hold = Enumerable.Range(1, 2000).Select(id => DebrisModule.TumbleOf(id, "cargoHold")).ToList();
        Assert.All(small, t => Assert.InRange(Math.Abs(t.Spin), Spec.Debris.TumbleMax / 2, Spec.Debris.TumbleMax));
        // A 4-cell wreck at most half the fastest; a 12-cell derelict near the slowest.
        Assert.All(rack, t => Assert.InRange(Math.Abs(t.Spin), 40, 80));
        Assert.All(hold, t => Assert.InRange(Math.Abs(t.Spin), Spec.Debris.TumbleMin, 26));
        Assert.Contains(small, t => t.Spin < 0);
        Assert.Contains(small, t => t.Spin > 0);
        Assert.True(small.Select(t => t.Angle / 4096).Distinct().Count() == 16, "angles spread round the turn");
        Assert.Equal(small[41], DebrisModule.TumbleOf(42, "scrapChunk"));
    }

    // The fan.

    [Fact]
    public void OnAWreckTurnedAnEighthAFanThroughItsUprightCornerMissesAndOneThroughItsTurnedTipHits()
    {
        var w = ShotsTests.Space();
        var pod = Wreck(w, 20, 10, "podCluster");
        // From the upright box's top right corner cell, aimed out: a cell of it upright, past the turned box's edge.
        (int X, int Y) corner = (24_500, 6_500), tip = (25_500, 10_500);
        Assert.Equal([pod], w.M.Debris.Struck(Axe, corner.X, corner.Y, UpRight));
        Assert.Empty(w.M.Debris.Struck(Axe, tip.X, tip.Y, Right));
        pod.Angle = Eighth;
        Assert.Empty(w.M.Debris.Struck(Axe, corner.X, corner.Y, UpRight));
        Assert.Equal([pod], w.M.Debris.Struck(Axe, tip.X, tip.Y, Right));
        w.Do(new HewWrecks(Axe, tip.X, tip.Y, Right)).Run();
        Assert.Empty(w.Refusals);
        Assert.Equal(Spec.Weapons.Axe.Damage, pod.Damage);
    }

    [Fact]
    public void AnOldReelOnATumblingWreckPullsItInTurningAndAHewLandsOnItsTurnedShape()
    {
        var w = ShotsTests.Space();
        var hold = Wreck(w, 30, 10, "cargoHold", spin: 820);
        // Eight cells at the reel's speed: ten ticks, an eighth of a turn and a little.
        w.Do(new Reel(hold.Id, 38_500, 10_500)).Run(10);
        Assert.Null(hold.Reel);
        Assert.Equal((38_500, 10_500, 8_200), (hold.X, hold.Y, hold.Angle));
        var shape = Shape(w, hold);
        var (x, y) = shape.At(shape.HalfW - 300, 0);
        int cell = y / Unit * w.M.Grid.Width + x / Unit;
        Assert.False(TurnedBox.Of(hold.Id, hold.X, hold.Y, shape.HalfW, shape.HalfH, 0).Covers(cell, w.M.Grid.Width), "the tip is off the upright box");
        w.Do(new HewWrecks(Axe, x, y, DownRight)).Run();
        Assert.Empty(w.Refusals);
        Assert.Equal(Spec.Weapons.Axe.Damage, hold.Damage);
    }

    // Shots and bursts.

    [Fact]
    public void ANailMeetsTheTurnedBoxWhereTheUprightOneIsNot()
    {
        Piece Shot(int angle)
        {
            var w = ShotsTests.Space();
            var hold = Wreck(w, 60, 10, "cargoHold", angle);
            // Along row 5: over the upright hold's top (rows 7 to 13), through the one turned a quarter (rows 4 to 16).
            w.Do(new Fire(Nailer, 40 * Unit + 500, 5 * Unit + 500, Right)).Run(20);
            return hold;
        }
        Assert.Equal(0, Shot(0).Damage);
        Assert.Equal(Nail, Shot(Quarter).Damage);
    }

    [Fact]
    public void ABurstReachesAWreckByItsGapToTheTurnedBox()
    {
        Piece Burst(int angle)
        {
            var w = ShotsTests.Space();
            var hold = Wreck(w, 30, 10, "cargoHold", angle);
            var drum = BarrelsTests.Put(w, 30, 0, damage: Spec.Wreckage.HpOf("fuelDrum")!.Value - 1);
            // Six cells' reach from (30.5, 0.5): the upright hold's top is 7 off, the one turned a quarter's 4.
            Assert.Equal(7_000, TurnedBox.Of(hold.Id, hold.X, hold.Y, 6_000, 3_000, 0).Gap(30_500, 500));
            w.Do(new HewPieces(Axe, [drum.Id])).Run();
            Assert.Empty(w.Refusals);
            return hold;
        }
        Assert.Equal(0, Burst(0).Damage);
        Assert.Equal(Spec.Blasting.Drum.WreckDamage, Burst(Quarter).Damage);
    }

    // Creatures on a turning wreck.

    [Fact]
    public void ASentryRidesItsTurnedPerchAndANailMeetsItsTurnedBody()
    {
        var (w, piece, sentry) = SentryTests.OnAWreck();
        w.GiveItems(0, (Nailer, 1), ("nail", 20)).Wields(Nailer).Run();
        Assert.Equal((500, -3_000), Shape(w, piece).Local(sentry.X, sentry.Y));
        // A quarter turn clockwise: its top faces right, the sentry standing on it, its head to the right.
        piece.Angle = Quarter;
        w.Run();
        Assert.Equal((sentry.Recall(ClingerBody.Along), -3_000), Shape(w, piece).Local(sentry.X, sentry.Y));
        Assert.Equal(83_000, sentry.X);
        Assert.Equal(Quarter, w.M.Creatures.StanceOf(sentry));
        var box = w.M.Creatures.BoxOf(sentry);
        Assert.Equal((sentry.X + 1_500, sentry.Y), (box.X, box.Y));
        // Row 41 is under its feet upright, through its body turned: a nail along it from the right meets it before its wreck.
        int row = sentry.Y / Unit + 1;
        Assert.True(box.Covers(row * 160 + 85, 160));
        Assert.False(TurnedBox.Of(sentry.Id, sentry.X, sentry.Y - 1_500, 1_500, 1_500, 0).Covers(row * 160 + 85, 160));
        w.Do(new Fire(Nailer, 95 * Unit + 500, row * Unit + 500, Left)).Run(10);
        Assert.Equal(Nail, sentry.Damage);
        Assert.Equal(0, piece.Damage);
    }

    // Saves.

    [Fact]
    public void AFormatSeventyTwoSaveGivesEveryPieceItsIdsAngleAndSpin()
    {
        string save = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "format-72.json"));
        var m = new Registry();
        World.Load(Migrations.Upgrade(save), m.All);
        Assert.NotEmpty(m.Debris.Pieces);
        Assert.All(m.Debris.Pieces, p => Assert.Equal(DebrisModule.TumbleOf(p.Id, p.Item), (p.Angle, p.Spin)));
    }

    // Invariants.

    private static Gen<(int X, int Y, int HalfW, int HalfH, int Angle)> Boxes =>
        from x in Gen.Choose(0, 500_000)
        from y in Gen.Choose(0, 300_000)
        from halfW in Gen.Choose(1, 8_000)
        from halfH in Gen.Choose(1, 8_000)
        from angle in Gen.Choose(0, 0xFFFF)
        select (x, y, halfW, halfH, angle);

    /// <summary>Every piece of a world running on, of every zone, keeps its angle within the turn and its spin within the content's bounds.</summary>
    [Property(MaxTest = 8)]
    public Property EveryPiecesAngleIsWithinTheTurnAndItsSpinWithinItsBounds()
    {
        return Prop.ForAll(Gen.Choose(1, 10_000).ToArbitrary(), seed =>
        {
            var w = new Scenario(Registry.Wreck with { Meteors = false }, (uint)seed);
            for (int i = 0; i < 4; i++)
            {
                w.Run(Spec.Debris.Every);
                foreach (var p in w.M.Debris.Pieces)
                {
                    Assert.InRange(p.Angle, 0, 0xFFFF);
                    Assert.InRange(Math.Abs(p.Spin), Spec.Debris.TumbleMin, Spec.Debris.TumbleMax);
                }
            }
        });
    }

    /// <summary>A turned box's gap to its own middle is none, and it covers its middle's cell.</summary>
    [Property(MaxTest = 500)]
    public Property ATurnedBoxsGapToItsMiddleIsNone() =>
        Prop.ForAll(Boxes.ToArbitrary(), b =>
        {
            var box = TurnedBox.Of(1, b.X, b.Y, b.HalfW, b.HalfH, b.Angle);
            Assert.Equal(0, box.Gap(b.X, b.Y));
            Assert.True(box.Covers(b.Y / Unit * 1000 + b.X / Unit, 1000));
        });

    /// <summary>A box turned half a turn more is the same box: every point's gap the same, every cell covered the same.</summary>
    [Property(MaxTest = 500)]
    public Property ABoxTurnedHalfATurnGivesTheSameGaps()
    {
        var gen = from b in Boxes from dx in Gen.Choose(-20_000, 20_000) from dy in Gen.Choose(-20_000, 20_000) select (b, dx, dy);
        return Prop.ForAll(gen.ToArbitrary(), t =>
        {
            var (b, dx, dy) = t;
            var one = TurnedBox.Of(1, b.X, b.Y, b.HalfW, b.HalfH, b.Angle);
            var other = TurnedBox.Of(1, b.X, b.Y, b.HalfW, b.HalfH, b.Angle + Half);
            int x = Math.Max(0, b.X + dx), y = Math.Max(0, b.Y + dy);
            Assert.Equal(one.Gap(x, y), other.Gap(x, y));
            Assert.Equal(one.Covers(y / Unit * 1000 + x / Unit, 1000), other.Covers(y / Unit * 1000 + x / Unit, 1000));
        });
    }
}
