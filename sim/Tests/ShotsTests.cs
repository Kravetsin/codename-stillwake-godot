using Stillwake.Sim.Creatures;
using Stillwake.Sim.Debris;
using Stillwake.Sim.Items;
using Xunit;
using Spec = Stillwake.Content;

namespace Stillwake.Sim.Tests;

/// <summary>Shots (shots-are-the-sims): a gun's <see cref="Fire"/> spends its ammo, and the sim flies the shot to what it meets.</summary>
public class ShotsTests
{
    private const int Unit = CreaturesModule.Unit;
    private static readonly Spec.Projectile Nail = Spec.Shots.Nail;
    private static readonly string Nailer = Spec.Shots.Nailer.Item;

    /// <summary>Headings: along +x, down, and up and to the right at 45°.</summary>
    private const int Right = 0, Down = 64, UpRight = 224;

    /// <summary>
    /// Ground on the test strip, the first planet's rules, a cell a character: 100 open cells by 30 over three rows of soil, a soil
    /// pillar at column 90, bedrock at column 95, a floor at columns 50 to 53 of row 25.
    /// </summary>
    private static Scenario Ground()
    {
        var rows = new List<string>();
        var looks = new List<string>();
        for (int y = 0; y < 30; y++)
        {
            rows.Add(y == 25 ? new string('.', 50) + "====" + new string('.', 36) + "#....#...." : new string('.', 90) + "#....#....");
            looks.Add(new string('.', 90) + "s....b....");
        }
        for (int y = 0; y < 3; y++)
        {
            rows.Add(new string('#', 100));
            looks.Add(new string('s', 100));
        }
        var layout = Layout.Parse(string.Join('\n', rows)).WithMaterials(string.Join('\n', looks)) with { Kind = Spec.Surfaces.TestStrip.Id };
        return new Scenario(layout).Do(new Crew.Enter(29 * 100 + 2)).GiveItems(0, (Nailer, 1), ("nail", 20)).Run();
    }

    /// <summary>Open space aboard the ship's surface, a cell a character: 320 cells by 20, no hull, so no stream of debris.</summary>
    private static Scenario Space() =>
        new Scenario(string.Join('\n', Enumerable.Repeat(new string('.', 320), 20)), cells: true)
            .Do(new Crew.Enter(10 * 320 + 2)).GiveItems(0, (Nailer, 1), ("nail", 20)).Run();

    private static int Nails(Scenario w, int peer = 0) => w.M.Items.Totals(peer).Where(t => t.Item == "nail").Sum(t => t.Count);

    private static Fire At(int column, int row, int heading, int dx = Unit / 2, int dy = Unit / 2) => new(Nailer, column * Unit + dx, row * Unit + dy, heading);

    private static int? CrackAt(Scenario w, int cell) => w.M.Building.Cracks.FirstOrDefault(c => c.Cell == cell)?.Damage;

    [Fact]
    public void AShotSpendsOneNailAndFliesFromTheMuzzle()
    {
        var w = Space();
        w.Do(At(10, 10, Right));
        w.Run();
        Assert.Empty(w.Refusals);
        Assert.Equal(19, Nails(w));
        var shot = w.M.Creatures.Shots.Single();
        Assert.Equal((10_500 + Nail.Speed, 10_500, Nail.Speed, 0), (shot.X, shot.Y, shot.VX, shot.VY));
        Assert.Equal(Ref.Hero(0), shot.By);
    }

    [Fact]
    public void ItIsRefusedWithNoGunCarriedTheMuzzleOffTheGridAndNoNailsButSpendsNoneInCreative()
    {
        var w = Space();
        w.Do(new Fire(Spec.Weapons.Axe.Item, 10_500, 10_500, Right)).Run();
        w.Do(At(10, 10, Right) with { From = 1 }).Run();
        w.Do(new Fire(Nailer, -1, 10_500, Right)).Run();
        w.Do(new Fire(Nailer, 10_500, 20 * Unit, Right)).Run();
        Assert.Equal(["creatures.noGun", "creatures.noGun", "creatures.blocked", "creatures.blocked"], w.Refusals.Select(r => r.Reason));
        Assert.Empty(w.M.Creatures.Shots);

        var dry = Space();
        while (Nails(dry) > 0) dry.Do(At(10, 10, Right)).Run();
        dry.Do(At(10, 10, Right)).Run();
        Assert.Equal("items.notEnough", dry.Refusals.Single().Reason);
        dry.Do(new SetCreative(true)).Run();
        int shots = dry.M.Creatures.Shots.Count;
        dry.Do(At(10, 10, Right)).Run();
        Assert.Equal(shots + 1, dry.M.Creatures.Shots.Count);
        Assert.Equal(0, Nails(dry));
        dry.GiveItems(0, ("nail", 3)).Do(At(10, 10, Right)).Run();
        Assert.Equal(3, Nails(dry));
    }

    [Fact]
    public void InOpenSpaceANailFliesStraightTillItsReachRunsOut()
    {
        var w = Space();
        int ticks = Nail.Reach * Unit / Nail.Speed;
        w.Do(At(2, 10, Right)).Run(ticks - 1);
        var shot = w.M.Creatures.Shots.Single();
        Assert.Equal((0, 10_500), (shot.VY, shot.Y));
        w.Run();
        Assert.Empty(w.M.Creatures.Shots);
        // From 2.5 cells, its reach's 96 ticks at 3 cells a tick: it ends over cell 290.
        Assert.Equal(10 * 320 + 290, w.M.Creatures.Ended.Single().Cell);
    }

    [Fact]
    public void OnTheFirstPlanetANailFiredUpAt45DegreesFallsBackToTheGroundAbout30CellsOut()
    {
        var w = Ground();
        // From just over the ground's top row, so its own cell is open.
        w.Do(At(2, 29, UpRight, dy: Unit - 1));
        var ends = new List<ShotEnd>();
        for (int i = 0; i < 40 && ends.Count == 0; i++)
        {
            w.Run();
            ends.AddRange(w.M.Creatures.Ended);
        }
        int cell = ends.Single().Cell;
        Assert.Equal(30, cell / 100);
        Assert.InRange(cell % 100 - 2, 30, 31);
        Assert.Equal(Nail.BlockDamage, CrackAt(w, cell));
    }

    [Fact]
    public void ANailFiredFromInsideAWallEndsThereAtOnce()
    {
        var w = Ground();
        w.Do(At(10, 31, Right)).Run();
        Assert.Empty(w.M.Creatures.Shots);
        Assert.Equal(31 * 100 + 10, w.M.Creatures.Ended.Single().Cell);
        Assert.Equal(Nail.BlockDamage, CrackAt(w, 31 * 100 + 10));
    }

    [Fact]
    public void ThroughAnExactCornerItMeetsTheCellBesideItNotTheOneBelow()
    {
        // In open space, so it flies straight: from the middle of (1, 1), down and right at 45°, through the corner of (2, 1) and
        // (1, 2), both walls.
        var w = new Scenario("......\n..#...\n.#....\n......", cells: true).Do(new Crew.Enter(3 * 6 + 5)).GiveItems(0, (Nailer, 1), ("nail", 1)).Run();
        w.Do(At(1, 1, 32)).Run();
        Assert.Equal(1 * 6 + 2, w.M.Creatures.Ended.Single().Cell);
    }

    /// <summary>Fires a nail as <paramref name="fire"/> and runs till it ends: the cell it ended in.</summary>
    private static int EndOf(Scenario w, Fire fire)
    {
        w.Do(fire);
        for (int i = 0; i < 200; i++)
        {
            w.Run();
            if (w.M.Creatures.Ended.Count > 0) return w.M.Creatures.Ended.Single().Cell;
        }
        throw new InvalidOperationException("the nail never ended");
    }

    [Fact]
    public void SoilTakesANailsBlockDamageAndBreaksAtItsStrengthBedrockNeverAndAboardNothing()
    {
        var w = Ground();
        int pillar = EndOf(w, At(80, 20, Right));
        Assert.Equal(90, pillar % 100);
        Assert.Equal(Nail.BlockDamage, CrackAt(w, pillar));
        for (int i = 1; i < Spec.Materials.Get("soil").Strength; i++) Assert.Equal(pillar, EndOf(w, At(80, 20, Right)));
        Assert.Null(w.M.Grid.WallAt(pillar));
        Assert.Equal(1, w.M.Items.Totals(0).Where(t => t.Item == "soil").Sum(t => t.Count));
        int bedrock = EndOf(w, At(92, 20, Right));
        Assert.Equal("bedrock", w.M.Grid.WallAt(bedrock)?.Id);
        Assert.Empty(w.M.Building.Cracks);

        var aboard = new Scenario(".....#....\n..........", cells: true).Do(new Crew.Enter(2)).GiveItems(0, (Nailer, 1), ("nail", 1)).Run();
        Assert.Equal(5, EndOf(aboard, At(1, 0, Right)));
        Assert.Empty(aboard.M.Building.Cracks);
        Assert.NotNull(aboard.M.Grid.WallAt(5));
    }

    [Fact]
    public void WithItsShooterGoneFromTheSurfaceWhatItBreaksFallsThereAnyones()
    {
        var w = Ground().GiveItems(1, (Nailer, 1), ("nail", 4));
        int pillar = -1;
        for (int i = 0; i < 4; i++) pillar = EndOf(w, At(80, 20, Right) with { From = 1 });
        Assert.Null(w.M.Grid.WallAt(pillar));
        var soil = w.M.Items.Floor.Single(f => f.Item == "soil");
        Assert.Equal(FloorStack.Nobody, soil.By);
    }

    [Fact]
    public void ALoperTakesANailsDamageAndBoltsTheShotsWayNotRetaliatingFromAfar()
    {
        var w = CreaturesTesting.Planet(Map, CreaturesTesting.Lifeless, cells: true).Do(new Crew.Enter(19 * 80 + 2)).GiveItems(0, (Nailer, 1), ("nail", 5)).Run();
        var loper = w.Let(Spec.Creatures.Loper, 40, 19);
        // The muzzle is the client's word: here two cells short of the loper, its hero 38 cells off.
        w.Do(new Fire(Nailer, 38 * Unit, 18 * Unit, Right)).Run();
        Assert.Equal(Nail.Damage, loper.Damage);
        Assert.Equal(((Spec.Creatures.Walker)Spec.Creatures.Loper.Body).Run, loper.VX);
        Assert.Equal(Posture.Flee, loper.Posture);
    }

    [Fact]
    public void ANailStraightDownKnocksACreatureAwayFromItsShooter()
    {
        var w = CreaturesTesting.Planet(Map, CreaturesTesting.Lifeless, cells: true).Do(new Crew.Enter(19 * 80 + 70)).GiveItems(0, (Nailer, 1), ("nail", 5)).Run();
        var loper = w.Let(Spec.Creatures.Loper, 40, 19);
        w.Do(new Fire(Nailer, loper.X, 12 * Unit, Down)).Run(3);
        Assert.Equal(Nail.Damage, loper.Damage);
        Assert.Equal(-((Spec.Creatures.Walker)Spec.Creatures.Loper.Body).Run, loper.VX);
    }

    [Fact]
    public void OfTwoCreaturesInOneCellTheLowerIdTakesTheNailAndAHeroInTheWayIsPassed()
    {
        var w = CreaturesTesting.Planet(Map, CreaturesTesting.Lifeless, cells: true).Do(new Crew.Enter(19 * 80 + 2)).GiveItems(0, (Nailer, 1), ("nail", 5)).Run();
        var first = w.Let(Spec.Creatures.Loper, 40, 19);
        var second = w.Let(Spec.Creatures.Loper, 40, 19);
        w.Do(new Crew.Enter(19 * 80 + 36)).Do(new Fire(Nailer, 34 * Unit, 18 * Unit, Right)).Run(3);
        Assert.Equal((Nail.Damage, 0), (first.Damage, second.Damage));
    }

    /// <summary>Flat open ground, a cell a character: 80 cells by 20 over a row of walls.</summary>
    private static readonly string Map = string.Join('\n', Enumerable.Repeat(new string('.', 80), 20).Append(new string('#', 80)));

    [Fact]
    public void AWreckTakesANailsDamageAndIsStruckASmallPieceIsPassed()
    {
        var w = Space();
        var debris = (DebrisState)w.M.Debris.Slice;
        var scrap = new Piece { Id = debris.NextId++, Zone = Spec.Debris.Far.Id, Item = "scrapChunk", X = 20_500, Y = 10_500, Start = 20_500, SeenAt = debris.Clock };
        var wreck = new Piece { Id = debris.NextId++, Zone = Spec.Debris.Far.Id, Item = "engineBlock", X = 60_000, Y = 10_000, Start = 60_000, SeenAt = debris.Clock };
        debris.Pieces.AddRange([scrap, wreck]);
        SentryTests.Guarded(w);
        w.Do(At(10, 10, Right)).Run(20);
        Assert.Equal(Nail.Damage, wreck.Damage);
        Assert.Equal(0, wreck.Struck!.By);
        Assert.Contains(scrap, w.M.Debris.Pieces);
    }

    [Fact]
    public void ANailOnASentryThenAnAxeOnItsWreckTheNextTickAreBothAnswered()
    {
        var w = Space();
        var debris = (DebrisState)w.M.Debris.Slice;
        var wreck = new Piece { Id = debris.NextId++, Zone = Spec.Debris.Far.Id, Item = "engineBlock", X = 60_000, Y = 10_000, Start = 60_000, SeenAt = debris.Clock };
        debris.Pieces.Add(wreck);
        SentryTests.Guarded(w);
        w.Do(new Crew.Enter(10 * 320 + 54)).GiveItems(0, (Spec.Weapons.Axe.Item, 1)).Run();
        w.Do(new Release(Spec.Creatures.Sentry.Id, 8 * 320 + 60)).Run();
        var sentry = w.M.Creatures.Creatures.Single();
        // Fired from beside it, so the nail lands this very tick, after the debris drifted.
        w.Do(new Fire(Nailer, sentry.X - 2 * Unit, sentry.Y - Unit, Right)).Run();
        Assert.Equal(Nail.Damage, sentry.Damage);
        int answered = sentry.Recall(GuardDrive.Answered);
        w.Do(HewTesting.Hit(w, wreck.Id, Spec.Weapons.Axe.Item)).Run();
        Assert.True(wreck.Struck!.At + 1 > answered);
    }

    [Fact]
    public void ASaveMidFlightPlaysOnAsOneNeverSaved()
    {
        Scenario Fired() => Ground().Do(At(2, 29, UpRight, dy: Unit - 1)).Run(4);
        var kept = Fired();
        var loaded = Fired().Reload();
        kept.Run(20);
        loaded.Run(20);
        Assert.Equal(kept.M.Building.Cracks.Select(c => (c.Cell, c.Damage)), loaded.M.Building.Cracks.Select(c => (c.Cell, c.Damage)));
        Assert.Equal(kept.World.Hash(), loaded.World.Hash());
    }

    [Fact]
    public void ACreatureANailKillsIsGoneFromASaveMadeThatTick()
    {
        Scenario Killed()
        {
            var w = CreaturesTesting.Planet(Map, CreaturesTesting.Lifeless, cells: true).Do(new Crew.Enter(19 * 80 + 2)).GiveItems(0, (Nailer, 1), ("nail", 5)).Run();
            w.Let(Spec.Creatures.Loper, 40, 19);
            // Two nails from beside it on one tick: 12, the loper's hit points.
            return w.Do(new Fire(Nailer, 38 * Unit, 18 * Unit, Right)).Do(new Fire(Nailer, 38 * Unit, 18 * Unit, Right)).Run();
        }
        var kept = Killed();
        var loaded = Killed().ReloadOnPlanet();
        Assert.Empty(loaded.M.Creatures.Creatures);
        kept.Run();
        loaded.Run();
        Assert.Equal(kept.World.Hash(), loaded.World.Hash());
    }

    [Fact]
    public void AFloorTakesANailsBlockDamage()
    {
        var w = Ground();
        int cell = EndOf(w, At(45, 25, Right));
        Assert.Equal(25 * 100 + 50, cell);
        Assert.Equal(Nail.BlockDamage, w.M.Building.Cracks.Single().Damage);
    }

    [Fact]
    public void ANailLeavingTheGridEndsInTheLastCellItCrossed()
    {
        var w = Space();
        Assert.Equal(10 * 320 + 319, EndOf(w, At(300, 10, Right)));
    }
}
