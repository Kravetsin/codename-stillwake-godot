using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Stillwake.Content;
using Stillwake.Game.I18n;
using Stillwake.Game.Shared;
using Stillwake.Sim;
using Stillwake.Sim.Atmosphere;

namespace Stillwake.Game.Features.Atmosphere;

/// <summary>What the air overlay says of a room (features/atmosphere/explain.ts, draining.ts).</summary>
public static class Explain
{
    /// <summary><paramref name="ticks"/> as a player reads a span: seconds under a minute, whole minutes past it.</summary>
    public static string Span(int ticks)
    {
        int seconds = (ticks + World.TicksPerSecond - 1) / World.TicksPerSecond;
        return seconds < 60 ? En.T("time.seconds", ("n", seconds)) : En.T("time.minutes", ("n", seconds / 60));
    }

    /// <summary>What <paramref name="breathers"/> heroes breathe every <c>Air.LeakEvery</c> ticks, a breath a unit: rates are counted so.</summary>
    private static double Breathing(int breathers) => (double)breathers * Air.LeakEvery / Stillwake.Content.Crew.BreathEvery;

    /// <summary>
    /// How long <paramref name="units"/> of oxygen last a room leaking <paramref name="leak"/> every
    /// <c>Air.LeakEvery</c> ticks with <paramref name="breathers"/> heroes breathing in it, a breath a unit;
    /// infinity when nothing draws. A forecast for the player, not a rule.
    /// </summary>
    private static string Lasting(double units, double leak, int breathers)
    {
        double perPeriod = leak + Breathing(breathers);
        return perPeriod > 0 ? Span((int)Math.Floor(units * Air.LeakEvery / perPeriod)) : "∞";
    }

    /// <summary>A rate counted every <c>Air.LeakEvery</c> ticks, as a player reads it: a minute's, to a tenth.</summary>
    private static string PerMinute(double perPeriod) =>
        (Math.Floor(perPeriod * 60 * World.TicksPerSecond * 10 / Air.LeakEvery) / 10).ToString(CultureInfo.InvariantCulture);

    /// <summary>Whether a room's generators keep up with its leak and its breathers, and by how much a minute; or, falling short, how long its air lasts the difference.</summary>
    private static string Generated(RoomAir air, int breathers)
    {
        double spare = air.Made - air.Leak - Breathing(breathers);
        return spare >= 0
            ? En.T("air.keepsUp", ("n", PerMinute(spare)))
            : En.T("air.fallsShort", ("time", Lasting(air.Oxygen + air.Aired, -spare, 0)));
    }

    /// <summary>
    /// What the air overlay says of a room of <paramref name="volume"/> cells with <paramref name="breathers"/>
    /// heroes awake in it, in two short lines and a third only for what it lacks: its state, then what
    /// holds it up and how long, the hull's leak and the breathing counted in.
    /// </summary>
    public static List<string> Of(RoomAir air, int volume, int breathers = 0)
    {
        // With no supply, the room lives on its own air.
        string lasts = air.Made > 0
            ? Generated(air, breathers)
            : air.Oxygen > 0
                ? En.T("air.lasts", ("oxygen", air.Oxygen), ("time", Lasting(air.Oxygen, air.Leak, breathers)))
                : En.T("air.lastsNot", ("time", Lasting(air.Aired, air.Leak, breathers)));
        string[] problem = air.Problem is { } why ? [En.T(why)] : [];
        return air.Status switch
        {
            RoomStatus.Aired => [En.T("air.aired", ("cells", volume)), lasts],
            RoomStatus.Filling =>
            [
                En.T("air.filling", ("aired", air.Aired), ("cells", volume)),
                air.Made > 0 ? lasts : En.T("air.pipes", ("oxygen", air.Oxygen)),
            ],
            // With no piped vent in it, nothing drains: there are no pipes to speak of.
            RoomStatus.Venting => air.Lasts is int ticks
                ? [En.T("air.venting"), En.T("air.drains", ("oxygen", air.Oxygen), ("time", Span(ticks)))]
                : [En.T("air.venting")],
            _ => air.Aired > 0
                ? [En.T("air.holding", ("aired", air.Aired), ("cells", volume)), lasts, .. problem]
                : [En.T("air.vacuum", ("cells", volume)), .. problem],
        };
    }

    /// <summary>
    /// The rooms losing oxygen into space: venting through a piped vent, a breach or a meteor's hole. A
    /// room open to space with no piped vent loses nothing, so the start's breached room stays quiet.
    /// </summary>
    public static HashSet<int> Draining(IEnumerable<RoomAir> rooms) =>
        rooms.Where(r => r.Status == RoomStatus.Venting && r.Lasts != null).Select(r => r.Room).ToHashSet();
}
