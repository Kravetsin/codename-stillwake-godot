using Stillwake.Sim.Creatures;
using Stillwake.Sim.Debris;
using Xunit;
using Spec = Stillwake.Content;
using Tune = Stillwake.Content.Creatures;

namespace Stillwake.Sim.Tests;

/// <summary>The sentry (2026-10-26-creatures-are-composed): a robot clamped to a wreck in space, guarding it.</summary>
public class SentryTests
{
    private const int Unit = CreaturesModule.Unit;
    private static readonly Tune.Species Sentry = Tune.Sentry;
    private static readonly Tune.Pounce Pounce = (Tune.Pounce)Sentry.Moves[0];
    private static readonly Tune.Guard Guard = Sentry.Brain.OfType<Tune.Guard>().Single();
    private static readonly string Axe = Spec.Weapons.Axe.Item;

    /// <summary>Open space 160 cells by 80, a cell a character: no hull, so no stream of debris.</summary>
    private static readonly string Space = string.Join('\n', Enumerable.Repeat(new string('.', 160), 80));

    /// <summary>An engine block (10 by 6 cells) still at (80, 40), hero 0 far left, and a sentry let out on its top middle.</summary>
    private static (Scenario W, Piece Piece, Creature Sentry) OnAWreck(int speed = 0)
    {
        var w = new Scenario(Space, cells: true).Do(new Crew.Enter(40 * 160 + 4)).Run();
        var debris = (DebrisState)w.M.Debris.Slice;
        var piece = new Piece { Id = debris.NextId++, Zone = Spec.Debris.Far.Id, Item = "engineBlock", X = 80_000, Y = 40_000, Speed = speed, Start = 80_000, SeenAt = debris.Clock };
        debris.Pieces.Add(piece);
        w.Do(new Release(Sentry.Id, 38 * 160 + 80)).Run();
        Assert.Empty(w.Refusals);
        return (w, piece, w.M.Creatures.Creatures.Single());
    }

    private static Scenario HeroAt(Scenario w, int x, int y, int peer = 0) => w.Do(new Crew.Enter(y * 160 + x) { From = peer });

    private static bool Alerted(Creature c) => Sentry.Brain[c.Drive] is Tune.Guard && c.Step == 1;

    private static bool Mounted(Creature c) => c.Recall(ClingerBody.Mounted) == 1;

    [Fact]
    public void ASentryClampsOnTheTopOfTheWreckItIsLetOutOnAndOnNothingElse()
    {
        var (w, piece, c) = OnAWreck();
        Assert.True(Mounted(c));
        Assert.Equal((80_500, 37_000), (c.X, c.Y));
        Assert.Equal(piece.Id, c.Recall(ClingerBody.Piece));
        w.Do(new Release(Sentry.Id, 10 * 160 + 10)).Run();
        Assert.Equal("creatures.noPiece", w.Refusals.Single().Reason);
    }

    [Fact]
    public void ItRidesItsWrecksDrift()
    {
        var (w, piece, c) = OnAWreck(speed: 150);
        w.Run(40);
        Assert.Equal((piece.X + 500, piece.Y - 3000), (c.X, c.Y));
        Assert.True(c.X < 80_500);
    }

    [Fact]
    public void AHeroWithinItsNoticeAlertsItAndOneLeavingCalmsIt()
    {
        var (w, _, c) = OnAWreck();
        Assert.False(Alerted(c));
        HeroAt(w, 80 - Guard.Notice - 3, 36).Run(2);
        Assert.False(Alerted(c));
        HeroAt(w, 80 - Guard.Notice + 1, 36).Run(2);
        Assert.True(Alerted(c));
        HeroAt(w, 40, 36).Run(2);
        Assert.False(Alerted(c));
        Assert.Null(c.Act);
    }

    [Fact]
    public void AlertedThroughItsNoticeItPouncesStraightAtTheHeroThenFliesBackAndClampsOn()
    {
        var (w, _, c) = OnAWreck();
        HeroAt(w, 72, 30).Run(1);
        Assert.True(Alerted(c));
        w.Run(Guard.AlertTicks);
        Assert.Equal(ActPhase.WindUp, c.Act!.Phase);
        w.Run(Pounce.WindUp);
        Assert.Equal(ActPhase.Active, c.Act!.Phase);
        Assert.False(Mounted(c));
        // Toward the hero up-left, its speed on the longer axis.
        Assert.Equal(-Pounce.Speed, c.VX);
        Assert.InRange(c.VY, -Pounce.Speed, -1);
        int x = c.X;
        w.Run();
        Assert.Equal(x - Pounce.Speed, c.X);
        w.Run(Pounce.Ticks - 1);
        Assert.Null(c.Act);
        Assert.Equal(typeof(Tune.Return), Sentry.Brain[c.Drive].GetType());
        for (int t = 0; t < 200 && !Mounted(c); t++) w.Run();
        Assert.True(Mounted(c));
        Assert.Equal((80_500, 37_000), (c.X, c.Y));
        Assert.Equal(Ref.None, c.Target);
    }

    [Fact]
    public void ItsWreckStruckItCrouchesAtOnceToPounceOnTheStriker()
    {
        var (w, piece, c) = OnAWreck();
        HeroAt(w, 85, 30, peer: 1).Run();
        piece.Struck = new Blow { By = 1, At = 7 };
        w.Run();
        Assert.Equal(ActPhase.WindUp, c.Act!.Phase);
        Assert.Equal(Ref.Hero(1), c.Target);
        // The striker gone from its notice: answered once, the same blow seen again starts nothing.
        HeroAt(w, 30, 30, peer: 1);
        w.Run(Pounce.WindUp + Pounce.Ticks + 120);
        Assert.True(Mounted(c));
        Assert.Null(c.Act);
    }

    [Fact]
    public void AHewOnTheWreckLeavesItsBlowForTheSentry()
    {
        var (w, piece, _) = OnAWreck();
        HeroAt(w, 72, 37, peer: 0).Run();
        int heading = 0;
        w.Do(new HewWrecks(Axe, 72_500, 37_500, heading)).Run();
        Assert.Empty(w.Refusals);
        Assert.Equal(0, piece.Struck!.By);
    }

    [Fact]
    public void StruckItTurnsOnTheStrikerAndABlowNeverCutsItsPounceShort()
    {
        var (w, _, c) = OnAWreck();
        HeroAt(w, 76, 34).Run();
        w.Do(new Strike(c.Id, Axe)).Run();
        Assert.Equal(ActPhase.WindUp, c.Act!.Phase);
        Assert.Equal(Ref.Hero(0), c.Target);
        w.Run(Pounce.WindUp);
        Assert.Equal(ActPhase.Active, c.Act!.Phase);
        var (vx, vy) = (c.VX, c.VY);
        w.Do(new Strike(c.Id, Axe)).Run();
        Assert.Equal((ActPhase.Active, vx, vy), (c.Act!.Phase, c.VX, c.VY));
    }

    [Fact]
    public void APounceThatHitsCostsSixOfTheSuitOncePerPounce()
    {
        var (w, _, c) = OnAWreck();
        HeroAt(w, 76, 34).HeroAt(76, 34, peer: 1);
        w.Do(new Items.SetCreative(false)).Do(new Strike(c.Id, Axe)).Run(Pounce.WindUp + 1);
        Assert.Equal(ActPhase.Active, c.Act!.Phase);
        w.Do(new HitBy(c.Id)).Do(new HitBy(c.Id) { From = 1 }).Run();
        // Both breathe alike: only the one hit pays the pounce.
        Assert.Equal(w.M.Crew.HeroOf(1)!.Suit - Pounce.Damage, w.M.Crew.HeroOf(0)!.Suit);
        Assert.Equal(["creatures.notAttacking"], w.Refusals.Select(r => r.Reason));
        Assert.Equal(typeof(Tune.Return), Sentry.Brain[c.Drive].GetType());
    }

    [Fact]
    public void AHarpoonOnItsWreckIsCutTheTickItIsReeledThePieceAStepOn()
    {
        var (w, piece, _) = OnAWreck();
        HeroAt(w, 60, 40).Run();
        int x = piece.X;
        w.Do(new Reel(piece.Id, 62_000, 40_000)).Run();
        Assert.Empty(w.Refusals);
        Assert.Null(piece.Reel);
        Assert.True(piece.X < x, "the reel moved it a step before the sentry cut it");
        w.Run(10);
        Assert.Null(piece.Reel);
    }

    [Fact]
    public void KilledItBreaksIntoScrap()
    {
        var (w, _, c) = OnAWreck();
        HeroAt(w, 79, 36).Run();
        c.Damage = Sentry.Life.Hp - 1;
        w.Do(new Strike(c.Id, Axe)).Run(30);
        Assert.Empty(w.M.Creatures.Creatures);
        Assert.InRange(w.M.Items.Floor.Where(s => s.Item == "scrapChunk").Sum(s => s.Count), 3, 5);
    }

    [Fact]
    public void ItsWreckGoneItHoldsTheLastPointItClampedToAndGuardsOn()
    {
        var (w, piece, c) = OnAWreck(speed: 100);
        w.Run(20);
        var (x, y) = (c.X, c.Y);
        ((DebrisState)w.M.Debris.Slice).Pieces.Remove(piece);
        w.Run(20);
        Assert.Equal((x, y), (c.X, c.Y));
        HeroAt(w, Down(x) - 5, Down(y) - 1).Run(2);
        Assert.True(Alerted(c));
    }

    [Fact]
    public void ItThreatensAHeroNearItHuntingTheCrew()
    {
        var (w, _, _) = OnAWreck();
        HeroAt(w, 70, 40).Run();
        Assert.True(w.M.Creatures.Threatens(0));
    }

    [Fact]
    public void ItsKeptNumbersLoadInTheOrderTheySavedKeysDifferingInCaseToo()
    {
        var (w, _, c) = OnAWreck();
        foreach (var (key, value) in new[] { ("alerted", 1), ("alertTicks", 2), ("Along", 3), ("ALONG", 4) }) c.Keep(key, value);
        ulong hash = w.World.Hash();
        w.Reload();
        var back = w.M.Creatures.Creatures.Single();
        Assert.Equal(hash, w.World.Hash());
        Assert.Equal((1, 2, 3, 4), (back.Recall("alerted"), back.Recall("alertTicks"), back.Recall("Along"), back.Recall("ALONG")));
        w.Run(30);
    }

    private static int Down(int a) => CreaturesModule.Down(a, Unit);
}
