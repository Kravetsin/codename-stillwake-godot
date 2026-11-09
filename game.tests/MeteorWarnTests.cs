using Stillwake.Game.Features.Hazards;
using Stillwake.Sim.Rooms;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>Where a meteor's strike warns (unified-damage): the first room left of the wall struck, if it is to be warned.</summary>
public class MeteorWarnTests
{
    /// <summary>One row, ten wide: room 3 in cells 0 to 3, hull at 4 and 5, space from 6; <c>null</c> a solid cell.</summary>
    private static int? RoomAt(int cell) => cell switch
    {
        <= 3 => 3,
        4 or 5 => null,
        _ => RoomsModule.Space,
    };

    [Fact]
    public void WarnTheRoomBehindAHullTwoThickAtItsCellNearestTheWall()
    {
        Assert.Equal(3, MeteorWarn.Cell(5, 10, RoomAt, _ => true));
        // The outer cell holed: from it, through the hull's other cell, still the room.
        Assert.Equal(3, MeteorWarn.Cell(6, 10, RoomAt, _ => true));
    }

    [Fact]
    public void WarnNoRoomNotToBeWarnedNorOneFartherThanItLooks()
    {
        Assert.Null(MeteorWarn.Cell(5, 10, RoomAt, _ => false));
        Assert.Null(MeteorWarn.Cell(9, 10, RoomAt, _ => true));
        // Never past the row's start into the row above.
        Assert.Null(MeteorWarn.Cell(10, 10, c => c == 9 ? 3 : null, _ => true));
    }
}
