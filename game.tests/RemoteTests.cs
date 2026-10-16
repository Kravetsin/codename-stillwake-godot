using Stillwake.Game.Features.Crew;
using Xunit;
using static Stillwake.Game.Features.Crew.RemoteHeroes;

namespace Stillwake.Game.Tests;

/// <summary>features/crew/remote.test.ts, ported.</summary>
public class RemoteTests
{
    private static Hero HeroAt(int x, int y = 0, int facing = 1) => new(x, y, 0, 0, false, facing);
    private static SentHero Sent(Hero hero, double? at = null, double? look = null, bool plan = false) => new(new Seen(hero, look, plan), at);
    private static Seen Seen(Hero hero) => new(hero);

    // Others' heroes.

    [Fact]
    public void AreDrawnBetweenTheTwoSamplesAroundAMomentALittleBehind()
    {
        var heroes = new RemoteHeroes();
        heroes.Add(2, Sent(HeroAt(0)), 1000);
        heroes.Add(2, Sent(HeroAt(1000)), 1050);
        Assert.Equal([(2, Seen(HeroAt(500)))], heroes.At(1000 + BehindMs + 25));
    }

    [Fact]
    public void WaitAtTheFirstSampleBeforeItAndStandAtTheLastAfterIt()
    {
        var heroes = new RemoteHeroes();
        heroes.Add(2, Sent(HeroAt(0)), 1000);
        heroes.Add(2, Sent(HeroAt(1000, 0, -1)), 1050);
        Assert.Equal([(2, Seen(HeroAt(0)))], heroes.At(1000));
        Assert.Equal([(2, Seen(HeroAt(1000, 0, -1)))], heroes.At(1050 + BehindMs + 500));
    }

    [Fact]
    public void JumpRatherThanSlideAcrossAGapTooWideToWalkAsWakingAtTheSpawn()
    {
        var heroes = new RemoteHeroes();
        heroes.Add(2, Sent(HeroAt(0)), 1000);
        heroes.Add(2, Sent(HeroAt(40 * Hero.Unit)), 1050);
        Assert.Equal([(2, Seen(HeroAt(0)))], heroes.At(1000 + BehindMs + 25));
        Assert.Equal([(2, Seen(HeroAt(40 * Hero.Unit)))], heroes.At(1050 + BehindMs));
    }

    [Fact]
    public void KeepEachPeerApartAndDropOneSilentForTooLong()
    {
        var heroes = new RemoteHeroes();
        heroes.Add(1, Sent(HeroAt(10)), 0);
        heroes.Add(3, Sent(HeroAt(30)), 2000);
        Assert.Equal([1, 3], heroes.At(2500).Select(h => h.Id));
        Assert.Equal([3], heroes.At(GoneMs + 1).Select(h => h.Id));
    }

    [Fact]
    public void SetOffFromWhereAHeroStoodStillNotMostOfTheWayAlongItsFirstStep()
    {
        var heroes = new RemoteHeroes();
        heroes.Add(2, Sent(HeroAt(0)), 1000);
        // Standing still, it is heard again only when it moves, a second later.
        heroes.Add(2, Sent(HeroAt(500)), 2000);
        Assert.Equal([(2, Seen(HeroAt(0)))], heroes.At(2000 + BehindMs - SendMs));
        Assert.Equal(250, heroes.At(2000 + BehindMs - SendMs / 2)[0].Seen.Hero.X);
    }

    [Fact]
    public void KeepTheirTethersTiedWhereTheyWereBetweenSamplesToo()
    {
        var heroes = new RemoteHeroes();
        var anchor = new Point(9000, 4000);
        heroes.Add(2, Sent(HeroAt(0) with { Anchor = anchor }), 1000);
        heroes.Add(2, Sent(HeroAt(1000) with { Anchor = anchor }), 1050);
        Assert.Equal(anchor, heroes.At(1000 + BehindMs + 25)[0].Seen.Hero.Anchor);
    }

    [Fact]
    public void TurnTheirHeadsBetweenSamplesToo()
    {
        var heroes = new RemoteHeroes();
        heroes.Add(2, Sent(HeroAt(0), look: 0), 1000);
        heroes.Add(2, Sent(HeroAt(0), look: 0.4), 1050);
        Assert.Equal(0.2, heroes.At(1000 + BehindMs + 25)[0].Seen.Look!.Value, 2);
    }

    [Fact]
    public void MoveAtThePaceTheyWereSentHoweverUnevenlyTheLinkDeliversThem()
    {
        var heroes = new RemoteHeroes();
        // Sent every 50 ms, walking 50 a send; delivered after 10, 100, 20 and 60 ms on the way.
        heroes.Add(2, Sent(HeroAt(0), at: 0), 1010);
        heroes.Add(2, Sent(HeroAt(50), at: 50), 1150);
        heroes.Add(2, Sent(HeroAt(100), at: 100), 1120 + 50);
        heroes.Add(2, Sent(HeroAt(150), at: 150), 1210);
        // Placed by the quickest delivery, 10 ms: the sample sent at 75 is shown at 85 + BehindMs.
        var xs = new[] { 85, 110, 135 }.Select(t => heroes.At(1000 + t + BehindMs)[0].Seen.Hero.X).ToArray();
        // Within the slip the estimate allows, a hundredth of the time between deliveries.
        int[] expected = [75, 100, 125];
        for (int i = 0; i < 3; i++) Assert.True(Math.Abs(xs[i] - expected[i]) < 3, $"{xs[i]} for {expected[i]}");
    }

    [Fact]
    public void StartOverWhenASenderWakesFromSleepRatherThanLagBehindForGood()
    {
        var heroes = new RemoteHeroes();
        heroes.Add(2, Sent(HeroAt(0), at: 0), 1000);
        // It slept 10 s, and its steps with it: its clock moved on 50 ms.
        heroes.Add(2, Sent(HeroAt(50), at: 50), 11_000);
        heroes.Add(2, Sent(HeroAt(100), at: 100), 11_050);
        Assert.Equal(75, heroes.At(11_025 + BehindMs)[0].Seen.Hero.X);
    }

    [Fact]
    public void ComeBackWhereTheyAreAfterThisMachineStalledItsSamplesTakenAllAtOnce()
    {
        var heroes = new RemoteHeroes();
        heroes.Add(2, Sent(HeroAt(0), at: 0), 1000);
        // Stalled for a minute, this machine drew nothing; its frames' clock moved on 100 ms meanwhile.
        for (int t = 50; t <= 60_000; t += (int)SendMs) heroes.Add(2, Sent(HeroAt(t), at: t), 1100);
        Assert.Equal(60_000, heroes.At(1100 + BehindMs)[0].Seen.Hero.X);
        heroes.Add(2, Sent(HeroAt(60_050), at: 60_050), 1150);
        Assert.Equal(60_050, heroes.At(1150 + BehindMs)[0].Seen.Hero.X);
    }

    [Fact]
    public void WalkOnAlongTheirLastVelocityThroughAShortGapInDelivery()
    {
        var heroes = new RemoteHeroes();
        // Walking 6 a controller step, so 18 a send.
        heroes.Add(2, Sent(HeroAt(0) with { Vx = 6 }), 1000);
        heroes.Add(2, Sent(HeroAt(18) with { Vx = 6 }), 1050);
        double late = ExtrapolateMs / 2;
        var shown = heroes.At(1050 + BehindMs + late)[0].Seen.Hero;
        Assert.Equal(18 + 6 * late / Hero.StepMs, shown.X, 0);
        Assert.Equal(6, shown.Vx);
    }

    [Fact]
    public void StandDrawnStandingOnceAGapOutlastsTheWalkOnAndSetOffAgain()
    {
        var heroes = new RemoteHeroes();
        heroes.Add(2, Sent(HeroAt(0) with { Vx = 6 }), 1000);
        heroes.Add(2, Sent(HeroAt(18) with { Vx = 6 }), 1050);
        double stood = 6 * ExtrapolateMs / Hero.StepMs;
        foreach (double late in new[] { ExtrapolateMs + 1, 1000 })
        {
            var shown = heroes.At(1050 + BehindMs + late)[0].Seen.Hero;
            Assert.Equal(18 + stood, shown.X, 0);
            Assert.Equal((0, 0), (shown.Vx, shown.Vy));
        }
        heroes.Add(2, Sent(HeroAt(500) with { Vx = 6 }), 2100);
        Assert.Equal(500, heroes.At(2100 + BehindMs)[0].Seen.Hero.X);
    }

    [Fact]
    public void ForgetSamplesOnceALaterOneIsPastSoTheBufferStaysShort()
    {
        var heroes = new RemoteHeroes();
        for (int t = 0; t < 10_000; t += (int)SendMs) heroes.Add(1, Sent(HeroAt(t)), t);
        Assert.Equal(10_000 - BehindMs, heroes.At(10_000)[0].Seen.Hero.X);
    }

    // Sending the hero.

    [Fact]
    public void SendsAMovingHeroEverySoOftenNotEveryFrame()
    {
        var sent = new List<SentHero>();
        var sender = new HeroSender(sent.Add);
        for (int frame = 0; frame < 60; frame++) sender.Step(1000.0 / 60, Seen(HeroAt(frame)), frame * 1000.0 / 60);
        // A second of frames: the first at once, then one per SendMs.
        Assert.InRange(sent.Count, 1000 / SendMs - 1, 1000 / SendMs + 1);
    }

    [Fact]
    public void StampsEachHeroSentWithTheTimeOfTheStepItStandsAt()
    {
        var sent = new List<SentHero>();
        new HeroSender(sent.Add).Step(SendMs, Seen(HeroAt(1)), 1250);
        Assert.Equal(1250, sent[0].At);
    }

    [Fact]
    public void SendsAHeroThatStopsWhereItIsSoItIsNotLeftMidStride()
    {
        var sent = new List<SentHero>();
        var sender = new HeroSender(sent.Add);
        sender.Step(SendMs, Seen(HeroAt(5) with { Vx = 240 }), 0);
        // The step it stops in leaves it where it was: only its speed says so.
        sender.Step(SendMs, Seen(HeroAt(5) with { Vx = 0 }), 0);
        Assert.Equal([240, 0], sent.Select(h => h.Seen.Hero.Vx));
    }

    [Fact]
    public void SendsAHeroStandingStillWhenItsHeadTurns()
    {
        var sent = new List<SentHero>();
        var sender = new HeroSender(sent.Add);
        sender.Step(SendMs, new Seen(HeroAt(5), 0), 0);
        sender.Step(SendMs, new Seen(HeroAt(5), 0.01), 0);
        sender.Step(SendMs, new Seen(HeroAt(5), 0.3), 0);
        Assert.Equal([0, 0.3], sent.Select(h => h.Seen.Look));
    }

    [Fact]
    public void SendsAHeroStandingStillWhenItTakesThePlanOutOrPutsItAway()
    {
        var sent = new List<SentHero>();
        var sender = new HeroSender(sent.Add);
        sender.Step(SendMs, Seen(HeroAt(5)), 0);
        sender.Step(SendMs, new Seen(HeroAt(5), HoldsPlan: true), 0);
        sender.Step(SendMs, Seen(HeroAt(5)), 0);
        Assert.Equal([false, true, false], sent.Select(h => h.Seen.HoldsPlan));
    }

    [Fact]
    public void SendsAHeroStandingStillWhenItsTetherIsTiedOrLetGo()
    {
        var sent = new List<SentHero>();
        var sender = new HeroSender(sent.Add);
        var anchor = new Point(500, 500);
        sender.Step(SendMs, Seen(HeroAt(5)), 0);
        sender.Step(SendMs, Seen(HeroAt(5) with { Anchor = anchor }), 0);
        sender.Step(SendMs, Seen(HeroAt(5) with { Anchor = new Point(500, 500) }), 0);
        sender.Step(SendMs, Seen(HeroAt(5)), 0);
        Assert.Equal([null, anchor, null], sent.Select(h => h.Seen.Hero.Anchor));
    }

    [Fact]
    public void SendsAHeroStandingStillOnlyOnTheBeat()
    {
        var sent = new List<SentHero>();
        var sender = new HeroSender(sent.Add);
        for (int ms = 0; ms <= 3 * BeatMs; ms += 10) sender.Step(10, Seen(HeroAt(5)), ms);
        Assert.Equal(4, sent.Count);
    }

    // On the wire: net carries the hero as a string it never reads.

    [Fact]
    public void GoesThroughTheWireAsItWasSentAndNothingElseIsTakenForAHero()
    {
        var sent = Sent(HeroAt(1200, 3400, -1) with { Vx = 6, Climbing = true, Anchor = new Point(9000, 4000) }, at: 1250, look: -0.3, plan: true);
        Assert.Equal(sent, SentHero.Decode(sent.Encode()));
        Assert.Equal(Sent(HeroAt(1)), SentHero.Decode(Sent(HeroAt(1)).Encode()));
        Assert.Null(SentHero.Decode("{\"tick\":20}"));
    }

    // Its hands: what they wield and what they are busy with, so others draw them as its own client does.

    private static readonly Point Wreck = new(14_500, 7_250);

    [Theory]
    [InlineData(Handwork.Chop, "fireAxe")]
    [InlineData(Handwork.Work, null)]
    [InlineData(Handwork.Beam, null)]
    public void GoThroughTheWireWithWhatTheyWieldAndWorkAt(Handwork work, string? wields)
    {
        var sent = new SentHero(new Seen(HeroAt(1200, 3400), 0.2, work == Handwork.Beam, wields, new Busy(work, Wreck)), 1250);
        Assert.Equal(sent, SentHero.Decode(sent.Encode()));
    }

    [Fact]
    public void GoThroughTheWireWieldingWithIdleHands()
    {
        var sent = new SentHero(new Seen(HeroAt(1), Wields: "fireAxe"), 50);
        Assert.Equal(sent, SentHero.Decode(sent.Encode()));
    }

    [Fact]
    public void AreWrittenAsBeforeWhileEmptyAndIdle()
    {
        string written = Sent(HeroAt(1), at: 50, plan: true).Encode();
        Assert.DoesNotContain("wields", written);
        Assert.DoesNotContain("busy", written);
    }

    [Fact]
    public void AreEmptyAndIdleInAHeroWrittenBeforeThereWereHandsToSend()
    {
        var decoded = SentHero.Decode("{\"x\":1200,\"y\":3400,\"vx\":0,\"vy\":0,\"climbing\":false,\"facing\":1,\"plan\":true,\"at\":50}");
        Assert.Equal(new SentHero(new Seen(HeroAt(1200, 3400), HoldsPlan: true), 50), decoded);
        Assert.Null(decoded!.Value.Seen.Wields);
        Assert.Null(decoded.Value.Seen.Busy);
    }

    [Theory]
    [InlineData("\"juggle\"")]
    [InlineData("7")]
    [InlineData("null")]
    public void MakeNoHeroOfWorkNoFigureKnows(string work)
    {
        string state = "{\"x\":1,\"y\":0,\"vx\":0,\"vy\":0,\"climbing\":false,\"facing\":1,\"plan\":false,\"busy\":{\"work\":" + work + ",\"at\":{\"x\":1,\"y\":2}}}";
        Assert.Null(SentHero.Decode(state));
    }

    [Fact]
    public void MakeNoHeroOfAShortOrGarbledState()
    {
        string whole = new SentHero(new Seen(HeroAt(1), Wields: "fireAxe", Busy: new Busy(Handwork.Chop, Wreck)), 50).Encode();
        for (int cut = 0; cut < whole.Length; cut++) Assert.Null(SentHero.Decode(whole[..cut]));
        Assert.Null(SentHero.Decode(whole.Replace("\"fireAxe\"", "[1]")));
    }

    [Fact]
    public void AreSentStandingStillWhenTheyTakeUpAToolBeginWorkOrStop()
    {
        var sent = new List<SentHero>();
        var sender = new HeroSender(sent.Add);
        var chop = new Busy(Handwork.Chop, Wreck);
        Seen[] frames =
        [
            Seen(HeroAt(5)),
            new(HeroAt(5), Wields: "fireAxe"),
            new(HeroAt(5), Wields: "fireAxe", Busy: chop),
            new(HeroAt(5), Wields: "fireAxe", Busy: chop),
            new(HeroAt(5), Wields: "fireAxe", Busy: chop with { At = new Point(14_400, 7_250) }),
            new(HeroAt(5), Wields: "fireAxe"),
        ];
        foreach (var frame in frames) sender.Step(SendMs, frame, 0);
        Assert.Equal([frames[0], frames[1], frames[2], frames[4], frames[5]], sent.Select(h => h.Seen));
    }

    [Fact]
    public void AreDrawnAsTheSampleAtOrBeforeSaysNeverBetweenTwo()
    {
        var heroes = new RemoteHeroes();
        heroes.Add(2, new SentHero(new Seen(HeroAt(0), Wields: "fireAxe", Busy: new Busy(Handwork.Chop, Wreck))), 1000);
        heroes.Add(2, Sent(HeroAt(1000)), 1050);
        var between = heroes.At(1000 + BehindMs + 25)[0].Seen;
        Assert.Equal(500, between.Hero.X);
        Assert.Equal(("fireAxe", new Busy(Handwork.Chop, Wreck)), (between.Wields, between.Busy));
        var after = heroes.At(1050 + BehindMs)[0].Seen;
        Assert.Equal((null, null), (after.Wields, after.Busy));
    }

    [Fact]
    public void BeamOncePerPlacingThoughTheBeamIsSentWhileItFades()
    {
        var beam = new Busy(Handwork.Beam, Wreck);
        Assert.True(Busy.BeamsAnew(null, beam));
        Assert.False(Busy.BeamsAnew(beam, beam));
        Assert.True(Busy.BeamsAnew(beam, beam with { At = new Point(1500, 500) }));
        Assert.False(Busy.BeamsAnew(null, new Busy(Handwork.Work, Wreck)));
        Assert.False(Busy.BeamsAnew(beam, null));
    }
}
