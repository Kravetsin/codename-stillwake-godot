using Stillwake.Game.Platform;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>Who is who in a world (0032): a player keeps their seat, a newcomer takes one never given.</summary>
public class RosterTests
{
    [Fact]
    public void GivesAKeyTheSameSeatEveryTimeAndANewcomerTheNextNeverGiven()
    {
        var roster = Roster.Empty();
        Assert.Equal(0, roster.SeatOf("host"));
        Assert.Equal(1, roster.SeatOf("friend"));
        Assert.Equal(2, roster.SeatOf("other"));
        Assert.Equal(1, roster.SeatOf("friend"));
    }

    [Fact]
    public void LetsTheHostOfAWorldFromBeforeRostersTakeSeatZeroAndSeatsOthersPastItsOldGuests()
    {
        var old = Roster.Read(null, "host");
        Assert.Equal(0, old.SeatOf("host"));
        Assert.Equal(Roster.LegacyFirst, old.SeatOf("friend"));
    }

    [Fact]
    public void KeepsItsSeatsAndItsCountAcrossASave()
    {
        var roster = Roster.Empty();
        roster.SeatOf("host");
        roster.SeatOf("friend");
        var again = Roster.Read(roster.ToJson(), "someone else");
        Assert.Equal((0, 1, 2), (again.SeatOf("host"), again.SeatOf("friend"), again.SeatOf("third")));
        Assert.Equal(3, again.Seats.Count);
    }
}
