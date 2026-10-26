using System;
using Stillwake.Sim;

namespace Stillwake.Game.Features.Atmosphere;

/// <summary>
/// The smallest box holding every cell in a room: all the air overlay draws. A planet is built in nearly every cell but holds
/// rooms only where the crew sealed some, so the overlay keeps to them rather than to all that is built.
/// </summary>
public static class RoomsBox
{
    /// <summary>The box of the cells <paramref name="roomOf"/> puts in a room (a room id, never negative) within <paramref name="within"/>; <see cref="Box.None"/> for none.</summary>
    public static Box Of(ReadOnlySpan<int> roomOf, int width, Box within)
    {
        int left = int.MaxValue, top = int.MaxValue, right = -1, bottom = -1;
        for (int y = within.Top; y <= within.Bottom; y++)
        {
            var row = roomOf.Slice(y * width + within.Left, within.Width);
            int first = row.IndexOfAnyInRange(0, int.MaxValue);
            if (first < 0) continue;
            int last = row.LastIndexOfAnyInRange(0, int.MaxValue);
            (left, right) = (Math.Min(left, within.Left + first), Math.Max(right, within.Left + last));
            (top, bottom) = (Math.Min(top, y), y);
        }
        return bottom < 0 ? Box.None : new Box(left, top, right, bottom);
    }
}
