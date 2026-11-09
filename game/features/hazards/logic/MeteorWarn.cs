using System;
using Stillwake.Sim.Rooms;

namespace Stillwake.Game.Features.Hazards;

/// <summary>
/// Where a meteor's strike warns (unified-damage): the breach alarm's red, weak and once, in the room behind the wall struck when
/// that room is aired and still sealed, so a crack is seen before a breach. A breach is the alarm's own.
/// </summary>
public static class MeteorWarn
{
    /// <summary>How many cells left of the wall struck a room may lie and still be warned: a hull two thick, a hole before it.</summary>
    public const int Inside = 4;

    /// <summary>
    /// The first cell of a room met walking left from <paramref name="cell"/>, through solid and open space, within
    /// <see cref="Inside"/>, when <paramref name="warned"/> says that room is to be warned; null for none, or one not to be.
    /// </summary>
    public static int? Cell(int cell, int width, Func<int, int?> roomAt, Func<int, bool> warned)
    {
        for (int i = 0; i < Inside && cell % width > 0; i++)
        {
            cell--;
            if (roomAt(cell) is not int room || room == RoomsModule.Space) continue;
            return warned(room) ? cell : null;
        }
        return null;
    }
}
