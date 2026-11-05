using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Stillwake.Content;
using Stillwake.Game.I18n;
using Stillwake.Game.Shared;
using Stillwake.Sim;
using Stillwake.Sim.Atmosphere;
using World = Stillwake.Sim.World;

namespace Stillwake.Game.Features.Atmosphere;

/// <summary>What the air overlay says of a room (features/atmosphere/explain.ts, draining.ts), in the bubble's terms (tip-signs).</summary>
public static class Explain
{
    /// <summary><paramref name="ticks"/> as a player reads a span: seconds under a minute, whole minutes past it.</summary>
    public static string Span(int ticks)
    {
        int seconds = (ticks + World.TicksPerSecond - 1) / World.TicksPerSecond;
        return seconds < 60 ? En.T("time.seconds", ("n", seconds)) : En.T("time.minutes", ("n", seconds / 60));
    }

    /// <summary>A rate of units every <c>Air.LeakEvery</c> ticks, as a player reads it: a minute's, to a tenth, rounded down.</summary>
    public static string PerMinute(long perPeriod) =>
        (Math.Floor(perPeriod * 60.0 * World.TicksPerSecond * 10 / Air.LeakEvery) / 10).ToString(CultureInfo.InvariantCulture);

    /// <summary>
    /// What the air overlay says of a room of <paramref name="volume"/> cells, its first line naming its state and its aired
    /// cells: while it has a share of its vents' making, that share and what its air leaks, both a minute, the leak red when
    /// it stops the bubble short; the liquid behind its vaporizers and how long it lasts (none for the pod's, which never
    /// runs out); and why it falls short.
    /// </summary>
    public static List<TipLine> Of(RoomAir air, int volume)
    {
        var lines = new List<TipLine> { new(Named(air, volume)) };
        if (air.Budget > 0)
        {
            lines.Add(new TipLine(En.T("air.made", ("n", PerMinute(air.Budget))), Sign: Sign.Oxygen));
            lines.Add(new TipLine(En.T("air.leak", ("n", PerMinute(air.Leak))), air.Problem == "atmosphere.leaky" ? Tone.Bad : Tone.Plain, Sign: Sign.Leak));
        }
        // With no piped vent in a venting room, nothing drains: there is no liquid to speak of.
        if (air.Oxygen is int liquid && (air.Status != RoomStatus.Venting || air.Lasts != null))
            lines.Add(liquid == 0
                ? new TipLine(liquid.ToString(CultureInfo.InvariantCulture), Tone.Bad, Canister)
                : new TipLine(air.Lasts is int ticks ? En.T("air.liquid", ("held", liquid), ("time", Span(ticks))) : liquid.ToString(CultureInfo.InvariantCulture), Icon: Canister));
        if (air.Problem is { } why) lines.Add(new TipLine(En.T(why), Tone.Bad));
        return lines;
    }

    /// <summary>The item whose icon stands for the liquid oxygen behind a room's vaporizers.</summary>
    private const string Canister = "canister";

    private static string Named(RoomAir air, int volume) => air.Status switch
    {
        RoomStatus.Aired => En.T("air.aired", ("cells", volume)),
        RoomStatus.Filling => En.T("air.filling", ("aired", air.Aired), ("cells", volume)),
        RoomStatus.Venting => En.T("air.venting"),
        _ => air.Aired > 0 ? En.T("air.holding", ("aired", air.Aired), ("cells", volume)) : En.T("air.vacuum", ("cells", volume)),
    };

    /// <summary>
    /// Whether the room tip speaks over a cell holding structure <paramref name="structure"/> and <paramref name="machine"/>:
    /// only over a back wall or an empty cell of the room. A door, ladder, platform or machine is what the pointer is on.
    /// </summary>
    public static bool SpeaksOver(int structure, bool machine) => structure == Structure.Empty && !machine;

    /// <summary>
    /// The rooms losing oxygen into space: venting through a piped vent, a breach or a meteor's hole, stored air running out or
    /// made air pouring through (the pod's). A room open to space with no piped vent loses nothing, so it stays quiet.
    /// </summary>
    public static HashSet<int> Draining(IEnumerable<RoomAir> rooms) =>
        rooms.Where(r => r.Status == RoomStatus.Venting && (r.Lasts != null || r.Made > 0)).Select(r => r.Room).ToHashSet();
}
