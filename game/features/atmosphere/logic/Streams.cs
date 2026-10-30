using System;
using System.Collections.Generic;
using Stillwake.Sim.Atmosphere;

namespace Stillwake.Game.Features.Atmosphere;

/// <summary>
/// Where air streams out of a room (features/atmosphere/streams.ts): the middle of an opening, in cells,
/// and the heading toward the space beside it, in radians screen-wise; none where it leaves through a
/// missing back wall. <see cref="PerSecond"/> is how thick the stream is.
/// </summary>
public sealed record AirStream(double X, double Y, double? Heading, double PerSecond)
{
    /// <summary>How thick one opening's stream is, and the most a room gives off through all of them.</summary>
    public const double PerOpening = 24, PerRoom = 160;

    /// <summary>
    /// The streams of every room venting air into space while it still holds some, or its vents still
    /// pour oxygen in, a powered vaporizer turning a tank's liquid into air: one per opening, heading out into the space beside it,
    /// thinner the more openings a room has. A room open to space with no air and nothing pouring in is
    /// quiet.
    /// </summary>
    public static List<AirStream> Of(
        IReadOnlyList<RoomAir> air, Func<int, IReadOnlyList<int>> openingsOf, int width, int height, Func<int, bool> isSpace)
    {
        var streams = new List<AirStream>();
        foreach (var room in air)
        {
            bool pouring = room.Oxygen > 0 && room.Made > 0;
            if (room.Status != RoomStatus.Venting || (room.Aired == 0 && !pouring)) continue;
            var openings = openingsOf(room.Room);
            double perSecond = Math.Min(PerOpening, PerRoom / Math.Max(1, openings.Count));
            foreach (int cell in openings)
            {
                int x = cell % width, y = cell / width, dx = 0, dy = 0;
                foreach (var (nx, ny) in new[] { (0, -1), (-1, 0), (1, 0), (0, 1) })
                {
                    int ax = x + nx, ay = y + ny;
                    if (ax < 0 || ay < 0 || ax >= width || ay >= height || isSpace(ay * width + ax))
                    {
                        dx += nx;
                        dy += ny;
                    }
                }
                double? heading = dx == 0 && dy == 0 ? null : Math.Atan2(dy, dx);
                streams.Add(new AirStream(x + 0.5, y + 0.5, heading, perSecond));
            }
        }
        return streams;
    }
}
