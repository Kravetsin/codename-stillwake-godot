using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Stillwake.Sim;
using Xunit;

namespace Stillwake.Net.Tests;

/// <summary>
/// How a session's time runs: apps/game/src/session/game.test.ts (a joined game), clock.test.ts and
/// round-trip.test.ts, ported.
/// </summary>
public class PaceTests
{
    private const double FrameMs = 1000.0 / 60;
    private const double TickMs = 1000.0 / World.TicksPerSecond;

    /// <summary>
    /// A game joined to a host that is not here: ticks reach it when <paramref name="arrival"/> says, in
    /// ms, and frames run every FrameMs. <c>Frame</c> runs one and says how many ticks it ran.
    /// </summary>
    private sealed class Joined
    {
        private readonly Func<int, double> _arrival;
        private readonly Fed<ToClient, ToHost> _feed = new();
        private int _released;
        private double _now;

        public Joined(Func<int, double> arrival)
        {
            _arrival = arrival;
            Client = new Client(World.Create(1, Layout.Empty, []), _feed, 1);
            Game = new Session(null, Client);
        }

        public Client Client { get; }
        public Session Game { get; }

        public int Frame()
        {
            _now += FrameMs;
            while (_arrival(_released) <= _now) _feed.Receive(new TickMessage(_released++, []));
            int before = Client.World.Tick;
            Game.Frame(FrameMs);
            return Client.World.Tick - before;
        }

        public List<int> Frames(int n) => Enumerable.Range(0, n).Select(_ => Frame()).ToList();
    }

    [Fact]
    public void AJoinedGameHoldsATickThatCameForAWhileBeforeItRunsIt()
    {
        var g = new Joined(tick => tick == 0 ? 0 : double.PositiveInfinity);
        // The reserve before anything is measured is 100 ms: six frames.
        Assert.Equal(0, g.Frames(5).Sum());
        Assert.Equal(1, g.Frames(5).Sum());
    }

    [Fact]
    public void AJoinedGameRunsTicksThatComeInBurstsOneAtATimeAsTheHostReleasedThem()
    {
        // Three ticks every 150 ms, as a jittery link delivers them.
        var g = new Joined(tick => tick / 3 * 3 * TickMs);
        var ran = g.Frames(180).Skip(30).ToList();
        Assert.Equal(1, ran.Max());
        Assert.True(ran.Sum() >= 45, $"{ran.Sum()} ran");
    }

    [Fact]
    public void AJoinedGameShrinksItsReserveOnAnEvenLinkAndGrowsItOnAJitteryOne()
    {
        var even = new Joined(tick => tick * TickMs + 30);
        even.Frames(600);
        Assert.True(even.Game.Reserve < 40, $"reserve {even.Game.Reserve}");

        // Each tick late by up to 120 ms, in a fixed but uneven pattern.
        static double LateBy(int tick) => (tick * 7919 % 13) * 10;
        var rough = new Joined(tick => tick * TickMs + LateBy(tick));
        var ran = rough.Frames(600).Skip(300).ToList();
        Assert.True(rough.Game.Reserve > 100, $"reserve {rough.Game.Reserve}");
        // With a reserve that wide, the world runs without a pause.
        Assert.Equal(1, ran.Max());
        Assert.True(ran.Sum() >= 98, $"{ran.Sum()} ran");
    }

    [Fact]
    public void AJoinedGameHoldsWhenTicksStopComingTheTimeDrawnBetweenTicksWithItAndGoesOnAfter()
    {
        var g = new Joined(tick => tick < 20 ? tick * TickMs : tick * TickMs + 2000);
        g.Frames(120);
        double held = g.Game.Progress;
        Assert.Equal(0, g.Frames(30).Sum());
        Assert.Equal(held, g.Game.Progress);
        // The host woke: ticks come again, and the world goes on without them piling up.
        g.Frames(180);
        Assert.True(g.Frames(60).Sum() >= 18);
    }

    [Fact]
    public void AJoinedGameDrainsABacklogWithinAFewSecondsAFewTicksAFrame()
    {
        // A minute of ticks at once, as after a host that slept, or one at four times the speed.
        var g = new Joined(tick => tick < 1200 ? 0 : double.PositiveInfinity);
        int count = 1;
        int most = g.Frame();
        Assert.True(g.Client.Queued > 1000);
        for (; g.Client.Queued > 2 && count < 1000; count++) most = Math.Max(most, g.Frame());
        Assert.True(count / 60.0 < 5, $"{count} frames");
        Assert.True(most <= 5);
    }

    [Property(MaxTest = 200)]
    public Property RunsEveryTickReleasedInTheEndNeverMoreThanFiveAFrame()
    {
        // Each tick late by up to 400 ms, as a link at its worst, but never before the one ahead of it.
        var arrivals = Gen.Choose(0, 400).ArrayOf().Select(late =>
        {
            var times = late.Select((l, tick) => tick * TickMs + l).ToArray();
            for (int t = 1; t < times.Length; t++) times[t] = Math.Max(times[t], times[t - 1]);
            return times;
        });
        return Prop.ForAll(arrivals.ToArbitrary(), times =>
        {
            var g = new Joined(tick => tick < times.Length ? times[tick] : double.PositiveInfinity);
            var ran = g.Frames(1200);
            return ran.All(n => n is >= 0 and <= 5) && g.Client.World.Tick == times.Length;
        });
    }

    // The clock.

    [Fact]
    public void TheClockReleasesATickEvery50MsAndCarriesTheRemainder()
    {
        var clock = new Clock(TickMs);
        Assert.Equal(0, clock.Advance(30));
        Assert.Equal(1, clock.Advance(30));
        Assert.Equal(1, clock.Advance(40));
        Assert.Equal(2, clock.Advance(100));
    }

    [Fact]
    public void TheClockCapsCatchingUpAfterALongGapAt250MsAndDropsTheRest()
    {
        var clock = new Clock(TickMs);
        Assert.Equal(5, clock.Advance(10_000));
        Assert.Equal(0, clock.Advance(10));
    }

    [Fact]
    public void TheClockNeverDropsTimeFromAFrameOfUpTo100MsHoweverShortTheStep()
    {
        // A slow machine's long frames must not slow the hero down.
        var clock = new Clock(1000.0 / 60);
        int steps = 0;
        for (int i = 0; i < 10; i++) steps += clock.Advance(100);
        Assert.Equal(60, steps);
    }

    // The round trip of this peer's commands.

    [Fact]
    public void TheRoundTripTimesEachFromWhenItWasFirstSeenInFlightToWhenItRanOldestFirst()
    {
        var trip = new RoundTrip();
        Assert.Equal(new RoundTripTimes(null, null), trip.Read());
        trip.Sent(2, 0);
        trip.Ran(2, 0);
        trip.Sent(3, 100);
        trip.Ran(2, 250);
        Assert.Equal(new RoundTripTimes(250, 250), trip.Read());
        trip.Sent(2, 300);
        trip.Ran(0, 400);
        // Sent at 0 and 100, both ran at 400.
        Assert.Equal(new RoundTripTimes(300, 300), trip.Read());
    }

    [Fact]
    public void TheRoundTripGivesTheMedianOfTheLastTwenty()
    {
        var trip = new RoundTrip();
        for (int n = 0; n < 30; n++)
        {
            trip.Sent(1, n * 1000);
            trip.Ran(0, n * 1000 + (n < 10 ? 900 : 100 + n));
        }
        Assert.Equal(120, trip.Read().Median);
        Assert.Equal(129, trip.Read().Last);
    }

    // A load swaps the whole Session, and the round trip with it: no reset to forget the old one.
    [Fact]
    public void TheRoundTripOfANewSessionKnowsNothingOfTheOld()
    {
        var (_, client) = LocalSession.Create(World.Create(1, Layout.Empty, []));
        Assert.Equal(new RoundTripTimes(null, null), new Session(null, client).RoundTrip);
    }
}
