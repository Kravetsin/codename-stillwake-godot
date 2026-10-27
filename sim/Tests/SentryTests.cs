using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
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
    internal static readonly Tune.Shoot Shoot = (Tune.Shoot)Sentry.Moves[1];
    private static readonly Tune.Guard Guard = Sentry.Brain.OfType<Tune.Guard>().Single();
    private static readonly Tune.Return Return = Sentry.Brain.OfType<Tune.Return>().Single();
    private static readonly Tune.CutTether Cut = Sentry.Brain.OfType<Tune.CutTether>().Single();
    private static readonly string Axe = Spec.Weapons.Axe.Item;

    /// <summary>Open space 160 cells by 80, a cell a character: no hull, so no stream of debris.</summary>
    private static readonly string Space = string.Join('\n', Enumerable.Repeat(new string('.', 160), 80));

    /// <summary>An engine block (10 by 6 cells) still at (80, 40), hero 0 far left, and a sentry let out on its top middle.</summary>
    internal static (Scenario W, Piece Piece, Creature Sentry) OnAWreck(int speed = 0)
    {
        var w = new Scenario(Space, cells: true).Do(new Crew.Enter(40 * 160 + 4)).Run();
        var debris = (DebrisState)w.M.Debris.Slice;
        var piece = new Piece { Id = debris.NextId++, Zone = Spec.Debris.Far.Id, Item = "engineBlock", X = 80_000, Y = 40_000, Speed = speed, Start = 80_000, SeenAt = debris.Clock };
        debris.Pieces.Add(piece);
        Guarded(w);
        w.Do(new Release(Sentry.Id, 38 * 160 + 80)).Run();
        Assert.Empty(w.Refusals);
        return (w, piece, w.M.Creatures.Creatures.Single());
    }

    internal static Scenario HeroAt(Scenario w, int x, int y, int peer = 0) => w.Do(new Crew.Enter(y * 160 + x) { From = peer });

    /// <summary>The pieces put out by hand have drawn whether a sentry comes with them, and none came: the tests let theirs out.</summary>
    internal static void Guarded(Scenario w) => ((CreaturesState)w.M.Creatures.Slice).Watermark = ((DebrisState)w.M.Debris.Slice).NextId - 1;

    private static bool Alerted(Creature c) => CreaturesModule.Alerted(c);

    private static bool Mounted(Creature c) => CreaturesModule.Clamped(c);

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
        HeroAt(w, 80 - Guard.Notice - 10, 36).Run(2);
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
    public void AHarpoonOnItsWreckIsCutItsCutTicksAfterItBitesTheWreckReeledTillThen()
    {
        var (w, piece, c) = OnAWreck();
        HeroAt(w, 60, 40).Run();
        int x = piece.X;
        w.Do(new Reel(piece.Id, 62_000, 40_000)).Run();
        Assert.Empty(w.Refusals);
        Assert.Equal((Cut.Ticks, Cut.Ticks), w.M.Creatures.CutLeft(c));
        w.Run(Cut.Ticks - 1);
        Assert.NotNull(piece.Reel);
        Assert.Equal((1, Cut.Ticks), w.M.Creatures.CutLeft(c));
        Assert.Equal(0, CreaturesModule.LastCut(c));
        w.Run();
        Assert.Null(piece.Reel);
        Assert.True(CreaturesModule.LastCut(c) > 0);
        Assert.Equal((0, 0), w.M.Creatures.CutLeft(c));
        Assert.True(piece.X < x, "the reel moved it till the sentry cut it");
        w.Run(10);
        Assert.Null(piece.Reel);
    }

    [Fact]
    public void ALineLetGoBeforeItIsCutIsForgottenAndTheNextCountsAnew()
    {
        var (w, piece, c) = OnAWreck();
        HeroAt(w, 60, 40).Run();
        w.Do(new Reel(piece.Id, 62_000, 40_000)).Run(Cut.Ticks / 2);
        ((IDebrisOps)w.M.Debris).Unreel(piece.Id);
        w.Run();
        Assert.Equal((0, 0), w.M.Creatures.CutLeft(c));
        w.Do(new Reel(piece.Id, 20_000, 40_000)).Run(Cut.Ticks);
        Assert.NotNull(piece.Reel);
        w.Run();
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
    public void ItsWreckGoneItDiesWithItGivingItsScrap()
    {
        var (w, piece, _) = OnAWreck(speed: 100);
        w.Run(20);
        ((DebrisState)w.M.Debris.Slice).Pieces.Remove(piece);
        w.Run(2);
        Assert.Empty(w.M.Creatures.Creatures);
        Assert.InRange(w.M.Items.Floor.Where(s => s.Item == "scrapChunk").Sum(s => s.Count), 3, 5);
    }

    [Fact]
    public void ItsWreckGoneMidPounceItDiesInFlight()
    {
        var (w, piece, c) = OnAWreck();
        HeroAt(w, 76, 34).Run();
        w.Do(new Strike(c.Id, Axe)).Run(Pounce.WindUp + 3);
        Assert.False(Mounted(c));
        ((DebrisState)w.M.Debris.Slice).Pieces.Remove(piece);
        w.Run(2);
        Assert.Empty(w.M.Creatures.Creatures);
    }

    [Fact]
    public void NoticingAHeroPastItsStrikeItWindsUpThenStrikesAnArcAtItStayingOnItsWreck()
    {
        var (w, _, c) = OnAWreck();
        // Thirty cells off: within its notice, past its strike.
        HeroAt(w, 80 - 30, 36).Run();
        Assert.True(Alerted(c));
        w.Run(Guard.AlertTicks);
        Assert.Equal((ActPhase.WindUp, (Tune.Shoot)Sentry.Moves[c.Act!.Move]), (c.Act.Phase, Shoot));
        w.Run(Shoot.WindUp - 2);
        Assert.Empty(w.M.Creatures.Arcs);
        w.Run();
        Assert.Single(w.M.Creatures.Arcs);
        Assert.True(Mounted(c));
    }

    [Fact]
    public void ItFliesHomeFasterThanAHerosJetpack()
    {
        // A hero leading it off by striking it: it outruns them home, back on its wreck before they are.
        Assert.True(Return.Speed > 1500, $"{Return.Speed} a tick against the jetpack's 1500");
        var (w, _, c) = OnAWreck();
        HeroAt(w, 66, 30).Run();
        w.Do(new Strike(c.Id, Axe)).Run();
        for (int t = 0; t < 100 && Sentry.Brain[c.Drive] is not Tune.Return; t++) w.Run();
        Assert.Equal(typeof(Tune.Return), Sentry.Brain[c.Drive].GetType());
        int far = Math.Max(Math.Abs(c.X - 80_500), Math.Abs(c.Y - 37_000));
        Assert.True(far > 10 * Unit, $"{far} away");
        int ticks = 0;
        for (; ticks < 300 && !Mounted(c); ticks++) w.Run();
        Assert.InRange(ticks, 1, far / Return.Speed + 2);
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
        Assert.True(Mounted(back));
    }

    private static int Down(int a) => CreaturesModule.Down(a, Unit);

    private static int Suit(Scenario w, int peer) => w.M.Crew.HeroOf(peer)!.Suit;

    /// <summary>Runs <paramref name="w"/> till <paramref name="c"/> is clamped on and idle again, at most <paramref name="ticks"/>.</summary>
    private static void Home(Scenario w, Creature c, int ticks = 300)
    {
        for (int t = 0; t < ticks && !(Mounted(c) && c.Act == null); t++) w.Run();
        Assert.True(Mounted(c) && c.Act == null);
    }

    [Fact]
    public void AWorldSavedAndLoadedEveryTickPlaysOnAsOneNeverSavedThroughItsCrouchFlightAndReturn()
    {
        var (a, _, _) = OnAWreck();
        var (b, _, _) = OnAWreck();
        var seen = new HashSet<string>();
        foreach (var w in new[] { a, b }) HeroAt(w, 72, 30).Run();
        for (int t = 0; t < 220; t++)
        {
            if (t == 120) foreach (var w in new[] { a, b }) w.Do(new Strike(w.M.Creatures.Creatures[0].Id, Axe) { From = 0 });
            a.Run();
            b.Reload().Run();
            Assert.Equal(a.World.Hash(), b.World.Hash());
            var c = a.M.Creatures.Creatures[0];
            seen.Add(c.Act is { } act ? act.Phase.ToString() : Sentry.Brain[c.Drive].GetType().Name);
        }
        Assert.Superset(new HashSet<string> { "Guard", "WindUp", "Active", "Return" }, seen);
    }

    [Fact]
    public void AlertedForItsTwentyTicksItCrouchesOnTheTwentiethNotBefore()
    {
        // The tick it notices the hero is the first of its alert.
        var (w, _, c) = OnAWreck();
        HeroAt(w, 72, 30).Run();
        Assert.True(Alerted(c));
        w.Run(Guard.AlertTicks - 2);
        Assert.Null(c.Act);
        w.Run();
        Assert.Equal(ActPhase.WindUp, c.Act!.Phase);
    }

    [Fact]
    public void ItsNoticeEndsAtItsCellsByBoxDistance()
    {
        // Its body's cell (80, 36): a hero at (80 - Notice, 36) is within it, one a cell farther out.
        var (w, _, c) = OnAWreck();
        HeroAt(w, 80 - Guard.Notice - 1, 36).Run();
        Assert.False(Alerted(c));
        HeroAt(w, 80 - Guard.Notice, 36).Run();
        Assert.True(Alerted(c));
    }

    [Fact]
    public void StruckByAHeroBeyondItsRunItShootsAtItNeverLeavingItsWreckAndFromNearItPounces()
    {
        var (w, _, c) = OnAWreck();
        HeroAt(w, 80 - 30, 36).Run();
        w.Do(new Strike(c.Id, Axe)).Run();
        Assert.Equal((Ref.Hero(0), (Tune.Move)Shoot), (c.Target, Sentry.Moves[c.Act!.Move]));
        for (int t = 0; t < Shoot.WindUp + Shoot.Recover + 2; t++)
        {
            w.Run();
            Assert.True(Mounted(c));
            if (t == Shoot.WindUp) Assert.Single(w.M.Creatures.Arcs);
        }
        Assert.Null(c.Act);
        Assert.Equal(Ref.None, c.Target);

        // At its strike it pounces, a cell past it shoots.
        foreach (var (far, move) in new[] { (Guard.Strike, (Tune.Move)Pounce), (Guard.Strike + 1, Shoot) })
        {
            var (near, _, d) = OnAWreck();
            HeroAt(near, 80 - far, 36).Run();
            near.Do(new Strike(d.Id, Axe)).Run();
            Assert.Equal(move, Sentry.Moves[d.Act!.Move]);
        }
    }

    [Fact]
    public void ItPouncesAtItsGrudgeBeforeAHeroNearer()
    {
        // Hero 0 eight cells left, hero 1 fifteen right strikes it: it flies right, at hero 1.
        var (w, _, c) = OnAWreck();
        HeroAt(w, 72, 36).HeroAt(95, 36, peer: 1).Run();
        w.Do(new Strike(c.Id, Axe) { From = 1 }).Run(Pounce.WindUp + 1);
        Assert.Equal(ActPhase.Active, c.Act!.Phase);
        Assert.Equal(Pounce.Speed, c.VX);
    }

    [Fact]
    public void ItNeverPouncesAtAHeroPassedOutButAtOneAwake()
    {
        var (w, _, c) = OnAWreck();
        HeroAt(w, 85, 36).HeroAt(70, 36, peer: 1).Run();
        w.M.Crew.HeroOf(0)!.Out = 1000;
        w.Do(new Strike(c.Id, Axe) { From = 0 }).Run(Pounce.WindUp + 1);
        Assert.Equal(-Pounce.Speed, c.VX);
    }

    [Fact]
    public void AHitByDuringItsCrouchIsRefusedNotAttackingAndCostsNothing()
    {
        var (w, _, c) = OnAWreck();
        HeroAt(w, 76, 34).HeroAt(76, 34, peer: 1).Run();
        w.Do(new Strike(c.Id, Axe)).Run();
        Assert.Equal(ActPhase.WindUp, c.Act!.Phase);
        w.Do(new HitBy(c.Id) { From = 1 }).Run();
        Assert.Equal(["creatures.notAttacking"], w.Refusals.Select(r => r.Reason));
        Assert.Equal(Suit(w, 0), Suit(w, 1));
    }

    [Fact]
    public void HewnByAHeroItPouncesAtThemAndAgainAtTheNextBlowOnceHome()
    {
        var (w, piece, c) = OnAWreck();
        HeroAt(w, 72, 37, peer: 1).Run();
        int clock = ((DebrisState)w.M.Debris.Slice).Clock;
        w.Do(new HewWrecks(Axe, 72_500, 37_500, 0) { From = 1 }).Run();
        Assert.Equal((1, clock), (piece.Struck!.By, piece.Struck.At));
        Assert.Equal((ActPhase.WindUp, Ref.Hero(1)), (c.Act!.Phase, c.Target));
        HeroAt(w, 30, 37, peer: 1);
        Home(w, c);
        HeroAt(w, 72, 37, peer: 1).Run();
        w.Do(new HewWrecks(Axe, 72_500, 37_500, 0) { From = 1 }).Run();
        Assert.Equal(ActPhase.WindUp, c.Act!.Phase);
    }

    [Fact]
    public void OneSwingThatHitsItAndItsWreckPouncesOnce()
    {
        var (w, _, c) = OnAWreck();
        HeroAt(w, 72, 37, peer: 1).Run();
        w.Do(new Strike(c.Id, Axe) { From = 1 }).Do(new HewWrecks(Axe, 72_500, 37_500, 0) { From = 1 }).Run();
        HeroAt(w, 20, 37, peer: 1);
        int crouches = 0;
        bool was = true;
        for (int t = 0; t < 400; t++)
        {
            w.Run();
            bool now = c.Act is { Phase: ActPhase.WindUp };
            if (now && !was) crouches++;
            was = now;
        }
        Assert.Equal(0, crouches);
        Assert.True(Mounted(c));
    }

    [Fact]
    public void ABlowOnItsWreckWhileItIsAwayIsAnsweredOnceItIsHome()
    {
        var (w, piece, c) = OnAWreck();
        HeroAt(w, 72, 30).Run();
        w.Do(new Strike(c.Id, Axe)).Run(Pounce.WindUp + Pounce.Ticks + 1);
        Assert.False(Mounted(c));
        piece.Struck = new Blow { By = 0, At = ((DebrisState)w.M.Debris.Slice).Clock };
        w.Run();
        Assert.Null(c.Act);
        for (int t = 0; t < 300 && c.Act == null; t++) w.Run();
        Assert.True(Mounted(c));
        Assert.Equal((ActPhase.WindUp, Ref.Hero(0)), (c.Act!.Phase, c.Target));
    }

    [Fact]
    public void ItsPounceRunsItsTicksThenItHangsAndFliesHomeAtItsSpeedItsGrudgeSpent()
    {
        var (w, _, c) = OnAWreck();
        HeroAt(w, 60, 37).Run();
        w.Do(new Strike(c.Id, Axe)).Run();
        while (c.Act!.Phase != ActPhase.Active) w.Run();
        // Launched: it has moved its first tick; Ticks moves in all, none the tick it ends.
        int vx = c.VX, x = c.X, moves = 1;
        while (c.Act != null)
        {
            w.Run();
            if (c.Act != null) moves++;
        }
        Assert.Equal(Pounce.Ticks, moves);
        Assert.Equal((x + (Pounce.Ticks - 1) * vx, 0, 0), (c.X, c.VX, c.VY));
        // The return takes over the next tick and flies from the one after.
        w.Run(2);
        int speed = Sentry.Brain.OfType<Tune.Return>().Single().Speed;
        Assert.Equal(speed, Math.Max(Math.Abs(c.VX), Math.Abs(c.VY)));
        Home(w, c);
        // Clamped on, the return yields the next tick, its grudge spent.
        w.Run();
        Assert.Equal(Ref.None, c.Target);
    }

    [Fact]
    public void ABlowNeverChangesItsFallInFlight()
    {
        // Pouncing down at a hero below: a blow keeps its speed down, with no hop to give.
        var (w, _, c) = OnAWreck();
        HeroAt(w, 78, 50).Run();
        w.Do(new Strike(c.Id, Axe)).Run(Pounce.WindUp + 1);
        Assert.True(c.VY > 0);
        int vy = c.VY;
        w.Do(new Strike(c.Id, Axe)).Run();
        Assert.Equal(vy, c.VY);
    }

    [Fact]
    public void AHarpoonOnItsWreckIsCutMidPounceToo()
    {
        var (w, piece, c) = OnAWreck();
        HeroAt(w, 60, 40).Run();
        w.Do(new Strike(c.Id, Axe)).Run(3);
        Assert.NotNull(c.Act);
        w.Do(new Reel(piece.Id, 62_000, 40_000)).Run();
        Assert.Empty(w.Refusals);
        Assert.NotNull(piece.Reel);
        w.Run(Cut.Ticks);
        Assert.Null(piece.Reel);
        Assert.True(CreaturesModule.LastCut(c) > 0);
    }

    [Fact]
    public void LetOutOnAWreckStruckBeforeItIgnoresThatBlow()
    {
        var w = new Scenario(Space, cells: true).Do(new Crew.Enter(40 * 160 + 4)).Run();
        var debris = (DebrisState)w.M.Debris.Slice;
        debris.Pieces.Add(new Piece { Id = debris.NextId++, Zone = Spec.Debris.Far.Id, Item = "engineBlock", X = 80_000, Y = 40_000, Struck = new Blow { By = 0, At = 0 } });
        Guarded(w);
        w.Run(50).Do(new Release(Sentry.Id, 38 * 160 + 80)).Run(5);
        Assert.Null(w.M.Creatures.Creatures.Single().Act);
    }

    [Fact]
    public void OnAWreckAtTheGridsTopItStaysWhollyOnTheGridAndComesHome()
    {
        var w = new Scenario(Space, cells: true).Do(new Crew.Enter(1 * 160 + 4)).Run();
        var debris = (DebrisState)w.M.Debris.Slice;
        debris.Pieces.Add(new Piece { Id = debris.NextId++, Zone = Spec.Debris.Far.Id, Item = "engineBlock", X = 2_000, Y = 3_000 });
        Guarded(w);
        w.Do(new Release(Sentry.Id, 1 * 160 + 1)).Run();
        var c = w.M.Creatures.Creatures.Single();
        Assert.Equal((Sentry.Body.Width * Unit / 2, Sentry.Body.Height * Unit), (c.X, c.Y));
        HeroAt(w, 6, 1).Run();
        w.Do(new Strike(c.Id, Axe)).Run(Pounce.WindUp + 2);
        HeroAt(w, 90, 60);
        Home(w, c);
    }
}

/// <summary>
/// What a sentry must always keep (2026-10-26-creatures-are-composed), over drifting wrecks with sentries let out on them,
/// heroes moving, striking, hewing and harpooning: its body wholly on the grid, damage under its hit points, a drive's and
/// an act's ticks within their lengths, a pounce hurting each hero at most once.
/// </summary>
public class SentryInvariantTests
{
    private const int Width = 120, Height = 60;
    private static readonly Tune.Species Sentry = Tune.Sentry;
    private static readonly Tune.Pounce Pounce = (Tune.Pounce)Sentry.Moves[0];

    /// <summary>The most ticks <paramref name="act"/>'s phase lasts: a pounce's crouch and flight, a shot's crouch and recovery.</summary>
    private static int Longest(Act act) => Sentry.Moves[act.Move] switch
    {
        Tune.Shoot s => act.Phase == ActPhase.WindUp ? s.WindUp : s.Recover,
        _ => act.Phase == ActPhase.WindUp ? Pounce.WindUp : Pounce.Ticks,
    };

    public sealed record Run(int Seed, int Ticks);

    private static Gen<Run> Runs() =>
        from seed in Gen.Choose(0, int.MaxValue)
        from ticks in Gen.Choose(200, 900)
        select new Run(seed, ticks);

    [Property(MaxTest = 40)]
    public Property SentriesStayOnTheGridWithinTheirClocksAndHurtOncePerPounceOrArc() =>
        Prop.ForAll(Runs().ToArbitrary(), run =>
        {
            var rng = new Rng(new RngState { S = (uint)run.Seed });
            var w = new Scenario(string.Join('\n', Enumerable.Repeat(new string('.', Width), Height)), cells: true);
            for (int h = 0; h < 2; h++) w.Do(new Crew.Enter((int)rng.Below(Height) * Width + (int)rng.Below(Width)) { From = h });
            w.Run();
            var debris = (DebrisState)w.M.Debris.Slice;
            string[] kinds = ["engineBlock", "cargoHold", "asteroidSmall", "hullSection"];
            for (int i = 0; i < 3; i++)
            {
                int x = 6_000 + (int)rng.Below((Width - 12) * 1000), y = 6_000 + (int)rng.Below((Height - 12) * 1000);
                debris.Pieces.Add(new Piece { Id = debris.NextId++, Zone = Spec.Debris.Far.Id, Item = kinds[rng.Below(4)], X = x, Y = y, Speed = (int)rng.Below(150), Start = x });
            }
            SentryTests.Guarded(w);
            foreach (var p in debris.Pieces.ToList()) w.Do(new Release(Sentry.Id, Debris.DebrisModule.CellOf(p.X, p.Y, Width)));
            w.Run();
            string? broken = null;
            var hits = new Dictionary<(int Creature, int Peer), int>();
            var paid = new HashSet<(int Arc, int Peer)>();
            for (int t = 0; t < run.Ticks && broken == null; t++)
            {
                var all = w.M.Creatures.Creatures;
                if (rng.Below(20) == 0) w.Do(new Crew.Enter((int)rng.Below(Height) * Width + (int)rng.Below(Width)) { From = (int)rng.Below(2) });
                if (all.Count > 0 && rng.Below(15) == 0) w.Do(new Strike(all[(int)rng.Below((uint)all.Count)].Id, Spec.Weapons.Axe.Item) { From = (int)rng.Below(2) });
                if (debris.Pieces.Count > 0 && rng.Below(40) == 0)
                {
                    var p = debris.Pieces[(int)rng.Below((uint)debris.Pieces.Count)];
                    w.Do(new Reel(p.Id, p.X - 5000, p.Y) { From = (int)rng.Below(2) });
                }
                var sent = new List<HitBy>();
                foreach (var c in all)
                    for (int h = 0; h < 2; h++)
                        if (rng.Below(3) == 0) sent.Add(new HitBy(c.Id) { From = h });
                var bolts = new List<Bolted>();
                foreach (var arc in w.M.Creatures.Arcs)
                    for (int h = 0; h < 2; h++)
                        if (rng.Below(4) == 0) bolts.Add(new Bolted(arc.Id) { From = h });
                foreach (var hit in sent) w.Do(hit);
                foreach (var bolt in bolts) w.Do(bolt);
                int refused = w.Refusals.Count;
                w.Run();
                var turnedDown = w.Refusals.Skip(refused).Select(r => r.Command).ToHashSet(ReferenceEqualityComparer.Instance);
                foreach (var hit in sent.Where(h => !turnedDown.Contains(h)))
                    hits[(hit.Creature, hit.From)] = hits.GetValueOrDefault((hit.Creature, hit.From)) + 1;
                foreach (var bolt in bolts.Where(b => !turnedDown.Contains(b)))
                    if (!paid.Add((bolt.Arc, bolt.From))) broken ??= $"arc {bolt.Arc} hurt hero {bolt.From} twice";
                foreach (var arc in w.M.Creatures.Arcs)
                {
                    if (arc.Left > SentryTests.Shoot.Lingers) broken ??= $"arc {arc.Id} {arc.Left} left";
                    if (Math.Max(Math.Abs(arc.X1 - arc.X0), Math.Abs(arc.Y1 - arc.Y0)) > SentryTests.Shoot.Reach * CreaturesModule.Unit)
                        broken ??= $"arc {arc.Id} past its reach";
                }
                foreach (var c in w.M.Creatures.Creatures)
                {
                    if (c.Act is not { Phase: ActPhase.Active }) foreach (var key in hits.Keys.Where(k => k.Creature == c.Id).ToList()) hits.Remove(key);
                    int half = Sentry.Body.Width * 1000 / 2;
                    if (c.X - half < 0 || c.X + half > Width * 1000 || c.Y - Sentry.Body.Height * 1000 < 0 || c.Y > Height * 1000)
                        broken ??= $"sentry {c.Id} off the grid at ({c.X}, {c.Y})";
                    if (c.Damage >= Sentry.Life.Hp) broken ??= $"sentry {c.Id} alive at {c.Damage}";
                    if (c.Act is { } act && act.Left > Longest(act)) broken ??= $"sentry {c.Id} act {act.Phase} {act.Left} left";
                    if (c.Act == null && c.DriveLeft > Sentry.Brain.OfType<Tune.Guard>().Single().AlertTicks) broken ??= $"sentry {c.Id} drive {c.DriveLeft} left";
                }
                if (hits.Values.Any(n => n > 1)) broken ??= "a pounce hurt one hero twice";
            }
            return (broken == null).Label(broken ?? "");
        });
}
